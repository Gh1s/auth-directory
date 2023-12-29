using Csb.Directory.Users;
using Grpc.Core;
using Grpc.Net.Client;
using IdentityModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Csb.Directory.Api.IntegrationTests
{
    public class WebApplicationFactory : WebApplicationFactory<Startup>
    {
        public string DbName { get; } = Guid.NewGuid().ToString();

        // These access_token are fake ones. They're never introspected by the IDP as we mock its response.

        public string AccessToken { get; } = Guid.NewGuid().ToString();

        public string InactiveAccessToken { get; } = Guid.NewGuid().ToString();

        public string FailedAccessToken { get; } = Guid.NewGuid().ToString();

        public Mock<HttpMessageHandler> IdpMessageHandlerMock { get; } = new Mock<HttpMessageHandler>();

        public Mock<HttpMessageHandler> GrpcMessageHandlerMock { get; } = new Mock<HttpMessageHandler>();

        public WebApplicationFactory()
        {
            IdpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (message, cancellationToken) =>
                {
                    HttpResponseMessage response;

                    if (message.RequestUri.ToString().EndsWith("/oauth2/introspect"))
                    {
                        string accessToken = null;

                        if (message.Content is FormUrlEncodedContent content)
                        {
                            var formData = QueryHelpers.ParseQuery(await message.Content.ReadAsStringAsync(CancellationToken.None));
                            if (formData.TryGetValue("token", out var tokenValues))
                            {
                                accessToken = tokenValues.ToString();
                            }
                        }

                        if (accessToken == FailedAccessToken)
                        {
                            throw new InvalidOperationException("This is failing access token");
                        }

                        var token = new
                        {
                            active = accessToken == AccessToken,
                            sub = TestConstants.User.Id,
                            client_id = "directory_test_client",
                            ext = new
                            {
                                preferred_username = TestConstants.User.Username,
                                given_name = TestConstants.User.FirstName,
                                family_name = TestConstants.User.LastName,
                                name = $"{TestConstants.User.LastName} {TestConstants.User.FirstName}"
                            }
                        };
                        response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                JsonSerializer.Serialize(token),
                                Encoding.UTF8,
                                MediaTypeNames.Application.Json
                            )
                        };
                    }
                    else
                    {
                        response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    }

                    return response;
                })
                .Verifiable();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "Grpc:CertificatePath", "../../../../../docker/certs/tls-grpc-ldap.crt" }
                    });
                })
                .ConfigureServices((ctx, services) =>
            {
                services.AddHttpClient(IntrospectionOptions.HttpClient).ConfigurePrimaryHttpMessageHandler(() => IdpMessageHandlerMock.Object);
                services.AddHttpClient(GrpcOptions.HttpClient)
                    .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://localhost:5500"))
                    .ConfigurePrimaryHttpMessageHandler(() => GrpcMessageHandlerMock.Object);
                services.AddScoped(p =>
                {
                    var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient(GrpcOptions.HttpClient);
                    var channel = GrpcChannel.ForAddress(httpClient.BaseAddress, new GrpcChannelOptions
                    {
                        HttpClient = httpClient
                    });
                    var userClientMock = new Mock<User.UserClient>(channel);
                    userClientMock
                        .Setup(m => m
                            .FindClaimsAsync(
                                It.Is<ClaimsRequest>(r =>
                                    r.Identifier == TestConstants.User.Id &&
                                    r.IdentifierType == IdentifierType.Subject
                                ),
                                null,
                                null,
                                It.IsAny<CancellationToken>()
                            )
                        )
                        .Returns(
                            new AsyncUnaryCall<ClaimsResponse>(
                                Task.FromResult(new ClaimsResponse
                                {
                                    Succeeded = true,
                                    Claims =
                                    {
                                        { JwtClaimTypes.Subject, TestConstants.User.Id },
                                        { JwtClaimTypes.PreferredUserName, TestConstants.User.Username },
                                        { JwtClaimTypes.GivenName, TestConstants.User.FirstName },
                                        { JwtClaimTypes.FamilyName, TestConstants.User.LastName },
                                        { JwtClaimTypes.Name, TestConstants.User.FullName },
                                        { JwtClaimTypes.Email, TestConstants.User.Email },
                                    }
                                }),
                                Task.FromResult(new Metadata()),
                                () => Status.DefaultSuccess,
                                () => new Metadata(),
                                () => { }
                            )
                        );
                    userClientMock
                        .Setup(m => m
                            .SearchClaimsAsync(
                                It.IsAny<SearchRequest>(),
                                null,
                                null,
                                It.IsAny<CancellationToken>()
                            )
                        )
                        .Returns(
                            new AsyncUnaryCall<SearchResponse>(
                                Task.FromResult(new SearchResponse
                                {
                                    Succeeded = true,
                                    Results =
                                    {
                                        new SearchResponseResult
                                        {
                                            Properties =
                                            {
                                                { JwtClaimTypes.Subject, TestConstants.User.Id },
                                                { JwtClaimTypes.PreferredUserName, TestConstants.User.Username },
                                                { JwtClaimTypes.GivenName, TestConstants.User.FirstName },
                                                { JwtClaimTypes.FamilyName, TestConstants.User.LastName },
                                                { JwtClaimTypes.Name, TestConstants.User.FullName },
                                                { JwtClaimTypes.Email, TestConstants.User.Email }
                                            }
                                        }
                                    }
                                }),
                                Task.FromResult(new Metadata()),
                                () => Status.DefaultSuccess,
                                () => new Metadata(),
                                () => { }
                            )
                        );
                    return userClientMock.Object;
                });
            });
        }

        public HttpClient CreateClientWithAuthentication()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IntrospectionOptions.Scheme, AccessToken);
            return client;
        }
    }
}
