#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'
# Regenerate the markout grounding eval. Requires the `grounding` CLI:
#   https://github.com/richlander/dotnet-package-grounding (build from source)
# The CLI reads this repo's grounding/markout/AGENTS.md IN PLACE (via --root) — no
# packing or publishing needed to iterate. Datasets are regenerable and NOT committed:
# they land in the grounding cache ($GROUNDING_DATA_DIR, else $XDG_CACHE_HOME/grounding,
# else ~/.cache/grounding). Paste the printed cards into the PR.
$ROOT = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path   # markout repo root
$cacheBase =
    if     ($env:GROUNDING_DATA_DIR) { $env:GROUNDING_DATA_DIR }
    elseif ($env:XDG_CACHE_HOME)     { Join-Path $env:XDG_CACHE_HOME 'grounding' }
    else                             { Join-Path $HOME '.cache/grounding' }
$DATA = Join-Path $cacheBase 'markout-6q'
$MODELS = "claude-haiku-4.5 claude-opus-4.8"

grounding run markout --root $ROOT --source agents --runs 5 --model "$MODELS"
grounding run markout --root $ROOT --source readme --readme-file (Join-Path $ROOT 'README.md') --runs 5 --model "$MODELS"
grounding analyze --card `
    (Join-Path $DATA 'markout.haiku.json') (Join-Path $DATA 'markout.opus.json')
grounding analyze --source-diff `
    (Join-Path $DATA 'markout.haiku.json') (Join-Path $DATA 'markout-readme.haiku.json') `
    (Join-Path $DATA 'markout.opus.json') (Join-Path $DATA 'markout-readme.opus.json')
