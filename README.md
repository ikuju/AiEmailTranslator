# AI Email Translator

A Windows Forms utility for translating selected text into business-email style output.

## Features

- Global hotkey: `Win+T` translates selected text.
- `Ctrl+Win+T` opens settings.
- Generates both an email subject and translated body.
- Supports DeepSeek, Gemini, and OpenAI-compatible chat-completions APIs.
- Light, dark, and system theme modes.
- Single-instance tray application.

## Privacy

API keys are stored only in the local user profile:

`%AppData%\AiEmailTranslator\settings.json`

This file is intentionally not committed to Git.

## Build

```powershell
dotnet build .\AiEmailTranslator\AiEmailTranslator.csproj
dotnet publish .\AiEmailTranslator\AiEmailTranslator.csproj -c Release
```

