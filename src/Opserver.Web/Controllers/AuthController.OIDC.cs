﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Jil;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Opserver.Helpers;
using Opserver.Security;
using Opserver.Views.Login;
using StackExchange.Utils;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace Opserver.Controllers
{
    partial class AuthController
    {
        private const string OidcCookieName = "oidc-id";
        private const string OidcIdentifierKey = "id";
        private const string OidcReturnUrlKey = "returnUrl";

        [AllowAnonymous]
        [HttpGet("login/oauth/callback")]
        public async Task<IActionResult> OAuthCallback(string code, string state, string error = null)
        {
            IActionResult Error(string message) => View("Login", new LoginModel { ErrorMessage = message });

            if (!Current.Security.IsConfigured)
            {
                return View("NoConfiguration");
            }

            if (Current.Security.FlowType != SecurityProviderFlowType.OIDC)
            {
                return new NotFoundResult();
            }

            if (error.HasValue())
            {
                return Error(error);
            }

            if (!code.HasValue())
            {
                return Error("no authorization code");
            }
            // decode the state and ensure the passed identifier matches
            // what we have in the cookies passed from the user agent
            var decodedState = QueryHelpers.ParseQuery(state);
            if (!decodedState.TryGetValue(OidcIdentifierKey, out var actualIdentifier))
            {
                return Error("invalid state - id not found on querystring");
            }

            var expectedIdentifier = Request.Cookies[OidcCookieName];
            if (!expectedIdentifier.HasValue())
            {
                return Error("invalid state - id not found in cookie ");
            }

            if (expectedIdentifier != actualIdentifier)
            {
                return Error("invalid state - id does not match");
            }

            // hooray! we're all set, let's go fetch our access token
            var oidcSettings = (OIDCSecuritySettings) Current.Security.Settings;
            var scopes = oidcSettings.Scopes ?? OIDCSecuritySettings.DefaultScopes;
            var redirectUri = Url.Action(
                nameof(OAuthCallback),
                ControllerContext.ActionDescriptor.ControllerName,
                null,
                Request.Scheme,
                Request.Host.Value,
                null
            );

            var form = new NameValueCollection
            {
                ["code"] = code,
                ["client_id"] = oidcSettings.ClientId,
                ["client_secret"] = oidcSettings.ClientSecret,
                ["scope"] = string.Join(' ', scopes),
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            };

            var response = await Http.Request(oidcSettings.AccessTokenUrl)
                .WithoutLogging(HttpStatusCode.BadRequest)
                .SendFormUrlEncoded(form)
                .ExpectString()
                .PostAsync();

            if (!response.Success)
            {
                return Error(
                    $"failed to exchange authorization code for access token. {response.StatusCode} - {response.Data}"
                );
            }

            AccessTokenResponse responsePayload;
            try
            {
                responsePayload = JSON.Deserialize<AccessTokenResponse>(response.Data);
            }
            catch (Exception ex)
            {
                ex.Log();
                return Error(
                    $"could not deserialize access token. {ex.Message}"
                );
            }


            response = await Http.Request(oidcSettings.UserInfoUrl)
                .AddHeader(HeaderNames.Authorization, "Bearer " + responsePayload.AccessToken)
                .ExpectString()
                .PostAsync();

            if (!response.Success)
            {
                return Error(
                    $"failed to retrieve user info. {response.StatusCode} - {response.Data}"
                );
            }


            JsonElement userInfo;
            try
            {
                userInfo = JsonSerializer.Deserialize<JsonElement>(response.Data);
            }
            catch (Exception ex)
            {
                ex.Log();
                return Error(
                    $"could not deserialize user info. {ex.Message}"
                );
            }

            // convert the user info into claims
            static IEnumerable<Claim> ConvertToClaims(string name, JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in jsonElement.EnumerateObject())
                    {
                        foreach (var claim in ConvertToClaims(property.Name, property.Value))
                        {
                            yield return claim;
                        }
                    }
                }
                else if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var childElement in jsonElement.EnumerateArray())
                    {
                        foreach (var claim in ConvertToClaims(name, childElement))
                        {
                            yield return claim;
                        }
                    }
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    yield return new Claim(name, jsonElement.GetString());
                }

                // TODO: if we need more than just strings / arrays / objects
                // then add support here!
            }

            var claims = ConvertToClaims(null, userInfo);
            if (!Current.Security.TryValidateToken(new OIDCToken(claims), out var claimsPrincipal))
            {
                return Error("could not validate ID token" + responsePayload.IdToken);
            }

            await HttpContext.SignInAsync(claimsPrincipal);

            if (!decodedState.TryGetValue(OidcReturnUrlKey, out var returnUrl))
            {
                returnUrl = "~/";
            }

            return Redirect(returnUrl);
        }

        private IActionResult RedirectToProvider(string returnUrl)
        {
            // first write a cookie representing some unique identifier
            // we'll use this to validate that the OIDC flow is for the specific
            // user agent that started it in the callback above
            Span<byte> identifierBytes = stackalloc byte[32];
            CryptoRandom.Instance.NextBytes(identifierBytes);
            var oidcIdentifier = Convert.ToBase64String(identifierBytes).Replace("+", "-");

            Response.Cookies.Append(
                OidcCookieName,
                oidcIdentifier,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5),
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Secure = true,
                    IsEssential = true
                });

            var oidcSettings = (OIDCSecuritySettings) Current.Security.Settings;
            var redirectUri = Url.Action(
                nameof(OAuthCallback),
                ControllerContext.ActionDescriptor.ControllerName,
                null,
                Request.Scheme,
                Request.Host.Value,
                null
            );

            // construct the URL to the authorization endpoint
            var authorizationUrl = new UriBuilder(oidcSettings.AuthorizationUrl);
            var scopes = oidcSettings.Scopes ?? OIDCSecuritySettings.DefaultScopes;
            var encodedState = new QueryString()
                .Add(OidcIdentifierKey, oidcIdentifier)
                .Add(OidcReturnUrlKey, returnUrl ?? "/");
            var queryString = new QueryString(authorizationUrl.Query)
                .Add("response_type", "code")
                .Add("client_id", oidcSettings.ClientId)
                .Add("scope", string.Join(' ', scopes))
                .Add("redirect_uri", redirectUri)
                .Add("state", encodedState.ToUriComponent())
                .Add("nonce", Guid.NewGuid().ToString("N"));

            authorizationUrl.Query = queryString.ToUriComponent();

            return Redirect(authorizationUrl.ToString());
        }

        [DataContract]
        private class AccessTokenResponse
        {
            [DataMember(Name = "access_token")]
            public string AccessToken { get; set; }

            [DataMember(Name = "expires_in")]
            public int ExpiresIn { get; set; }

            [DataMember(Name = "id_token")]
            public string IdToken { get; set; }

            [DataMember(Name = "scopes")]
            public string Scopes { get; set; }

            [DataMember(Name = "token_type")]
            public string TokenType { get; set; }
        }
    }
}
