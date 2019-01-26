// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using IdentityServer4.Quickstart.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using IdentityServer4;

namespace identityserverEF
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }

        public Startup(IConfiguration config, IHostingEnvironment env)
        {
            Configuration = config;
            Environment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (!Environment.IsDevelopment()) {
                services.AddDataProtection()
                    .PersistKeysToAWSSystemsManager("/IdentityBrokerApp/DataProtection");
            }
            
            services.AddMvc().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_1);

            services.Configure<IISOptions>(options =>
            {
                options.AutomaticAuthentication = false;
                options.AuthenticationDisplayName = "Windows";
            });

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var identityServer = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
            })
            .LoadSigningCredentialFrom(Configuration["Cert:MySigningCert"], Configuration["Cert:MySigningCertPassword"])
            .AddTestUsers(TestUsers.Users)
            // this adds the config data from DB (clients, resources, CORS)
            .AddConfigurationStore(options =>
            {
                //options.ConfigureDbContext = builder => builder.UseSqlite(connectionString);
                options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString);
            })
            // this adds the operational data from DB (codes, tokens, consents)
            .AddOperationalStore(options =>
            {
                //options.ConfigureDbContext = builder => builder.UseSqlite(connectionString);
                options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString);
                // this enables automatic token cleanup. this is optional.
                options.EnableTokenCleanup = true;
            });

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.ClientId = Configuration["Google:ClientId"];
                    options.ClientSecret = Configuration["Google:ClientSecret"];
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseIdentityServer();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }
    }

    public static class IdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder LoadSigningCredentialFrom(this IIdentityServerBuilder builder, string certAsString, string certPassword)
        {
            if (!string.IsNullOrEmpty(certAsString))
            {
                //so when on aws, should load from ssm. Would need to figure out how to rotate
                byte[] decodedPfxBytes = Convert.FromBase64String(certAsString);
                builder.AddSigningCredential(new System.Security.Cryptography.X509Certificates.X509Certificate2(decodedPfxBytes, certPassword));
            }
            else
            {
                builder.AddDeveloperSigningCredential();
            }

            return builder;
        }
    }
}
