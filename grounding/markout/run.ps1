#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'
# Datasets are regenerable and NOT committed: they land in the grounding cache
# ($GROUNDING_DATA_DIR, else $XDG_CACHE_HOME/grounding, else ~/.cache/grounding).
# Paste the printed cards into the PR.
$cacheBase =
    if     ($env:GROUNDING_DATA_DIR) { $env:GROUNDING_DATA_DIR }
    elseif ($env:XDG_CACHE_HOME)     { Join-Path $env:XDG_CACHE_HOME 'grounding' }
    else                             { Join-Path $HOME '.cache/grounding' }
$DATA = Join-Path $cacheBase 'markout-6q'

grounding run markout --source agents --runs 3 --model "claude-haiku-4.5 claude-opus-4.8"
grounding run markout --source readme --readme-file README.md --runs 3 --model "claude-haiku-4.5 claude-opus-4.8"
grounding analyze --card `
    (Join-Path $DATA 'markout.haiku.json') (Join-Path $DATA 'markout.opus.json')
grounding analyze --source-diff `
    (Join-Path $DATA 'markout.haiku.json') (Join-Path $DATA 'markout-readme.haiku.json') `
    (Join-Path $DATA 'markout.opus.json') (Join-Path $DATA 'markout-readme.opus.json')
