using Csb.Directory.Api.Controllers;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Csb.Directory.Api.IntegrationTests.Controllers
{
    public class UserControllerTests : IClassFixture<WebApplicationFactory>
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly WebApplicationFactory _factory;

        public UserControllerTests(WebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Test()
        {
            // Setup
            var client = _factory.CreateClientWithAuthentication();

            // Act
            var response = await client.GetAsync($"/users/{TestConstants.User.Id}");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            response.Content.Headers.ContentType.MediaType.Should().Be(MediaTypeNames.Application.Json);
            var json = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<UserItem>(json, SerializerOptions);
            user.Id.Should().Be(TestConstants.User.Id);
            user.Username.Should().Be(TestConstants.User.Username);
            user.FirstName.Should().Be(TestConstants.User.FirstName);
            user.LastName.Should().Be(TestConstants.User.LastName);
            user.FullName.Should().Be(TestConstants.User.FullName);
            user.Email.Should().Be(TestConstants.User.Email);
        }

        [Fact]
        public async Task Search_Test()
        {
            // Setup
            var client = _factory.CreateClientWithAuthentication();

            // Act
            var response = await client.GetAsync("/users?search=test");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            response.Content.Headers.ContentType.MediaType.Should().Be(MediaTypeNames.Application.Json);
            var json = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<IEnumerable<UserItem>>(json, SerializerOptions);
            users.Should().HaveCount(1);
            users.First().Id.Should().Be(TestConstants.User.Id);
            users.First().Username.Should().Be(TestConstants.User.Username);
            users.First().FirstName.Should().Be(TestConstants.User.FirstName);
            users.First().LastName.Should().Be(TestConstants.User.LastName);
            users.First().FullName.Should().Be(TestConstants.User.FullName);
            users.First().Email.Should().Be(TestConstants.User.Email);
        }
    }
}
