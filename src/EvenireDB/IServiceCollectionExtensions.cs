using EvenireDB.Extents;
using EvenireDB.Persistence;
using EvenireDB.Server;
using EvenireDB.Utils;
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

            services
            .AddSingleton<ICache<Guid, CachedEvents>>(ctx =>
            {
                return new LRUCache<Guid, CachedEvents>(settings.MaxInMemoryStreamsCount);
            })
            .AddSingleton<IEventsCache, EventsCache>()
            .AddSingleton(ctx =>
            {

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
                return new EventDataValidator(settings.MaxEventDataSize);
            })
            .AddSingleton(new FileEventsRepositoryConfig(settings.MaxEventsPageSizeFromDisk))
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

                return new ExtentInfoProviderConfig(dataPath);
            })
            .AddSingleton<IExtentInfoProvider, ExtentInfoProvider>()
            .AddSingleton<IDataRepository, DataRepository>()
            .AddSingleton<IHeadersRepository, HeadersRepository>()
            .AddSingleton<IEventsProvider, EventsProvider>()
            .AddHostedService<IncomingEventsPersistenceWorker>()
            .AddSingleton(ctx =>
            {
                return new MemoryWatcherSettings(settings.MemoryWatcherInterval, settings.MaxAllowedAllocatedBytes);
            }).AddHostedService<MemoryWatcher>();

            return services;
        }
    }
}