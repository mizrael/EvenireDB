using EvenireDB.Common;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EvenireDB;

// TODO: append to a transaction log
public class EventsWriter : IEventsWriter
{
    private readonly IStreamsCache _cache;
    private readonly ChannelWriter<IncomingEventsBatch> _writer;
    private readonly IEventIdGenerator _idGenerator;
    private readonly ILogger<EventsWriter> _logger;

    public EventsWriter(IStreamsCache cache, ChannelWriter<IncomingEventsBatch> writer, IEventIdGenerator idGenerator, ILogger<EventsWriter> logger)
    {
        _cache = cache;
        _writer = writer;
        _idGenerator = idGenerator;
        _logger = logger;
    }
    
    public async ValueTask<IOperationResult> AppendAsync(
        StreamId streamId, 
        IEnumerable<EventData> incomingEvents, 
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        if(incomingEvents is null)
            return FailureResult.NullEvents(streamId);

        if (!incomingEvents.Any())
            return new SuccessResult();

        CachedEvents entry = await _cache.GetEventsAsync(streamId, cancellationToken).ConfigureAwait(false);

        if (expectedVersion.HasValue && entry.Events.Count != expectedVersion)
            return FailureResult.VersionMismatch(streamId, expectedVersion.Value, entry.Events.Count);    

        entry.Semaphore.Wait(cancellationToken);
        try
        {
            // TODO: add a metadata field on the event data, use it to allow check for duplicate events
            //if (entry.Events.Count > 0 &&
            //    HasDuplicateEvent(incomingEvents, entry, out var duplicate))
            //    return FailureResult.DuplicateEvent(duplicate);

            _logger.AppendingEventsToStream(incomingEvents.Count(), streamId);

            var events = new List<Event>();

            EventId? previousEventId = null;
            var lastEvent = entry.Events.LastOrDefault();
            if(lastEvent != null)
                previousEventId = lastEvent.Id;
            
            for (int i = 0; i < incomingEvents.Count(); i++)
            {
                var eventData = incomingEvents.ElementAt(i);

                var eventId = _idGenerator.Generate(previousEventId);
                var @event = new Event(eventId, eventData.Type, eventData.Data);

                events.Add(@event);

                previousEventId = eventId;
            }

            var group = new IncomingEventsBatch(streamId, events);
            if (!_writer.TryWrite(group))
                return FailureResult.CannotInitiateWrite(streamId);

            entry.Events.AddRange(events);
            _cache.Update(streamId, entry);
        }
        finally
        {
            entry.Semaphore.Release();
        }

        return new SuccessResult();
    }
}