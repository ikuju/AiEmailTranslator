using System.ComponentModel;
using Microsoft.Win32;

namespace AiEmailTranslator;

public sealed class MainForm : Form
{
    private readonly AiTranslatorClient _client = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _appIcon;
    private readonly KeyboardHook _keyboardHook;

    private AppConfig _config = ConfigService.Load();
    private TranslationResult? _lastResult;
    private IntPtr _lastSourceWindow;
    private CancellationTokenSource? _translationCts;

    private readonly Label _currentSettingsLabel = new() { AutoSize = false, AutoEllipsis = true };
    private readonly TextBox _sourceBox = new() { Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox _titleBox = new();
    private readonly TextBox _bodyBox = new() { Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly ThemedStatusBar _statusBar = new() { Dock = DockStyle.Bottom };
    private readonly Button _translateButton = new() { Text = "Translate", Width = 92 };
    private readonly Button _replaceAllButton = new() { Text = "Paste all", Width = 86 };
    private readonly Button _replaceBodyButton = new() { Text = "Paste body", Width = 96 };
    private readonly Button _copyTitleButton = new() { Text = "Copy title", Width = 88 };

    public MainForm()
    {
        Text = "AI Email Translator";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(680, 520);
        Size = new Size(760, 580);
        Font = new Font("Segoe UI", 10F);
        _appIcon = AppIcon.Create();
        Icon = _appIcon;

        _notifyIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "AI Email Translator",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();

        BuildUi();
        RefreshCurrentSettingsLabel();
        ApplyTheme();

        _keyboardHook = new KeyboardHook(this);
        _keyboardHook.TranslatePressed += async (_, _) => await TranslateSelectionFromActiveWindowAsync();
        _keyboardHook.SettingsPressed += (_, _) => OpenSettings();
        StartKeyboardHook();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _keyboardHook.Dispose();
        _notifyIcon.Dispose();
        _appIcon.Dispose();
        base.OnHandleDestroyed(e);
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Settings", null, (_, _) => OpenSettings());
        menu.Items.Add("Translate selected text", null, async (_, _) => await TranslateSelectionFromActiveWindowAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) =>
        {
            _notifyIcon.Visible = false;
            Application.Exit();
        });
        return menu;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(BuildMenu(), 0, 0);
        root.Controls.Add(BuildHeader(), 0, 1);
        root.Controls.Add(BuildContentPanel(), 0, 2);
        root.Controls.Add(BuildButtonPanel(), 0, 3);
        root.Controls.Add(BuildStatusBar(), 0, 4);

        _translateButton.Click += async (_, _) => await TranslateSourceAsync();
        _replaceAllButton.Click += (_, _) => PasteToSourceWindow(_lastResult?.WithSubjectLine() ?? "");
        _replaceBodyButton.Click += (_, _) => PasteToSourceWindow(_lastResult?.Body ?? "");
        _copyTitleButton.Click += (_, _) => CopyToClipboard(_titleBox.Text);

        _titleBox.TextChanged += (_, _) => UpdateActionButtons();
        _bodyBox.TextChanged += (_, _) => UpdateActionButtons();
        UpdateActionButtons();
    }

    private Control BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };
        var file = new ToolStripMenuItem("File");
        file.DropDownItems.Add("Settings", null, (_, _) => OpenSettings());
        file.DropDownItems.Add("Exit", null, (_, _) =>
        {
            _notifyIcon.Visible = false;
            Application.Exit();
        });

        var actions = new ToolStripMenuItem("Actions");
        actions.DropDownItems.Add("Translate selected text", null, async (_, _) => await TranslateSelectionFromActiveWindowAsync());
        actions.DropDownItems.Add("Translate source box", null, async (_, _) => await TranslateSourceAsync());

        menu.Items.Add(file);
        menu.Items.Add(actions);
        return menu;
    }

    private Control BuildHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0, 2, 0, 8)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _currentSettingsLabel.Anchor = AnchorStyles.Left;
        _currentSettingsLabel.Dock = DockStyle.Fill;
        _currentSettingsLabel.Height = 30;
        var settingsButton = new Button { Text = "Settings", Width = 86, Height = 32 };
        settingsButton.Click += (_, _) => OpenSettings();

        panel.Controls.Add(_currentSettingsLabel, 0, 0);
        panel.Controls.Add(settingsButton, 1, 0);
        return panel;
    }

    private Control BuildContentPanel()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            Panel1MinSize = 40,
            Panel2MinSize = 80
        };

        split.Panel1.Controls.Add(BuildSourcePanel());
        split.Panel2.Controls.Add(BuildResultPanel());
        return split;
    }

    private Control BuildSourcePanel()
    {
        var group = new ThemedGroupBox { Text = "Source text", Dock = DockStyle.Fill, Padding = new Padding(8, 18, 8, 8) };
        _sourceBox.Dock = DockStyle.Fill;
        group.Controls.Add(_sourceBox);
        return group;
    }

    private Control BuildResultPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label { Text = "Subject / title", AutoSize = true }, 0, 0);
        panel.Controls.Add(_titleBox, 0, 1);
        panel.Controls.Add(new Label { Text = "Translated body", AutoSize = true, Margin = new Padding(0, 8, 0, 4) }, 0, 2);
        panel.Controls.Add(_bodyBox, 0, 3);

        _titleBox.Dock = DockStyle.Top;
        _bodyBox.Dock = DockStyle.Fill;

        return panel;
    }

    private Control BuildButtonPanel()
    {
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 0)
        };
        var copyAllButton = new Button { Text = "Copy all", Width = 82 };
        copyAllButton.Click += (_, _) => CopyToClipboard(BuildCurrentFullResult());

        buttons.Controls.Add(_translateButton);
        buttons.Controls.Add(_replaceAllButton);
        buttons.Controls.Add(_replaceBodyButton);
        buttons.Controls.Add(_copyTitleButton);
        buttons.Controls.Add(copyAllButton);

        foreach (Control control in buttons.Controls)
        {
            control.Height = 32;
            control.Margin = new Padding(0, 3, 6, 5);
        }

        return buttons;
    }

    private Control BuildStatusBar()
    {
        return _statusBar;
    }

    private void OpenSettings()
    {
        ShowMainWindow();
        using var dialog = new SettingsForm(_config);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _config = ConfigService.Load();
            RefreshCurrentSettingsLabel();
            ApplyTheme();
            SetStatus("Settings saved.");
        }
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (_config.ThemeMode == ThemeMode.System && !IsDisposed)
        {
            BeginInvoke(ApplyTheme);
        }
    }

    private void ApplyTheme()
    {
        ThemeService.Apply(this, _config);
    }

    private void RefreshCurrentSettingsLabel()
    {
        var provider = _config.ActiveProvider;
        _currentSettingsLabel.Text = $"Provider: {provider.Name}    Model: {provider.Model}    Target: {_config.TargetLanguage}";
    }

    private async Task TranslateSelectionFromActiveWindowAsync()
    {
        _lastSourceWindow = NativeMethods.GetForegroundWindow();
        if (_lastSourceWindow == Handle || _lastSourceWindow == IntPtr.Zero)
        {
            _lastSourceWindow = IntPtr.Zero;
        }

        var selected = await ClipboardHelpers.CopySelectedTextAsync();
        if (string.IsNullOrWhiteSpace(selected))
        {
            ShowMainWindow();
            SetStatus("No selected text was detected. Paste text into the source box or select text in another app.");
            return;
        }

        ShowMainWindow();
        _sourceBox.Text = selected.Trim();
        await TranslateSourceAsync();
    }

    private async Task TranslateSourceAsync()
    {
        var source = _sourceBox.Text.Trim();
        if (source.Length == 0)
        {
            MessageBox.Show(this, "Please enter or select text to translate.", "AI Email Translator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (source.Length > 10000)
        {
            MessageBox.Show(this, "The text is quite long. Please keep it under 10000 characters.", "AI Email Translator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.ActiveProvider.ApiKey))
        {
            OpenSettings();
            if (string.IsNullOrWhiteSpace(_config.ActiveProvider.ApiKey))
            {
                SetStatus("API key is required before translation.");
                return;
            }
        }

        _translationCts?.Cancel();
        _translationCts = new CancellationTokenSource();
        SetBusy(true);

        try
        {
            var provider = _config.ActiveProvider;
            SetStatus($"Calling {provider.Name}...");
            _lastResult = await _client.TranslateAsync(source, _config.TargetLanguage, provider, _translationCts.Token);
            _titleBox.Text = _lastResult.Title;
            _bodyBox.Text = _lastResult.Body;
            SetStatus("Translation complete.");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Translation cancelled.");
        }
        catch (Exception ex)
        {
            SetStatus("Translation failed.");
            MessageBox.Show(this, ex.Message, "AI Email Translator", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            UpdateActionButtons();
        }
    }

    private void PasteToSourceWindow(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        CopyToClipboard(text);
        if (_lastSourceWindow == IntPtr.Zero)
        {
            SetStatus("Result copied. No original source window is available for automatic paste.");
            return;
        }

        NativeMethods.SetForegroundWindow(_lastSourceWindow);
        Task.Delay(160).ContinueWith(_ => BeginInvoke(() =>
        {
            SendKeys.SendWait("^v");
            SetStatus("Result pasted into the source window.");
        }));
    }

    private void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Clipboard.SetText(text);
        SetStatus("Copied to clipboard.");
    }

    private string BuildCurrentFullResult()
    {
        var title = _titleBox.Text.Trim();
        var body = _bodyBox.Text.Trim();
        return string.IsNullOrWhiteSpace(title) ? body : $"Subject: {title}\r\n\r\n{body}";
    }

    private void StartKeyboardHook()
    {
        if (_keyboardHook.Start())
        {
            SetStatus("Ready. Hotkeys: Win+T translate, Ctrl+Win+T settings.");
            return;
        }

        SetStatus("Keyboard hook could not be installed. Try running the app as administrator.");
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void SetBusy(bool isBusy)
    {
        _translateButton.Enabled = !isBusy;
        UseWaitCursor = isBusy;
    }

    private void UpdateActionButtons()
    {
        var hasResult = !string.IsNullOrWhiteSpace(_bodyBox.Text);
        _replaceAllButton.Enabled = hasResult;
        _replaceBodyButton.Enabled = hasResult;
        _copyTitleButton.Enabled = !string.IsNullOrWhiteSpace(_titleBox.Text);
    }

    private void SetStatus(string text)
    {
        _statusBar.StatusText = text;
        _statusBar.Invalidate();
    }
}

internal static class ClipboardHelpers
{
    public static async Task<string> CopySelectedTextAsync()
    {
        await WaitForHotkeyReleaseAsync();
        var previousText = Clipboard.ContainsText() ? Clipboard.GetText() : null;
        Clipboard.Clear();
        SendKeys.SendWait("^c");
        await Task.Delay(320);

        if (Clipboard.ContainsText())
        {
            return Clipboard.GetText();
        }

        if (previousText is not null)
        {
            Clipboard.SetText(previousText);
        }

        return "";
    }

    private static async Task WaitForHotkeyReleaseAsync()
    {
        var timeoutAt = DateTime.UtcNow.AddMilliseconds(1500);
        while (DateTime.UtcNow < timeoutAt && AnyModifierKeyDown())
        {
            await Task.Delay(25);
        }

        await Task.Delay(80);
    }

    private static bool AnyModifierKeyDown() =>
        IsKeyDown(NativeMethods.VkLwin)
        || IsKeyDown(NativeMethods.VkRwin)
        || IsKeyDown(NativeMethods.VkControl)
        || IsKeyDown(NativeMethods.VkShift)
        || IsKeyDown(NativeMethods.VkAlt);

    private static bool IsKeyDown(int virtualKey) =>
        (NativeMethods.GetAsyncKeyState(virtualKey) & unchecked((short)0x8000)) != 0;
}
