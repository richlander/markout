#!/usr/bin/env bash
set -euo pipefail
# Regenerate the markout grounding eval. Requires the `grounding` CLI:
#   https://github.com/richlander/dotnet-package-grounding (build from source)
# The CLI reads this repo's grounding/markout/AGENTS.md IN PLACE (via --root) — no
# packing or publishing needed to iterate. Datasets are regenerable and NOT committed:
# they land in the grounding cache ($GROUNDING_DATA_DIR, else $XDG_CACHE_HOME/grounding,
# else ~/.cache/grounding). Paste the printed cards into the PR.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"   # markout repo root
DATA="${GROUNDING_DATA_DIR:-${XDG_CACHE_HOME:-$HOME/.cache}/grounding}/markout-6q"
MODELS="claude-haiku-4.5 claude-opus-4.8"

grounding run markout --root "$ROOT" --source agents --runs 5 --model "$MODELS"
grounding run markout --root "$ROOT" --source readme --readme-file "$ROOT/README.md" --runs 5 --model "$MODELS"
grounding analyze --card        "$DATA/markout.haiku.json" "$DATA/markout.opus.json"
grounding analyze --source-diff "$DATA/markout.haiku.json" "$DATA/markout-readme.haiku.json" "$DATA/markout.opus.json" "$DATA/markout-readme.opus.json"
