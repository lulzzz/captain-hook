using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Eshopworld.Core;
using Eshopworld.DevOps;
using Eshopworld.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CaptainHook.Test.Api
{
    public class Startup
    {
        private readonly TelemetrySettings _telemetrySettings = new TelemetrySettings();
        private readonly IBigBrother _bb;

        public Startup(IHostingEnvironment env)
        {
            //try
            //{
            //    var configuration = EswDevOpsSdk.BuildConfiguration(env.ContentRootPath, env.EnvironmentName);
            //    configuration.GetSection("Telemetry").Bind(_telemetrySettings);
            //    _bb = new BigBrother(_telemetrySettings.InstrumentationKey, _telemetrySettings.InternalKey);
            //}
            //catch (Exception e)
            //{
            //    BigBrother.Write(e);
            //    throw;
            //}
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //var builder = new ContainerBuilder();
            //builder.Populate(services);
            //builder.RegisterInstance(_bb).As<IBigBrother>().SingleInstance();
            //builder.RegisterInstance(_telemetrySettings).As<TelemetrySettings>().SingleInstance();

            //var container = builder.Build();
            //return new AutofacServiceProvider(container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc();
        }
    }
}
