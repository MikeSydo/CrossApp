using Core.Abstractions;

namespace Core.Services;

public sealed class ConsoleSink : INotificationSink
{
    public void Write(string message) => Console.Write(message);
    public void WriteLine(string message) =>  Console.WriteLine(message);
}