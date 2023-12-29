using Microsoft.AspNetCore.Authentication;

namespace Csb.Directory.Api
{
    /// <summary>
    /// Configuration options for the Bearer scheme.
    /// </summary>
    public class IntrospectionOptions : AuthenticationSchemeOptions
    {
        public const string Scheme = "Bearer";
        public const string HttpClient = "introspection";

        /// <summary>
        /// The identity provider address.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// The audience to validate.
        /// </summary>
        public string Audience { get; set; }
    }
}
