using Csb.Directory.Users;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Csb.Directory.Api.Controllers
{
    [Route("/users")]
    public class UserController : ControllerBase
    {
        private readonly User.UserClient _userClient;
        private readonly IOptionsMonitor<GrpcOptions> _optionsMonitor;

        private GrpcOptions 
            Options => _optionsMonitor.CurrentValue;

        public UserController(User.UserClient userClient, IOptionsMonitor<GrpcOptions> optionsMonitor)
        {
            _userClient = userClient;
            _optionsMonitor = optionsMonitor;
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<UserItem> Get(string id, CancellationToken cancellationToken)
        {
            var request = new ClaimsRequest
            {
                Identifier = id,
                IdentifierType = IdentifierType.Subject,
                Claims =
                {
                    Options.Claims
                }
            };
            var response = await _userClient.FindClaimsAsync(request, cancellationToken: cancellationToken);
            if (response.Succeeded)
            {
                return new UserItem
                {
                    Id = response.Claims[JwtClaimTypes.Subject],
                    Username = response.Claims[JwtClaimTypes.PreferredUserName],
                    FirstName = response.Claims[JwtClaimTypes.GivenName],
                    LastName = response.Claims[JwtClaimTypes.FamilyName],
                    FullName = response.Claims[JwtClaimTypes.Name],
                    Email = response.Claims[JwtClaimTypes.Email]
                };
            }

            return null;
        }

        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<UserItem>> Search(string search, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return Enumerable.Empty<UserItem>();
            }

            var request = new SearchRequest
            {
                Search = search,
                Claims =
                {
                    Options.Claims
                }
            };
            var response = await _userClient.SearchClaimsAsync(request, cancellationToken: cancellationToken);
            if (response.Succeeded)
            {
                return response.Results
                    .Select(r => new UserItem
                    {
                        Id = r.Properties[JwtClaimTypes.Subject],
                        Username = r.Properties[JwtClaimTypes.PreferredUserName],
                        FirstName = r.Properties[JwtClaimTypes.GivenName],
                        LastName = r.Properties[JwtClaimTypes.FamilyName],
                        FullName = r.Properties[JwtClaimTypes.Name],
                        Email = r.Properties[JwtClaimTypes.Email]
                    })
                    .ToList();
            }

            return Enumerable.Empty<UserItem>();
        }
    }

    public class UserItem
    {
        public string Id { get; set; }

        public string Username { get; set; }

        public string FirstName { get; set; }

        public string FullName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }
    }
}
