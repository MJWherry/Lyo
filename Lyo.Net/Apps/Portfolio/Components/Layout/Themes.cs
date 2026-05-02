using MudBlazor;

namespace Lyo.Portfolio.Components.Layout;

public static class Themes
{
    public static readonly MudTheme Default = new() {
        PaletteLight = new PaletteLight {
            Primary = "#1B5E20",
            Secondary = "#6B6B6B",
            Tertiary = "#F5F5F5",
            Info = "#2196F3",
            Success = "#4CAF50",
            Warning = "#FF9800",
            Error = "#F44336",
            Dark = "#424242",

            TextPrimary = "#212121",
            TextSecondary = "#757575",
            TextDisabled = "#BDBDBD",

            ActionDefault = "#757575",
            ActionDisabled = "#BDBDBD",
            ActionDisabledBackground = "#E0E0E0",

            Background = "#FFFFFF",
            BackgroundGray = "#F5F5F5",
            Surface = "#FFFFFF",

            Divider = "#E0E0E0",
            DividerLight = "#F0F0F0",

            TableLines = "#E0E0E0",
            TableStriped = "#FAFAFA",
            TableHover = "#F5F5F5",

            OverlayDark = "rgba(33,33,33,0.4)",
            OverlayLight = "rgba(255,255,255,0.4)",

            AppbarBackground = "#1B5E20",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#212121",
            DrawerIcon = "#757575"
        },
        PaletteDark = new PaletteDark {
            Primary = "#66BB6A",
            Secondary = "#9E9E9E",
            Tertiary = "#424242",
            Info = "#2196F3",
            Success = "#4CAF50",
            Warning = "#FF9800",
            Error = "#F44336",
            Dark = "#2A2A2A",

            TextPrimary = "#FFFFFF",
            TextSecondary = "#BDBDBD",
            TextDisabled = "#757575",

            ActionDefault = "#BDBDBD",
            ActionDisabled = "#757575",
            ActionDisabledBackground = "#424242",

            Background = "#2E2E2E",
            BackgroundGray = "#3A3A3A",
            Surface = "#3A3A3A",

            Divider = "#505050",
            DividerLight = "#454545",

            TableLines = "#505050",
            TableStriped = "#333333",
            TableHover = "#424242",

            OverlayDark = "rgba(0,0,0,0.6)",
            OverlayLight = "rgba(255,255,255,0.1)",

            AppbarBackground = "#2E7D32",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#3A3A3A",
            DrawerText = "#FFFFFF",
            DrawerIcon = "#BDBDBD"
        },
        LayoutProperties = new() {
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px",
            AppbarHeight = "64px",
            DefaultBorderRadius = "4px"
        },
        ZIndex = new() {
            Drawer = 1200,
            AppBar = 1100,
            Dialog = 1300,
            Popover = 1400,
            Snackbar = 1500,
            Tooltip = 1600
        }
    };
}
