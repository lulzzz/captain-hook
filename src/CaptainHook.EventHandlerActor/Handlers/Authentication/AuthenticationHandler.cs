﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using CaptainHook.Common;
using CaptainHook.Common.Telemetry;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using IdentityModel.Client;

namespace CaptainHook.EventHandlerActor.Handlers.Authentication
{
    public class AuthenticationHandler : IAuthenticationHandler
    {
        protected readonly AuthenticationConfig AuthenticationConfig;
        private readonly IBigBrother _bigBrother;

        //todo cache and make it thread safe, ideally should have one per each auth domain and have the expiry set correctly
        private readonly AuthToken _token = new AuthToken();

        public AuthenticationHandler(AuthenticationConfig authenticationConfig, IBigBrother bigBrother)
        {
            AuthenticationConfig = authenticationConfig;
            _bigBrother = bigBrother;
        }

        /// <summary>
        /// This may vary a lot depending on the implementation of each customers auth system
        /// Ideally they implement OIDC/oAuth2 and with credentials we get an access token and refresh token.
        /// Access token may expire after one time use or after a period of time. Refresh is used to get a new access token.
        /// Or they may not give a refresh token at all...annoying. Override, implement and inject as needed.
        /// </summary>
        /// <returns></returns>
        public virtual async Task GetToken(HttpClient client)
        {
            //get initial access token and refresh token
            //if (_token.AccessToken == null)
            //{
                var response = await client.RequestTokenAsync(new ClientCredentialsTokenRequest 
                {
                    Address = AuthenticationConfig.Uri,
                    ClientId = AuthenticationConfig.ClientId,
                    ClientSecret = AuthenticationConfig.ClientSecret,
                    GrantType = AuthenticationConfig.GrantType,
                    Scope = AuthenticationConfig.Scopes
                });

                ReportTokenUpdateFailure(response);
                UpdateToken(response);
            //}

            //get a new access token from the refresh token
            //if (_token.ExpireTime >= DateTime.UtcNow)
            //{
            //    var response = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
            //    {
            //        Address = AuthenticationConfig.Uri,
            //        RefreshToken = _token.RefreshToken
            //    });

            //    ReportTokenUpdateFailure(response);
            //    UpdateToken(response);
            //}

            client.SetBearerToken(_token.AccessToken);
        }

        private void ReportTokenUpdateFailure(TokenResponse response)
        {
            if (!response.IsError)
            {
                return;
            }
            throw new Exception($"Unable to get access token from STS. Error = {response.ErrorDescription}");
        }

        /// <summary>
        /// Updates the local cached token
        /// </summary>
        /// <param name="response"></param>
        private void UpdateToken(TokenResponse response)
        {
            _token.AccessToken = response.AccessToken;
            _token.RefreshToken = response.RefreshToken;
            _token.ExpiresIn = response.ExpiresIn;
        }
    }
}
