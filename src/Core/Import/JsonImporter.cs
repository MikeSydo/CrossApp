using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class JsonImporter
{
    public static ImportResult<ImportRowDto> Load(string path)
    {
        var items = new List<ImportRowDto>();
        var errors = new List<string>();

        string json = File.ReadAllText(path);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        List<JsonRow> rows =
            JsonSerializer.Deserialize<List<JsonRow>>(json, options) ?? [];

        for (int i = 0; i < rows.Count; i++)
        {
            int number = i + 1;

            switch (ParseRow(rows[i]))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;

                case ParseFailed failed:
                    errors.Add($"об'єкт {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ImportRowDto>(items, errors);
    }

    private static ParseOutcome ParseRow(JsonRow row)
    {
        return row switch
        {
            { Type: "P", Id: null or "" }
                => new ParseFailed("у товару id порожній"),

            { Type: "P", Name: null or "" }
                => new ParseFailed("у товару назва порожня"),

            { Type: "P", Price: < 0 }
                => new ParseFailed(
                    $"ціна товару '{row.Price}' не є невід'ємним числом"),

            { Type: "P", Id: not null and not "", Name: not null and not "" }
                => new ParseOk(new ProductDto(
                    row.Id,
                    row.Name,
                    row.Price)),

            { Type: "C", Id: null or "" }
                => new ParseFailed("у клієнта id порожній"),

            { Type: "C", FullName: null or "" }
                => new ParseFailed("у клієнта ім'я порожнє"),

            { Type: "C", Email: null or "" }
                => new ParseFailed("у клієнта email порожній"),

            {
                Type: "C",
                Id: not null and not "",
                FullName: not null and not "",
                Email: not null and not ""
            }
                => new ParseOk(new CustomerDto(
                    row.Id,
                    row.FullName,
                    row.Email)),

            { Type: null or "" }
                => new ParseFailed("відсутнє поле type"),

            { Type: var type }
                => new ParseFailed($"невідомий тип запису '{type}'")
        };
    }

    private sealed record JsonRow(
        string? Type,
        string? Id,
        string? Name,
        decimal Price,
        string? FullName,
        string? Email);

    private abstract record ParseOutcome;

    private sealed record ParseOk(ImportRowDto Value) : ParseOutcome;

    private sealed record ParseFailed(string Reason) : ParseOutcome;
}