# Orc.Extensibility

Orc.Extensibility is a library that provides classes to support pluggable components inside applications — including plugin discovery, loading, instantiation, and lifecycle management.

The library consists of one main package:

- **Orc.Extensibility** — Core library providing plugin discovery, factory, manager, and runtime assembly-resolver services.

---

## Critical Rules (Read First)

These rules are **non-negotiable**. Violating them causes broken builds, crashes, or downstream breakage.

### 1. Never Edit Generated Files

Files matching `*.generated.cs` are auto-generated.

- **NEVER** manually edit these files

### 2. ABI / API Stability

This project maintains stable ABI / API. Breaking changes break downstream apps.

| Allowed | Never |
|---------|-------|
| Add new overloads | Modify existing signatures |
| Add new methods | Remove public APIs |
| Add new classes | Change return types |

### 3. Tests Are Mandatory

**Building alone is NOT sufficient.** Run tests before claiming completion (see [Commands](#commands)).

### 4. Branch Protection (COMPLIANCE REQUIRED)

**Direct commits to protected branches are a policy violation.**

| Repository | Protected Branches |
|------------|-------------------|
| Orc.Extensibility | `master` |
| Orc.Extensibility | `develop` |

**Required workflow:**

1. **Create a feature branch FIRST** — Use naming convention: `feature/issue-NNNN-description`
2. **Make all commits on the feature branch** — Never commit directly to protected branches
3. **Submit a Pull Request** — Changes must be reviewed by a human before merging

```bash
# CORRECT — Always create a feature branch first
git checkout -b feature/issue-1234-fix-description

# NEVER DO THIS — Policy violation
git checkout develop && git commit  # FORBIDDEN

# NEVER DO THIS — Policy violation
git checkout master && git commit  # FORBIDDEN
```

The repository has protected branches that must be respected.

---

## Commands

Single source of truth for all commands:

| Task | Command |
|------|---------|
| **Build** | `dotnet cake --target=build` |
| **Test** | `dotnet cake --target=test` |
| **Build and test** | `dotnet cake --target=buildandtest` |

---

## Architecture & Directories

### Layer Overview

```
Orc.Extensibility  =>  Core plugin infrastructure (discovery, loading, lifecycle)
```

### Key Components

| Component | Purpose |
|-----------|---------|
| `IPluginManager` / `PluginManager` | Orchestrates plugin discovery and loading |
| `IPluginFactory` / `PluginFactory` | Creates plugin instances |
| `IPluginFinder` / `PluginFinderBase` | Locates plugins on disk or in memory |
| `IPluginInfoProvider` / `PluginInfoProvider` | Reads metadata from plugin assemblies |
| `IPluginLocationsProvider` / `PluginLocationsProvider` | Provides probe paths for plugin discovery |
| `ISinglePluginService` / `SinglePluginService` | Manages a single active plugin |
| `IMultiplePluginsService` / `MultiplePluginsService` | Manages multiple active plugins |
| `ILoadedPluginService` / `LoadedPluginService` | Tracks currently loaded plugins |
| `IPluginCleanupService` / `PluginCleanupService` | Handles plugin unloading and cleanup |
| `IRuntimeAssemblyResolverService` | Resolves assemblies at runtime (Costura / file-based) |
| `IAssemblyReflectionService` | Reflects over assemblies without fully loading them |

### Models

| Model | Description |
|-------|-------------|
| `Plugin` | Represents a loaded plugin instance |
| `PluginInfo` | Metadata for a discovered plugin |
| `PluginTypeInfo` | Type-level metadata for a plugin |
| `PluginLoadContext` | Isolated `AssemblyLoadContext` for a plugin |
| `PluginProbingContext` | Context used during the probing/discovery phase |
| `PluginProbingLocation` | A single file-system location to probe for plugins |
| `RuntimeAssembly` | Base type for a resolved runtime assembly |

### Directory Guide

| Directory | Editable? | Notes |
|-----------|-----------|-------|
| `*.generated.cs` | No | Leave as-is — auto-generated |
| `deployment/` | No | Build / deployment scripts |
| `src/Orc.Extensibility/` | Yes | Main library source |
| `src/Orc.Extensibility.Tests/` | Yes | NUnit test suite |
| `src/Orc.Extensibility.Example*/` | Yes | Example host and plugin assemblies |

---

## Writing Code

### Anti-Patterns (Never Do This)

| Anti-Pattern | Why |
|-------------|-----|
| Modifying method signatures | ABI breaking |
| Manual edits to `*.generated.cs` | Overwritten on regenerate |
| Using default parameters in public APIs | ABI breaking |
| **Skipping failing tests** | **Unacceptable — tests must pass** |

---

## Testing & Debugging

### Running Tests

```bash
dotnet cake --target=test
```

### Tests MUST Pass

> **NON-NEGOTIABLE:** Tests must PASS before claiming completion.
>
> - Do NOT skip failing tests
> - Do NOT claim completion if tests fail
> - Do NOT use `SkipException` to work around failures

### Writing Tests

1. Use NUnit to write tests
2. Create a `Facts` class per feature area
3. Combine Pascal / Snake case for test method names (e.g. `Feature_Does_Work`)

```csharp
[Test]
public void Feature_Does_Work()
{
    var result = 47 - 5;

    Assert.That(result, Is.EqualTo(42));
}
```

### Public API Stability Tests

The project uses `PublicApiGenerator` + `Verify` to detect accidental ABI breakage.  
After intentional public API changes, update the approved snapshot:

```
src/Orc.Extensibility.Tests/PublicApiFacts.Orc_Extensibility_HasNoBreakingChanges_Async.verified.txt
```

**Philosophy:** Tests FAIL when wrong, never skip (except missing hardware).

### Debugging Methodology

1. **Establish baseline** — What's the known-good state?
2. **One change at a time** — Verify each change before proceeding
3. **Track changes in a table** — Log what you changed and the result
4. **Platform differences are signals** — If X works and Y fails, the difference IS the answer
5. **Revert if worse** — Don't pile fixes on top of failures

---

## Further Reading

| Topic | Document |
|-------|---------|
| Contributing guidelines | [CONTRIBUTING.md](CONTRIBUTING.md) |
| Project README | [README.md](README.md) |
| WildGums open-source docs | https://opensource.wildgums.com |
