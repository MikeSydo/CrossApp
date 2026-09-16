using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class CsvImporter
{
    private const char Separator = ';';

    public static ImportResult<ImportRowDto> Load(string path)
    {
        var items = new List<ImportRowDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;

                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ImportRowDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", _, "", _]
                => new ParseFailed("у товару назва порожня"),

            ["P", _, _, var price] when !decimal.TryParse(
                price,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal parsedPrice) || parsedPrice < 0
                => new ParseFailed($"ціна товару '{price}' не є невід'ємним числом"),

            ["P", var id, var name, var price]
                => new ParseOk(new ProductDto(
                    id,
                    name,
                    decimal.Parse(
                        price,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture))),

            ["C", _, "", _]
                => new ParseFailed("у клієнта ім'я порожнє"),

            ["C", _, _, ""]
                => new ParseFailed("у клієнта email порожній"),

            ["C", var id, var fullName, var email]
                => new ParseOk(new CustomerDto(id, fullName, email)),

            ["P", ..]
                => new ParseFailed(
                    $"для товару очікую 4 колонки, отримав {parts.Length}"),

            ["C", ..]
                => new ParseFailed(
                    $"для клієнта очікую 4 колонки, отримав {parts.Length}"),

            [var prefix, ..]
                => new ParseFailed($"невідомий префікс типу '{prefix}'"),

            _
                => new ParseFailed("порожній або некоректний рядок")
        };
    }

    private abstract record ParseOutcome;

    private sealed record ParseOk(ImportRowDto Value) : ParseOutcome;

    private sealed record ParseFailed(string Reason) : ParseOutcome;
}