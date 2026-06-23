using Microsoft.Maui.Controls;

namespace CampLedger.Resources.Styles;

public class Colors : ResourceDictionary
{
    public Colors()
    {
        AddPalette("regal", "#4B1D6B", "#C89B3C", "#F4E9D8", "#FFF9F0", "#E7D3B0", "#C8B99A", "#1E1E1E", "#4A4038", "#7A7067", "#1F5E3B", "#9A5A00", "#8E1B2D", "#006D77", "#C7B7E8");
        AddPalette("wine", "#6B1E36", "#B85C00", "#F6E7D2", "#FFF7EC", "#E9D8BF", "#B99A77", "#241A17", "#5A463D", "#8A776A", "#286140", "#B85C00", "#A31621", "#2C6F73", "#D9B88F");
        AddPalette("forest", "#184D37", "#C7B7E8", "#D9E8DD", "#F4F7F5", "#E8F0EA", "#A9BBAE", "#202124", "#4B4F46", "#6B6F66", "#2E7D32", "#B7791F", "#8B1E3F", "#007C89", "#C7B7E8");
    }

    private void AddPalette(string prefix, string brand, string accent, string background, string surface, string surfaceAlt, string border, string textPrimary, string textSecondary, string textMuted, string success, string warning, string error, string info, string decorative)
    {
        this[$"{prefix}BrandColor"] = Color.FromArgb(brand);
        this[$"{prefix}AccentColor"] = Color.FromArgb(accent);
        this[$"{prefix}BackgroundColor"] = Color.FromArgb(background);
        this[$"{prefix}SurfaceColor"] = Color.FromArgb(surface);
        this[$"{prefix}SurfaceAltColor"] = Color.FromArgb(surfaceAlt);
        this[$"{prefix}BorderColor"] = Color.FromArgb(border);
        this[$"{prefix}TextPrimaryColor"] = Color.FromArgb(textPrimary);
        this[$"{prefix}TextSecondaryColor"] = Color.FromArgb(textSecondary);
        this[$"{prefix}TextMutedColor"] = Color.FromArgb(textMuted);
        this[$"{prefix}SuccessColor"] = Color.FromArgb(success);
        this[$"{prefix}WarningColor"] = Color.FromArgb(warning);
        this[$"{prefix}ErrorColor"] = Color.FromArgb(error);
        this[$"{prefix}InfoColor"] = Color.FromArgb(info);
        this[$"{prefix}DecorativeColor"] = Color.FromArgb(decorative);
    }
}
