using MudBlazor;

namespace Lyo.Gateway.Components.Layout;

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

            // Text colors
            TextPrimary = "#212121",
            TextSecondary = "#757575",
            TextDisabled = "#BDBDBD",

            // Action colors
            ActionDefault = "#757575",
            ActionDisabled = "#BDBDBD",
            ActionDisabledBackground = "#E0E0E0",

            // Background colors
            Background = "#FFFFFF",
            BackgroundGray = "#F5F5F5",
            Surface = "#FFFFFF",

            // Divider
            Divider = "#E0E0E0",
            DividerLight = "#F0F0F0",

            // Table colors
            TableLines = "#E0E0E0",
            TableStriped = "#FAFAFA",
            TableHover = "#F5F5F5",

            // Overlay
            OverlayDark = "rgba(33,33,33,0.4)",
            OverlayLight = "rgba(255,255,255,0.4)",

            // AppBar and Drawer
            AppbarBackground = "#1B5E20",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#212121",
            DrawerIcon = "#757575"
        },
        PaletteDark = new PaletteDark {
            // Brighter accent in dark mode for better contrast in query builder controls.
            Primary = "#66BB6A",
            Secondary = "#9E9E9E",
            Tertiary = "#424242",
            Info = "#2196F3",
            Success = "#4CAF50",
            Warning = "#FF9800",
            Error = "#F44336",
            Dark = "#2A2A2A", // Lighter than pure black
            // Text colors for dark mode
            TextPrimary = "#FFFFFF",
            TextSecondary = "#BDBDBD",
            TextDisabled = "#757575",
            // Action colors for dark mode
            ActionDefault = "#BDBDBD",
            ActionDisabled = "#757575",
            ActionDisabledBackground = "#424242",
            // Background colors for dark mode - Updated to grayish tones
            Background = "#2E2E2E", // Main background - medium gray
            BackgroundGray = "#3A3A3A", // Slightly lighter gray for contrast
            Surface = "#3A3A3A", // Card/surface background - matches BackgroundGray
            // Divider for dark mode
            Divider = "#505050", // Lighter dividers for better visibility on gray
            DividerLight = "#454545", // Subtle divider color
            // Table colors for dark mode - Updated for gray theme
            TableLines = "#505050", // Table borders
            TableStriped = "#333333", // Alternating row color
            TableHover = "#424242", // Hover effect color
            // Overlay for dark mode
            OverlayDark = "rgba(0,0,0,0.6)",
            OverlayLight = "rgba(255,255,255,0.1)",
            // AppBar and Drawer for dark mode
            AppbarBackground = "#2E7D32",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#3A3A3A", // Updated to match Surface color
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