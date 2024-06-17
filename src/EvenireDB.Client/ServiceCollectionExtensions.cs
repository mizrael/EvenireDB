using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace EvenireDB.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEvenireDB(this IServiceCollection services, EvenireClientConfig config)
    {
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        if (config.UseGrpc)
        {
            services.AddGrpcClient<GrpcEvents.EventsGrpcService.EventsGrpcServiceClient>(client =>
            {
                var builder = new UriBuilder(config.ServerUri);
                if (config.GrpcSettings is not null)
                    builder.Port = config.GrpcSettings.Port;
                client.Address = builder.Uri;
            });
            services.AddTransient<IEventsClient, GrpcEventsClient>();
        }
        else
        {   
            services.ConfigureHttpClient<IEventsClient, HttpEventsClient>(config);
        }

        // this is mostly for admin operations and there're no gRPC services for this (yet)
        services.ConfigureHttpClient<IStreamsClient, HttpStreamsClient>(config);

        return services;
    }

    private static IHttpClientBuilder ConfigureHttpClient<TClient, TImplementation>(this IServiceCollection services, EvenireClientConfig config)
        where TClient : class
        where TImplementation : class, TClient
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

        return services.AddHttpClient<TClient, TImplementation>(client => ConfigureClientSettings(client, config))
                        .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(delay));
    }

    private static void ConfigureClientSettings(HttpClient client, EvenireClientConfig config)
    {
        var builder = new UriBuilder(config.ServerUri);
        if(config.HttpSettings is not null)
            builder.Port = config.HttpSettings.Port;
        client.BaseAddress = builder.Uri;
        client.Timeout = config.Timeout;
    }
}