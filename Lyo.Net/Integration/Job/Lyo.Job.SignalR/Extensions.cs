using Lyo.MessageQueue;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.SignalR;

/// <summary>DI extensions for job SignalR dashboard.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="JobHub" /> and <see cref="JobEventBroadcaster" />. Requires <see cref="IMqService" />.</summary>
        public IServiceCollection AddJobSignalR()
        {
            services.AddSignalR();
            services.AddHostedService<JobEventBroadcaster>();
            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>Maps the <see cref="JobHub" /> endpoint at <c>/hubs/job</c>.</summary>
        public WebApplication MapJobHub(string path = "/hubs/job")
        {
            app.MapHub<JobHub>(path);
            return app;
        }
    }
}
