namespace Core.Events;

public sealed class OrderChangedEventArgs(string id, OrderChangeKind kind, 
    int linesCount, decimal total) : EventArgs
{
    public string Id { get; } =  id;
    public OrderChangeKind Kind { get; } = kind;
    public int LinesCount { get; } = linesCount;
    public decimal Total { get; } = total;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}