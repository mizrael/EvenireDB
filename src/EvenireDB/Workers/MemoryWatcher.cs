using EvenireDB.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EvenireDB.Server
{
    public record MemoryWatcherSettings(
        TimeSpan Interval,
        long MaxAllowedAllocatedBytes);

    public class MemoryWatcher : BackgroundService
    {
        private readonly MemoryWatcherSettings _settings;
        private readonly ILogger<MemoryWatcher> _logger;
        private readonly IServiceProvider _sp;
        private Process? process;

        public MemoryWatcher(MemoryWatcherSettings settings, ILogger<MemoryWatcher> logger, IServiceProvider sp)
        {
            _settings = settings;
            _logger = logger;
            _sp = sp;
        }

        public override void Dispose()
        {
            process?.Dispose();
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                process ??= Process.GetCurrentProcess();
                process.Refresh();

                _logger.MemoryUsageBelowTreshold(process.PrivateMemorySize64, _settings.MaxAllowedAllocatedBytes);

                bool needDrop = process.PrivateMemorySize64 > _settings.MaxAllowedAllocatedBytes;
                if (needDrop)
                {
                    _logger.HighMemoryUsageDetected(process.PrivateMemorySize64, _settings.MaxAllowedAllocatedBytes);
                                        
                    using var scope = _sp.CreateScope();
                    var cache = scope.ServiceProvider.GetRequiredService<ICache<Guid, CachedEvents>>();

                    var dropCount = cache.Count / 3;
                    cache.DropOldest(dropCount);

                    GC.Collect();
                }

                await Task.Delay(_settings.Interval, stoppingToken);
            }
        }
    }
}