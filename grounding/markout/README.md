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
harm no model. Numbers below are the isolated-arm, n=3 runs under price-weighted IET
(`input + 0.1·cache + 5·output`).

## What AGENTS.md protects (vs. baseline)

Markout is a niche, source-generated serializer: its API *looks* like System.Text.Json source-gen
but has **no reflection fallback**, so every `Serialize` call needs a `MarkoutSerializerContext`.
Ungrounded, an agent hallucinates `Json*`-style or context-less calls that don't compile, then digs
through the NuGet cache and the web to recover. AGENTS.md supplies exactly the missing pattern, so
the agent compiles first try.

Core-6, baseline → AGENTS.md (~958-tok doc):

| | mini (haiku-4.5) | frontier (opus-4.8) |
| --- | --- | --- |
| tasks correct | 5/6 → **6/6** | 6/6 → 6/6 |
| func assertions | 19/20 → **20/20** | 20/20 → 20/20 |
| archaeology | 50 → **29** | 48 → **21** |
| cost | **−40%** | **−25%** |
| verdict | **BETTER** | **BETTER** |

Against baseline, AGENTS.md is BETTER on both tiers: it fixes the lone mini-tier failure, roughly
halves archaeology, and cuts cost 25–40% by removing the failed-compile-and-search loop.

## What AGENTS.md earns over README.md (its real competition)

The baseline is a low bar; the *Brochure* is the real one — if AGENTS.md can't beat a package's own
README, why maintain two docs? Head-to-head on Core-6 (baseline removed):

- **Mini — BETTER.** Same 6/6 correctness, **−37% work-IET / −31% output** vs the README. The terse
  gap-fill pays its way on the cheap model.
- **Frontier — not ahead; the head-to-head grades WORSE on cost (functionally tied).** Same 6/6, but
  AGENTS.md induced *more* work than the README (archaeology 21 vs ~10, **+35% work-IET**). Opus
  already knows Markout's basics, so on Core-6 the README is the more efficient path and the extra
  always-on prose is mildly counterproductive.

Read: **AGENTS.md's Core-6 premium is a mini-tier phenomenon.** On the frontier its value has to come
from *depth* — gaps the model still has — which is what MM-12 tests.

## Generalization (MM-12) and the ceiling (CT-24)

- **MM-12 — AGENTS.md generalizes.** The gap-fill content, authored against Core-6, carries to the +6
  harder tasks at **12/12** — evidence the tiers share connective tissue and the doc isn't overfit to
  the basic six. (A 6/12 here would have signalled Core-6 overfit or a domain jump between rungs.)
- **CT-24 — the Textbook is the ceiling; AGENTS.md deliberately stops short.** `SKILL.md` nails
  **24/24 on frontier**. AGENTS.md does *not* carry the advanced-12 content — we compressed that out
  on purpose (it's ~958 tok paid on every call) — so it falls off on CT-24 **by design**. That falloff
  is a *clean tier boundary, not overfit*: the depth is there when you opt into the Textbook, while
  the always-on doc stays lean. The CT-24 generalization signal is one you act on or not; here we
  chose "not," and left depth to `SKILL.md`.

## Bottom line

AGENTS.md protects every agent from Markout's compile-or-hallucinate trap (BETTER vs baseline on both
tiers), earns a real premium over the README on the mini tier and at MM-12, and stays deliberately
lean — ceding the advanced tier to the opt-in `SKILL.md` rather than bloating the always-on path.
