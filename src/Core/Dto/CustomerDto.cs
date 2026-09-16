namespace Core.Dto;

public record CustomerDto(string Id, string FullName, string Email) :  ImportRowDto(Id);