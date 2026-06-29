# Markout grounding

Self-contained eval bundle: `AGENTS.md`, `TASKS.md` (jobs-to-be-done), `eval.yaml` +
`fixtures/`, `data/` (n=3 haiku+opus, AGENTS+README), `run.sh`. Engine + tooling:
github.com/richlander/dotnet-package-grounding.

n=3: AGENTS.md is BETTER than baseline on both tiers (archaeology→0/7, cost −71%/−22%);
vs the README it is BETTER on mini, NEUTRAL on frontier. README is functionally 6/6.
