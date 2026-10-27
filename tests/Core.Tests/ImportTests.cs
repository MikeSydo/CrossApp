using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class ImportTests
{
    [Fact] //Скасування із заздалегідь скасованим токеном
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var service = new CatalogService(new InMemoryOrderStore());
        var importer = new JsonCatalogImporter();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(importer, "any.json", cts.Token));
    }

    [Fact] //Тимчасовий файл із некоректним записом
    public async Task ImportAsync_WithCorruptDataInTempFile_ThrowsDomainException_AndCleansUpTempFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"corrupt_orders_{Guid.NewGuid():N}.json");
        var corruptJson = """
        [
          {
            "Id": "ORD-ERR-1",
            "CustomerId": "C-ERR",
            "Status": 0,
            "Lines": [
              {
                "ProductId": "P-ERR",
                "Name": "Бракований товар",
                "Price": -50.0,
                "Quantity": 1
              }
            ]
          }
        ]
        """;

        await File.WriteAllTextAsync(tempFile, corruptJson);
        var service = new CatalogService(new InMemoryOrderStore());
        var importer = new JsonCatalogImporter();

        try
        {
            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                service.ImportAsync(importer, tempFile));

            Assert.Contains("Запис 1", ex.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact] //Додаткове завдання 1: потоковий імпорт через IAsyncEnumerable
    public async Task ImportStreamAsync_ReadsAllOrders_ViaAsyncEnumerable()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"stream_{Guid.NewGuid():N}.json");
        var json = """
        [
          {
            "Id": "ORD-S1",
            "CustomerId": "C-S1",
            "Status": 0,
            "Lines": [{ "ProductId": "P-S1", "Name": "Товар 1", "Price": 50, "Quantity": 1 }]
          },
          {
            "Id": "ORD-S2",
            "CustomerId": "C-S2",
            "Status": 0,
            "Lines": [{ "ProductId": "P-S2", "Name": "Товар 2", "Price": 75, "Quantity": 2 }]
          }
        ]
        """;

        await File.WriteAllTextAsync(tempFile, json);
        var store = new InMemoryOrderStore();
        var service = new CatalogService(store);
        var importer = new JsonOrderImporter();

        try
        {
            var added = await service.ImportStreamAsync(importer, tempFile);

            Assert.Equal(2, added);
            Assert.Equal(2, store.List().Count);
            Assert.NotNull(store.GetById("ORD-S1"));
            Assert.NotNull(store.GetById("ORD-S2"));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact] //Додаткове завдання 2: зв'язані токени та доказ спрацювання саме таймауту
    public async Task ImportAsync_WithLinkedTokens_FailsByTimeout()
    {
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

        //Очікуємо спрацювання таймауту
        // ReSharper disable once MethodSupportsCancellation
        await Task.Delay(100);

        Assert.True(timeoutCts.IsCancellationRequested, "Токен таймауту повинен бути скасований");
        Assert.False(userCts.IsCancellationRequested, "Токен користувача НЕ повинен бути скасований");
        Assert.True(linkedCts.IsCancellationRequested, "Зв'язаний токен повинен бути скасований");

        var service = new CatalogService(new InMemoryOrderStore());
        var importer = new JsonCatalogImporter();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(importer, "non_existent.json", linkedCts.Token));
    }

    [Fact] //Додаткове завдання 3: звіт про хід імпорту через IProgress<int>
    public async Task ImportAsync_ReportsProgress_ViaIProgress()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"progress_{Guid.NewGuid():N}.json");
        var json = """
        [
          {
            "Id": "ORD-P1",
            "CustomerId": "C-P1",
            "Status": 0,
            "Lines": [{ "ProductId": "P-1", "Name": "Товар 1", "Price": 10, "Quantity": 1 }]
          },
          {
            "Id": "ORD-P2",
            "CustomerId": "C-P2",
            "Status": 0,
            "Lines": [{ "ProductId": "P-2", "Name": "Товар 2", "Price": 20, "Quantity": 1 }]
          }
        ]
        """;

        await File.WriteAllTextAsync(tempFile, json);
        var store = new InMemoryOrderStore();
        var service = new CatalogService(store);
        var importer = new JsonCatalogImporter();

        var progressValues = new List<int>();
        var progress = new SynchronousProgress<int>(progressValues.Add);

        try
        {
            var added = await service.ImportAsync(importer, tempFile, CancellationToken.None, progress);

            Assert.Equal(2, added);
            Assert.NotEmpty(progressValues);
            Assert.Equal(100, progressValues.Last());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private sealed class SynchronousProgress<T>(Action<T> action) : IProgress<T>
    {
        public void Report(T value) => action(value);
    }
}
