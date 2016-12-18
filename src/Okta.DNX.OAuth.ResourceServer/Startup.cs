using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Okta.DNX.OAuth.ResourceServer.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using System.Security.Claims;

namespace Okta.DNX.OAuth.ResourceServer
{
    public class Startup
    {
        string clientId = string.Empty;
        string issuer = string.Empty;
        string authorizationServerIssuer = string.Empty;
        string audience = string.Empty;


        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                 .AddJsonFile($"config.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsEnvironment("Development"))
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddEnvironmentVariables();

            Configuration = builder.Build();


            clientId = Configuration["okta:clientId"] as string;
            issuer = Configuration["okta:organizationUrl"];
            authorizationServerIssuer = Configuration["okta:authorizationServerIssuer"];
            audience = Configuration["okta:audience"] as string;
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            //services.AddApplicationInsightsTelemetry(Configuration);

            //     var scopePolicy = new AuthorizationPolicyBuilder()
            //.RequireAuthenticatedUser()
            //.RequireClaim("scp", "call-api")
            //.Build();

            services.AddAuthentication();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("todo.read",
                    policy =>
                    {
                        policy
                        .RequireClaim("cid", clientId)
                       .RequireClaim("http://schemas.microsoft.com/identity/claims/scope", "call-api");
                    }
                );
            }
            );

            services.AddMvc();

            services.AddSingleton<ITodoRepository, TodoRepository>();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Trace);
            loggerFactory.AddDebug();

            //app.UseApplicationInsightsRequestTelemetry();

            //app.UseApplicationInsightsExceptionTelemetry();


            TokenValidationParameters tvps = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = audience,

                ValidateIssuer = true,
                ValidIssuer = authorizationServerIssuer,

                ValidateLifetime = true,


                ClockSkew = TimeSpan.FromMinutes(5)
            };

            // Configure the app to use Jwt Bearer Authentication
            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                //MetadataAddress is critical for the JwtBearerAuthentication middleware to retrieve the OIDC metadata and be able to perform signing key validation
                MetadataAddress = authorizationServerIssuer + "/.well-known/openid-configuration",
                TokenValidationParameters = tvps
            });

            app.UseMvc();
        }
    }
}
