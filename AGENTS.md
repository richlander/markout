# Agent Instructions

## File-Based Apps

Do NOT use `dotnet-script`, `dotnet script`, `dotnet-fsi`, or `.csx` files. Always use file-based apps (new in .NET 10). Always prefer file-based apps over Python, unless a specific Python library is needed.

Run with `dotnet run /tmp/check.cs`. Write throwaway scripts to `/tmp/`.

Reference: <https://raw.githubusercontent.com/dotnet/docs/refs/heads/main/docs/core/sdk/file-based-apps.md>

### File-based app with project reference

```csharp
#:project ../src/MyLib/MyLib.csproj

using MyLib.Domain;

var items = await MyService.LoadAsync();
Console.WriteLine($"Found {items.Count} items");
```

## Branching

The `main` branch is protected. All work must be done on a feature branch.

Create feature branches with descriptive names, e.g.:
- `feature/issue-3-assembly-references`
- `fix/null-reference-in-parser`

## Repository map

Read this file first, then use the docs it points to:

- `README.md`: human entrypoint, quick start, formatter overview, and sample map.
- `docs/overview.md`: minimum architecture and agent context.
- `docs/user-guide.md`: user-facing feature reference.
- `docs/specification.md`: Markdown/Markout output syntax details.
- `docs/design/`: focused design notes; update current design docs when public behavior changes.
- `SKILL.md`: agent workflow guidance; keep it concise and current with formatter behavior.
- `grounding/markout/AGENTS.md`: the **consumer** grounding that ships in the NuGet package (see below). This is NOT repo guidance — do not put maintainer instructions there.

## Package grounding (two audiences, two files)

There are two distinct AGENTS.md files, for two different audiences. Keep them separate:

- **This file (`/AGENTS.md`)** is for **maintainers working on this repo** — build rules,
  branching, the repo map. It does not ship.
- **`grounding/markout/AGENTS.md`** is for **agents consuming the Markout package** — the
  source-generated serializer API, attributes, and the required `MarkoutSerializerContext`
  pattern. `src/Markout/Markout.csproj` packs it to the nupkg root, where
  `dotnet-inspect package Markout --readme` and the NuGet MCP resolve it ahead of the long
  human `README.md`. Keep it tight and API-focused; it is the package's agent-facing doc.

This mirrors dotnet-inspect's maintainer/consumer split. dotnet-inspect is a *tool*, so its
consumer doc is an invokable *skill* (`skills/dotnet-inspect/SKILL.md`, surfaced by
`dotnet-inspect skill`). Markout is a *library*: there is no command to surface, so its
consumer doc is *grounding* shipped as a file in the package — hence `grounding/` (not
`skills/`) and the name stays `AGENTS.md` so package-doc resolvers find it. Grounding is the
content; a skill is the invokable delivery form of it.

## Markdown Linting

All markdown files must pass `markdownlint` before committing. When there are lint errors, run the auto-fixer first:

```bash
npx markdownlint-cli --fix <file>
npx markdownlint-cli <file>
```

Run `markdownlint` on all changed markdown files as part of preparing a PR.
