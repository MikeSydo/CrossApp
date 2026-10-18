using Core.Domain;
using Core.Dto;

namespace Core.Reports;

public static class ReportService
{
    // Фільтр + сортування + проєкція
    // Детерміноване сортування за сумою та Id
    public static IReadOnlyList<OrderReportLine> LowTotal(IEnumerable<Order> source, decimal threshold) =>
        source
            .Where(o => o.Total < threshold)
            .OrderBy(o => o.Total)
            .ThenBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Status.ToString(), o.Lines.Count))
            .ToList();

    // Топ-N зі стабільним порядком
    public static IReadOnlyList<OrderReportLine> TopByTotal(IEnumerable<Order> source, int n) =>
        source
            .TopBy(o => o.Total, o => o.Id, n)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Status.ToString(), o.Lines.Count))
            .ToList();

    // Групування з підрахунком і сумою
    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source
            .GroupBy(o => o.Status.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupSummary(
                Key: g.Key,
                Count: g.Count(),
                TotalAmount: g.Sum(o => o.Total)))
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

    // Групування з підрахунком і сумою (Query Syntax)
    public static IReadOnlyList<GroupSummary> ByStatusQuerySyntax(IEnumerable<Order> source) =>
        (from o in source
         group o by o.Status.ToString() into g
         orderby g.Sum(x => x.Total) descending
         select new GroupSummary(g.Key, g.Count(), g.Sum(x => x.Total)))
        .ToList();

    // Агрегати з безпечною обробкою порожньої послідовності
    public static (int Items, decimal Total, decimal Average, decimal Max) Totals(IEnumerable<Order> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0m, 0m, 0m)
            : (
                list.Count,
                list.Sum(o => o.Total),
                list.Average(o => o.Total),
                list.Max(o => o.Total)
            );
    }

    // GroupJoin двох колекцій (клієнти та замовлення)
    public static IReadOnlyList<CustomerOrdersSummary> CustomerOrdersReport(IEnumerable<CustomerDto> customers,
        IEnumerable<Order> orders) =>
        customers
            .GroupJoin(
                orders,
                c => c.Id,
                o => o.CustomerId,
                (customer, customerOrders) =>
                {
                    var enumerable = customerOrders.ToList();
                    return new CustomerOrdersSummary(
                        customer.Id,
                        customer.FullName,
                        enumerable.Count,
                        enumerable.Sum(o => o.Total));
                })
            .OrderByDescending(c => c.TotalSpent)
            .ThenBy(c => c.CustomerId, StringComparer.Ordinal)
            .ToList();

    // TopBy над зведенням клієнтів
    public static IReadOnlyList<CustomerOrdersSummary> TopCustomersReport(
        IEnumerable<CustomerDto> customers,
        IEnumerable<Order> orders,
        int n) =>
        CustomerOrdersReport(customers, orders)
            .TopBy(c => c.TotalSpent, c => c.CustomerId, n)
            .ToList();

    // Ланцюжок без ToList у середині
    public static IReadOnlyList<OrderReportLine> PipelineStreamed(IEnumerable<Order> source) =>
        source
            .Where(o => o.Total >= 1000m)
            .Take(100)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Status.ToString(), o.Lines.Count))
            .ToList();

    // Ланцюжок із ToList() у середині ланцюжка
    public static IReadOnlyList<OrderReportLine> PipelineWithIntermediateToList(IEnumerable<Order> source) =>
        source
            .Where(o => o.Total >= 1000m)
            .ToList() // матеріалізує всі записи джерела замість зупинки на 100
            .Take(100)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Status.ToString(), o.Lines.Count))
            .ToList();
}