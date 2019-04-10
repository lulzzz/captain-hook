using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.ServiceFabric;
using CaptainHook.Common.Configuration;
using CaptainHook.Common.Telemetry.Api;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace CaptainHook.Api
{
    public class Startup
    {
        private IBigBrother _bb;
        private ConfigurationSettings _settings;
        private IConfigurationRoot _configuration;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
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

                services.AddApplicationInsightsTelemetry(_configuration);

                services.AddMvc(options =>
                {
                    var policy = ScopePolicy.Create(_settings.RequiredScopes.ToArray());
                    options.Filters.Add(new AuthorizeFilter(policy));

                }).AddNewtonsoftJson();

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
                        c.SwaggerDoc(CaptainHookVersion.ApiVersion, new OpenApiInfo { Version = CaptainHookVersion.ApiVersion, Title = "Captain Hook API" });
                        c.CustomSchemaIds(x => x.FullName);
                        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                        {
                            In = ParameterLocation.Header,
                            Description = "Please insert JWT with Bearer into field",
                            Name = "Authorization",
                            Type = CaptainHookVersion.UseOpenApi ? SecuritySchemeType.ApiKey : SecuritySchemeType.Http,
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

                var builder = new ContainerBuilder();
                builder.Populate(services);

                builder.RegisterInstance(_configuration).As<IConfigurationRoot>().SingleInstance();
                builder.RegisterInstance(_settings).As<ConfigurationSettings>().SingleInstance();
                builder.RegisterInstance(_bb).As<IBigBrother>().SingleInstance();

                //registers an application wide cancellation token so that shutdowns are graceful
                builder.Register(_ => _cancellationTokenSource.Token).As<CancellationToken>();
                builder.RegisterServiceFabricSupport();

                return new AutofacServiceProvider(builder.Build());
            }
            catch (Exception e)
            {
                _bb.Publish(e.ToExceptionEvent());
                throw;
            }

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime hostApplicationLifetime)
        {
            try
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseBigBrotherExceptionHandler();
                app.UseSwagger(c =>
                {
                    c.SerializeAsV2 = CaptainHookVersion.UseOpenApi;
                    c.RouteTemplate = "swagger/{documentName}/swagger.json";
                });

                app.UseSwaggerUI(o =>
                {
                    o.SwaggerEndpoint($"{CaptainHookVersion.ApiVersion}/swagger.json", $"Captain Hook API Version {CaptainHookVersion.ApiVersion}");
                    o.RoutePrefix = "swagger";
                });

                app.UseRouting(routes => { routes.MapControllers(); });

                app.UseAuthentication();
                app.UseMvc();


                hostApplicationLifetime.ApplicationStarted.Register(OnStartup);
                hostApplicationLifetime.ApplicationStopped.Register(OnShutdown);
            }
            catch (Exception e)
            {
                _bb.Publish(e.ToExceptionEvent());
                throw;
            }
        }

        /// <summary>
        /// On Startup callback which gets called when the service is started.
        /// </summary>
        private void OnStartup()
        {
            _bb.Publish(new ApiStartedEvent());
        }

        /// <summary>
        /// On Shutdown callback which gets called when the service is asked to shutdown, cancels the applications cancellation token
        /// </summary>
        private void OnShutdown()
        {
            _bb.Publish(new ApiShutdownEvent());
            _bb.Flush();

            _cancellationTokenSource.Cancel();
        }
    }
}
