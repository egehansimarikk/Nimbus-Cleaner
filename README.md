# Nimbus

Windows cleanup utility for temporary files, app caches, and basic system tools.

Nimbus does **not** delete documents, cookies, or saved passwords. Locked files are skipped.

## Large-file analysis

Open **Tools > Disk & power > Analyze large files** to scan a ready drive. The default threshold is 100 MB and can be changed before scanning. Inaccessible folders are skipped; results can be opened in Explorer or deleted after confirmation.

## Requirements

- Windows 7 or later
- .NET Framework 4.8
- Visual Studio 2022 **or** [.NET SDK](https://dotnet.microsoft.com/download) (to build)

## Build

```bat
dotnet publish WinCleaner.csproj -c Release -o publish
```

Output: `publish\Nimbus.exe`  
No extra `.config` file is required.

## Settings

Language and theme are stored in:

`%AppData%\Roaming\NimbusCleaner\`

- `dil.txt`
- `tema.txt`

## Installer

`NimbusCleaner.iss` is an [Inno Setup 6](https://jrsoftware.org/isinfo.php) script.

1. Build `Nimbus.exe` as above.
2. Copy `Nimbus.exe` next to `NimbusCleaner.iss`.
3. Open the script in Inno Setup and click **Build**.

Default install path: `C:\Program Files\Nimbus Cleaner`

## Project layout

| File | Role |
| --- | --- |
| `Program.cs` | Entry point |
| `MainForm.cs` | UI, themes, language switch |
| `DiskAnalyzer.cs` | Asynchronous large-file scanning |
| `DiskAnalyzerForm.cs` | Disk analyzer UI and result actions |
| `CleanEngine.cs` | Cleanup and system tools |
| `Lang.cs` | Translations (14 languages) |
| `WinApi.cs` | Native Windows calls |
| `WinCleaner.csproj` | .NET Framework 4.8 WinForms project |
| `app.manifest` | DPI + requestedExecutionLevel |
| `app.ico` / `Nimbus.ico` | Application icon |

## Languages

English, Türkçe, Deutsch, Français, Español, Azərbaycan, Русский, Italiano, Português, Nederlands, Polski, Українська, Indonesia, Română

UI language follows Windows on first launch if supported; otherwise English.
