using System.Collections.Generic;
using CaptainHook.Common.Authentication;
using CaptainHook.EventHandlerActorService.Handlers.Authentication;
using Eshopworld.Tests.Core;
using Xunit;

namespace CaptainHook.Tests.Authentication
{
    public class AuthenticationFactoryTests
    {
        public static IEnumerable<object[]> AuthenticationTestData =>
            new List<object[]>
            {
                new object[] { new BasicAuthenticationConfig{Type = AuthenticationType.Basic}, new BasicAuthenticationHandler(new BasicAuthenticationConfig()),  },
                new object[] { new OidcAuthenticationConfig(), new OidcAuthenticationHandler(new OidcAuthenticationConfig()) },
                new object[] { new OidcAuthenticationConfig{ Type = AuthenticationType.Custom}, new MmAuthenticationHandler(new OidcAuthenticationConfig())  }
            };

        public static IEnumerable<object[]> NoneAuthenticationTestData =>
            new List<object[]>
            {
                new object[] {new AuthenticationConfig()}
            };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authenticationConfig"></param>
        /// <param name="expectedHandler"></param>
        [IsLayer0]
        [Theory]
        [MemberData(nameof(AuthenticationTestData))]
        public void GetTokenProvider(AuthenticationConfig authenticationConfig, IAcquireTokenHandler expectedHandler)
        {
            var factory = new AuthenticationHandlerFactory();

            var handler = factory.Get(authenticationConfig);

            Assert.Equal(expectedHandler.GetType(), handler.GetType());
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="authenticationConfig"></param>
        [IsLayer0]
        [Theory]
        [MemberData(nameof(NoneAuthenticationTestData))]
        public void NoAuthentication(AuthenticationConfig authenticationConfig)
        {
            var factory = new AuthenticationHandlerFactory();

            var handler = factory.Get(authenticationConfig);

            Assert.Null(handler);
        }
    }
}
