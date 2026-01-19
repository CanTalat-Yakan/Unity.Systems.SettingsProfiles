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

> Quick overview: Lightweight, versioned settings profiles backed by JSON files. Use `SettingsProfile<T>` for strongly-typed settings, or `SettingsProfile`/`SettingsProfileBase` for flexible key/value storage.

This module provides a small settings system built on top of plain C# types.

- **Typed profiles**: `SettingsProfile<T>` wraps a settings object, loads/saves JSON, and supports optional schema versioning, validation, and migration via the interfaces below.
- **Key/value profiles**: `SettingsProfile` is a convenience wrapper around `SettingsProfile<SettingsProfileBase>`, where `SettingsProfileBase` is a `SerializedDictionary<string, JToken>` with PlayerPrefs-like extension helpers.
- **Profile managers**: `SettingsProfileManager<T>` and `SettingsProfileManager` help you keep multiple named profiles and switch a “current” profile.

## Features

- Strongly-typed profiles (`SettingsProfile<T>`)
  - Lazy load via `Value` (read intent) or `GetValue()` (write intent)
  - Manual persistence via `Save()` / `SaveIfDirty()`
  - Reset and delete helpers: `ResetToDefaults(...)`, `DeleteFile()`
- JSON persistence
  - Each profile is stored as a `*.json` file
  - Writes are atomic via `SettingsJsonStore.WriteAllTextAtomic`
- Change notifications
  - `OnChanged` event fires when the profile is changed via `GetValue(...)` (or when a dictionary value changes)
- Versioning, validation, migration (optional)
  - `ISettingsVersioned` for `SchemaVersion`
  - `ISettingsValidate` for clamping/normalization
  - `ISettingsMigrate` for upgrades from older schema versions
- Multiple profiles (pattern)
  - `SettingsProfileManager<T>` / `SettingsProfileManager` choose and cache named profiles and track a `CurrentProfileName`
- Key/value style settings
  - For `SettingsProfile` / `SettingsProfileBase` you can store arbitrary values by string key
  - Helpers: `GetInt`, `GetBool`, `GetFloat`, `GetString`, `SetInt`, `SetBool`, `SetFloat`, `SetString`, etc.

## Requirements

- Unity 6000.0+
- Runtime module; no external package dependencies beyond what ships with the repo
- For typed profiles: a settings type `T` with a public parameterless constructor (`where T : new()`)
- Write access to the project folder (profiles are written to disk)

## Core Concepts / API Contract

- **`Value` (read-intent)**
  - Ensures the profile is loaded.
  - For reference types, avoid mutating from `Value` directly unless the type emits change notifications (like `SerializedDictionary`).
- **`GetValue(markDirty = true, notify = true)` (write-intent)**
  - Ensures the profile is loaded.
  - Marks the profile dirty and can invoke `OnChanged` immediately.
  - Use this for typed settings objects where mutations can’t be detected automatically.
- **Dirty flag**
  - `IsDirty` is set when you write via `GetValue()`.
  - For key/value profiles, `IsDirty` is also set automatically when the underlying dictionary changes.

## Usage

### 1) Define a settings type (optional versioning/migration/validation)

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
            anisotropicFiltering = 8;
            return true;
        }
        return false;
    }
}
```

### 2) Create and use a typed profile

Use a static `SettingsProfile<T>` for a single named JSON-backed settings object.

```csharp
using UnityEngine;
using UnityEssentials;

public class GraphicsBoot : MonoBehaviour
{
    public static readonly SettingsProfile<GraphicsSettings> Graphics =
        SettingsProfileFactory.Create("Graphics", () => new GraphicsSettings());

    private void Awake()
    {
        // Read (loads on demand)
        Apply(Graphics.Value);

        // React to changes (triggered by GetValue(...) calls)
        Graphics.OnChanged += Apply;

        // Write-intent: mark dirty + notify
        Graphics.GetValue().msaa = 4;
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

### 3) Manage multiple named typed profiles with a manager

Use `SettingsProfileManager<T>` for cases like per-player or per-environment settings.

```csharp
using UnityEngine;
using UnityEssentials;

public class GraphicsBoot : MonoBehaviour
{
    public static readonly SettingsProfileManager<GraphicsSettings> GraphicsManager =
        SettingsProfileFactory.CreateManager("GraphicsManager", () => new GraphicsSettings());

    private void Awake()
    {
        GraphicsManager.SetCurrentProfile("Player2", loadIfNeeded: true);
        var profile = GraphicsManager.GetCurrentProfile();

        // Read
        var msaa = profile.Value.msaa;

        // Write-intent
        profile.GetValue().msaa = msaa;

        profile.OnChanged += g => QualitySettings.antiAliasing = g.msaa;
    }

    private void OnApplicationQuit()
    {
        GraphicsManager?.GetCurrentProfile().SaveIfDirty();
    }
}
```

### 4) Key/value style profiles (flexible storage)

If you don’t want a fixed schema, use `SettingsProfile` (dictionary-based). Under the hood it uses a `SerializedDictionary<string, JToken>`.

```csharp
using UnityEngine;
using UnityEssentials;

public class GraphicsBoot : MonoBehaviour
{
    public static readonly SettingsProfile GraphicsDict =
        SettingsProfileFactory.Create("GraphicsDict");

    private void Awake()
    {
        var profile = GraphicsDict;

        // Read
        var windowMode = profile.Value.Get("window_mode", 3);
        var vSync = profile.Value.Get("v-sync", false);
        var masterVolume = profile.Value.Get("master_volume", 100f);

        // Write
        profile.Value.SetInt("window_mode", windowMode);
        profile.Value.SetBool("v-sync", vSync);
        profile.Value.SetFloat("master_volume", masterVolume);

        // Fine-grained: listen for changes on a specific key
        profile.Value.OnValueChanged += key =>
        {
            if (key == "window_mode")
                ApplyWindowMode(profile.Value.Get(key, 0));
        };
    }

    private static void ApplyWindowMode(int mode)
    {
        // Apply window mode logic here
    }
}
```

### 5) Key/value profile manager

```csharp
using UnityEssentials;

public class GraphicsBoot
{
    public static readonly SettingsProfileManager GraphicsDictManager =
        SettingsProfileFactory.CreateManager("GraphicsDictManager");

    private void UseValueProfileManager()
    {
        GraphicsDictManager.SetCurrentProfile("Player2", loadIfNeeded: true);
        var profile = GraphicsDictManager.GetCurrentProfile();

        var volume = profile.Value.GetFloat("master_volume", 80f);
        profile.Value.SetFloat("master_volume", volume);
    }
}
```

## Persistence / File Location

Profiles are stored as JSON files at:

- `Path.Combine(Application.dataPath, "..", "Resources", "{ProfileName}.json")`

This means profiles are created in a project-level `Resources` folder next to your `Assets` folder.

> Note: this is **project-folder storage**, not `Application.persistentDataPath`. It’s intended for editor/dev workflows and for cases where the project directory is writable.

## How Versioning / Migration Works

- On **load**:
  - Defaults are created.
  - If a file exists, it’s deserialized as a `SettingsEnvelope<T>`.
  - If `T : ISettingsMigrate` and the file provides a `schemaVersion`, `TryMigrate(fromVersion)` is called.
  - If `T : ISettingsValidate`, `Validate()` is called.
- On **save**:
  - If `T : ISettingsVersioned`, the current `SchemaVersion` is written into the envelope.

Envelope fields:
- `type`: `typeof(T).Name`
- `profile`: profile name
- `schemaVersion`: integer schema version (0 if not versioned)
- `updatedUtc`: ISO-8601 timestamp (`DateTime.UtcNow.ToString("O")`)
- `data`: the settings object

## Notes / Gotchas

- Profiles don’t auto-save. Call `Save()` or `SaveIfDirty()` at a time you control (often `OnApplicationQuit`).
- For typed profiles, `OnChanged` is triggered by `GetValue(...)` and by `ResetToDefaults(...)` when `notify = true`.
- For key/value profiles, `IsDirty` / `OnChanged` are also updated when the underlying dictionary emits `OnValueChanged`.
- `ISettingsValidate.Validate()` may throw to signal unrecoverable invalid state.

## Files in This Package

- `Runtime/SettingsProfile.cs` – Core profile wrapper (`SettingsProfile<T>` + `SettingsProfile` convenience type)
- `Runtime/SettingsProfileManager.cs` – Managers for switching between named profiles
- `Runtime/SettingsProfileFactory.cs` – Central creation helpers
- `Runtime/SettingsEnvelope.cs` – JSON envelope with metadata
- `Runtime/SettingsPath.cs` – Path builder (`Assets/../Resources/{Profile}.json`)
- `Runtime/SettingsJson.cs` – Shared Json.NET settings for profiles
- `Runtime/SettingsJsonStore.cs` – Atomic file IO utilities
- `Samples/GraphicsBoot.cs` – Example usage (typed, managers, and key/value)

## Tags

unity, settings, configuration, profiles, json, runtime, environment
