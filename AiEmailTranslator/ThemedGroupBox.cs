namespace AiEmailTranslator;

internal sealed class ThemedGroupBox : GroupBox
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = SystemColors.ControlDark;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color CaptionBackColor { get; set; } = SystemColors.Control;

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);

        var textSize = TextRenderer.MeasureText(Text, Font);
        var textRect = new Rectangle(10, 0, textSize.Width + 6, textSize.Height);
        var borderTop = textRect.Height / 2;
        var borderRect = new Rectangle(0, borderTop, Width - 1, Height - borderTop - 1);

        using var pen = new Pen(BorderColor);
        e.Graphics.DrawRectangle(pen, borderRect);

        using var captionBrush = new SolidBrush(CaptionBackColor);
        e.Graphics.FillRectangle(captionBrush, textRect);
        TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }
}
