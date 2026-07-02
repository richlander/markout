#!/usr/bin/env bash
set -euo pipefail
# Datasets are regenerable and NOT committed: they land in the grounding cache
# ($GROUNDING_DATA_DIR, else $XDG_CACHE_HOME/grounding, else ~/.cache/grounding).
# Paste the printed cards into the PR.
DATA="${GROUNDING_DATA_DIR:-${XDG_CACHE_HOME:-$HOME/.cache}/grounding}/markout-6q"
grounding run markout --source agents --runs 3 --model "claude-haiku-4.5 claude-opus-4.8"
grounding run markout --source readme --readme-file README.md --runs 3 --model "claude-haiku-4.5 claude-opus-4.8"
grounding analyze --card        "$DATA/markout.haiku.json" "$DATA/markout.opus.json"
grounding analyze --source-diff "$DATA/markout.haiku.json" "$DATA/markout-readme.haiku.json" "$DATA/markout.opus.json" "$DATA/markout-readme.opus.json"
