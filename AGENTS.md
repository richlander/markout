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
- `skills/`: **consumer** skills for agents using the Markout package — invokable `SKILL.md` workflows (see below). NOT repo guidance — do not put maintainer instructions there.

## Package grounding (maintainer vs consumer)

Keep maintainer guidance and consumer grounding separate:

- **This file (`/AGENTS.md`)** is for **maintainers working on this repo** — build rules,
  branching, the repo map. It does not ship.
- **`skills/`** holds the **consumer** grounding for **agents using the Markout package** —
  the source-generated serializer API, attributes, the required `MarkoutSerializerContext`
  pattern, and the higher-value rendering workflows. These are authored as invokable
  `SKILL.md` skills (a brief base skill plus discrete domain-workflow skills).

This mirrors dotnet-inspect's maintainer/consumer split, and now the *delivery form* matches
too: both projects surface consumer grounding as invokable *skills* (`skills/<name>/SKILL.md`).
Markout previously shipped a packed `grounding/markout/AGENTS.md` at the nupkg root; that prose
doc has been retired in favor of skills, and the package ships no agent-facing *doc* of its own —
the human `README.md` remains the packaged readme.

### Shipping the shelf

The skill shelf **is** packed into `Markout.nupkg`, at `skills/<name>/` in the package root
(`src/Markout/Markout.csproj`). This is the *package skill* delivery route from
[dotnet-package-skills](https://github.com/richlander/dotnet-package-skills): the shelf restores
to `~/.nuget/packages/markout/<version>/skills/`, and a package-skill installer copies the skills
a consumer wants into their repo's `.github/skills/<name>/`, where they persist as ordinary
in-repo skills — checked in, reviewable, deletable. Packing the shelf pins the grounding to the
package version that produced it; it does not push anything into a consumer's context, because
installation stays an explicit, visible act.

Consequences for maintainers:

- **Every skill is named for the package.** The base skill is `markout`; every workflow skill is
  `markout-<domain>`. The directory name and the frontmatter `name` are the same string, and that
  string is the name used everywhere the skill is referred to. Skill names are a **flat, global
  namespace** in a consuming repo — this shelf installs next to every other package's, with no
  coordinating authority — so a bare `output-formats` is a name any other maintainer could also
  ship. Deriving from the package id inherits uniqueness from the NuGet id namespace instead of
  negotiating it. `dotnet pack` **fails** on a skill directory that is neither `markout` nor
  `markout-*`, or whose frontmatter `name` disagrees with its directory. See
  [authoring-principles.md](https://github.com/richlander/dotnet-package-skills/blob/main/docs/authoring-principles.md#naming-derive-every-skill-name-from-the-package-id).
- The `version:` stamp in every `skills/*/SKILL.md` and in `skills/plugin.json` tracks the
  **Markout package version**. `dotnet pack` **fails** if they disagree with `<Version>` in
  `Markout.csproj`, because the shipped shelf must not misstate which release it describes.
- `skills/` is globbed into the package, so **any file committed there ships**. Releases pack
  a clean CI checkout, so what is in git is exactly what consumers get. Keep it to skills.
- Only `Markout` carries the shelf. `Markout.Templates` and `MarkdownTable.Formatting` do not.

## XML documentation

The five shipping libraries — `Markout`, `Markout.Templates`, `MarkdownTable.Formatting`,
`MarkdownTable.Query`, and `Markout.Ansi.Spectre` — set `GenerateDocumentationFile`, so
`dotnet pack` puts `lib/<tfm>/<Assembly>.xml` next to the DLL and consumers get IntelliSense
and agent-readable API grounding from the package itself.

Combined with the repo-wide `TreatWarningsAsErrors`, this means **every new public member
needs a doc comment** — CS1591 is a build error, not a warning. Malformed comments and
broken `cref` targets (CS1570/CS1574/CS1734) fail the build too, so `<see cref="..."/>` must
point at something that actually resolves from the *referencing* project. `Markout` cannot
`cref` into `Markout.Templates`, for example, because the dependency runs the other way;
use `<c>...</c>` for those. Use `<inheritdoc/>` when an interface member already carries
the documentation.

Tests, samples, tools, and `Markout.Demo` are not packed and do not generate a doc file.

## Markdown Linting

All markdown files must pass `markdownlint` before committing. When there are lint errors, run the auto-fixer first:

```bash
npx markdownlint-cli --fix <file>
npx markdownlint-cli <file>
```

Run `markdownlint` on all changed markdown files as part of preparing a PR.
