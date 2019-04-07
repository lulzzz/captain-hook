using System;
using System.Fabric;
using Eshopworld.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace CaptainHook.Api.Controllers
{
    /// <summary>
    /// base class for some controller base logic
    /// </summary>
    public abstract class CaptainHookControllerBase : Controller
    {
        internal IWebHostEnvironment HostingEnvironment { get; }

        internal IBigBrother BigBrother { get; }

        internal StatefulServiceContext StatelessServiceContext { get; }

        /// <summary>
        /// constructor to inject hosting environment
        /// </summary>
        /// <param name="hostingEnvironment">hosting environment descriptor</param>
        /// <param name="bigBrother">big brother instance</param>
        /// <param name="sfContext">service fabric context</param>
        protected CaptainHookControllerBase(
            IWebHostEnvironment hostingEnvironment, 
            IBigBrother bigBrother, 
            StatefulServiceContext sfContext)
        {
            HostingEnvironment = hostingEnvironment;
            BigBrother = bigBrother;
            StatelessServiceContext = sfContext;
        }

        /// <summary>
        /// get actor proxy via SF API
        /// </summary>
        /// <typeparam name="T">desired interface</typeparam>
        /// <param name="serviceName">name of the service</param>
        /// <returns>proxy instance</returns>
        internal T GetActorRef<T>(string serviceName) where T : IActor
        {
            var actorUri = new Uri($"{StatelessServiceContext.CodePackageActivationContext.ApplicationName}/{serviceName}");

            return ActorProxy.Create<T>(ActorId.CreateRandom(), actorUri);
        }
    }
}