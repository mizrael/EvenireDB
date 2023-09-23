﻿namespace EvenireDB.Server.DTO
{
    public record EventDTO(Guid Id, string Type, byte[] Data)
    {
        public static EventDTO FromModel(Event @event)
        => new EventDTO(@event.Id, @event.Type, @event.Data);
    }
}