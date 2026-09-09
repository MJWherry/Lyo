using System.IO.Compression;

namespace Lyo.QRCode.Encoding.Iso;

/// <summary>QR code module matrix and raw-data helpers.</summary>
internal sealed class QRIsoMatrix : IDisposable
{
    /// <summary>Compression used for raw data.</summary>
    public enum Compression
    {
        /// <summary>Uncompressed payload.</summary>
        Uncompressed,

        /// <summary>Deflate-compressed payload.</summary>
        Deflate,

        /// <summary>GZip-compressed payload.</summary>
        GZip
    }

    /// <summary>Module matrix of the QR code.</summary>
    public List<BitArray> ModuleMatrix { get; set; }

    /// <summary>QR version.</summary>
    public int Version { get; private set; }

    /// <summary>Builds a <see cref="QRIsoMatrix" /> for the given version.</summary>
    /// <param name="version">QR version.</param>
    public QRIsoMatrix(int version)
    {
        Version = version;
        var size = ModulesPerSideFromVersion(version);
        ModuleMatrix = new(size);
        for (var i = 0; i < size; i++)
            ModuleMatrix.Add(new(size));
    }

    /// <summary>Builds a <see cref="QRIsoMatrix" /> for the given version, optionally with padding.</summary>
    /// <param name="version">QR version.</param>
    /// <param name="addPadding">If true, add padding around the QR code.</param>
    public QRIsoMatrix(int version, bool addPadding)
    {
        Version = version;
        var size = ModulesPerSideFromVersion(version) + (addPadding ? 8 : 0);
        ModuleMatrix = new(size);
        for (var i = 0; i < size; i++)
            ModuleMatrix.Add(new(size));
    }

    /// <summary>Builds a <see cref="QRIsoMatrix" /> from a raw-data file and compression mode.</summary>
    /// <param name="pathToRawData">Path to the raw data file.</param>
    /// <param name="compressMode">Compression used for the raw data.</param>
    public QRIsoMatrix(string pathToRawData, Compression compressMode)
        : this(File.ReadAllBytes(pathToRawData), compressMode) { }

    /// <summary>Builds a <see cref="QRIsoMatrix" /> from raw bytes and compression mode.</summary>
    /// <param name="rawData">Raw QR bytes.</param>
    /// <param name="compressMode">Compression used for the raw data.</param>
    public QRIsoMatrix(byte[] rawData, Compression compressMode)
    {
        // Inflate compressed payloads
        if (compressMode == Compression.Deflate) {
            using var input = new MemoryStream(rawData);
            using var output = new MemoryStream();
            using (var dstream = new DeflateStream(input, CompressionMode.Decompress))
                dstream.CopyTo(output);

            rawData = output.ToArray();
        }
        else if (compressMode == Compression.GZip) {
            using var input = new MemoryStream(rawData);
            using var output = new MemoryStream();
            using (var dstream = new GZipStream(input, CompressionMode.Decompress))
                dstream.CopyTo(output);

            rawData = output.ToArray();
        }

        if (rawData.Length < 5)
            throw new InvalidDataException("Invalid raw data file. File too short.");

        if (rawData[0] != 0x51 || rawData[1] != 0x52 || rawData[2] != 0x52)
            throw new InvalidDataException("Invalid raw data file. Filetype doesn't match \"QRR\".");

        // Version from side length (includes 8-module quiet zone)
        var sideLen = (int)rawData[4];
        if (sideLen < 29) // Micro QR: sideLen = 19 + 2*(m-1), m in [1..4] → versions -1..-4
        {
            if (((sideLen - 19) & 1) != 0)
                throw new InvalidDataException("Invalid raw data file. Side length not valid for Micro QR.");

            var m = (sideLen - 19) / 2 + 1;
            Version = -m;
        }
        else // Standard QR: sideLen = 29 + 4*(v-1), v in [1..40]
        {
            if ((sideLen - 29) % 4 != 0)
                throw new InvalidDataException("Invalid raw data file. Side length not valid for QR.");

            Version = (sideLen - 29) / 4 + 1;
        }

        // Unpack bits from payload bytes
        var modules = new Queue<bool>(8 * (rawData.Length - 5));
        for (var j = 5; j < rawData.Length; j++) {
            var b = rawData[j];
            for (var i = 7; i >= 0; i--)
                modules.Enqueue((b & (1 << i)) != 0);
        }

        // Fill the module matrix
        ModuleMatrix = new(sideLen);
        for (var y = 0; y < sideLen; y++) {
            ModuleMatrix.Add(new(sideLen));
            for (var x = 0; x < sideLen; x++)
                ModuleMatrix[y][x] = modules.Dequeue();
        }
    }

    /// <summary>Releases resources held by this <see cref="QRIsoMatrix" />.</summary>
    public void Dispose()
    {
        ModuleMatrix = null!;
        Version = 0;
    }

    /// <summary>Raw QR bytes using the given compression mode.</summary>
    /// <param name="compressMode">Compression used for the raw data.</param>
    /// <returns>Raw QR bytes.</returns>
    public byte[] GetRawData(Compression compressMode)
    {
        using var output = new MemoryStream();
        Stream targetStream = output;
        DeflateStream? deflateStream = null;
        GZipStream? gzipStream = null;

        // Wrap with a compressor when requested
        if (compressMode == Compression.Deflate) {
            deflateStream = new(output, CompressionMode.Compress, true);
            targetStream = deflateStream;
        }
        else if (compressMode == Compression.GZip) {
            gzipStream = new(output, CompressionMode.Compress, true);
            targetStream = gzipStream;
        }

        try {
            // Header signature ("QRR")
#if HAS_SPAN
            targetStream.Write([0x51, 0x52, 0x52, 0x00]);
#else
            targetStream.Write(new byte[] { 0x51, 0x52, 0x52, 0x00 }, 0, 4);
#endif

            // Header row size
            targetStream.WriteByte((byte)ModuleMatrix.Count);

            // Queue module bits
            var capacity = ModuleMatrix.Count * ModuleMatrix.Count + 7; // modules plus max byte-alignment pad
            var dataQueue = new Queue<int>(capacity);
            foreach (var row in ModuleMatrix) {
                for (var i = 0; i < row.Length; i++)
                    dataQueue.Enqueue(row[i] ? 1 : 0);
            }

            var mod = (int)((uint)ModuleMatrix.Count * (uint)ModuleMatrix.Count % 8);
            for (var i = 0; i < 8 - mod; i++)
                dataQueue.Enqueue(0);

            // Pack the queue into bytes
            while (dataQueue.Count > 0) {
                byte b = 0;
                for (var i = 7; i >= 0; i--)
                    b += (byte)(dataQueue.Dequeue() << i);

                targetStream.WriteByte(b);
            }
        }
        finally {
            // Dispose compressors so remaining bytes flush
            deflateStream?.Dispose();
            gzipStream?.Dispose();
        }

        return output.ToArray();
    }

    /// <summary>Writes raw QR bytes to a file using the given compression mode.</summary>
    /// <param name="filePath">Destination path for the raw data.</param>
    /// <param name="compressMode">Compression used for the raw data.</param>
    public void SaveRawData(string filePath, Compression compressMode) => File.WriteAllBytes(filePath, GetRawData(compressMode));

    /// <summary>Modules per side for the given version.</summary>
    /// <param name="version">QR version (1 to 40, or -1 to -4 for Micro QR).</param>
    /// <returns>Modules per side.</returns>
    private static int ModulesPerSideFromVersion(int version) => version > 0 ? 21 + (version - 1) * 4 : 11 + (-version - 1) * 2;
}