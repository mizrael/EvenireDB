using GrpcEventsClient;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace EvenireDB.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEvenireDB(this IServiceCollection services, EvenireConfig config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            if (config.UseGrpc)
            {
                services.AddGrpcClient<EventsGrpcService.EventsGrpcServiceClient>(o =>
                {
                    o.Address = config.Uri;
                });
                services.AddTransient<IEventsClient, GrpcEventsClient>();
            }
            else
            {
                var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);
                services.AddHttpClient<IEventsClient, HttpEventsClient>(client => {
                    client.BaseAddress = config.Uri;
                })
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(delay));                    
            }

            return services;
        }
    }
}