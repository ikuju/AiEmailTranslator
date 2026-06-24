namespace AiEmailTranslator;

public sealed class SettingsForm : Form
{
    private readonly AppConfig _config;
    private ThemeMode _selectedThemeMode;
    private readonly ComboBox _providerCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _providerTypeCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _targetLanguageBox = new();
    private readonly TextBox _apiKeyBox = new() { UseSystemPasswordChar = true };
    private readonly TextBox _modelBox = new();
    private readonly TextBox _endpointBox = new();
    private readonly ComboBox _themeCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };

    public SettingsForm(AppConfig config)
    {
        _config = config;
        _selectedThemeMode = config.ThemeMode;

        Text = "Settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(620, 370);
        Font = new Font("Segoe UI", 10F);

        BuildUi();
        LoadConfig();
        ThemeService.Apply(this, _config);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(16)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        AddRow(root, 0, "Provider", _providerCombo);
        AddRow(root, 1, "Type", _providerTypeCombo);
        AddRow(root, 2, "Target language", _targetLanguageBox);
        AddRow(root, 3, "API key", _apiKeyBox);
        AddRow(root, 4, "Model", _modelBox);
        AddRow(root, 5, "Endpoint", _endpointBox);
        AddRow(root, 6, "Theme", _themeCombo);

        var hint = new Label
        {
            Text = "Gemini endpoint can contain {model}. Use OpenAI Compatible for DeepSeek, OpenAI, SiliconFlow, Moonshot and similar chat-completions APIs.",
            AutoSize = false,
            Dock = DockStyle.Fill
        };
        root.Controls.Add(hint, 0, 7);
        root.SetColumnSpan(hint, 2);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        var saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        saveButton.Click += (_, _) => SaveConfig();
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        root.Controls.Add(buttons, 0, 8);
        root.SetColumnSpan(buttons, 2);

        _providerTypeCombo.Items.Add("OpenAI Compatible");
        _providerTypeCombo.Items.Add("Gemini");
        _themeCombo.Items.Add("Use system setting");
        _themeCombo.Items.Add("Light");
        _themeCombo.Items.Add("Dark");
        _providerCombo.SelectedIndexChanged += (_, _) => LoadSelectedProvider();
        _themeCombo.SelectedIndexChanged += (_, _) =>
        {
            _selectedThemeMode = SelectedThemeMode();
            ThemeService.Apply(this, new AppConfig { ThemeMode = _selectedThemeMode });
        };

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static void AddRow(TableLayoutPanel root, int row, string label, Control input)
    {
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 8, 8)
        }, 0, row);

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 5, 0, 5);
        root.Controls.Add(input, 1, row);
    }

    private void LoadConfig()
    {
        _providerCombo.Items.Clear();
        foreach (var provider in _config.Providers)
        {
            _providerCombo.Items.Add(provider);
        }

        _targetLanguageBox.Text = _config.TargetLanguage;
        _themeCombo.SelectedIndex = _selectedThemeMode switch
        {
            ThemeMode.Light => 1,
            ThemeMode.Dark => 2,
            _ => 0
        };
        _providerCombo.SelectedItem = _config.ActiveProvider;
        if (_providerCombo.SelectedIndex < 0 && _providerCombo.Items.Count > 0)
        {
            _providerCombo.SelectedIndex = 0;
        }
    }

    private void LoadSelectedProvider()
    {
        if (_providerCombo.SelectedItem is not ProviderConfig provider)
        {
            return;
        }

        _providerTypeCombo.SelectedIndex = provider.Kind == ProviderKind.Gemini ? 1 : 0;
        _apiKeyBox.Text = provider.ApiKey;
        _modelBox.Text = provider.Model;
        _endpointBox.Text = provider.Endpoint;
    }

    private void SaveConfig()
    {
        if (_providerCombo.SelectedItem is not ProviderConfig provider)
        {
            return;
        }

        provider.Kind = _providerTypeCombo.SelectedIndex == 1 ? ProviderKind.Gemini : ProviderKind.OpenAiCompatible;
        provider.ApiKey = _apiKeyBox.Text.Trim();
        provider.Model = _modelBox.Text.Trim();
        provider.Endpoint = _endpointBox.Text.Trim();
        _config.TargetLanguage = string.IsNullOrWhiteSpace(_targetLanguageBox.Text) ? "English" : _targetLanguageBox.Text.Trim();
        _config.ThemeMode = _selectedThemeMode;
        _config.ActiveProviderId = provider.Id;
        ConfigService.Save(_config);
    }

    private ThemeMode SelectedThemeMode() =>
        _themeCombo.SelectedIndex switch
        {
            1 => ThemeMode.Light,
            2 => ThemeMode.Dark,
            _ => ThemeMode.System
        };
}
