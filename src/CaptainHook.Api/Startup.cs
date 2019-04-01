using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Eshopworld.Core;
using Eshopworld.Web;
using Eshopworld.Telemetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Threading;
using Autofac.Integration.ServiceFabric;
using CaptainHook.Common.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.OpenApi.Models;

namespace CaptainHook.Api
{
    /// <summary>
    /// Startup class for ASP.NET runtime
    /// </summary>
    public class Startup
    {
        private readonly IBigBrother _bb;
        private readonly ConfigurationSettings _settings;
        private readonly IConfigurationRoot _configuration;
        private IContainer _applicationContainer;

        /// <summary>
        /// Constructor
        /// </summary>
        public Startup()
        {
            try
            {
                var kvUri = Environment.GetEnvironmentVariable(ConfigurationSettings.KeyVaultUriEnvVariable);

                _configuration = new ConfigurationBuilder().AddAzureKeyVault(
                    kvUri,
                    new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)),
                    new DefaultKeyVaultSecretManager()).Build();

                _settings = new ConfigurationSettings();
                _configuration.Bind(_settings);

                _bb = new BigBrother(_settings.InstrumentationKey, _settings.InstrumentationKey);
                _bb.UseEventSourceSink().ForExceptions();
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
        /// <param name="applicationLifetime"></param>
        /// <returns>service provider instance (Autofac provider)</returns>
        public IServiceProvider ConfigureServices(IServiceCollection services, IApplicationLifetime applicationLifetime)
        {
            try
            {
                services.AddMvc(options =>
                {
                    //todo this needs wiring up to keyvault or perhaps we need to import from kv first and then take in the data from appsettings
                    var policy = ScopePolicy.Create(_settings.RequiredScopes.ToArray());

                    var filter = EnvironmentHelper.IsInFabric ?
                        (IFilterMetadata)new AuthorizeFilter(policy) :
                        new AllowAnonymousFilter();

                    options.Filters.Add(filter);
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
                        c.SwaggerDoc(CaptainHookVersion.ApiVersion, new OpenApiInfo { Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(), Title = CaptainHookVersion.CaptainHook });
                        c.CustomSchemaIds(x => x.FullName);
                        c.AddSecurityDefinition("Bearer",
                            new OpenApiSecurityScheme
                            {
                                In = ParameterLocation.Header,
                                Description = "Please insert JWT with Bearer into field",
                                Name = "Authorization",
                                Type = CaptainHookVersion.UseOpenApi
                                    ? SecuritySchemeType.ApiKey
                                    : SecuritySchemeType.Http,
                                Scheme = "bearer",
                                BearerFormat = "JWT",
                            });
                        c.AddSecurityRequirement(new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                                },
                                new string[0]
                            }
                        });
                        var docFiles = Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CaptainHook.*.xml").ToList();
                        if (docFiles.Count > 0)
                        {
                            foreach (var file in docFiles)
                            {
                                c.IncludeXmlComments(file);
                            }
                        }
                        else
                        {
                            if (Debugger.IsAttached)
                            {
                                // Couldn't find the XML file! check that XML comments are being built and that the file name checks
                                Debugger.Break();
                            }
                        }

                        c.OperationFilter<DefaultResponseFixFilter>();
                    });
                }

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddIdentityServerAuthentication(
                    x =>
                    {
                        x.ApiName = _settings.ApiName;
                        x.ApiSecret = _settings.ApiSecret;
                        x.Authority = _settings.Authority;
                        x.RequireHttpsMetadata = _settings.IsHttps;
                    });

                services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
                
                var builder = new ContainerBuilder();
                builder.Populate(services);
                _applicationContainer = builder.Build();

                builder.RegisterInstance(_configuration).As<IConfigurationRoot>().SingleInstance();
                builder.RegisterInstance(_settings).As<ConfigurationSettings>().SingleInstance();
                builder.RegisterInstance(_bb).As<IBigBrother>().SingleInstance();

                //registers an application wide cancellation token so that shutdown can be graceful.
                builder.Register(_ => applicationLifetime.ApplicationStopping).As<CancellationToken>();
                builder.RegisterServiceFabricSupport();

                return new AutofacServiceProvider(_applicationContainer);
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
            try
            {
                app.UseBigBrotherExceptionHandler();
                app.UseSwagger(c => c.SerializeAsV2 = CaptainHookVersion.UseOpenApi);
                app.UseSwagger(o => o.RouteTemplate = "swagger/{documentName}/swagger.json");
                app.UseSwaggerUI(o =>
                {
                    o.SwaggerEndpoint($"{CaptainHookVersion.ApiVersion}/swagger.json", $"Captain Hook API Version {CaptainHookVersion.ApiVersion}");
                    o.RoutePrefix = "swagger";
                });

                app.UseAuthentication();
                app.UseMvc();
            }
            catch (Exception e)
            {
                _bb.Publish(e.ToExceptionEvent());
                _bb.Flush();
                throw;
            }
        }
    }
}
