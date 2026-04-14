using Lyo.Images.Sprite.Models;

namespace Lyo.Images.Sprite;

public static class SpriteSheetCalculator
{
    private const int MaxRows = 256;
    private const int MaxCols = 4096;

    /// <summary>Split <paramref name="total" /> pixels across <paramref name="count" /> cells; first <c>total % count</c> cells get one extra pixel so the grid covers the full span.</summary>
    public static int[] DistributeSizes(int total, int count)
    {
        if (count <= 0)
            return [];

        total = Math.Max(0, total);
        var baseSize = total / count;
        var rem = total % count;
        var ar = new int[count];
        for (var i = 0; i < count; i++)
            ar[i] = baseSize + (i < rem ? 1 : 0);

        return ar;
    }

    public static SpriteSheetCalculation Calculate(SpriteSheetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var sourceWidth = Math.Max(0, options.SourceWidth);
        var sourceHeight = Math.Max(0, options.SourceHeight);
        var leftTrim = Math.Clamp(options.LeftTrim, 0, sourceWidth);
        var topTrim = Math.Clamp(options.TopTrim, 0, sourceHeight);
        var rightTrim = Math.Clamp(options.RightTrim, 0, Math.Max(0, sourceWidth - leftTrim));
        var bottomTrim = Math.Clamp(options.BottomTrim, 0, Math.Max(0, sourceHeight - topTrim));
        var trimmedWidth = Math.Max(0, sourceWidth - leftTrim - rightTrim);
        var trimmedHeight = Math.Max(0, sourceHeight - topTrim - bottomTrim);
        var offsetX = Math.Clamp(options.OffsetX, 0, trimmedWidth);
        var offsetY = Math.Clamp(options.OffsetY, 0, trimmedHeight);
        var usableHeight = Math.Max(0, trimmedHeight - offsetY);
        var usableWidth = Math.Max(0, trimmedWidth - offsetX);
        var rowCount = Math.Clamp(options.RowCount, 1, Math.Min(MaxRows, Math.Max(1, usableHeight)));
        var framesPerRow = Math.Clamp(options.FramesPerRow, 1, Math.Min(MaxCols, Math.Max(1, usableWidth)));
        var colWidths = DistributeSizes(usableWidth, framesPerRow);
        var rowHeights = DistributeSizes(usableHeight, rowCount);
        var colPrefix = new int[framesPerRow + 1];
        for (var i = 0; i < framesPerRow; i++)
            colPrefix[i + 1] = colPrefix[i] + colWidths[i];

        var rowPrefix = new int[rowCount + 1];
        for (var i = 0; i < rowCount; i++)
            rowPrefix[i + 1] = rowPrefix[i] + rowHeights[i];

        var columnCount = framesPerRow;
        var maxFrameCount = columnCount * rowCount;
        var requestedFrameCount = options.RequestedFrameCount.GetValueOrDefault(maxFrameCount);
        var actualFrameCount = Math.Clamp(requestedFrameCount, 0, maxFrameCount);
        var excludedFrames = options.ExcludedFrames?.Where(index => index >= 0).ToHashSet() ?? [];
        var frames = new List<SpriteFrameRect>(actualFrameCount);
        for (var index = 0; index < actualFrameCount; index++) {
            if (columnCount <= 0)
                break;

            var row = index / columnCount;
            var column = index % columnCount;
            var x = leftTrim + offsetX + colPrefix[column];
            var y = topTrim + offsetY + rowPrefix[row];
            var fw = colWidths[column];
            var fh = rowHeights[row];
            var isIncluded = !excludedFrames.Contains(index);
            frames.Add(new(index, row, column, x, y, fw, fh, isIncluded));
        }

        var maxFw = colWidths.Length > 0 ? colWidths.Max() : 1;
        var maxFh = rowHeights.Length > 0 ? rowHeights.Max() : 1;
        return new() {
            Frames = frames,
            IncludedFrames = frames.Where(frame => frame.IsIncluded).ToList(),
            SourceWidth = sourceWidth,
            SourceHeight = sourceHeight,
            FrameWidth = maxFw,
            FrameHeight = maxFh,
            RowCount = rowCount,
            ColumnCount = columnCount,
            FramesPerSecond = Math.Clamp(options.FramesPerSecond, 1, 60),
            OffsetX = offsetX,
            OffsetY = offsetY,
            LeftTrim = leftTrim,
            TopTrim = topTrim,
            RightTrim = rightTrim,
            BottomTrim = bottomTrim,
            MaxFrameCount = maxFrameCount
        };
    }
}