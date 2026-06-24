namespace AiEmailTranslator;

internal sealed class ThemedStatusBar : Control
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string StatusText { get; set; } = "";

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(68, 68, 68);

    public ThemedStatusBar()
    {
        DoubleBuffered = true;
        Height = 24;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        using var pen = new Pen(BorderColor);
        e.Graphics.DrawLine(pen, 0, 0, Width, 0);

        var rect = new Rectangle(8, 2, Width - 16, Height - 4);
        TextRenderer.DrawText(e.Graphics, StatusText, Font, rect, ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
