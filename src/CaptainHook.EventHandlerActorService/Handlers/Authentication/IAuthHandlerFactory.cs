using CaptainHook.Common.Authentication;

namespace CaptainHook.EventHandlerActorService.Handlers.Authentication
{
    public interface IAuthHandlerFactory
    {
        IAcquireTokenHandler Get(AuthenticationConfig authenticationConfig);
    }
}
