using FluentAssertions;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Csb.Directory.Api.IntegrationTests
{
    public class IntrospectionHandlerTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly IServiceScope _scope;

        private IServiceProvider Provider => _scope.ServiceProvider;

        public IntrospectionHandlerTests(WebApplicationFactory factory)
        {
            _factory = factory;
            _scope = factory.Services.CreateScope();
        }

        [Fact]
        public async Task HandleAuthenticateAsync_Test()
        {
            // Setup
            var handler = Provider.GetRequiredService<IntrospectionHandler>();
            var scheme = new AuthenticationScheme(IntrospectionOptions.Scheme, IntrospectionOptions.Scheme, typeof(IntrospectionHandler));
            var httpContext = new DefaultHttpContext
            {
                RequestServices = Provider,
                Request =
                {
                    Headers =
                    {
                        { HeaderNames.Authorization, $"{IntrospectionOptions.Scheme} {_factory.AccessToken}" }
                    }
                }
            };
            await handler.InitializeAsync(scheme, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Principal.FindFirstValue(JwtClaimTypes.Subject).Should().Be(TestConstants.User.Id);
            result.Principal.FindFirstValue(JwtClaimTypes.PreferredUserName).Should().Be(TestConstants.User.Username);
            result.Principal.FindFirstValue(JwtClaimTypes.GivenName).Should().Be(TestConstants.User.FirstName);
            result.Principal.FindFirstValue(JwtClaimTypes.FamilyName).Should().Be(TestConstants.User.LastName);
            result.Principal.FindFirstValue(JwtClaimTypes.Name).Should().Be($"{TestConstants.User.LastName} {TestConstants.User.FirstName}");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_MissingAuthorizationHeader_Test()
        {
            // Setup
            var handler = Provider.GetRequiredService<IntrospectionHandler>();
            var scheme = new AuthenticationScheme(IntrospectionOptions.Scheme, IntrospectionOptions.Scheme, typeof(IntrospectionHandler));
            var httpContext = new DefaultHttpContext
            {
                RequestServices = Provider
            };
            await handler.InitializeAsync(scheme, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure.Message.Should().Be("Missing authorization header");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_MissingScheme_Test()
        {
            // Setup
            var handler = Provider.GetRequiredService<IntrospectionHandler>();
            var scheme = new AuthenticationScheme(IntrospectionOptions.Scheme, IntrospectionOptions.Scheme, typeof(IntrospectionHandler));
            var httpContext = new DefaultHttpContext
            {
                RequestServices = Provider,
                Request =
                {
                    Headers =
                    {
                        { HeaderNames.Authorization, "" }
                    }
                }
            };
            await handler.InitializeAsync(scheme, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure.Message.Should().Be("Unsupported scheme");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_UnsupportedScheme_Test()
        {
            // Setup
            var handler = Provider.GetRequiredService<IntrospectionHandler>();
            var scheme = new AuthenticationScheme(IntrospectionOptions.Scheme, IntrospectionOptions.Scheme, typeof(IntrospectionHandler));
            var httpContext = new DefaultHttpContext
            {
                RequestServices = Provider,
                Request =
                {
                    Headers =
                    {
                        { HeaderNames.Authorization, "Fake token" }
                    }
                }
            };
            await handler.InitializeAsync(scheme, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure.Message.Should().Be("Unsupported scheme");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_MissingAccessToken_Test()
        {
            // Setup
            var handler = Provider.GetRequiredService<IntrospectionHandler>();
            var scheme = new AuthenticationScheme(IntrospectionOptions.Scheme, IntrospectionOptions.Scheme, typeof(IntrospectionHandler));
            var httpContext = new DefaultHttpContext
            {
                RequestServices = Provider,
                Request =
                {
                    Headers =
                    {
                        { HeaderNames.Authorization, $"{IntrospectionOptions.Scheme} " }
                    }
                }
            };
            await handler.InitializeAsync(scheme, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure.Message.Should().Be("Missing access token");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidAccessToken_Test()
        {
            // Setup
            var handler = Provider.GetRequiredService<IntrospectionHandler>();
            var scheme = new AuthenticationScheme(IntrospectionOptions.Scheme, IntrospectionOptions.Scheme, typeof(IntrospectionHandler));
            var httpContext = new DefaultHttpContext
            {
                RequestServices = Provider,
                Request =
                {
                    Headers =
                    {
                        { HeaderNames.Authorization, $"{IntrospectionOptions.Scheme} {_factory.InactiveAccessToken}" }
                    }
                }
            };
            await handler.InitializeAsync(scheme, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure.Message.Should().Be("The provided access token isn't valid");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_IntrospectionFailed_Test()
        {
            // Setup
            var handler = Provider.GetRequiredService<IntrospectionHandler>();
            var scheme = new AuthenticationScheme(IntrospectionOptions.Scheme, IntrospectionOptions.Scheme, typeof(IntrospectionHandler));
            var httpContext = new DefaultHttpContext
            {
                RequestServices = Provider,
                Request =
                {
                    Headers =
                    {
                        { HeaderNames.Authorization, $"{IntrospectionOptions.Scheme} {_factory.FailedAccessToken}" }
                    }
                }
            };
            await handler.InitializeAsync(scheme, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure.Message.Should().Be("Could not introspect the access token");
        }
    }
}
