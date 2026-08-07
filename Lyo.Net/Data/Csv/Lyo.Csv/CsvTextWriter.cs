using Lyo.Csv.Models;

namespace Lyo.Csv;

/// <summary>Writes CSV fields/rows using <see cref="CsvOptions" /> delimiter, quote, and escape rules.</summary>
internal sealed class CsvTextWriter : IDisposable
#if !NETSTANDARD2_0
    , IAsyncDisposable
#endif
{
    private readonly TextWriter _writer;
    private readonly CsvOptions _options;
    private readonly char _delimiter;
    private readonly char _quote;
    private readonly char _escape;
    private bool _disposed;
    private bool _fieldOnRow;

    public CsvTextWriter(TextWriter writer, CsvOptions options)
    {
        _writer = writer;
        _options = options;
        _delimiter = options.Delimiter[0];
        _quote = options.Quote;
        _escape = options.Escape;
    }

    public void WriteField(string? value)
    {
        if (_fieldOnRow)
            _writer.Write(_delimiter);

        WriteEscaped(value);
        _fieldOnRow = true;
    }

    public void WriteFields(IEnumerable<string?> values)
    {
        foreach (var value in values)
            WriteField(value);
    }

    public void NextRecord()
    {
        _writer.WriteLine();
        _fieldOnRow = false;
    }

    public void Flush() => _writer.Flush();

#if !NETSTANDARD2_0
    /// <summary>Writes a field using sync I/O (preferred for memory/file streams).</summary>
    public Task WriteFieldAsync(string? value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        WriteField(value);
        return Task.CompletedTask;
    }

    public Task WriteFieldsAsync(IEnumerable<string?> values, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        WriteFields(values);
        return Task.CompletedTask;
    }

    public Task NextRecordAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        NextRecord();
        return Task.CompletedTask;
    }

    public Task FlushAsync(CancellationToken ct = default) => _writer.FlushAsync(ct);
#endif

    private void WriteEscaped(string? s)
    {
        s ??= "";
        var needsQuotes = false;
        for (var i = 0; i < s.Length; i++) {
            var c = s[i];
            if (c == _delimiter || c == _quote || c == _escape || c is '\r' or '\n') {
                needsQuotes = true;
                break;
            }
        }

        if (!needsQuotes) {
            _writer.Write(s);
            return;
        }

        _writer.Write(_quote);
        if (_escape == _quote) {
            for (var i = 0; i < s.Length; i++) {
                var c = s[i];
                if (c == _quote)
                    _writer.Write(_quote);

                _writer.Write(c);
            }
        }
        else {
            for (var i = 0; i < s.Length; i++) {
                var c = s[i];
                if (c == _quote || c == _escape)
                    _writer.Write(_escape);

                _writer.Write(c);
            }
        }

        _writer.Write(_quote);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }

#if !NETSTANDARD2_0
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
#endif
}
