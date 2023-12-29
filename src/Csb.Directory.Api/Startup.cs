using Csb.Directory.Users;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Csb.Directory.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Cors.
            services.AddCors(options => options
                .AddDefaultPolicy(p => p
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .DisallowCredentials()
                )
            );

            // Mvc.
            services.AddControllers();

            // Security.
            // Clearing the default claims map to avoid claim missmap.
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services
                .AddAuthentication(IntrospectionOptions.Scheme)
                .AddScheme<IntrospectionOptions, IntrospectionHandler>(IntrospectionOptions.Scheme, options =>
                    Configuration.GetSection("Authentication:Bearer").Bind(options));
            services.AddHttpClient(IntrospectionOptions.HttpClient, client =>
                client.BaseAddress = new Uri(Configuration.GetValue<string>("Authentication:Authority")));
            services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

            // Grpc.
            services
                .AddHttpClient(GrpcOptions.HttpClient)
                .ConfigureHttpMessageHandlerBuilder(builder =>
                {
                    if (builder.PrimaryHandler is HttpClientHandler handler)
                    {
                        var serialNumber = X509Certificate
                            .CreateFromCertFile(Configuration.GetSection("Grpc:CertificatePath").Get<string>())
                            .GetSerialNumberString();
                        handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                            cert.SerialNumber.Equals(serialNumber, StringComparison.OrdinalIgnoreCase);
                    }
                });
            services.AddScoped(p =>
            {
                var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();
                var channel = GrpcChannel.ForAddress(
                    Configuration.GetValue<string>("Grpc:Address"),
                    new GrpcChannelOptions
                    {
                        HttpClient = httpClientFactory.CreateClient(GrpcOptions.HttpClient),
                        DisposeHttpClient = false
                    }
                );
                return new User.UserClient(channel);
            });
            services.Configure<GrpcOptions>(Configuration.GetSection("Grpc"));

            // Healthchecks.
            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UsePathBase(Configuration.GetValue<string>("PathBase"));

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
            });
        }
    }
}
