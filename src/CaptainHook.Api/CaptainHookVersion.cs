using System.Diagnostics.CodeAnalysis;

namespace CaptainHook.Api
{
    [ExcludeFromCodeCoverage]
    internal static class CaptainHookVersion
    {
        public static string CaptainHook = "1.5";

        public static string ApiVersion = "v1";

        public static bool UseOpenApi = true;
    }
}