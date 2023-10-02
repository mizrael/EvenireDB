using GrpcEventsClient;
using Microsoft.Extensions.DependencyInjection;

namespace EvenireDB.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEvenireDB(this IServiceCollection services, EvenireConfig config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            services.AddGrpcClient<EventsGrpcService.EventsGrpcServiceClient>(o =>
            {
                o.Address = config.Uri;
            });

            return services;
        }
    }
}