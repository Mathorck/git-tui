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

The solution (`GitTui.slnx`) has two projects: `GitTui` (the app) and `GitTui.Generators` (a Roslyn source generator, referenced by `GitTui` as an analyzer-only `ProjectReference`).

- **Entry point**: `GitTui/Program.cs` — creates the Terminal.Gui `Application`, builds a `Window`, and attaches a `MenuBar` via `CreateNavBar()`. New top-level UI composition (windows, menus, views) starts here.
- **Localization**: driven by `GitTui.Interfaces.ILocalizer`, implemented in `GitTui/Utils/Localizer.cs`. `Reload(locale)` loads `<trad name="KEY" content="Value"/>` entries from `GitTui/Ressources/Localization/lang.<locale>.xml` into memory. `Get`/the indexers resolve a key against the current locale, fall back to `DEFAULT_LOCALE` ("en") if missing there, and fall back to the key itself if missing from both.
  - Locale files: `GitTui/Ressources/Localization/lang.<locale>.xml` (e.g. `lang.en.xml`, `lang.fr.xml`), copied to the output directory via a `None Update="Ressources\Localization\**\*.xml"` glob in the `.csproj`.
  - `GitTui.Generators/LocalizationKeysGenerator.cs` reads `lang.en.xml` (registered as an `AdditionalFiles` item in `GitTui.csproj`) at build time and emits `GitTui.Utils.LocalizationKeys`, a `static class` of `const string` fields — one per `<trad name="...">` in the reference (English) file. Use `localizer[LocalizationKeys.MENU_FILE]` instead of a raw string literal so the IDE autocompletes/typo-checks keys; add new keys to `lang.en.xml` first; other locale files don't need to be in sync for the generator (missing keys there just fall back per the `Get` fallback logic above).
  - Generated source lands in `GitTui/obj/Generated/...` (visible because `EmitCompilerGeneratedFiles`/`CompilerGeneratedFilesOutputPath` are set in `GitTui.csproj`) — gitignored, regenerated on every build.
- **Models**: `GitTui/Models/` folder is declared in the `.csproj` but currently has no files.
- **Target framework**: `GitTui` targets net10.0; `GitTui.Generators` targets netstandard2.0 (required for Roslyn analyzers). Nullable reference types and implicit usings enabled on both.

## Terminal.Gui notes

This project uses Terminal.Gui v2 (namespaces `Terminal.Gui.App`, `Terminal.Gui.ViewBase`, `Terminal.Gui.Views`), which has a different API shape from v1 (e.g. `Application.Create()` returning `IApplication`, disposable `Window`/`app` via `using`). When adding UI code, follow the v2 patterns already used in `Program.cs` rather than older Terminal.Gui examples/documentation.