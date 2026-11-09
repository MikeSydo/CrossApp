using System.Text.Json;
using Core.Configuration;
using Core.Storage;

namespace Core.Tests;

public sealed class FileOrderStoreTests
{
    [Fact]
    public void StorageOptions_FilePathOverridesDirectoryAndFileName()
    {
        var root = CreateTempDirectory();
        var source = Path.Combine(root, "jsample.json");

        try
        {
            var options = new StorageOptions
            {
                DataPath = source,
                FileName = "orders.json"
            };

            Assert.Equal(Path.GetFullPath(source), options.ResolveFullPath(root));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void FileOrderStore_RejectsUnsupportedFormat()
    {
        Assert.Throws<ArgumentException>(() =>
            new FileOrderStore("orders.data", "xml"));
    }

    [Fact]
    public void FileOrderStore_RejectsMalformedJson()
    {
        var root = CreateTempDirectory();
        var path = Path.Combine(root, "orders.json");

        try
        {
            File.WriteAllText(path, "{ invalid json");

            using var store = new FileOrderStore(path);
            Assert.Throws<JsonException>(store.List);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void FileOrderStore_RejectsInvalidCsvHeader()
    {
        var root = CreateTempDirectory();
        var path = Path.Combine(root, "orders.csv");

        try
        {
            File.WriteAllText(path, "orderId,customerId,wrong,status,productId,name,price");

            using var store = new FileOrderStore(path, "csv");
            var exception = Assert.Throws<FormatException>(store.List);

            Assert.Contains("CSV має містити заголовок", exception.Message);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"crossapp-orders-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
