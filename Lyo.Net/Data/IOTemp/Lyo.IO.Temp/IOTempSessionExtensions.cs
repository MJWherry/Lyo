using Lyo.Exceptions;
using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp;

/// <summary>Assertion and verification helpers for <see cref="IIOTempSession" />.</summary>
public static class IOTempSessionExtensions
{
    extension(IIOTempSession session)
    {
        /// <summary>Throws <see cref="InvalidOperationException" /> if any file in <see cref="IIOTempSession.Files" /> no longer exists on disk (e.g. deleted by external code).</summary>
        public void AssertFilesExist()
        {
            foreach (var file in session.Files)
                OperationHelpers.ThrowIf(!File.Exists(file), $"Expected tracked temp file does not exist on disk: {file}");
        }

        /// <summary>
        /// Throws <see cref="InvalidOperationException" /> if the session's total byte usage differs from <paramref name="expectedBytes" /> by more than
        /// <paramref name="toleranceBytes" />.
        /// </summary>
        public void AssertTotalSize(long expectedBytes, long toleranceBytes = 0)
        {
            var actual = session.GetTotalBytesUsed();
            var diff = Math.Abs(actual - expectedBytes);
            OperationHelpers.ThrowIf(diff > toleranceBytes, $"Session total size mismatch: expected {expectedBytes:N0} bytes (±{toleranceBytes:N0}) but was {actual:N0} bytes.");
        }
    }
}