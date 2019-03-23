﻿using System.Collections.Generic;
using CaptainHook.Common.Authentication;
using CaptainHook.Common.Configuration;
using CaptainHook.EventHandlerActorService.Handlers.Authentication;
using Eshopworld.Core;
using Eshopworld.Tests.Core;
using Moq;
using Xunit;

namespace CaptainHook.Tests.Authentication
{
    public class AuthenticationFactoryTests
    {
        public static IEnumerable<object[]> AuthenticationTestData =>
            new List<object[]>
            {
                new object[] { "basic", new BasicAuthenticationConfig(), new BasicAuthenticationHandler(new BasicAuthenticationConfig()),  },
                new object[] { "oidc", new OidcAuthenticationConfig(), new OidcAuthenticationHandler(new OidcAuthenticationConfig()) },
                new object[] { "custom", new OidcAuthenticationConfig{ Type = AuthenticationType.Custom}, new MmAuthenticationHandler(new OidcAuthenticationConfig())  }
            };

        public static IEnumerable<object[]> NoneAuthenticationTestData =>
            new List<object[]>
            {
                new object[] {"none", new AuthenticationConfig()}
            };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationName"></param>
        /// <param name="authenticationConfig"></param>
        /// <param name="expectedHandler"></param>
        [IsLayer0]
        [Theory]
        [MemberData(nameof(AuthenticationTestData))]
        public void GetTokenProvider(string configurationName, AuthenticationConfig authenticationConfig, IAcquireTokenHandler expectedHandler)
        {
            var indexedDictionary = new IndexDictionary<string, WebhookConfig>
            {
                {
                    configurationName, new WebhookConfig
                    {
                        Type = configurationName,
                        AuthenticationConfig = authenticationConfig
                    }
                }
            };

            var factory = new AuthenticationHandlerFactory(indexedDictionary, new Mock<IBigBrother>().Object);

            var handler = factory.Get(configurationName);

            Assert.Equal(expectedHandler.GetType(), handler.GetType());
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationName"></param>
        /// <param name="authenticationConfig"></param>
        [IsLayer0]
        [Theory]
        [MemberData(nameof(NoneAuthenticationTestData))]
        public void NoAuthentication(string configurationName, AuthenticationConfig authenticationConfig)
        {
            var indexedDictionary = new IndexDictionary<string, WebhookConfig>
            {
                {
                    configurationName, new WebhookConfig
                    {
                        Type = configurationName,
                        AuthenticationConfig = authenticationConfig
                    }
                }
            };

            var factory = new AuthenticationHandlerFactory(indexedDictionary, new Mock<IBigBrother>().Object);

            var handler = factory.Get(configurationName);

            Assert.Null(handler);
        }
    }
}
