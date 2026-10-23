using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Lyo.Desktop.Components;

/// <summary>Small owned dialogs: a text prompt, a confirm, and a read-only text box. Domain dialogs reuse the same chrome.</summary>
public static class PromptDialog
{
    /// <summary>Asks for one line. Returns null when the window closes without OK.</summary>
    public static async Task<string?> AskAsync(Window owner, string title, string label, string initial = "")
    {
        var box = new TextBox { Text = initial, PlaceholderText = label };
        var ok = false;
        var window = new Window {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = Stack(
                new TextBlock { Text = label }, box, Buttons(
                    () => {
                        ok = true;
                        return true;
                    }, "OK"))
        };

        ApplyChrome(window);
        await window.ShowDialog(owner);
        return ok ? box.Text : null;
    }

    /// <summary>Asks the user to confirm <paramref name="message" />. The button reads <paramref name="accept" />.</summary>
    public static async Task<bool> ConfirmAsync(Window owner, string title, string message, string accept = "OK")
    {
        var ok = false;
        var window = new Window {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = Stack(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap }, Buttons(() => ok = true, accept))
        };

        ApplyChrome(window);
        await window.ShowDialog(owner);
        return ok;
    }

    /// <summary>Shows <paramref name="text" /> in a read-only box.</summary>
    public static async Task ShowTextAsync(Window owner, string title, string text)
    {
        var box = new TextBox {
            Text = text,
            AcceptsReturn = true,
            IsReadOnly = true,
            Height = 160
        };

        var window = new Window {
            Title = title,
            Width = 520,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = Stack(box, Buttons(() => true, "Close"))
        };

        ApplyChrome(window);
        await window.ShowDialog(owner);
    }

    /// <summary>Dark fill and a 1px border. Call this after the window content is set.</summary>
    public static void ApplyChrome(Window window)
    {
        var background = new SolidColorBrush(Color.Parse("#16181D"));
        window.Background = background;
        window.TransparencyLevelHint = [WindowTransparencyLevel.None];
        if (window.Content is not Control content)
            return;

        window.Content = null;
        window.Content = new Border {
            BorderThickness = new(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#5C677A")),
            Background = background,
            Child = content
        };
    }

    /// <summary>A caption above <paramref name="child" />.</summary>
    public static Control Labeled(string label, Control child)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = label });
        panel.Children.Add(child);
        return panel;
    }

    /// <summary>A vertical stack with the dialog margin.</summary>
    public static StackPanel Stack(params Control[] children)
    {
        var panel = new StackPanel { Margin = new(12), Spacing = 8 };
        foreach (var child in children)
            panel.Children.Add(child);

        return panel;
    }

    /// <summary>A right-aligned button. It closes the dialog when <paramref name="accept" /> returns true.</summary>
    public static Button Buttons(Func<bool> accept, string label)
    {
        var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Right };
        button.Click += (_, _) => {
            if (accept())
                Close(button);
        };

        return button;
    }

    private static void Close(Control control)
    {
        var window = TopLevel.GetTopLevel(control) as Window;
        window?.Close();
    }
}