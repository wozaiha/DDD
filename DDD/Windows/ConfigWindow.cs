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
        "DDD Config",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(233, 95);
        SizeCondition = ImGuiCond.Always;

        Configuration = DalamudApi.Configuration;
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
        if (ImGui.Checkbox("输出调试信息", ref Configuration.DebugInfo))
        {
            Configuration.Save();
        }
    }
}
