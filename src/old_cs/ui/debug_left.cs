using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

public partial class debug_left : RichTextLabel
{
    public List<Line> lines = new();
    private bool hasChanges = false;

    public void Redraw()
    {
        StringBuilder sb = new();
        foreach (Line line in lines)
        {
            if (line.IsBlank)
                sb.AppendLine();
            else
                sb.AppendLine($"[color={line.TitleColor}]{line.Title}:[/color][color={line.TextColor}]{line.Text}[/color]");
        }
        Text = sb.ToString();
    }
    public void SetLine(int idx, Line line)
    {
        if (idx > lines.Count - 1)
            for (int i = 0; i <= idx - lines.Count; ++i)
                lines.Add(new());

        lines[idx] = line;
        hasChanges = true;
    }

    public void OnPlayerPositionChange(Vector3 pos)
    { 
        SetLine(0, new("Pos", $"{pos.X:n4},{pos.Y:n4},{pos.Z:n4}","Gold","PaleTurquoise"));
    }

    public override void _Process(double delta)
    {
        if (hasChanges)
        {
            Redraw();
            hasChanges = false;
        }
    }
}

public struct Line
{
    public string Title;
    public string Text;
    public string TitleColor;
    public string TextColor;
    public bool IsBlank;

    public Line(string title, string text, string title_color, string text_color)
    {
        Title = title;
        Text = text;
        TitleColor = title_color;
        TextColor = text_color;
        IsBlank = false;
    }
    public Line()
    {
        Title = "";
        Text = "";
        TitleColor = "";
        TextColor = "";
        IsBlank = true;
    }
}