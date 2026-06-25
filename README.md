# AI Email Translator

A Windows Forms utility for translating selected text into business-email style output.

## Features

- Select text in any editable Windows app, press `Win+T`, and translate it immediately.
- Review the translated result, then paste it back to replace the original selected text.
- Generate both a professional email subject and a translated email body.
- Paste the full result, paste only the body, copy only the subject, or copy everything.
- Open settings with `Ctrl+Win+T`.
- Configure DeepSeek, Gemini, or any OpenAI-compatible chat-completions API.
- Store API keys locally in the user's Windows profile, not in the project directory.
- Use light, dark, or system-following theme modes.
- Run as a single-instance tray application with a custom app icon.

## Privacy

API keys are stored only in the local user profile:

`%AppData%\AiEmailTranslator\settings.json`

This file is intentionally not committed to Git.

## Build

```powershell
dotnet build .\AiEmailTranslator\AiEmailTranslator.csproj
dotnet publish .\AiEmailTranslator\AiEmailTranslator.csproj -c Release
```
