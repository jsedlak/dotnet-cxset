# Cxset

A .NET CLI tool for managing versions and changelogs across multiple projects in a repository.

## Installation

### From NuGet

```bash
dotnet tool install --global Cxset
```

### Uninstall

```bash
dotnet tool uninstall --global Cxset
```

## Usage

### Adding a Changeset

```bash
dnx cxset add
```

This will:

1. Scan for all `.csproj` files and warn about any that are missing a `<Version>` element
2. Present an interactive multi-select prompt with an **All Projects** group at the top — toggle it to select every project at once, or expand and pick individual projects
3. Ask for the change type: patch, minor, or major
4. Ask for a description of the changes (enter an empty line to finish)

A changeset file is created in `.changes/` with the following format:

```markdown
---
changeset: minor
timestamp: 2026-02-04T15:00:00Z
projects:
  - src/MyProject/MyProject.csproj
  - src/MyLibrary/MyLibrary.csproj
---

Added new feature X
Fixed bug Y
```

### Explaining Project Status

```bash
dnx cxset explain
```

Prints a table summarizing every `.csproj` discovered in the repository:

| Version | Versioned | Packable | Project          | Path         |
| ------- | --------- | -------- | ---------------- | ------------ |
| 1.2.0   | ✔         | ✔        | MyLib.csproj     | src/MyLib    |
| -       | ❌        | ✔        | MyOther.csproj   | src/MyOther  |
| -       | ❌        | ❌        | Tests.csproj     | tests        |

- **Version** — the current `<Version>` value, or `-` if missing
- **Versioned** — whether the project has a `<Version>` element
- **Packable** — whether `<IsPackable>true</IsPackable>` or `<PackAsTool>true</PackAsTool>` is set in the `.csproj` or an ancestor `Directory.Build.props`

### Validating Projects

```bash
dnx cxset validate
```

Checks that every packable project has a `<Version>` element. A project is considered packable if `<IsPackable>true</IsPackable>` or `<PackAsTool>true</PackAsTool>` is set in the `.csproj` itself or in a `Directory.Build.props` file in any ancestor directory. Casing of `true` does not matter.

- ✔ (green) — packable and has a `<Version>` element
- ❌ (red) — packable but **missing** a `<Version>` element
- `-` (grey) — not packable, skipped

Returns exit code `1` if any project fails validation, making it suitable for CI pipelines.

### Publishing Changesets

```bash
dnx cxset publish
```

This will:

1. Read all pending changesets from `.changes/`
2. Determine the version bump based on the largest change type (major > minor > patch)
3. Bump the version (stored in `.changes/.version`, starting from `0.0.0`)
4. Update the `<Version>` element in each affected `.csproj` file
5. Append entries to `CHANGELOG.md` in each affected project's directory
6. Delete the processed changeset files

### Version Bumping

| Change Type | Example           |
| ----------- | ----------------- |
| patch       | `1.2.3` → `1.2.4` |
| minor       | `1.2.3` → `1.3.0` |
| major       | `1.2.3` → `2.0.0` |

When multiple changesets exist, the largest change type wins.

### Version

```bash
dnx cxset version
```

Prints the current tool version.

## Project Structure

For a project to be eligible for version management, it must have a `<Version>` element in its `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>
```

Packable projects (`<IsPackable>true</IsPackable>` or `<PackAsTool>true</PackAsTool>`) that are missing a `<Version>` element will be flagged by `cxset validate` and warned about during `cxset add`. These properties are also detected in `Directory.Build.props` files in ancestor directories.

## Files

| Path                     | Description             |
| ------------------------ | ----------------------- |
| `.changes/*.md`          | Pending changeset files |
| `.changes/.version`      | Current version tracker |
| `{project}/CHANGELOG.md` | Per-project changelog   |

## Example Workflow

```bash
# Make some changes to ProjectA and ProjectB
git add .

# Record what changed
dnx cxset add
# Select: "All Projects" or pick individual projects
# Type: minor
# Description: Added user authentication

# Make more changes to just ProjectA
git add .

dnx cxset add
# Select: ProjectA only
# Type: patch
# Description: Fixed login bug

# When ready to release
dnx cxset publish
# Both projects get version bump (minor wins)
# ProjectA CHANGELOG has both entries
# ProjectB CHANGELOG has only the first entry
```

## Requirements

- .NET 10.0 SDK or later

## License

MIT
