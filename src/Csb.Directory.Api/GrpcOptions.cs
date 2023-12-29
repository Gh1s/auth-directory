using System.Collections.Generic;

namespace Csb.Directory.Api
{
    /// <summary>
    /// Configuration options for the user gRPC feature.
    /// </summary>
    public class GrpcOptions
    {
        public const string HttpClient = "grpc";

        /// <summary>
        /// The claims that must be fetched from the gRPC.
        /// </summary>
        public IEnumerable<string> Claims { get; set; }
    }
}
