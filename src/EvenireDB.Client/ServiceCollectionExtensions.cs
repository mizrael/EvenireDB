﻿using Microsoft.Extensions.DependencyInjection;
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
            
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);
            services.AddHttpClient<IEventsClient, EventsClient>(client =>
            {
                client.BaseAddress = config.Uri;
            })
            .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(delay));

            return services;
        }
    }
}