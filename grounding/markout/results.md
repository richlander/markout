# Markout CT-24 — grounding evidence

What the skill shelf buys a consuming agent, measured on the [CT-24 workflow ladder](eval.yaml):
24 bare-fixture scenarios (6 basics + 18 domain, four difficulty rounds) where the agent authors
only the Markout rendering. Prompts describe the library functionally and never name it, so skill
discovery is organic.

- **Harness:** `richlander/dotnet-package-grounding@6814020` (two-axis verdict + IET decomposition, `analyze --view card`).
- **Arms:** `baseline` (no grounding) → `SKILL.md` (the shelf, agent self-selects). IET model `anthropic`.
- **Judge:** `claude-haiku-4.5`. **Runs:** n=5 per scenario (position-swapped).

## Results — baseline → `SKILL.md` (means across 24 scenarios)

| Metric (goal) | `claude-haiku-4.5` | `claude-opus-4.8` |
| --- | ---: | ---: |
| tasks correct (+) | 15/24 → **20/24** | 23/24 → **24/24** |
| relied on grounding: tasks (+) | 0/24 → **24/24** | 0/24 → **19/24** |
| relied on archaeology: cache / nuget.org (−) | 92 / 15 → **0 / 0** | 80 / 0 → **2 / 0** |
| unique skills used (of shelf) (context) | — → 6 | — → 5 |
| func passed (assertions) (+) | 115/126 → **122/126** | 125/126 → **126/126** |
| tool calls: web / bash / other (context) | 34/444/278 → **0**/70/244 | 1/201/190 → **0**/55/182 |
| grounding load (tok) (context) | 0 → 1912 | 0 → 1514 |
| output tok (% of IET) (−) | 9916 (28%) → **3579 (25%)** | 6024 (32%) → **2849 (25%)** |
| tool-call turns (% of total) (−) | 27 (95%) → **10 (88%)** | 12 (90%) → **6 (84%)** |
| Session turns (−) | 28 → **11** | 13 → **7** |
| Session wall-clock, end-to-end (−) | 150 → **49s (−67%)** | 150 → **56s (−63%)** |
| Total IET (−) | 172277 → **66491 (−61%)** | 92349 → **53446 (−42%)** |
| ↳ Grounding IET (doc) | 0 → 4210 | 0 → 2803 |
| ↳ Work IET (agent) (−) | 172277 → **62282** | 92349 → **50643** |
| **verdict** | **FAIL / BETTER** | **PASS / BETTER** |

> ⏳ **haiku column is the prior (MVV-inclusive) refresh** — the clean MVV-free re-run is in flight;
> the haiku numbers and histogram below will be refreshed when it lands. The **opus** column and chart
> are the clean 5-skill (MVV-deleted) shelf.

**Two axes** (independent). **Gate** (correctness): **PASS** = 100% of the tier correct, **FAIL** =
below the gate. **Efficiency**: **BETTER** = more tasks correct / archaeology→0 / work IET cut ≥20%;
a correctness regression forces WORSE (cheaper-but-wrong is never better); a doc can FAIL the gate
yet be BETTER on efficiency.

- **opus `PASS / BETTER`** — clears the 24/24 gate *and* is cheaper: web archaeology → **0**, Total IET **−42%**.
- **haiku `FAIL / BETTER`** — doesn't clear the gate (20/24; the gap is execution, not discovery —
  the skill activated **24/24**), but improves every efficiency axis: **+5 correct**, archaeology → **0**,
  turns 28→11, Total IET **−61%**.

> **On wall-clock:** end-to-end wall-clock (150→49s haiku, 150→56s opus) tracks the turn/IET drop but
> is machine- and load-dependent (it includes `dotnet build`/restore wait and parallelism), so it's an
> **informative** signal, not the normative gate — IET is the machine-independent cost metric. The
> numbers above were all measured on one host in the same run.

**Skills pulled** (self-select from shelf, ×scenarios):

- `claude-haiku-4.5` — markout×24 · conditional-composition×8 · output-formats×7 · built-in-shapes×4 · composite-cells-cards×4 · multi-view-verbosity×4 *(prior MVV-inclusive refresh; clean re-run in flight)*
- `claude-opus-4.8` — markout×19 · conditional-composition×8 · output-formats×7 · composite-cells-cards×4 · built-in-shapes×3

The shelf is now a compact base `markout` skill + **four** domain skills. On the 24-scenario ladder each
domain skill pulls well above the ×0–1 delete threshold (opus: built-in-shapes ×3 is the floor).
`multi-view-verbosity` self-selected only ×1 here and was removed; its collect-less *backpressure* value
is held out in [CT25/CT26](eval.yaml) and it can be restored if the benchmark grows past 24.

> **Caveat:** even ungrounded, the baseline self-grounds from the restored NuGet cache (README/AGENTS
> are packed in the nupkg) and the open web, so its archaeology counts are a **lower bound** —
> grounding's edge is understated. A hermetic (Docker) clean baseline is the follow-up.

## LIET — IET per correct answer vs. difficulty

The baseline (archaeology only) curve is the cost of getting each rung right *without* grounding; the
`SKILL.md` curve is the cost *with* the shelf. The gap between them, levelized across difficulty, is
the grounding lift. `floor` marks the 6 cheapest baseline-correct rungs.

| `claude-haiku-4.5` | `claude-opus-4.8` |
| --- | --- |
| ![LIET — haiku](charts/liet-haiku.svg) | ![LIET — opus](charts/liet-opus.svg) |

## Archaeology — fallback digging (cache + web) the agent resorts to

Archaeology is what the agent does when it *lacks* grounding: decompiling the restored package from
the NuGet cache and web-searching the API. Grounding drives it toward zero.

| `claude-haiku-4.5` | `claude-opus-4.8` |
| --- | --- |
| ![Archaeology — haiku](charts/arch-haiku.svg) | ![Archaeology — opus](charts/arch-opus.svg) |
