# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

GitTui is a terminal UI (TUI) Git client written in C#/.NET, built on the [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) v2 library. The project is in early scaffolding stages: the main window and a menu bar exist, but most functionality (localization, git operations, models) is unimplemented.

## Commands

```bash
# Build
dotnet build GitTui.slnx

# Run
dotnet run --project GitTui/GitTui.csproj

# Restore packages
dotnet restore
```

There is no test project yet.

## Architecture

- **Entry point**: `GitTui/Program.cs` — creates the Terminal.Gui `Application`, builds a `Window`, and attaches a `MenuBar` via `CreateNavBar()`. New top-level UI composition (windows, menus, views) starts here.
- **Localization**: driven by `GitTui.Interfaces.ILocalizer`, implemented in `GitTui/Utils/Localizer.cs`. The implementation is currently a stub (`Reload`/`ReloadLangFiles`/`GetLocales`/indexers/`Get`/`ContainsKey` all throw `NotImplementedException` or are empty) — treat it as unfinished, not as a working reference.
  - Locale strings live in `GitTui/Ressources/Localization/lang.<locale>.xml` (e.g. `lang.en.xml`, `lang.fr.xml`) as `<traductions><trad name="KEY" content="Value"/></traductions>`.
  - Note the `.csproj` currently references a `Assets\lang.en.xml` copy-to-output entry that does not match the actual `Ressources/Localization/` path — check this discrepancy before relying on localization files being copied to the output directory.
- **Models**: `GitTui/Models/` folder is declared in the `.csproj` but currently has no files.
- **Target framework**: net10.0, nullable reference types enabled, implicit usings enabled.

## Terminal.Gui notes

This project uses Terminal.Gui v2 (namespaces `Terminal.Gui.App`, `Terminal.Gui.ViewBase`, `Terminal.Gui.Views`), which has a different API shape from v1 (e.g. `Application.Create()` returning `IApplication`, disposable `Window`/`app` via `using`). When adding UI code, follow the v2 patterns already used in `Program.cs` rather than older Terminal.Gui examples/documentation.