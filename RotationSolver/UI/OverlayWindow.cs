using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.Logging;
using RotationSolver.UI.HighlightTeachingMode;

namespace RotationSolver.UI;

/// <summary>
/// The Overlay Window
/// </summary>
internal class OverlayWindow : Window
{
    private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoBackground
    | ImGuiWindowFlags.NoBringToFrontOnFocus
    | ImGuiWindowFlags.NoDecoration
    | ImGuiWindowFlags.NoDocking
    | ImGuiWindowFlags.NoFocusOnAppearing
    | ImGuiWindowFlags.NoInputs
    | ImGuiWindowFlags.NoNav;

    public OverlayWindow()
        : base(nameof(OverlayWindow), BaseFlags, true)
    {
        IsOpen = true;
        AllowClickthrough = true;
        RespectCloseHotkey = false;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);

        base.PreDraw();
    }

    public override unsafe void Draw()
    {
        if (!HotbarHighlightManager.Enable || Svc.ClientState == null || Svc.ClientState.LocalPlayer == null)
        {
            return;
        }

        ImGui.GetStyle().AntiAliasedFill = false;

        try
        {
            UpdateDrawingElementsAsync().GetAwaiter().GetResult();

            if (HotbarHighlightManager._drawingElements2D != null)
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                if (drawList.Handle == null)
                {
                    PluginLog.Warning($"{nameof(OverlayWindow)}: Window draw list is null.");
                    return;
                }

                IDrawing2D[] elements = HotbarHighlightManager._drawingElements2D;
                List<IDrawing2D> sortedElements = new(elements);
                sortedElements.Sort((a, b) => GetDrawingOrder(a).CompareTo(GetDrawingOrder(b)));
                foreach (IDrawing2D item in sortedElements)
                {
                    item.Draw();
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"{nameof(OverlayWindow)} failed to draw on Screen. {ex.Message}");
        }
    }

    private async Task UpdateDrawingElementsAsync()
    {
        if (!HotbarHighlightManager.UseTaskToAccelerate)
        {
            HotbarHighlightManager._drawingElements2D = await HotbarHighlightManager.To2DAsync();
        }
    }

    private int GetDrawingOrder(object drawing)
    {
        return drawing switch
        {
            PolylineDrawing poly => poly._thickness == 0 ? 0 : 1,
            ImageDrawing => 1,
            _ => 2,
        };
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        base.PostDraw();
    }
}