# markout — tasks the grounding is evaluated on

Real jobs a developer asks an AI to do with this package. Each is gated by a
build + run with a deterministic anchor, so the grounding (AGENTS.md) is proven
to move an agent from "fails / hand-rolls" to "uses the API correctly, first try."
Machine form + fixtures: `eval.yaml`. Regenerate results with `run.sh` — datasets
land in the grounding cache (`$GROUNDING_DATA_DIR`, not the repo); the distilled
quality card lives in the PR.

| # | Task | Key API | Anchor |
| --- | --- | --- | --- |
| 1 | Render a report as Markdown with Markout | `MarkoutSerializer.Serialize` | `# Security Report / \| Scanned \| 42 \| / ## Advisories / \| Contoso.Data \| Critical \| CVE-2025-0001 \|` |
| 2 | Serialize a list to a Markdown table | `MarkoutSerializer.Serialize` | `\| Serilog \| 3.1.1 \| / \| Polly \| 8.2.0 \| / \| Newtonsoft.Json \| 13.0.3 \|` |
| 3 | Build report with a callout and metric shapes | `MarkoutSerializer.Serialize`, `Callout`, `Metric` | `# Build Report / [!WARNING] / 3 steps slower than budget / Compile / 4.8 / 9.6` |
| 4 | Emit a package list as TSV | `Tsv` | `Serilogt3.1.1 / Pollyt8.2.0 / Newtonsoft.Jsont13.0.3` |
| 5 | Report with a section table and a dependency tree | `TreeNode` | `# Diagnostics / ## Errors / CS0103 / ## Dependencies / Serilog.Sinks.Console` |
| 6 | Grouped issue report with links and value-mapped badges | `MarkoutSerializer.Serialize` | `### v1.0 / ### v2.0 / [Crash on startup](https://github.com/acme/app/issues/1 / ✓ / ✗` |
