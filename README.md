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

> Quick overview: Lightweight, versioned settings profiles backed by JSON files. Use `SettingsProfile<T>` for strongly-typed settings, or `SettingsProfile` for flexible key/value storage.

This module provides a small settings system built on top of plain C# types.

- **Typed profiles**: `SettingsProfile<T>` wraps a settings object, loads/saves JSON, and supports optional schema versioning, validation, and migration.
- **Key/value profiles**: `SettingsProfile` is a convenience wrapper around `SettingsProfile<SettingsProfileBase>`, where `SettingsProfileBase` is a `SerializedDictionary<string, JToken>` (JSON-friendly flexible storage).
- **Managers**: `SettingsProfileManager<T>` and `SettingsProfileManager` help you keep multiple named profiles and switch a “current” profile.

Persistence is implemented via the shared Serializer package:

- `SerializerEnvelope<T>` for the on-disk envelope
- `SerializerJson` for Json.NET settings
- `SerializerJsonStore` for atomic file IO
- `SerializerUtility.GetPath<T>(name)` for the target file path

## Features

- Strongly-typed profiles (`SettingsProfile<T>`)
  - Lazy load via `Value` (read intent)
  - Write intent via `GetValue(markDirty: true, notify: true)`
  - Manual persistence via `Save()` / `SaveIfDirty()`
  - Reset and delete helpers: `ResetToDefaults(...)`, `DeleteFile()`
- JSON persistence
  - Each profile is stored as a `*.json` file with an envelope (`SerializerEnvelope<T>`)
  - Writes are atomic (`SerializerJsonStore.WriteAllTextAtomic`)
- Change notifications
  - `OnChanged` fires when you call `GetValue(..., notify: true)` and when the underlying dictionary changes
- Versioning, validation, migration (optional)
  - `ISettingsVersioned` exposes `SchemaVersion`
  - `ISettingsValidate` provides `Validate()` for clamping/normalization
  - `ISettingsMigrate` provides `TryMigrate(fromVersion)` for upgrades
- Multiple profiles
  - `SettingsProfileManager<T>` / `SettingsProfileManager` track `CurrentProfileName` and cache profiles by name

## Requirements

- Unity 6000.0+
- Runtime module
- For typed profiles: a settings type `T` with a public parameterless constructor (`where T : new()`)
- Write access to the project folder (profiles are written to disk)

## Core Concepts / API Contract

- **`Value` (read-intent)**
  - Ensures the profile is loaded.
  - For reference types, avoid mutating from `Value` directly unless the type emits change notifications (like `SerializedDictionary`).
- **`GetValue(markDirty = true, notify = true)` (write-intent)**
  - Ensures the profile is loaded.
  - Marks the profile dirty immediately.
  - Can invoke `OnChanged` immediately.
- **Dirty flag**
  - `IsDirty` is set when you write via `GetValue()`.
  - If the underlying value is a `SerializedDictionary<string, ...>`, `IsDirty` is also set automatically on dictionary changes.

## Usage

### 1) Define a settings type (optional versioning/migration/validation)

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

```csharp
using UnityEngine;
using UnityEssentials;

public class GraphicsBoot : MonoBehaviour
{
    public static readonly SettingsProfile<GraphicsSettings> Graphics =
        SettingsProfile<GraphicsSettings>.GetOrCreate("Graphics");

    private void Awake()
    {
        // Read (loads on demand)
        Apply(Graphics.Value);

        // React to explicit changes (GetValue(..., notify:true), ResetToDefaults(..., notify:true))
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

### 3) Manage multiple named typed profiles

```csharp
using UnityEngine;
using UnityEssentials;

public class GraphicsBoot : MonoBehaviour
{
    public static readonly SettingsProfileManager<GraphicsSettings> GraphicsManager =
        SettingsProfileManager<GraphicsSettings>.GetOrCreate("GraphicsManager");

    private void Awake()
    {
        GraphicsManager.SetCurrentProfile("Player2");
        var profile = GraphicsManager.GetCurrentProfile();

        // Read
        var msaa = profile.Value.msaa;

        // Write-intent
        profile.GetValue().msaa = msaa;

        profile.OnChanged += g => QualitySettings.antiAliasing = g.msaa;
    }

    private void OnApplicationQuit()
    {
        GraphicsManager.GetCurrentProfile().SaveIfDirty();
    }
}
```

### 4) Key/value style profiles (flexible storage)

```csharp
using UnityEngine;
using UnityEssentials;

public class GraphicsBoot : MonoBehaviour
{
    public static readonly SettingsProfile GraphicsDict =
        SettingsProfile.GetOrCreate("GraphicsDict");

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
    }
}
```

### 5) Key/value profile manager

```csharp
using UnityEssentials;

public class GraphicsBoot
{
    public static readonly SettingsProfileManager GraphicsDictManager =
        SettingsProfileManager.GetOrCreate("GraphicsDictManager");

    private void UseValueProfileManager()
    {
        GraphicsDictManager.SetCurrentProfile("Player2", load: true);
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
  - If a file exists, it’s deserialized as a `SerializerEnvelope<T>`.
  - If `T : ISettingsMigrate`, `TryMigrate(fromVersion)` is called.
  - If `T : ISettingsValidate`, `Validate()` is called.
- On **save**:
  - If `T : ISettingsVersioned`, the current `SchemaVersion` is written into the envelope.

Envelope fields:
- `Type`: `typeof(T).Name`
- `Name`: profile name
- `SchemaVersion`: integer schema version (0 if not versioned)
- `UpdatedUtc`: ISO-8601 timestamp (`DateTime.UtcNow.ToString("O")`)
- `Values`: the settings object

## Notes / Gotchas

- Profiles don’t auto-save. Call `Save()` or `SaveIfDirty()` at a time you control (often `OnApplicationQuit`).
- For typed profiles, `OnChanged` is triggered by `GetValue(..., notify:true)` and by `ResetToDefaults(..., notify:true)`.
- `ISettingsValidate.Validate()` may throw to signal unrecoverable invalid state.

## Files in This Package

- `Runtime/SettingsProfile.cs` – Core profile wrapper (`SettingsProfile<T>` + `SettingsProfile` convenience type)
- `Runtime/SettingsProfileManager.cs` – Managers for switching between named profiles
- `Runtime/SettingsProfileRegistry.cs` – Shared cache for profile instances
- `Runtime/SettingsContracts.cs` – `ISettingsVersioned`, `ISettingsValidate`, `ISettingsMigrate`

## Tags

unity, settings, configuration, profiles, json, runtime, environment
