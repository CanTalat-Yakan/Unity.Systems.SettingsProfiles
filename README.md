# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Settings Profile

> Quick overview: Lightweight, versioned settings profiles backed by JSON files. Define strongly‑typed settings classes, load and mutate them via `SettingsProfile<T>`, and optionally manage multiple named profiles with `SettingsProfileManager<T>` and global key/value helpers.

This module provides a small, runtime-only settings system built on top of plain C# types. A `SettingsProfile<T>` wraps a settings object, handles JSON persistence, and optionally supports schema versioning, validation, and migration through simple interfaces. Profiles are stored as JSON files next to your project and can be switched or managed via a `SettingsProfileManager<T>` for scenarios like per-player or per-environment settings.

A sample (`GraphicsBoot`) shows how to use typed graphics settings, a manager for multiple profiles (e.g., `Default`, `Player2`), and a key/value style profile for flexible storage.

## Features
- Strongly-typed settings profiles
  - `SettingsProfile<T>` wraps a plain C# settings type (e.g., `GraphicsSettings`)
  - Lazy loading via `GetOrLoad()`; direct mutation with `Set(...)` or `Mutate(...)`
- JSON persistence
  - Profiles are stored as JSON files under a simple `Resources`-adjacent folder
  - `Save()` and `SaveIfDirty()` write changes atomically via `SettingsJsonStore`
- Change notifications
  - `Changed` event on `SettingsProfile<T>` fires when the value is updated (e.g., `Mutate`, `Set`, `ResetToDefaults`)
  - Ideal for applying settings to Unity systems (e.g., `QualitySettings`, audio, input)
- Versioning, validation and migration
  - Optional interfaces on your settings type:
    - `ISettingsVersioned` to expose a `SchemaVersion`
    - `ISettingsValidate` to clamp or normalize values after load or mutation
    - `ISettingsMigrate` to handle schema changes between saved versions
  - `SettingsEnvelope<T>` records type, profile name, schema version and last update time
- Multiple profiles via managers (pattern)
  - `SettingsProfileManager<T>` (see sample) manages multiple named profiles (e.g., `Default`, `Player2`)
  - Switch the current profile and work with it as a regular `SettingsProfile<T>`
- Key/value style settings
  - Key/value settings (e.g., `KeyValuePair` in the sample) support flexible storage when you don’t want a fixed schema
  - Access helpers like `GetInt`, `GetBool`, `GetFloat` (see `GraphicsBoot` sample usage)
- Global settings service integration
  - Sample shows how to mix Unity’s `SettingsService` with typed profiles to keep a global/system-wide view in sync

## Requirements
- Unity 6000.0+
- Runtime module; no external package dependencies
- A serializable settings type `T` with a public parameterless constructor (`where T : new()`)
- Write access to the project folder to create/update JSON files for profiles

## Usage

### 1) Define a settings type
Create a simple C# class to hold your settings. Implement optional interfaces for versioning, validation, and migration.

```csharp
using System;
using UnityEngine;
using UnityEssentials;

[Serializable]
public sealed class GraphicsSettings : ISettingsVersioned, ISettingsValidate, ISettingsMigrate
{
    public int SchemaVersion => 2;

    [Range(0.5f, 2f)] public float renderScale = 1f;
    [Range(0, 4)] public int msaa = 2;
    public bool hdr = true;

    // Introduced in v2
    public int anisotropicFiltering = 8;

    public void Validate()
    {
        renderScale = Mathf.Clamp(renderScale, 0.5f, 2f);
        msaa = Mathf.Clamp(msaa, 0, 4);
        anisotropicFiltering = Mathf.Clamp(anisotropicFiltering, 1, 16);
    }

    public bool TryMigrate(int fromVersion)
    {
        if (fromVersion < 2)
        {
            // v1 had no anisotropicFiltering; choose a conservative default
            anisotropicFiltering = 8;
            return true;
        }
        return false;
    }
}
```

### 2) Create a typed profile
Create a static `SettingsProfile<T>` and use it to load, observe, and mutate your settings.

```csharp
using UnityEngine;
using UnityEssentials;

public class GraphicsBoot : MonoBehaviour
{
    public static readonly SettingsProfile<GraphicsSettings> Graphics =
        SettingsProfileFactory.Create("Graphics", () => new GraphicsSettings());

    private void Awake()
    {
        // Load and apply once
        Apply(Graphics.GetOrLoad());

        // React to changes
        Graphics.Changed += Apply;
    }

    private void OnApplicationQuit()
    {
        Graphics.SaveIfDirty();
    }

    private static void Apply(GraphicsSettings g)
    {
        QualitySettings.antiAliasing = g.msaa;
    }
}
```

### 3) Use a profile manager for multiple named profiles
Use `SettingsProfileManager<T>` (see sample) to manage multiple profiles such as `Default`, `Player2`, or different environments.

```csharp
using UnityEssentials;

public class GraphicsBoot : MonoBehaviour
{
    public static readonly SettingsProfileManager<GraphicsSettings> GraphicsManager =
        SettingsProfileFactory.CreateManager("GraphicsManager", () => new GraphicsSettings());

    private void Awake()
    {
        // Switch to a named profile and load it
        GraphicsManager.SetCurrentProfile("Player2", loadIfNeeded: true);

        var current = GraphicsManager.GetCurrentProfile();
        var g = current.GetOrLoad();

        // Mutate and save later
        current.Mutate(s => s.msaa = g.msaa, notify: false);
    }

    private void OnApplicationQuit()
    {
        GraphicsManager?.GetCurrentProfile().SaveIfDirty();
    }
}
```

### 4) Key/value style profiles
For very dynamic settings, use a key/value profile (see `GraphicsBoot` sample) instead of a fixed schema.

```csharp
using UnityEssentials;

public class GraphicsBoot : MonoBehaviour
{
    public static readonly SettingsProfile<KeyValuePair> GraphicsKvp =
        SettingsProfileFactory.Create("GraphicsKvp");

    private void UseKeyValueProfile()
    {
        var kv = GraphicsKvp.GetOrLoad();

        var windowMode  = kv.GetInt("window_mode", 3);
        var vSync       = kv.GetBool("v-sync");
        var masterVol   = kv.GetFloat("master_volume", 100f);

        GraphicsKvp.Mutate(s =>
        {
            s.SetInt("window_mode", windowMode);
            s.SetBool("v-sync", vSync);
            s.SetFloat("master_volume", masterVol);
        }, notify: false);
    }
}
```

### 5) Persistence and saving
Settings profiles track a dirty flag. Use `SaveIfDirty()` to avoid redundant writes or `Save()` to force a write.

```csharp
// Mark profile dirty via Mutate or Set
Graphics.Mutate(g => g.msaa = 4);

// Later, typically on shutdown or at checkpoints
Graphics.SaveIfDirty();
```

## How It Works
- Data model
  - `SettingsProfile<T>` holds the current settings value, dirty/loaded flags, and a JSON file path
  - `SettingsEnvelope<T>` wraps the data with metadata (type name, profile name, schema version, updated timestamp)
- File layout
  - `SettingsPath.GetPath<T>(profile)` builds a path under a `Resources`-adjacent folder next to your project
  - File names are sanitized (invalid characters replaced, empty names mapped to `Default`)
- Loading
  - On `GetOrLoad`/`Load`, a default instance is created via the provided factory
  - If a JSON file exists, it is read and deserialized into a `SettingsEnvelope<T>`
  - If the envelope or data is missing or invalid, defaults are kept and the profile is marked dirty
- Validation and migration
  - After load or mutation, `ApplyValidationAndMigration` is run:
    - If `T` implements `ISettingsMigrate`, `TryMigrate(fromVersion)` is called with the previous schema version
    - If `T` implements `ISettingsValidate`, `Validate()` is called to clamp/normalize values
- Saving
  - `Save` serializes the current value into a `SettingsEnvelope<T>` using `SettingsJson` and writes it with `SettingsJsonStore.WriteAllTextAtomic`
  - `SaveIfDirty` writes only when changes have been made
- Deletion and reset
  - `ResetToDefaults` recreates the settings from the defaults factory, marks them dirty, and optionally saves immediately
  - `DeleteFile` removes the JSON file on disk and clears the loaded/dirty flags

## Notes and Limitations
- Threading
  - Intended for main-thread use; do not mutate settings from background threads
- Lifetime
  - `SettingsProfile<T>` instances are regular C# objects; you control when they are created and when to call `Save`/`SaveIfDirty`
  - Profiles do not auto-save; you must decide when to persist changes (e.g., on application quit or at checkpoints)
- Error handling
  - If a profile file is missing, invalid, or fails to deserialize, defaults are used and the profile is marked dirty
  - Migration failures are ignored beyond the settings type’s own logic; you can enforce stricter behavior in your implementation
- Scope
  - Files are written next to your project; this is not a cloud or remote config system
  - Key/value helpers are suitable for simple settings; prefer strongly-typed settings classes for complex data

## Files in This Package
- `Runtime/SettingsProfile.cs` – Core generic profile wrapper for strongly-typed settings
- `Runtime/SettingsEnvelope.cs` – Serializable envelope used for JSON storage with metadata
- `Runtime/SettingsPath.cs` – Utility for building safe file paths for profile JSON files
- `Runtime/UnityEssentials.SettingsProfile.asmdef` – Runtime assembly definition
- `Samples~/GraphicsBoot.cs` – Example usage of typed, manager, and key/value settings profiles
- `Editor/UnityEssentials.SettingsProfile.Editor.asmdef` – Editor assembly definition (for future/editor utilities)

## Tags
unity, settings, configuration, profiles, json, scriptable, runtime, environment, graphics, players
