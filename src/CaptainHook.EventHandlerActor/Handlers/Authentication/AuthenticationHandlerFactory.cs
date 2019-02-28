using System;
using CaptainHook.Common.Authentication;

namespace CaptainHook.EventHandlerActor.Handlers.Authentication
{
    /// <summary>
    /// Selects the correct authentication handler based on the type specified by the authentication type.
    /// This implemented both Basic, OIDC and a custom implemented which will be moved to an integration layer.
    /// </summary>
    public class AuthenticationHandlerFactory : IAuthHandlerFactory
    {
        public IAcquireTokenHandler Get(AuthenticationConfig authenticationConfig)
        {
            switch (authenticationConfig.Type)
            {
                case AuthenticationType.None:
                    return null;
                case AuthenticationType.Basic:
                    return new BasicAuthenticationHandler(authenticationConfig);
                case AuthenticationType.OIDC:
                    return new OidcAuthenticationHandler(authenticationConfig);
                case AuthenticationType.Custom:
                    //todo hack for now until we move this out of here and into an integration layer
                    //todo if this is custom it should be another webhook which calls out to another place, this place gets a token on CH's behalf and then adds this into subsequent webhook requests.
                    return new MmAuthenticationHandler(authenticationConfig);
                default:
                    throw new ArgumentOutOfRangeException(nameof(authenticationConfig.Type), $"unknown configuration type of {authenticationConfig.Type}");
            }
        }
    }
}
