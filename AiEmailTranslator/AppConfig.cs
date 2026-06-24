using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiEmailTranslator;

public sealed class AppConfig
{
    public string TargetLanguage { get; set; } = "English";
    public string ActiveProviderId { get; set; } = "deepseek";
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;
    public List<ProviderConfig> Providers { get; set; } = ProviderConfig.CreateDefaults();

    [JsonIgnore]
    public ProviderConfig ActiveProvider =>
        Providers.FirstOrDefault(p => p.Id == ActiveProviderId) ?? Providers[0];
}

public sealed class ProviderConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public ProviderKind Kind { get; set; } = ProviderKind.OpenAiCompatible;
    public string Endpoint { get; set; } = "";
    public string Model { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string KeyUrl { get; set; } = "";

    public override string ToString() => Name;

    public static List<ProviderConfig> CreateDefaults() =>
    [
        new()
        {
            Id = "deepseek",
            Name = "DeepSeek",
            Kind = ProviderKind.OpenAiCompatible,
            Endpoint = "https://api.deepseek.com/v1/chat/completions",
            Model = "deepseek-v4-flash",
            KeyUrl = "https://platform.deepseek.com/api_keys"
        },
        new()
        {
            Id = "gemini",
            Name = "Gemini",
            Kind = ProviderKind.Gemini,
            Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
            Model = "gemini-2.5-flash",
            KeyUrl = "https://aistudio.google.com/app/apikey"
        },
        new()
        {
            Id = "openai_compatible",
            Name = "OpenAI Compatible",
            Kind = ProviderKind.OpenAiCompatible,
            Endpoint = "https://api.openai.com/v1/chat/completions",
            Model = "gpt-4o-mini"
        }
    ];
}

public enum ProviderKind
{
    OpenAiCompatible,
    Gemini
}

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public sealed class TranslationResult
{
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";

    public string WithSubjectLine() =>
        string.IsNullOrWhiteSpace(Title) ? Body : $"Subject: {Title}\r\n\r\n{Body}";
}

public static class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string ConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiEmailTranslator", "settings.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), JsonOptions);
                if (config is not null)
                {
                    MergeMissingDefaultProviders(config);
                    return config;
                }
            }
        }
        catch
        {
            // Fall back to defaults when the config file is damaged.
        }

        return new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }

    private static void MergeMissingDefaultProviders(AppConfig config)
    {
        foreach (var provider in ProviderConfig.CreateDefaults())
        {
            var existing = config.Providers.FirstOrDefault(p => p.Id == provider.Id);
            if (existing is null)
            {
                config.Providers.Add(provider);
                continue;
            }

            if (string.IsNullOrWhiteSpace(existing.Endpoint))
            {
                existing.Endpoint = provider.Endpoint;
            }

            if (string.IsNullOrWhiteSpace(existing.Model))
            {
                existing.Model = provider.Model;
            }

            if (string.IsNullOrWhiteSpace(existing.Name))
            {
                existing.Name = provider.Name;
            }
        }
    }
}
