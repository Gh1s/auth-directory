using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Csb.Directory.Api
{
    /// <summary>
    /// Provides an implementation of <see cref="IAuthenticationHandler"/> for the Bearer scheme.
    /// </summary>
    public class IntrospectionHandler : AuthenticationHandler<IntrospectionOptions>
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IntrospectionHandler(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<IntrospectionOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
        ) : base(options, logger, encoder, clock)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeaderValues))
            {
                return AuthenticateResult.Fail("Missing authorization header");
            }

            if (!AuthenticationHeaderValue.TryParse(authHeaderValues.ToString(), out var authHeader) ||
                authHeader.Scheme != IntrospectionOptions.Scheme)
            {
                return AuthenticateResult.Fail("Unsupported scheme");
            }

            var token = authHeader.Parameter;
            if (token == null)
            {
                return AuthenticateResult.Fail("Missing access token");
            }

            try
            {
                var client = _httpClientFactory.CreateClient(IntrospectionOptions.HttpClient);
                using var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("token", token),
                    new KeyValuePair<string, string>("audience", Options.Audience)
                });
                using var response = await client.PostAsync("/oauth2/introspect", content, Context.RequestAborted);
                var st = await response.Content.ReadAsStringAsync();
                using var stream = await response.Content.ReadAsStreamAsync();
                var introspection = await JsonSerializer.DeserializeAsync<IntrospectionResponse>(stream, cancellationToken: Context.RequestAborted);
                if (introspection.Active)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(JwtClaimTypes.Subject, introspection.Sub),
                        new Claim(JwtClaimTypes.ClientId, introspection.ClientId)
                    };
                    if (introspection.Ext != null)
                    {
                        if (introspection.Ext.TryGetValue(JwtClaimTypes.PreferredUserName, out var preferredUsername))
                        {
                            claims.Add(new Claim(JwtClaimTypes.PreferredUserName, preferredUsername));
                        }
                        if (introspection.Ext.TryGetValue(JwtClaimTypes.GivenName, out var givenName))
                        {
                            claims.Add(new Claim(JwtClaimTypes.GivenName, givenName));
                        }
                        if (introspection.Ext.TryGetValue(JwtClaimTypes.FamilyName, out var familyName))
                        {
                            claims.Add(new Claim(JwtClaimTypes.FamilyName, familyName));
                        }
                        if (introspection.Ext.TryGetValue(JwtClaimTypes.Name, out var name))
                        {
                            claims.Add(new Claim(JwtClaimTypes.Name, name));
                        }
                    }

                    return AuthenticateResult.Success(
                        new AuthenticationTicket(
                            new ClaimsPrincipal(
                                new ClaimsIdentity(
                                    claims,
                                    IntrospectionOptions.Scheme
                                )
                            ),
                            IntrospectionOptions.Scheme
                        )
                    );
                }
                else
                {
                    return AuthenticateResult.Fail("The provided access token isn't valid");
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "An error has occured while introspecting the access token.");
                return AuthenticateResult.Fail("Could not introspect the access token");
            }
        }

        private class IntrospectionResponse
        {
            [JsonPropertyName("active")]
            public bool Active { get; set; }

            [JsonPropertyName("scope")]
            public string Scope { get; set; }

            [JsonPropertyName("client_id")]
            public string ClientId { get; set; }

            [JsonPropertyName("sub")]
            public string Sub { get; set; }

            [JsonPropertyName("exp")]
            public int Expiry { get; set; }

            [JsonPropertyName("iat")]
            public int IssuedAt { get; set; }

            [JsonPropertyName("nbf")]
            public int NotBefore { get; set; }

            [JsonPropertyName("aud")]
            public IEnumerable<string> Audiences { get; set; }

            [JsonPropertyName("iss")]
            public string Issuer { get; set; }

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }

            [JsonPropertyName("token_use")]
            public string TokenUse { get; set; }

            [JsonPropertyName("ext")]
            public Dictionary<string, string> Ext { get; set; }
        }
    }
}
