namespace CaptainHook.EventHandlerActor.Handlers.Authentication
{
    public interface IAuthHandlerFactory
    {
        IAuthenticationHandler Get(string name);
    }
}
