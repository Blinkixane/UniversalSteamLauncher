# UniversalSteamLauncher

<p align="center">
  <img src="Assets/AppIcon.ico" width="128" />
</p>

A Windows utility to create custom Steam-based launchers with proper Windows AppUserModelID (AUMID) integration.

UniversalSteamLauncher allows applications and games launched through Steam to have better Windows integration, including custom icons, taskbar grouping, and shortcut management.

## Features

* Steam game launcher generation
* Windows AppUserModelID (AUMID) support
* Custom icon support
* Native Windows taskbar integration
* Automatic launcher generation
* Self-contained Windows executables
* Built-in launcher generator tool
* Multi-language support (currently French and English)

## Components

### UniversalSteamLauncher

Main launcher application.

Responsible for:

* Starting applications
* Managing Windows integration
* Applying AUMID runtime configuration

### LauncherGenerator

Tool used to create custom launchers.

Features:

* Game/application configuration
* Steam URI support
* Icon selection
* Launcher creation

### LauncherCore

Core library containing:

* AUMID handling
* Window detection
* Shortcut management

### LauncherShared

Shared components:

* Configuration models
* Localization
* Common utilities

### Development Tools

Utilities used during development:

* AumidDiag
* AumidWatcher

Used to analyze and debug Windows AUMID behavior.

## Installation

Download the latest installer from the Releases page.

Run:

```text
UniversalSteamLauncherSetup.exe
```

and follow the installation wizard.

## Build

### Requirements

* Windows 10/11
* .NET 8 SDK
* Visual Studio 2022 or compatible IDE
* Inno Setup (for installer creation)

Clone the repository:

```bash
git clone https://github.com/Blinkixane/UniversalSteamLauncher.git
```

Build the release:

```powershell
.\Installer\build-release.ps1
```

The compiled files will be available in:

```text
dist/
```

## Architecture

```text
LauncherGenerator
        |
        v
LauncherCore
        |
        v
UniversalSteamLauncher
        |
        v
Windows Shell / Steam
```

## License

This project is licensed under the MIT License.
