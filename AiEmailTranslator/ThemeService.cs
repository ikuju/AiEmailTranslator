using Microsoft.Win32;

namespace AiEmailTranslator;

internal static class ThemeService
{
    private static readonly ThemeColors Light = new(
        SystemColors.Control,
        SystemColors.Window,
        SystemColors.ControlText,
        SystemColors.WindowText,
        SystemColors.ControlDark,
        Color.FromArgb(245, 246, 248),
        Color.FromArgb(230, 232, 236),
        Color.FromArgb(222, 235, 247),
        Color.FromArgb(32, 124, 202),
        Color.FromArgb(232, 244, 255));

    private static readonly ThemeColors Dark = new(
        Color.FromArgb(43, 43, 43),
        Color.FromArgb(48, 48, 48),
        Color.FromArgb(202, 205, 208),
        Color.FromArgb(232, 234, 236),
        Color.FromArgb(92, 92, 92),
        Color.FromArgb(35, 35, 35),
        Color.FromArgb(68, 68, 68),
        Color.FromArgb(58, 58, 58),
        Color.FromArgb(83, 178, 242),
        Color.FromArgb(53, 73, 89));

    public static bool IsDark(AppConfig config) =>
        config.ThemeMode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            _ => IsSystemDark()
        };

    public static void Apply(Form form, AppConfig config)
    {
        var isDark = IsDark(config);
        var colors = isDark ? Dark : Light;

        ApplyToControl(form, colors, isDark);
        NativeMethods.SetImmersiveDarkMode(form.Handle, isDark);
        form.Invalidate(true);
    }

    private static void ApplyToControl(Control control, ThemeColors colors, bool isDark)
    {
        control.ForeColor = colors.Text;
        control.BackColor = control is TextBoxBase or ComboBox ? colors.Input : colors.Background;
        ApplyNativeControlTheme(control, isDark);

        switch (control)
        {
            case TextBoxBase textBox:
                textBox.BackColor = colors.Input;
                textBox.ForeColor = colors.InputText;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox combo:
                combo.BackColor = colors.Input;
                combo.ForeColor = colors.InputText;
                combo.FlatStyle = FlatStyle.Flat;
                break;
            case Button button:
                button.BackColor = colors.Surface;
                button.ForeColor = colors.Text;
                button.UseVisualStyleBackColor = false;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = colors.Border;
                button.FlatAppearance.MouseOverBackColor = colors.Hover;
                button.FlatAppearance.MouseDownBackColor = colors.AccentSoft;
                break;
            case ThemedGroupBox themedGroupBox:
                themedGroupBox.BackColor = colors.Background;
                themedGroupBox.ForeColor = colors.Text;
                themedGroupBox.BorderColor = colors.Border;
                themedGroupBox.CaptionBackColor = colors.Background;
                themedGroupBox.Invalidate();
                break;
            case ThemedStatusBar themedStatusBar:
                themedStatusBar.BackColor = colors.Surface;
                themedStatusBar.ForeColor = colors.Text;
                themedStatusBar.BorderColor = colors.Border;
                themedStatusBar.Invalidate();
                break;
            case Label label:
                label.ForeColor = colors.Text;
                break;
            case GroupBox:
            case Panel:
            case SplitContainer:
                control.BackColor = colors.Background;
                break;
            case MenuStrip menu:
                ApplyToToolStrip(menu, colors);
                break;
            case StatusStrip status:
                ApplyToToolStrip(status, colors);
                break;
        }

        if (control is SplitContainer split)
        {
            split.Panel1.BackColor = colors.Background;
            split.Panel2.BackColor = colors.Background;
        }

        foreach (Control child in control.Controls)
        {
            ApplyToControl(child, colors, isDark);
        }
    }

    private static void ApplyToToolStrip(ToolStrip strip, ThemeColors colors)
    {
        strip.BackColor = colors.Surface;
        strip.ForeColor = colors.Text;
        strip.Renderer = new ThemeToolStripRenderer(colors);
        foreach (ToolStripItem item in strip.Items)
        {
            ApplyToToolStripItem(item, colors);
        }
    }

    private static void ApplyToToolStripItem(ToolStripItem item, ThemeColors colors)
    {
        item.BackColor = colors.Surface;
        item.ForeColor = colors.Text;
        if (item is ToolStripMenuItem menuItem)
        {
            foreach (ToolStripItem child in menuItem.DropDownItems)
            {
                ApplyToToolStripItem(child, colors);
            }
        }
    }

    private static void ApplyNativeControlTheme(Control control, bool isDark)
    {
        if (control.IsHandleCreated)
        {
            NativeMethods.SetControlDarkTheme(control.Handle, isDark);
        }
        else
        {
            control.HandleCreated += (_, _) => NativeMethods.SetControlDarkTheme(control.Handle, isDark);
        }
    }

    private static bool IsSystemDark()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1);
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }

    private sealed record ThemeColors(
        Color Background,
        Color Input,
        Color Text,
        Color InputText,
        Color Muted,
        Color Surface,
        Color Border,
        Color Hover,
        Color Accent,
        Color AccentSoft);

    private sealed class ThemeColorTable(ThemeColors colors) : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => colors.Surface;
        public override Color ToolStripGradientMiddle => colors.Surface;
        public override Color ToolStripGradientEnd => colors.Surface;
        public override Color MenuStripGradientBegin => colors.Surface;
        public override Color MenuStripGradientEnd => colors.Surface;
        public override Color MenuItemSelected => colors.Input;
        public override Color MenuItemSelectedGradientBegin => colors.Hover;
        public override Color MenuItemSelectedGradientEnd => colors.Hover;
        public override Color MenuItemBorder => colors.Muted;
        public override Color ImageMarginGradientBegin => colors.Surface;
        public override Color ImageMarginGradientMiddle => colors.Surface;
        public override Color ImageMarginGradientEnd => colors.Surface;
        public override Color SeparatorDark => colors.Border;
        public override Color SeparatorLight => colors.Border;
        public override Color ToolStripBorder => colors.Surface;
        public override Color StatusStripGradientBegin => colors.Surface;
        public override Color StatusStripGradientEnd => colors.Surface;
        public override Color ButtonSelectedBorder => colors.Accent;
        public override Color ButtonSelectedGradientBegin => colors.Hover;
        public override Color ButtonSelectedGradientMiddle => colors.Hover;
        public override Color ButtonSelectedGradientEnd => colors.Hover;
    }

    private sealed class ThemeToolStripRenderer(ThemeColors colors) : ToolStripProfessionalRenderer(new ThemeColorTable(colors))
    {
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var bounds = e.Item.Bounds;
            using var pen = new Pen(colors.Border);

            if (e.Vertical)
            {
                var x = bounds.Width / 2;
                e.Graphics.DrawLine(pen, x, 5, x, bounds.Height - 5);
            }
            else
            {
                var y = bounds.Height / 2;
                e.Graphics.DrawLine(pen, 24, y, bounds.Width - 4, y);
            }
        }
    }
}
