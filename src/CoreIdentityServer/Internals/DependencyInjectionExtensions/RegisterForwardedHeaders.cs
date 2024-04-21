using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterForwardedHeaders
    {
        // configures the headers to forward
        public static IServiceCollection AddForwardedHeadersMiddleware(this IServiceCollection services, IWebHostEnvironment environment)
        {
            if (environment.IsProduction())
            {
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                });
            }

            return services;
        }
    }
}
