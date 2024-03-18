using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AutoGenDotNet.Models.Logging;

/// <summary>
/// Represents a writer that raises an event when a string is written.
/// </summary>
public class StringEventWriter : StringWriter
{
    /// <summary>
    /// Event that is raised when a string is written.
    /// </summary>
    public event EventHandler<string?>? StringWritten;

    /// <inheritdoc/>
    public override void WriteLine(string? value)
    {
        StringWritten?.Invoke(this, value);
        base.WriteLine(value);
    }

    /// <inheritdoc/>
    public override void WriteLine(StringBuilder? stringBuilder)
    {
        StringWritten?.Invoke(this, stringBuilder?.ToString());
        base.WriteLine(stringBuilder);
    }

    /// <inheritdoc/>
    public override void WriteLine(object? value)
    {
        StringWritten?.Invoke(this, value?.ToString());
        base.WriteLine(value);
    }

    /// <inheritdoc/>
    public override void WriteLine(decimal value)
    {
        StringWritten?.Invoke(this, value.ToString(CultureInfo.CurrentCulture));
        base.WriteLine(value);
    }
}
/// <summary>
/// Represents a logger that writes log messages to a <see cref="StringEventWriter"/>.
/// </summary>
public class StringEventWriterLogger : ILogger
{
    private readonly StringEventWriter _stringEventWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringEventWriterLogger"/> class.
    /// </summary>
    /// <param name="stringEventWriter">The <see cref="StringEventWriter"/> to write log messages to.</param>
    public StringEventWriterLogger(StringEventWriter stringEventWriter)
    {
        _stringEventWriter = stringEventWriter;
    }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) => default;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        _stringEventWriter.WriteLine(formatter(state, exception!));
    }
}

/// <summary>
/// Represents a logger provider that creates instances of <see cref="StringEventWriterLogger"/>.
/// </summary>
public class StringEventWriterLoggerProvider : ILoggerProvider
{
    private readonly StringEventWriter _stringEventWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringEventWriterLoggerProvider"/> class.
    /// </summary>
    /// <param name="stringEventWriter">The <see cref="StringEventWriter"/> to write log messages to.</param>
    public StringEventWriterLoggerProvider(StringEventWriter stringEventWriter)
    {
        _stringEventWriter = stringEventWriter;
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
    {
        return new StringEventWriterLogger(_stringEventWriter);
    }

    /// <inheritdoc/>
    public void Dispose() { }
}

//public static class LoggerExtensions
//{
//    public static void LogInformation(this ILoggerFactory loggerFactory, string message, params object[] args)
//    {
//        loggerFactory.CreateLogger("Information").LogInformation(message, args);
//    }
//}