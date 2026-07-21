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
| tasks correct (+) | 8/24 → **16/24** | 23/24 → **24/24** |
| relied on grounding: tasks (+) | 0/24 → **12/24** | 0/24 → **19/24** |
| relied on archaeology: cache / nuget.org (−) | 15 / 2 → **7 / 1** | 80 / 0 → **2 / 0** |
| unique skills used (of shelf) (context) | — → 4 | — → 5 |
| func passed (assertions) (+) | 104/126 → **116/126** | 125/126 → **126/126** |
| tool calls: web / bash / other (context) | 5/201/230 → **2**/100/193 | 1/201/190 → **0**/55/182 |
| grounding load (tok) (context) | 0 → 956 | 0 → 1514 |
| output tok (% of IET) (−) | 6706 (34%) → **3857 (30%)** | 6024 (32%) → **2849 (25%)** |
| tool-call turns (% of total) (−) | 15 (87%) → **9 (86%)** | 12 (90%) → **6 (84%)** |
| Session turns (−) | 16 → **10** | 13 → **7** |
| Session wall-clock, end-to-end (−) | 84 → **50s (−40%)** | 150 → **56s (−63%)** |
| Total IET (−) | 95999 → **62580 (−35%)** | 92349 → **53446 (−42%)** |
| ↳ Grounding IET (doc) | 0 → 2074 | 0 → 2803 |
| ↳ Work IET (agent) (−) | 95999 → **60506** | 92349 → **50643** |
| **verdict** | **FAIL / BETTER** | **PASS / BETTER** |

**Two axes** (independent). **Gate** (correctness): **PASS** = 100% of the tier correct, **FAIL** =
below the gate. **Efficiency**: **BETTER** = more tasks correct / archaeology→0 / work IET cut ≥20%;
a correctness regression forces WORSE (cheaper-but-wrong is never better); a doc can FAIL the gate
yet be BETTER on efficiency.

- **opus `PASS / BETTER`** — clears the 24/24 correctness gate *and* is cheaper: web archaeology → **0**, Total IET **−42%**.
- **haiku `FAIL / BETTER`** — doesn't clear the gate (16/24), but improves every efficiency axis:
  **+8 correct**, archaeology 17→8 ops, turns 16→10, Total IET **−35%**. Discovery is imperfect here too
  (skill activated 18/24 vs opus 22/24) — some of the gap is the base skill not being pulled, not just execution.

> **On wall-clock:** end-to-end wall-clock (84→50s haiku, 150→56s opus) tracks the turn/IET drop but
> is machine- and load-dependent (it includes `dotnet build`/restore wait and parallelism), so it's an
> **informative** signal, not the normative gate — IET is the machine-independent cost metric. The
> numbers above were all measured on one host in the same run.

**Skills pulled** (self-select from shelf, ×scenarios):

- `claude-haiku-4.5` — markout×12 · conditional-composition×8 · composite-cells-cards×5 · output-formats×4 *(built-in-shapes ×0 on haiku — see note)*
- `claude-opus-4.8` — markout×19 · conditional-composition×8 · output-formats×7 · composite-cells-cards×4 · built-in-shapes×3

The shelf is now a compact base `markout` skill + **four** domain skills. Cross-model, every domain skill
clears the ×0–1 delete threshold: `built-in-shapes` is ×0 on haiku but ×3 on opus (opus is the floor that
keeps it on the shelf); the other three pull ≥4 on both. `multi-view-verbosity` self-selected only ×1 on
the 24-scenario ladder and was removed; its collect-less *backpressure* value is held out in
[CT25/CT26](eval.yaml) and it can be restored if the benchmark grows past 24.

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
