using Core.Abstractions;

namespace Core.Services;

public sealed class FileSink : INotificationSink
{
    private readonly string _path;
    
    public FileSink(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }
    
    public void Write(string message)
    {
        EnsureDirectoryExists();
        File.AppendAllText(_path, message);
    }

    public void WriteLine(string message)
    {
        EnsureDirectoryExists();
        File.AppendAllText(_path, message + Environment.NewLine);
    }

    private void EnsureDirectoryExists()
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}