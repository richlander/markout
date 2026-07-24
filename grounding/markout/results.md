# Markout CT-24 — grounding evidence (graded-yield model)

What the skill shelf buys a consuming agent, measured on the [CT-24 workflow ladder](eval.yaml):
24 bare-fixture scenarios (6 basics + 18 domain, four difficulty rounds) where the agent authors
only the Markout rendering. Prompts describe the library functionally and never name it, so skill
discovery is organic. Each scenario is run **k=5** times per arm; every run is graded independently
on the **Fails → Satisfies → Delivers** ladder, so the unit of evidence is a *yield* (`K/k` delivered
runs), not a single pass/fail.

- **Harness:** `richlander/dotnet-package-grounding@6b7e6ea` + `skill-validator@cb9e32a` (per-run
  capture; graded-yield quality card via `analyze --view ladder` — beta-binomial posterior + nested
  bootstrap bands on both per-dollar IET and per-day duration, plus the ≥20% economic gate).
- **Package:** Markout `0.23.0`; shelf `skills/markout-consumer@f14fec3` (MVV-free).
- **Arms:** `baseline` (no grounding) → `SKILL.md` (the shelf, agent self-selects). **Eval mode:**
  holistic (baseline vs plugin; isolated arm skipped). IET model `anthropic`.
- **Judge:** `claude-haiku-4.5`. **Runs:** n=5 per scenario. **Models:** three, novice → expert.
- **Methodology:** the ratified two-axis quality card — see
  [`quality-card-model.md`](https://github.com/richlander/dotnet-package-skills/blob/main/docs/quality-card-model.md)
  (the "why") and [`quality-card-spec.md`](https://github.com/richlander/dotnet-package-skills/blob/main/docs/quality-card-spec.md)
  (row-level reference). Only **verifiable requirements** are graded (does it use the taught API /
  approach, hit the technical constraints, and functionally work); subjective quality/idiom is out of
  scope — not gated, not reported.

## The quality card — baseline → `SKILL.md`, three models

Two independent axes — **return** (how often it delivers, scored over *all* runs) and
**efficiency** (the price *and* speed of a delivery, scored over *delivered* runs only) — plus two
gates: do-no-harm and a ≥20% economic-materiality premium. Bands are 95% credible intervals from
one seeded, finite-suite bootstrap (24 tasks held fixed, runs redrawn; beta-binomial posterior on
yield, joint lognormal cost+duration redraw; `S*` recomputed per iteration). Directional goal:
**↑** higher is better · **↓** lower is better.

| quantity (goal) | `claude-haiku-4.5` (mini) | `claude-sonnet-5` (mid) | `claude-opus-4.8` (frontier) |
| --- | ---: | ---: | ---: |
| **Coverage** — both-productive `S` (·) | 19 | 23 | 24 |
| ↳ grounded-only unlocks (↑) | **5** | 1 | 0 |
| ↳ baseline-only regressions (↓) | 0 | 0 | 0 |
| **Axis 1 — return (all runs).** mean yield `P` (↑) | 0.533 → 0.942 | 0.775 → 1.000 | 0.883 → 1.000 |
| ↳ ΔP\|both, C2 reliability [95% CrI] (↑) | +0.263 [+0.106, +0.307] | +0.191 [+0.052, +0.216] | +0.117 [−0.007, +0.144] |
| ↳ prior robustness (uniform vs Jeffreys) | robust (both exclude 0) | robust (both exclude 0) | ⚠ prior-sensitive |
| **Axis 2 — efficiency (delivered-only).** per-$ geo-mean IET `Lᵍ/Lᵇ` [95% CrI] (↓, **gate**) | ×0.20 [0.18, 0.33] | ×0.26 [0.23, 0.35] | ×0.40 [0.35, 0.52] |
| ↳ pooled `ΣLᵍ/ΣLᵇ` (Simpson guard) (↓) | ×0.11 | ×0.23 | ×0.32 |
| ↳ per-day geo-mean duration `Lᵍ/Lᵇ` [95% CrI] (↓, co-headline) | ×0.28 [0.26, 0.38] | ×0.21 [0.18, 0.26] | ×0.38 [0.33, 0.44] |
| ↳ Total on `S` — IET / duration (↓) | −75% / −77% | −64% / −79% | −56% / −71% |
| **Economic gate** — per-$ CrI upper ≤ ×0.80 (certified ≥20% cut) | ×0.33 ✅ | ×0.35 ✅ | ×0.52 ✅ |
| **C5** predictability `σ_g/σ_b` (↓) | 0.48 | 0.59 | 0.45 |
| **Do-no-harm gate** — loss mass / null-95 (↓) | 0.000 / 3.200 | 0.000 / 2.200 | 0.000 / 1.200 |
| **verdict** | both gates · win | both gates · win | both gates · win |

(C4 fidelity — the Delivers-vs-Satisfies rate — is **1.00 by construction** in this cut: Stage 1 uses
`Delivers ≡ Satisfies` as a labelled proxy until the delivers-tier assertions land. It is *not yet
independently measured*.)

## Reading the card — grounding buys more as capability falls

The three models trace a clean **monotone** curve: the weaker the model, the more grounding buys.

- **haiku (mini)** — the largest win. Grounding **unlocks 5 tasks the baseline never delivers** (C1
  capability), lifts reliability on the shared work by **+0.26 [robust under both priors]**, and cuts
  the typical delivery to **×0.20 per-dollar** and **×0.28 per-day**. Every axis moves, and the
  reliability gain is real, not a prior artifact.
- **sonnet (mid)** — reliability **and** efficiency both certified: **+0.19 [robust]**, **×0.26
  per-dollar / ×0.21 per-day**, with one capability unlock. The middle of the ladder on every measure.
- **opus (frontier)** — the baseline is already near the ceiling, so there is little reliability
  headroom: ΔP\|both is **+0.12 but its band brushes zero** and flips sign-significance between the
  uniform and Jeffreys priors (⚠ prior-sensitive). The win here is unambiguously on **efficiency** — a
  delivery is **×0.40 the price and ×0.38 the time** — not on reliability.

This is the predicted **frontier-cost / mini-capability asymmetry**, now band-certified. All three
clear **both gates** — do-no-harm (zero loss mass, well under the null-calibrated threshold) and the
≥20% economic-materiality premium (per-dollar CrI upper bound ≤ ×0.80 on every model) — and every known
bias in the certified path — the `K ≥ 1` winner's curse, the uniform prior's asymmetric shrinkage —
runs *against* grounding. The result survives all of them.

> **On "verdict."** This model has no binary 100%-correct gate. It has **two gates**: **do no harm**
> (no material baseline-only regression) and **economic materiality** (a certified ≥20% per-dollar cut —
> the minimum premium that pays for authoring plus ongoing drift maintenance). Duration co-headlines
> but does not gate. Beyond the gates, the card reports a *graded* two-axis win. A model delivering
> 0.94 yield with five unlocks and both gates clean is a strong win, not a "fail."

---

## Supporting signal — archaeology (the mechanism, not the verdict)

Archaeology is what the agent does when it **lacks** grounding: decompiling the restored package from
the NuGet cache and web-searching the API. It is not a graded axis — it is the **mechanism behind the
cost win** (a decomposition of Work-IET into "digging" vs "producing") and corroboration of the
Delivers *via*-test (heavy digging is the fingerprint of hand-rolling around the taught surface).
Grounding drives it toward zero on every model:

| signal (goal) | `claude-haiku-4.5` | `claude-sonnet-5` | `claude-opus-4.8` |
| --- | ---: | ---: | ---: |
| archaeology ops: cache / nuget.org (↓) | 85 / 19 → **0 / 0** | 124 / 1 → **2 / 0** | 104 / 2 → **1 / 0** |
| tool calls: web / bash / other (·) | 36/486/308 → 0/48/206 | 10/359/212 → 0/55/135 | 6/218/251 → 0/42/184 |
| session turns (↓) | 30 → 9 | 23 → 8 | 15 → 7 |
| wall-clock, raw end-to-end (↓, context) | 158s → 46s (−71%) | 219s → 45s (−79%) | 193s → 56s (−71%) |
| tasks correct, binary (↑, context) | 13/24 → 23/24 | 20/24 → 24/24 | 20/24 → 24/24 |
| func assertions passed (↑, context) | 110/126 → 125/126 | 122/126 → 126/126 | 122/126 → 126/126 |

The entry fee is the shelf load — ~1.9k tokens of grounding-doc IET per run (a per-trip **toll**, since
the harness runs a fresh session per task); it is already inside every cost number above.

> **On wall-clock, two cuts.** The card's **per-day duration** is the normative speed metric: a
> *delivered-only, paired geo-mean ratio* on one fixed host, so the machine constant cancels and it
> earns a band and a co-headline. The row above is the *raw end-to-end mean* (it includes
> `dotnet build`/restore wait and parallelism), kept as **context** — directionally identical but
> host- and load-dependent, so it is not banded. IET remains the machine-independent price and the
> singular economic gate; all numbers were measured on one host in the same run.

## Supporting signal — cost vs difficulty (the LIET view)

The certified cost number is the geo-mean above; this chart is a **difficulty-resolved view of that
same cost axis** (per-rung levelized IET, baseline curve vs `SKILL.md` curve, difficulty = measured
baseline IET where baseline delivers). The gap between the curves is the grounding lift; it is not a
separate metric.

| `claude-haiku-4.5` | `claude-sonnet-5` | `claude-opus-4.8` |
| --- | --- | --- |
| ![LIET — haiku](charts/liet-haiku.svg) | ![LIET — sonnet](charts/liet-sonnet.svg) | ![LIET — opus](charts/liet-opus.svg) |

The archaeology companion (same x-axis, external-digging on y) makes the mechanism visual:

| `claude-haiku-4.5` | `claude-sonnet-5` | `claude-opus-4.8` |
| --- | --- | --- |
| ![Archaeology — haiku](charts/liet-haiku-arch.svg) | ![Archaeology — sonnet](charts/liet-sonnet-arch.svg) | ![Archaeology — opus](charts/liet-opus-arch.svg) |

## Skills pulled (self-select from shelf, ×scenarios)

- `claude-haiku-4.5` — markout×24 · output-formats×9 · conditional-composition×8 · composite-cells-cards×4 · built-in-shapes×3
- `claude-sonnet-5` — markout×24 · conditional-composition×10 · output-formats×7 · built-in-shapes×4 · composite-cells-cards×4
- `claude-opus-4.8` — markout×24 · conditional-composition×8 · output-formats×7 · composite-cells-cards×4 · built-in-shapes×3

The shelf is a compact base `markout` skill + **four** domain skills. Cross-model, every domain skill
clears the ×0–1 delete threshold on all three models.

## Caveats

- **Stage 1 proxy:** `Delivers ≡ Satisfies` (functional-pass) until the delivers-tier assertions land,
  so C4 fidelity is 1.00 by construction and the *via*-fidelity is corroborated by archaeology→0, not
  yet independently graded.
- **Baseline self-grounds:** even ungrounded, the baseline reads the README/AGENTS packed in the
  restored nupkg and the open web, so its archaeology counts are a **lower bound** — grounding's edge
  is understated. A hermetic (Docker) clean baseline is the follow-up.
- **Bands are finite-suite** (this 24-task suite, tasks held fixed): the verdict generalizes to *this*
  suite's task mix. Bands are seeded and deterministic; a task-population read (outer task redraw) is a
  wider sensitivity bound, not the confirmatory estimand.
