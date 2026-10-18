using System.Diagnostics;
using System.Text;
using Cli;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;

Console.OutputEncoding = Encoding.UTF8;

IOrderStore store = StoreFactory.Create(args);
var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
INotificationSink sink = new FileSink(logPath);
var service = new OrderService(store, sink);

var items = service.All();
var customers = SampleData.Customers().ToList();
var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";

switch (command)
{
    case "report-top":
        Print(ReportService.TopByTotal(items, 5));
        break;

    case "report-low":
        Print(ReportService.LowTotal(items, 1000m));
        break;

    case "report-groups":
        foreach (var g in ReportService.ByStatus(items))
        {
            Console.WriteLine($" {g.Key,-12} замовлень: {g.Count,3} сумарно: {g.TotalAmount,10:F2} грн");
        }
        break;

    case "report-query-syntax":
        Console.WriteLine("=== Звіт ByStatus через Query Syntax (from ... group ... into) ===");
        foreach (var g in ReportService.ByStatusQuerySyntax(items))
        {
            Console.WriteLine($" {g.Key,-12} замовлень: {g.Count,3} сумарно: {g.TotalAmount,10:F2} грн");
        }
        break;

    case "report-sum":
        var t = ReportService.Totals(items);
        Console.WriteLine($" позицій {t.Items}, всього {t.Total:F2}, середнє {t.Average:F2}, макс {t.Max:F2}");
        break;

    case "report-join":
        Console.WriteLine("=== GroupJoin (Клієнти та Замовлення) ===");
        Console.WriteLine($" {"Id",-6} {"Ім'я клієнта",-20} {"Замовлень",10} {"Сума покупок (грн)",20}");
        Console.WriteLine(new string('-', 60));
        foreach (var c in ReportService.CustomerOrdersReport(customers, items))
        {
            Console.WriteLine($" {c.CustomerId,-6} {c.CustomerName,-20} {c.OrdersCount,10} {c.TotalSpent,20:F2}");
        }
        break;

    case "benchmark":
        RunBenchmark();
        break;

    case "demo-deferred":
        RunDeferredExecutionDemo(service);
        break;

    default:
        Console.WriteLine("Доступні команди: report-top | report-low | report-groups | report-query-syntax | report-sum | report-join | benchmark | demo-deferred");
        break;
}

static void Print(IReadOnlyList<OrderReportLine> lines)
{
    Console.WriteLine($" {"Id",-8} {"CustomerId",-12} {"Сума (грн)",12} {"Статус",-12} {"Позицій",8}");
    Console.WriteLine(new string('-', 56));
    foreach (var l in lines)
    {
        Console.WriteLine($" {l.Id,-8} {l.CustomerId,-12} {l.Total,12:F2} {l.Status,-12} {l.LinesCount,8}");
    }
}

static void RunDeferredExecutionDemo(OrderService service)
{
    Console.WriteLine("=== Демонстрація відкладеного виконання (Deferred Execution) ===");
    var source = service.All().ToList();

    var query = source.Where(o => o.Total < 1000m); // запит побудовано, не виконано
    Console.WriteLine($"Малих замовлень (< 1000 грн) до змін: {query.Count()}");

    var newOrder = Order.Create("O-TMP01", "C-TMP");
    newOrder.AddLine("P-004", "USB-кабель", 180m, 1);
    source.Add(newOrder);

    Console.WriteLine($"Малих замовлень після змін: {query.Count()}"); // число інше

    var snapshot = query.ToList(); // результат зафіксовано
    source.Clear();

    Console.WriteLine($"У знімку лишилось: {snapshot.Count}"); // не змінюється
}

static void RunBenchmark()
{
    const int count = 100_000;
    Console.WriteLine($"Генерація {count:N0} замовлень для бенчмарку...");
    var data = SampleData.GenerateOrders(count).ToList();

    ReportService.PipelineStreamed(data.Take(100));
    ReportService.PipelineWithIntermediateToList(data.Take(100));

    // Потоковий (без ToList посередині)
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var sw1 = Stopwatch.StartNew();
    var res1 = ReportService.PipelineStreamed(data);
    sw1.Stop();

    // ВЗ проміжними ToList()
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var sw2 = Stopwatch.StartNew();
    var res2 = ReportService.PipelineWithIntermediateToList(data);
    sw2.Stop();

    Console.WriteLine("\n=== Результати вимірювання Stopwatch (100 000 записів) ===");
    Console.WriteLine($"1. Без ToList у середині ланцюжка:  {sw1.ElapsedMilliseconds,5} мс ({sw1.ElapsedTicks,9:N0} тіків)");
    Console.WriteLine($"2. З проміжними ToList() у середині: {sw2.ElapsedMilliseconds,5} мс ({sw2.ElapsedTicks,9:N0} тіків)");
    Console.WriteLine($"Різниця: варіант без ToList швидший приблизно у {(double)sw2.ElapsedTicks / sw1.ElapsedTicks:F2} рази");
}