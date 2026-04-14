namespace Lyo.Images.Sprite.Models;

public readonly record struct SpriteFrameRect(int Index, int Row, int Column, int X, int Y, int Width, int Height, bool IsIncluded);