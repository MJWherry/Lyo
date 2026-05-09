namespace Lyo.Images.Sprite.Models;

/// <summary>One cell in a spritesheet grid: index, grid position, pixel rectangle, and inclusion flag.</summary>
/// <param name="Index">Zero-based frame order.</param>
/// <param name="Row">Row in the grid.</param>
/// <param name="Column">Column in the grid.</param>
/// <param name="X">Left pixel coordinate in the source image.</param>
/// <param name="Y">Top pixel coordinate in the source image.</param>
/// <param name="Width">Crop width in pixels.</param>
/// <param name="Height">Crop height in pixels.</param>
/// <param name="IsIncluded">False when the cell is padding rather than a real sample.</param>
public readonly record struct SpriteFrameRect(int Index, int Row, int Column, int X, int Y, int Width, int Height, bool IsIncluded);