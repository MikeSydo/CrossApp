namespace Core.Configuration;

public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public OrdersOptions Orders { get; init; } = new();
    public string EnvironmentName { get; init; } = "Development";
}

public sealed class OrdersOptions
{
    public string Currency { get; init; } = "UAH";
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "orders.json";
    public string Format { get; init; } = "json";

    public string ResolveFullPath(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(DataPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(FileName);

        var configuredPath = Path.IsPathRooted(DataPath)
            ? DataPath
            : Path.Combine(baseDirectory, DataPath);

        if (IsFilePath(configuredPath))
        {
            var fullFilePath = Path.GetFullPath(configuredPath);
            return fullFilePath;
        }

        return Path.GetFullPath(Path.Combine(configuredPath, FileName));
    }

    private static bool IsFilePath(string path) =>
        File.Exists(path) ||
        string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase);
}
