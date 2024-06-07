namespace EvenireDB.Server.DTO;
public record EventDataDTO(string Type, ReadOnlyMemory<byte> Data);
