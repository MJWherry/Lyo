using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Lyo.Desktop;

/// <summary>Plays pointer and key events with <c>SendInput</c>.</summary>
[SupportedOSPlatform("windows")]
internal static partial class WindowsDesktopInput
{
    private const uint MouseMove = 0x0001;
    private const uint MouseAbsolute = 0x8000;
    private const uint KeyUp = 0x0002;
    private const uint KeyUnicode = 0x0004;

    internal static void Play(IReadOnlyList<DesktopInputStep> steps)
    {
        foreach (var step in steps)
            Send(step);
    }

    private static void Send(DesktopInputStep step)
    {
        var input = new Input { Type = step.Kind == DesktopInputKind.Key ? 1u : 0u };
        if (step.Kind == DesktopInputKind.Move)
            input.Data.Mouse = Move(step.X, step.Y, MouseMove | MouseAbsolute);
        else if (step.Kind == DesktopInputKind.Button)
            input.Data.Mouse = Move(step.X, step.Y, ButtonFlag(step.Button, step.Down) | MouseMove | MouseAbsolute);
        else if (step.Character)
            input.Data.Key = new() { Scan = step.Key.Length == 0 ? (ushort)0 : step.Key[0], Flags = KeyUnicode | (step.Down ? 0u : KeyUp) };
        else
            input.Data.Key = new() { VirtualKey = (ushort)WindowsVirtualKey.Code(step.Key), Flags = step.Down ? 0u : KeyUp };

        if (SendInput(1, ref input, Marshal.SizeOf<Input>()) != 1)
            throw new InvalidOperationException("Could not send the input event.");
    }

    private static MouseInput Move(int x, int y, uint flags)
    {
        var left = GetSystemMetrics(76);
        var top = GetSystemMetrics(77);
        var width = Math.Max(GetSystemMetrics(78) - 1, 1);
        var height = Math.Max(GetSystemMetrics(79) - 1, 1);
        return new() { X = (x - left) * 65535 / width, Y = (y - top) * 65535 / height, Flags = flags };
    }

    private static uint ButtonFlag(DesktopMouseButton button, bool down)
        => button switch {
            DesktopMouseButton.Right => down ? 0x0008u : 0x0010u,
            DesktopMouseButton.Middle => down ? 0x0020u : 0x0040u,
            var _ => down ? 0x0002u : 0x0004u
        };

    [LibraryImport("user32.dll")]
    private static partial uint SendInput(uint count, ref Input input, int size);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyInput Key;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint Data;
        public uint Flags;
        public uint Time;
        public nint Extra;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyInput
    {
        public ushort VirtualKey;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public nint Extra;
    }
}