<p align="center">
  <img src="Assets/AppIcon.png" width="128" alt="Universal Steam Launcher">
</p>

<h1 align="center">Universal Steam Launcher</h1>

<p align="center">
Create standalone Steam launchers with native Windows integration.
</p>

<p align="center">

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6)
![.NET](https://img.shields.io/badge/.NET-8-512BD4)
![License](https://img.shields.io/badge/License-MIT-success)
![Release](https://img.shields.io/github/v/release/Blinkixane/UniversalSteamLauncher)

</p>
<p align="center">
  <img src="Assets/Banner.png" width="900" alt="Universal Steam Launcher banner">
</p>

Universal Steam Launcher is an open-source Windows application that generates standalone Steam launchers with proper Windows integration. Each launcher supports custom icons, AppUserModelID (AUMID), taskbar grouping, and desktop or Start Menu shortcuts.

## Screenshots

<p align="center">
  <img src="Assets/Screenshots/generator.png" width="800">
</p>

<p align="center">
Launcher Generator
</p>


<p align="center">
  <img src="Assets/Screenshots/launcher.png" width="800">
</p>

<p align="center">
Generated launcher integration
</p>


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

1. Download the latest release.
2. Run **UniversalSteamLauncherSetup.exe**.
3. Follow the installation wizard.
4. Launch Universal Steam Launcher.
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
```mermaid
flowchart TD
    Generator[LauncherGenerator]
    Core[LauncherCore]
    Launcher[UniversalSteamLauncher]
    Steam[Steam]
    Windows[Windows Shell]

    Generator --> Core
    Core --> Launcher
    Launcher --> Steam
    Launcher --> Windows
```

This project is licensed under the MIT License.
