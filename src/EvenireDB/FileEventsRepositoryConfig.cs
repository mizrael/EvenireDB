
namespace EvenireDB
{
    public record FileEventsRepositoryConfig(string BasePath, uint MaxPageSize = 100);
}