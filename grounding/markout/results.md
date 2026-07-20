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
| tasks correct (+) | 15/24 → **20/24** | 22/24 → **24/24** |
| relied on grounding: tasks (+) | 0/24 → **24/24** | 0/24 → **24/24** |
| relied on archaeology: cache / nuget.org (−) | 92 / 15 → **0 / 0** | 78 / 2 → **4 / 0** |
| unique skills used (of shelf) (context) | — → 6 | — → 6 |
| func passed (assertions) (+) | 115/126 → **122/126** | 124/126 → **126/126** |
| tool calls: web / bash / other (context) | 34/444/278 → **0**/70/244 | 11/203/240 → **0**/74/203 |
| grounding load (tok) (context) | 0 → 1912 | 0 → 1912 |
| output tok (% of IET) (−) | 9916 (28%) → **3579 (25%)** | 6788 (26%) → **3794 (24%)** |
| tool-call turns (% of total) (−) | 27 (95%) → **10 (88%)** | 12 (91%) → **8 (86%)** |
| Session turns (−) | 28 → **11** | 13 → **9** |
| Total IET (−) | 172277 → **66491 (−61%)** | 122442 → **73860 (−40%)** |
| ↳ Grounding IET (doc) | 0 → 4210 | 0 → 3915 |
| ↳ Work IET (agent) (−) | 172277 → **62282** | 122442 → **69946** |
| **verdict** | **FAIL / BETTER** | **PASS / BETTER** |

**Two axes** (independent). **Gate** (correctness): **PASS** = 100% of the tier correct, **FAIL** =
below the gate. **Efficiency**: **BETTER** = more tasks correct / archaeology→0 / work IET cut ≥20%;
a correctness regression forces WORSE (cheaper-but-wrong is never better); a doc can FAIL the gate
yet be BETTER on efficiency.

- **opus `PASS / BETTER`** — clears the 24/24 gate *and* is cheaper: web archaeology → **0**, Total IET **−40%**.
- **haiku `FAIL / BETTER`** — doesn't clear the gate (20/24; the gap is execution, not discovery —
  the skill activated **24/24**), but improves every efficiency axis: **+5 correct**, archaeology → **0**,
  turns 28→11, Total IET **−61%**.

**Skills pulled** (self-select from shelf, ×scenarios):

- `claude-haiku-4.5` — markout×24 · conditional-composition×8 · output-formats×7 · built-in-shapes×4 · composite-cells-cards×4 · multi-view-verbosity×4
- `claude-opus-4.8` — markout×24 · conditional-composition×7 · output-formats×7 · composite-cells-cards×4 · built-in-shapes×3 · multi-view-verbosity×3

Every domain skill earns its place (each pulled well above the ×0–1 delete threshold).

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
