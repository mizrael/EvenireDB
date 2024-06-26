﻿using EvenireDB.Common;
using EvenireDB.Persistence;
using EvenireDB.Server;
using EvenireDB.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace EvenireDB;

public static class IServiceCollectionExtensions
{
    public static EvenireServerSettings GetServerSettings(this IServiceProvider sp)
    {
        return sp.GetRequiredService<IOptions<EvenireServerSettings>>().Value;
    }

    public static IServiceCollection AddEvenire(this IServiceCollection services)
    {
        var channel = Channel.CreateUnbounded<IncomingEventsBatch>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false,
            AllowSynchronousContinuations = true
        });

        services
        .AddSingleton<ICache<StreamId, CachedEvents>>(ctx =>
        {
            var settings = ctx.GetServerSettings();
            return new LRUCache<StreamId, CachedEvents>(settings.MaxInMemoryStreamsCount);
        })
        .AddSingleton<IStreamsCache, StreamsCache>()
        .AddSingleton(ctx =>
        {
            var settings = ctx.GetServerSettings();
            return new EventsReaderConfig(settings.MaxPageSizeToClient);
        })
        .AddSingleton(TimeProvider.System)
        .AddSingleton<IEventIdGenerator, EventIdGenerator>()
        .AddSingleton<IEventsReader, EventsReader>()
        .AddSingleton<IEventsWriter, EventsWriter>()
        .AddSingleton(channel.Writer)
        .AddSingleton(channel.Reader)
        .AddSingleton<IEventDataValidator>(ctx =>
        {
            var settings = ctx.GetServerSettings();
            return new EventDataValidator(settings.MaxEventDataSize);
        })
        .AddSingleton(ctx =>
        {
            var settings = ctx.GetServerSettings();
            return new FileEventsRepositoryConfig(settings.MaxEventsPageSizeFromDisk);
        })
        .AddSingleton(ctx =>
        {
            var settings = ctx.GetServerSettings();
            var dataPath = settings.DataFolder;
            if (string.IsNullOrWhiteSpace(dataPath))
                dataPath = Path.Combine(AppContext.BaseDirectory, "data");
            else
            {
                if (!Path.IsPathFullyQualified(dataPath))
                    dataPath = Path.Combine(AppContext.BaseDirectory, dataPath);
            }

            return new ExtentsProviderConfig(dataPath);
        })
        .AddSingleton<IExtentsProvider, ExtentsProvider>()
        .AddSingleton<IStreamInfoProvider, StreamInfoProvider>()
        .AddSingleton<IDataRepository, DataRepository>()
        .AddSingleton<IHeadersRepository, HeadersRepository>()
        .AddSingleton<IEventsProvider, EventsProvider>()
        .AddHostedService<IncomingEventsPersistenceWorker>()
        .AddSingleton(ctx =>
        {
            var settings = ctx.GetServerSettings();
            return new MemoryWatcherSettings(settings.MemoryWatcherInterval, settings.MaxAllowedAllocatedBytes);
        }).AddHostedService<MemoryWatcher>();

        return services;
    }
}