using System.Buffers;
using System.Text;
using Lyo.Csv.Models;

namespace Lyo.Csv;

/// <summary>
/// Pull-based CSV field reader over a <see cref="TextReader" />. Parses from a rented char buffer; <see cref="ReadRow" /> / <see cref="ReadRowAsync" /> return a reused list
/// valid only until the next read.
/// </summary>
internal sealed class CsvTextReader
    : IDisposable
#if !NETSTANDARD2_0
        , IAsyncDisposable
#endif
{
    private const int DefaultBufferSize = 64 * 1024;
    private readonly char[] _buffer;
    private readonly char _delimiter;
    private readonly StringBuilder _field = new(128);
    private readonly CsvOptions _options;

    private readonly TextReader _reader;
    private readonly List<string> _row = [];
    private int _bufLen;
    private int _bufPos;
    private bool _disposed;
    private bool _eof;
    private int? _expectedColumnCount;

    /// <summary>Best-effort raw record for error reporting (joined fields; not built on the hot path).</summary>
    public string? RawRecord => _row.Count == 0 ? null : string.Join(_options.Delimiter, _row);

    /// <summary>1-based physical row counter (increments on each successful data row).</summary>
    public int RowNumber { get; private set; }

    public CsvTextReader(TextReader reader, CsvOptions options, int bufferSize = DefaultBufferSize)
    {
        _reader = reader;
        _options = options;
        _delimiter = options.Delimiter[0];
        _buffer = ArrayPool<char>.Shared.Rent(Math.Max(bufferSize, 1024));
    }

#if !NETSTANDARD2_0
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
#endif

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ArrayPool<char>.Shared.Return(_buffer);
        _reader.Dispose();
    }

    /// <summary>Reads the next data row. Returns the internal field list (reused on the next call), or null at EOF. Copy the list if you need to retain it across reads.</summary>
    public IReadOnlyList<string>? ReadRow()
    {
        while (true) {
            _row.Clear();
            _field.Clear();
            if (!ReadRowSync())
                return null;

            if (_row.Count == 1 && _row[0].Length == 0 && _options.IgnoreBlankLines)
                continue;

            EnforceColumnCount();
            RowNumber++;
            return _row;
        }
    }

#if !NETSTANDARD2_0
    /// <summary>Asynchronously reads the next data row. Returns the internal field list (reused on the next call), or null at EOF.</summary>
    public async Task<IReadOnlyList<string>?> ReadRowAsync(CancellationToken ct = default)
    {
        while (true) {
            ct.ThrowIfCancellationRequested();
            _row.Clear();
            _field.Clear();
            if (!await ReadRowAsyncCore(ct).ConfigureAwait(false))
                return null;

            if (_row.Count == 1 && _row[0].Length == 0 && _options.IgnoreBlankLines)
                continue;

            EnforceColumnCount();
            RowNumber++;
            return _row;
        }
    }
#endif

    private void EnforceColumnCount()
    {
        if (!_options.DetectColumnCountChanges)
            return;

        if (_expectedColumnCount is null) {
            _expectedColumnCount = _row.Count;
            return;
        }

        if (_row.Count != _expectedColumnCount.Value) {
            throw new CsvBadDataException($"Inconsistent number of columns at row {RowNumber + 1}: expected {_expectedColumnCount}, found {_row.Count}.");
        }
    }

    private bool ReadRowSync()
    {
        var inQuotes = false;
        var fieldStarted = false;
        var any = false;
        var atLineStart = true;
        while (true) {
            var ch = ReadChar();
            if (ch < 0) {
                if (!any && !fieldStarted && _field.Length == 0 && _row.Count == 0)
                    return false;

                FinishField();
                return true;
            }

            any = true;
            if (atLineStart && !inQuotes && _options.AllowComments) {
                if (ch is ' ' or '\t') {
                    // keep scanning
                }
                else if (ch == _options.Comment) {
                    SkipToEol();
                    _row.Clear();
                    _field.Clear();
                    any = false;
                    fieldStarted = false;
                    atLineStart = true;
                    continue;
                }
                else
                    atLineStart = false;
            }
            else if (atLineStart && ch is not ('\r' or '\n'))
                atLineStart = false;

            if (inQuotes) {
                if (ch == _options.Escape && _options.Escape != _options.Quote) {
                    var next = PeekChar();
                    if (next >= 0) {
                        ReadChar();
                        _field.Append((char)next);
                        fieldStarted = true;
                        continue;
                    }
                }

                if (ch == _options.Quote) {
                    if (_options.Escape == _options.Quote) {
                        var next = PeekChar();
                        if (next == _options.Quote) {
                            ReadChar();
                            _field.Append(_options.Quote);
                            fieldStarted = true;
                            continue;
                        }
                    }

                    inQuotes = false;
                    fieldStarted = true;
                    continue;
                }

                _field.Append((char)ch);
                fieldStarted = true;
                continue;
            }

            if (ch == _options.Quote && !fieldStarted) {
                inQuotes = true;
                fieldStarted = true;
                continue;
            }

            if (ch == _delimiter) {
                FinishField();
                fieldStarted = false;
                continue;
            }

            if (ch is '\r' or '\n') {
                if (ch == '\r' && PeekChar() == '\n')
                    ReadChar();

                FinishField();
                return true;
            }

            _field.Append((char)ch);
            fieldStarted = true;
        }
    }

#if !NETSTANDARD2_0
    private async Task<bool> ReadRowAsyncCore(CancellationToken ct)
    {
        // Prefer sync parse when the buffer already has data; refill with one ReadAsync when empty.
        var inQuotes = false;
        var fieldStarted = false;
        var any = false;
        var atLineStart = true;
        while (true) {
            if (_bufPos >= _bufLen && !_eof) {
                _bufLen = await _reader.ReadAsync(_buffer.AsMemory(0, _buffer.Length), ct).ConfigureAwait(false);
                _bufPos = 0;
                if (_bufLen == 0)
                    _eof = true;
            }

            var ch = ReadChar();
            if (ch < 0) {
                if (!any && !fieldStarted && _field.Length == 0 && _row.Count == 0)
                    return false;

                FinishField();
                return true;
            }

            any = true;
            if (atLineStart && !inQuotes && _options.AllowComments) {
                if (ch is ' ' or '\t') {
                    // keep scanning
                }
                else if (ch == _options.Comment) {
                    while (true) {
                        if (_bufPos >= _bufLen && !_eof) {
                            _bufLen = await _reader.ReadAsync(_buffer.AsMemory(0, _buffer.Length), ct).ConfigureAwait(false);
                            _bufPos = 0;
                            if (_bufLen == 0)
                                _eof = true;
                        }

                        var c = ReadChar();
                        if (c < 0 || c == '\n')
                            break;

                        if (c == '\r') {
                            if (PeekChar() == '\n')
                                ReadChar();

                            break;
                        }
                    }

                    _row.Clear();
                    _field.Clear();
                    any = false;
                    fieldStarted = false;
                    atLineStart = true;
                    continue;
                }
                else
                    atLineStart = false;
            }
            else if (atLineStart && ch is not ('\r' or '\n'))
                atLineStart = false;

            if (inQuotes) {
                if (ch == _options.Escape && _options.Escape != _options.Quote) {
                    if (_bufPos >= _bufLen && !_eof) {
                        _bufLen = await _reader.ReadAsync(_buffer.AsMemory(0, _buffer.Length), ct).ConfigureAwait(false);
                        _bufPos = 0;
                        if (_bufLen == 0)
                            _eof = true;
                    }

                    var next = PeekChar();
                    if (next >= 0) {
                        ReadChar();
                        _field.Append((char)next);
                        fieldStarted = true;
                        continue;
                    }
                }

                if (ch == _options.Quote) {
                    if (_options.Escape == _options.Quote) {
                        if (_bufPos >= _bufLen && !_eof) {
                            _bufLen = await _reader.ReadAsync(_buffer.AsMemory(0, _buffer.Length), ct).ConfigureAwait(false);
                            _bufPos = 0;
                            if (_bufLen == 0)
                                _eof = true;
                        }

                        var next = PeekChar();
                        if (next == _options.Quote) {
                            ReadChar();
                            _field.Append(_options.Quote);
                            fieldStarted = true;
                            continue;
                        }
                    }

                    inQuotes = false;
                    fieldStarted = true;
                    continue;
                }

                _field.Append((char)ch);
                fieldStarted = true;
                continue;
            }

            if (ch == _options.Quote && !fieldStarted) {
                inQuotes = true;
                fieldStarted = true;
                continue;
            }

            if (ch == _delimiter) {
                FinishField();
                fieldStarted = false;
                continue;
            }

            if (ch is '\r' or '\n') {
                if (ch == '\r') {
                    if (_bufPos >= _bufLen && !_eof) {
                        _bufLen = await _reader.ReadAsync(_buffer.AsMemory(0, _buffer.Length), ct).ConfigureAwait(false);
                        _bufPos = 0;
                        if (_bufLen == 0)
                            _eof = true;
                    }

                    if (PeekChar() == '\n')
                        ReadChar();
                }

                FinishField();
                return true;
            }

            _field.Append((char)ch);
            fieldStarted = true;
        }
    }
#endif

    private void FinishField()
    {
        var value = _field.ToString();
        if (_options.TrimFields)
            value = value.Trim();

        _row.Add(value);
        _field.Clear();
    }

    private void SkipToEol()
    {
        while (true) {
            var ch = ReadChar();
            if (ch < 0)
                return;

            if (ch == '\n')
                return;

            if (ch == '\r') {
                if (PeekChar() == '\n')
                    ReadChar();

                return;
            }
        }
    }

    private int ReadChar()
    {
        if (_bufPos >= _bufLen) {
            if (_eof)
                return -1;

            _bufLen = _reader.Read(_buffer, 0, _buffer.Length);
            _bufPos = 0;
            if (_bufLen == 0) {
                _eof = true;
                return -1;
            }
        }

        return _buffer[_bufPos++];
    }

    private int PeekChar()
    {
        if (_bufPos >= _bufLen) {
            if (_eof)
                return -1;

            _bufLen = _reader.Read(_buffer, 0, _buffer.Length);
            _bufPos = 0;
            if (_bufLen == 0) {
                _eof = true;
                return -1;
            }
        }

        return _buffer[_bufPos];
    }
}