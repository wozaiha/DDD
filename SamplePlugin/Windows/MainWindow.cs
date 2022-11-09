using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;

namespace DDD.Windows;

public class MainWindow : Window, IDisposable
{
    private TextureWrap GoatImage;
    private Plugin Plugin;

    public MainWindow(Plugin plugin, TextureWrap goatImage) : base(
        "My Amazing Window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        GoatImage = goatImage;
        Plugin = plugin;
    }

    public void Dispose()
    {
        GoatImage.Dispose();
    }

    public override void Draw()
    {
        ImGui.Text($"The random config bool is {Plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        if (ImGui.Button("Show Settings"))
        {
            Plugin.DrawConfigUI();
        }

        ImGui.Spacing();

        ImGui.Text("Have a goat:");
        ImGui.Indent(55);
        ImGui.Image(GoatImage.ImGuiHandle, new Vector2(GoatImage.Width, GoatImage.Height));
        ImGui.Unindent(55);
    }
}
