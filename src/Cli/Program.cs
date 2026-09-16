using Core.Dto;
using Core.Import;

string path = args.Length > 0
    ? args[0]
    : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

ImportResult<ImportRowDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => CsvImporter.Load(path),
    ".json" => JsonImporter.Load(path),
    var extension => throw new NotSupportedException(
        $"Непідтримуваний формат '{extension}'. Використовуйте .csv або .json.")
};

Console.WriteLine($"Шлях файлу: {path}");

int accepted = result.Items.Count;
int skipped = result.Errors.Count;
int total = accepted + skipped;

decimal errorPercent = total == 0
    ? 0
    : (decimal)skipped / total * 100;

Console.WriteLine(
    $"Усього: {total} | " +
    $"Прийнято: {accepted} | " +
    $"Пропущено: {skipped} | " +
    $"Помилок: {errorPercent:F2}%");

Console.WriteLine();

foreach (ImportRowDto item in result.Items)
{
    switch (item)
    {
        case ProductDto product:
            Console.WriteLine(
                $"Товар   | {product.Id,-6} | " +
                $"{product.Name,-30} | {product.Price,10:F2}");
            break;

        case CustomerDto customer:
            Console.WriteLine(
                $"Клієнт  | {customer.Id,-6} | " +
                $"{customer.FullName,-30} | {customer.Email}");
            break;
    }
}

if (result.Errors.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");

    foreach (string error in result.Errors)
    {
        Console.WriteLine($"! {error}");
    }
}

return 0;