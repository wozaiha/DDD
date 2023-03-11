using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace DDD.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    public ConfigWindow(Plugin plugin) : base(
        "A Wonderful Configuration Window",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(232, 75);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
        Plugin = plugin;
        Plugin.eventHandle.Output = Configuration.Output;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.Checkbox("输出Log文件", ref Configuration.Output))
        {
            Plugin.eventHandle.Output = Configuration.Output;
            Configuration.Save();
        }
    }
}
