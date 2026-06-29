#!/usr/bin/env bash
set -euo pipefail
grounding run markout --source agents --runs 3 --model "claude-haiku-4.5 claude-opus-4.8"
grounding run markout --source readme --readme-file README.md --runs 3 --model "claude-haiku-4.5 claude-opus-4.8"
grounding analyze --card        data/markout.haiku.json data/markout.opus.json
grounding analyze --source-diff data/markout.haiku.json data/markout-readme.haiku.json data/markout.opus.json data/markout-readme.opus.json
