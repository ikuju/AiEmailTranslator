using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AiEmailTranslator;

public sealed class AiTranslatorClient
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    public async Task<TranslationResult> TranslateAsync(
        string sourceText,
        string targetLanguage,
        ProviderConfig provider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            throw new InvalidOperationException($"Please set the API key for {provider.Name} first.");
        }

        var raw = provider.Kind switch
        {
            ProviderKind.Gemini => await CallGeminiAsync(sourceText, targetLanguage, provider, cancellationToken),
            _ => await CallOpenAiCompatibleAsync(sourceText, targetLanguage, provider, cancellationToken)
        };

        return ParseResult(raw);
    }

    private static async Task<string> CallOpenAiCompatibleAsync(
        string sourceText,
        string targetLanguage,
        ProviderConfig provider,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, provider.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);

        var payload = new
        {
            model = provider.Model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You translate and polish business email content. Return only the requested TITLE and BODY format."
                },
                new
                {
                    role = "user",
                    content = BuildInstruction(targetLanguage, sourceText)
                }
            },
            temperature = 0.3,
            max_tokens = 2500
        };

        request.Content = JsonContent(payload);
        using var response = await Http.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, text);

        var root = JsonNode.Parse(text);
        return root?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "";
    }

    private static async Task<string> CallGeminiAsync(
        string sourceText,
        string targetLanguage,
        ProviderConfig provider,
        CancellationToken cancellationToken)
    {
        var endpoint = provider.Endpoint.Replace("{model}", Uri.EscapeDataString(provider.Model));
        if (!endpoint.Contains("key=", StringComparison.OrdinalIgnoreCase))
        {
            endpoint += endpoint.Contains('?') ? "&" : "?";
            endpoint += "key=" + Uri.EscapeDataString(provider.ApiKey);
        }

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = BuildInstruction(targetLanguage, sourceText) }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 2500
            }
        };

        using var response = await Http.PostAsync(endpoint, JsonContent(payload), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, text);

        var root = JsonNode.Parse(text);
        return root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>() ?? "";
    }

    private static StringContent JsonContent(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static void EnsureSuccess(HttpResponseMessage response, string responseBody)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var snippet = responseBody.Length > 900 ? responseBody[..900] + "..." : responseBody;
        throw new HttpRequestException($"API returned {(int)response.StatusCode} {response.ReasonPhrase}\r\n\r\n{snippet}");
    }

    private static string BuildInstruction(string targetLanguage, string sourceText) =>
        $"""
        You are an expert business email translator.
        Translate the user's text into {targetLanguage}.
        Also write a concise, natural email subject/title for the translated content.
        The subject should fit professional email communication, not marketing copy.
        Preserve the meaning, names, numbers, bullet points and paragraph breaks.
        Output exactly in this format:
        TITLE: <email subject>
        BODY:
        <translated email body>

        User text:
        {sourceText}
        """;

    private static TranslationResult ParseResult(string raw)
    {
        raw = raw.Trim().TrimFence();
        var match = Regex.Match(raw, @"^\s*TITLE\s*:\s*(.*?)\r?\n+BODY\s*:\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return new TranslationResult { Body = raw };
        }

        return new TranslationResult
        {
            Title = match.Groups[1].Value.Trim(),
            Body = match.Groups[2].Value.Trim()
        };
    }
}

internal static class StringExtensions
{
    public static string TrimFence(this string value)
    {
        value = Regex.Replace(value, @"^\s*```(?:text|markdown)?\s*", "", RegexOptions.IgnoreCase);
        value = Regex.Replace(value, @"\s*```\s*$", "");
        return value.Trim();
    }
}
