# Markout grounding

This directory is the **grounding eval bundle** for Markout — the inputs that prove the
package's model-facing docs earn their keep. It ships **inputs only**:

- `AGENTS.md` — the *Missing Manual*: terse, model-only gap-fill, packed in the nupkg and
  always on. (Markout also ships this at the package root.)
- `SKILL.md` — the *Complete Textbook*: opt-in, the eval ceiling (see below). Not packed.
- `TASKS.md` / `eval.yaml` / `fixtures/` — the jobs-to-be-done, their machine form, and the
  starting projects each scenario builds and runs.
- `run.sh` / `run.ps1` — regenerate the results.

Engine, methodology, and the `grounding` CLI live in
**[richlander/dotnet-package-grounding](https://github.com/richlander/dotnet-package-grounding)**
— [build the tool](https://github.com/richlander/dotnet-package-grounding/blob/main/docs/getting-started.md#build--install-the-grounding-cli-from-source),
[full method spec](https://github.com/richlander/dotnet-package-grounding/blob/main/docs/overview.md).
Raw datasets are regenerable and live in a user cache, not here; **this summary is the durable
record** (the per-run quality cards go in the PR).

## How this is measured

Three documents, three tiers. Markdown files in a repo have different audiences — `README.md`
(*Brochure*, humans), `AGENTS.md` (*Missing Manual*, models, always-on), `SKILL.md` (*Textbook*,
models, opt-in). We run them head-to-head against a no-grounding **baseline** across a task ladder
that grows in *depth, not domain*:

- **Core-6** — the 6 most basic tasks.
- **MM-12** — +6 higher-value model-gap tasks (AGENTS.md's remit).
- **CT-24** — +12 advanced tasks (SKILL.md's remit).

The goal is [Pareto](https://en.wikipedia.org/wiki/Pareto_efficiency): help where a model has gaps,
harm no model. Numbers below are isolated-arm, **matched n=5** runs under price-weighted IET
(`input + 0.1·cache + 5·output`).

## What AGENTS.md protects (vs. baseline)

Markout is a niche, source-generated serializer: its API *looks* like System.Text.Json source-gen
but has **no reflection fallback**, so every `Serialize` call needs a `MarkoutSerializerContext`.
Ungrounded, an agent hallucinates `Json*`-style or context-less calls that don't compile, then digs
through the NuGet cache and the web to recover. AGENTS.md supplies exactly the missing pattern, so
the agent compiles first try.

Core-6, baseline → AGENTS.md (~1650-tok doc):

| | mini (haiku-4.5) | frontier (opus-4.8) |
| --- | --- | --- |
| tasks correct | 5/6 → 5/6 | 6/6 → 6/6 |
| func assertions | 19/20 → 19/20 | 20/20 → 20/20 |
| archaeology | 52 → **3** | 15 → **7** |
| output tok | 11963 → **4242** | 7724 → **4600** |
| cost | 12.95 → **4.28** (−67%) | 14.83 → **9.40** (−37%) |
| verdict | **BETTER** | **BETTER** |

Against baseline, AGENTS.md is BETTER on both tiers: it collapses archaeology (the failed-compile,
decompile-the-generator, search-the-web loop) by ~10× on mini and ~2× on frontier, and cuts cost
37–67%. The one mini task it doesn't lift (M3) fails identically *without* grounding — a residual
mini-model gap, not a regression.

## What AGENTS.md earns over README.md (its real competition)

The baseline is a low bar; the *Brochure* is the real one — if AGENTS.md can't beat a package's own
README, why maintain two docs? Head-to-head on Core-6 (baseline removed), AGENTS.md is **BETTER on
both tiers**: **+1 task** on each, **−30% cost** on mini, **−34% cost** on frontier, with lower
archaeology (mini 11→3, frontier 13→7). The terse, model-shaped gap-fill both answers more and costs
less than shipping the human README to the model.

**This size was found by eval, and the sizing matters.** An earlier, aggressively compressed ~950-tok
version of this doc lost that contest: on the frontier the model recovered the trimmed API by
*decompiling `Markout.SourceGeneration.dll`* (M3/M4/M6 archaeology), and the mini head-to-head graded
**WORSE than the README** (+53% cost). Adding back the Shape Library, the Renderers table, and the M6
attribute reference (`MarkoutLink` / `MarkoutValueMap` / `GroupBy`) — ~700 tokens — is what flips mini
from WORSE to BETTER and holds the frontier ahead. Over-compression is not free; the always-on doc has
to actually carry the facts the model would otherwise dig for.

## Generalization (MM-12) and the ceiling (CT-24)

- **MM-12 — AGENTS.md generalizes.** The gap-fill content, authored against Core-6, carries to the +6
  harder tasks at **12/12** — evidence the tiers share connective tissue and the doc isn't overfit to
  the basic six. (A 6/12 here would have signalled Core-6 overfit or a domain jump between rungs.)
- **CT-24 — the Textbook is the ceiling; AGENTS.md stops short by design.** `SKILL.md` nails
  **24/24 on frontier**. AGENTS.md carries the everyday shapes/attributes but *not* the full
  advanced-12 surface — that depth is `SKILL.md`'s remit, opt-in and unbounded by the always-on token
  budget. The falloff on CT-24 is a *clean tier boundary, not overfit*: everyday tasks are covered in
  the packed doc; the long tail is one `skill` invocation away.

## Bottom line

AGENTS.md protects every agent from Markout's compile-or-hallucinate trap (BETTER vs baseline on both
tiers), and — at the eval-tuned ~1650-token size — beats the package's own README on both tiers while
staying well short of the opt-in Textbook. Bigger is not always better, and neither is smaller: the
right size is the one the questions choose.
