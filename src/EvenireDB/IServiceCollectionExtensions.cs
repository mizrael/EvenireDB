﻿using EvenireDB.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EvenireDB
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddEvenire(this IServiceCollection services, EvenireServerSettings settings)
        {
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            });

            services.AddSingleton<IEventsCache>(ctx =>
            {
                var logger = ctx.GetRequiredService<ILogger<EventsCache>>();
                return new EventsCache(settings.MaxInMemoryStreamsCount, logger);
            })
            .AddSingleton(ctx =>
            {

                return new EventsProviderConfig(settings.MaxPageSizeToClient);
            })
            .AddSingleton<IEventsProvider, EventsReader>()
            .AddSingleton(channel.Writer)
            .AddSingleton(channel.Reader)
            .AddSingleton<IEventValidator>(ctx =>
            {
                return new EventValidator(settings.MaxEventDataSize);
            })
            .AddSingleton(ctx =>
            {
                var dataPath = settings.DataFolder;
                if (string.IsNullOrWhiteSpace(dataPath))
                    dataPath = Path.Combine(AppContext.BaseDirectory, "data");
                else
                {
                    if (!Path.IsPathFullyQualified(dataPath))
                        dataPath = Path.Combine(AppContext.BaseDirectory, dataPath);
                }

                return new FileEventsRepositoryConfig(dataPath, settings.MaxEventsPageSizeFromDisk);
            })
            .AddSingleton<IEventsRepository, FileEventsRepository>()
            .AddHostedService<IncomingEventsPersistenceWorker>()
            .AddSingleton(ctx =>
            {
                return new MemoryWatcherSettings(settings.MemoryWatcherInterval, settings.MaxAllowedAllocatedBytes);
            }).AddHostedService<MemoryWatcher>();

            return services;
        }
    }
}