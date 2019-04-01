﻿using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Eshopworld.Core;
using Eshopworld.DevOps;
using Eshopworld.Web;
using Eshopworld.Telemetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using System.Reflection;
using CaptainHook.Common.Configuration;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace CaptainHook.Api
{
    /// <summary>
    /// Startup class for ASP.NET runtime
    /// </summary>
    public class Startup
    {
        private readonly TelemetrySettings _telemetrySettings = new TelemetrySettings();
        private readonly IBigBrother _bb;
        private readonly IConfigurationRoot _configuration;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="env">hosting environment</param>
        public Startup(IHostingEnvironment env)
        {
            try
            {
                var kvUri = Environment.GetEnvironmentVariable(ConfigurationSettings.KeyVaultUriEnvVariable);

                _configuration = new ConfigurationBuilder().AddAzureKeyVault(
                    kvUri,
                    new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)),
                    new DefaultKeyVaultSecretManager()).Build();

                var settings = new ConfigurationSettings();
                _configuration.Bind(settings);

                //_configuration = EswDevOpsSdk.BuildConfiguration(env.ContentRootPath, env.EnvironmentName);
                //_configuration.GetSection("Telemetry").Bind(_telemetrySettings);
                _telemetrySettings.InstrumentationKey = settings.InstrumentationKey;
                _telemetrySettings.InternalKey = "captain-hook";
                _bb = new BigBrother(_telemetrySettings.InstrumentationKey, _telemetrySettings.InternalKey);
            }
            catch (Exception e)
            {
                BigBrother.Write(e);
                throw;
            }
        }

        /// <summary>
        /// configure services to be used by the asp.net runtime
        /// </summary>
        /// <param name="services">service collection</param>
        /// <returns>service provider instance (Autofac provider)</returns>
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddApplicationInsightsTelemetry(_telemetrySettings.InstrumentationKey);
                services.Configure<ServiceConfigurationOptions>(_configuration.GetSection("ServiceConfigurationOptions"));

                var serviceConfigurationOptions = services.BuildServiceProvider()
                    .GetService<IOptions<ServiceConfigurationOptions>>();

                services.AddMvc(options =>
                {
                    //todo this needs wiring up to keyvault or perhaps we need to import from kv first and then take in the data from appsettings
                    //var policy = ScopePolicy.Create(serviceConfigurationOptions.Value.RequiredScopes.ToArray());

                    //var filter = EnvironmentHelper.IsInFabric ? 
                    //    (IFilterMetadata) new AuthorizeFilter(policy): 
                    //    new AllowAnonymousFilter();

                    //options.Filters.Add(filter);

                    //todo remove for proper auth
                    options.Filters.Add(new AllowAnonymousFilter());
                });
                services.AddApiVersioning();

                //Get XML documentation
                var path = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

                //if not generated throw an event but it's not going to stop the app from starting
                if (!File.Exists(path))
                {
                    BigBrother.Write(new Exception("Swagger XML document has not been included in the project"));
                }
                else
                {
                    services.AddSwaggerGen(c =>
                    {
                        c.IncludeXmlComments(path);
                        c.DescribeAllEnumsAsStrings();
                        c.SwaggerDoc("v1", new Info { Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(), Title = "CaptainHook.Api" });
                        c.CustomSchemaIds(x => x.FullName);

                        //c.AddSecurityDefinition("Bearer",
                        //    new ApiKeyScheme
                        //    {
                        //        In = "header",
                        //        Description = "Please insert JWT with Bearer into field",
                        //        Name = "Authorization",
                        //        Type = "apiKey"
                        //    });

                        //c.AddSecurityRequirement(
                        //    new Dictionary<string, IEnumerable<string>>
                        //    {
                        //        {"Bearer", Array.Empty<string>()}
                        //    });
                    });
                }

                //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddIdentityServerAuthentication(
                //    x =>
                //    {
                //        x.ApiName = serviceConfigurationOptions.Value.ApiName;
                //        x.ApiSecret = serviceConfigurationOptions.Value.ApiSecret;
                //        x.Authority = serviceConfigurationOptions.Value.Authority;
                //        x.RequireHttpsMetadata = serviceConfigurationOptions.Value.IsHttps;

                //        //TODO: this requires Eshopworld.Beatles.Security to be refactored
                //        //x.AddJwtBearerEventsTelemetry(bb); 
                //    });

                var builder = new ContainerBuilder();
                builder.Populate(services);
                builder.RegisterInstance(_bb).As<IBigBrother>().SingleInstance();

                var container = builder.Build();
                return new AutofacServiceProvider(container);
            }
            catch (Exception e)
            {
                _bb.Publish(e.ToExceptionEvent());
                throw;
            }
        }

        /// <summary>
        /// configure asp.net pipeline
        /// </summary>
        /// <param name="app">application builder</param>
        public void Configure(IApplicationBuilder app)
        {
            app.UseBigBrotherExceptionHandler();
            app.UseSwagger(o => o.RouteTemplate = "swagger/{documentName}/swagger.json");
            app.UseSwaggerUI(o =>
            {
                o.SwaggerEndpoint("v1/swagger.json", "CaptainHook.Api");
                o.RoutePrefix = "swagger";
            });

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}
