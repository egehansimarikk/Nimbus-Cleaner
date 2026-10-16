<div align="center">

  # ☁️ Nimbus Cleaner

  **Fast, Lightweight, and Modern Windows System Cleaner**

  [![Version](https://img.shields.io/badge/version-1.3.0--beta-purple.svg)](https://github.com/egehansimarikk/Nimbus-Cleaner/releases)
  [![Framework](https://img.shields.io/badge/.NET-Framework%204.8-purple.svg)](https://dotnet.microsoft.com/)
  [![Platform](https://img.shields.io/badge/platform-Windows%207%20%7C%2010%20%7C%2011-0078D6.svg)](https://www.microsoft.com/windows)
  [![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

  <p align="center">
    Nimbus Cleaner is a powerful system optimization and cleaning utility built with C# and WinForms.
    It helps you free up disk space, wipe temporary caches, and keep your Windows machine running smoothly.
  </p>

</div>

---

## 📸 Overview

<div align="center">
  <img width="100%" alt="Nimbus Cleaner Main Interface" src="https://github.com/user-attachments/assets/2ba94c1a-7fa6-4bec-b64d-088c0a332c0f" />
</div>

---

## ✨ Features

- 🧹 **Safe & Deep Cleanup:** Removes system temp files, log files, prefetch, and application caches without touching personal documents or browser passwords.
- ⚡ **Centralized Engine:** Optimized cleaning algorithms with robust error handling and automatic locked-file skipping.
- 🚀 **Smart RAM Booster:** Win32 API integrated memory optimization to reclaim standby physical memory safely.
- 🖱️ **Context Menu Integration:** Native Windows Shell right-click menu integration for instant file and folder cleaning.
- 🌍 **Multilingual Interface:** Fully localized across 14 languages with automatic Windows language detection.
- 🎨 **Modern & Customizable UI:** Lightweight Windows Forms interface with dynamic theme presets.
- 📦 **Easy Setup:** Comes with a multi-language Inno Setup installer for quick deployment.

---

## 🛡️ Windows SmartScreen Notice

Since **Nimbus Cleaner** is an open-source tool built by an independent developer, it does not currently carry an expensive digital code-signing certificate. As a result, Windows SmartScreen may display a warning on your first launch.

> **How to launch the application:**
> 1. Click on **More info** (*Ek bilgi*).
> 2. Click **Run anyway** (*Yine de çalıştır*).

<p align="center">
  <img width="526" height="496" alt="smartscreen-guide" src="https://github.com/user-attachments/assets/390d6673-502d-4fee-962a-7bac4efcf804" />
</p>


---

## 🛠️ Requirements & Compatibility

| Requirement | Specification |
| :--- | :--- |
| **Operating System** | Windows 7 SP1 / 8.1 / 10 / 11 (x64) |
| **Framework** | .NET Framework 4.8 or higher |
| **Privileges** | Administrator rights (for system folder access) |

---

## 🚀 Getting Started

### Installation
1. Download the latest installer (`NimbusCleaner_Setup_v1.3.0-beta.exe`) from the [Releases](https://github.com/egehansimarikk/Nimbus-Cleaner/releases) page.
2. Run the setup wizard and follow the instructions.
3. Launch **Nimbus Cleaner** from your Desktop or Start Menu.

### Building from Source
1. Clone the repository:
   ```bash
   git clone [https://github.com/egehansimarikk/Nimbus-Cleaner.git](https://github.com/egehansimarikk/Nimbus-Cleaner.git)
2. Open `WinCleaner.csproj` in Visual Studio 2022.
3. Build the solution using **Release** mode (`x64`).

---

## 📝 Configuration & Storage

User preferences (such as selected language and UI theme) are safely stored in:

```text
%AppData%\Roaming\NimbusCleaner\

```

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](https://www.google.com/search?q=LICENSE) file for details.
