using CaptainHook.Common.Authentication;

namespace CaptainHook.EventHandlerActor.Handlers.Authentication
{
    public interface IAuthHandlerFactory
    {
        IAcquireTokenHandler Get(AuthenticationConfig authenticationConfig);
    }
}
