using ECommons.DalamudServices;

namespace RotationSolver.UI
{
    public static class FontManager
    {
        public static unsafe ImFontPtr GetFont(float size)
        {
            // Get the recommended font style based on the specified size
            Dalamud.Interface.GameFonts.GameFontStyle style = new(
                Dalamud.Interface.GameFonts.GameFontStyle.GetRecommendedFamilyAndSize(
                    Dalamud.Interface.GameFonts.GameFontFamily.Axis, size));

            // Create a new game font handle
            Dalamud.Interface.ManagedFontAtlas.IFontHandle handle = Svc.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(style);

            try
            {
                // Lock the handle to get the font
                using Dalamud.Interface.ManagedFontAtlas.ILockedImFont lockedHandle = handle.Lock();
                ImFontPtr font = lockedHandle.ImFont;

                // Check if the font pointer is valid
                if (!font.IsLoaded())
                {
                    return ImGui.GetFont();
                }

                // Scale the font to the desired size
                font.Scale = size / font.FontSize;
                return font;
            }
            catch (Exception)
            {
                return ImGui.GetFont();
            }
        }
    }
}