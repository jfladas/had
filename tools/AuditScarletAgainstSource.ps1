[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)]
  [string]$ProjectRoot,

  [int]$ShowExamples = 10,

  [switch]$RelaxThoughtPrefix,

  [switch]$NormalizePunctuation,

  [switch]$IgnoreThoughtPrefix,

  [switch]$StripSpeakerPrefixesInAssets,

  [switch]$SplitSourceSentences,

  [switch]$Strict
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Ellipsis = [string][char]0x2026

function Get-NormalizedLine {
  param(
    [Parameter(Mandatory=$true)][string]$Line,
    [switch]$StripSpeakerPrefix,
    [switch]$StripThoughtPrefix,
    [switch]$NormalizePunctuation
  )

  $s = $Line.Trim()
  if ($StripSpeakerPrefix) {
    # Strip visible speaker prefix like "Scarlet: ...", "Miss Moon: ...", "???: ...".
    # Do NOT strip thought prefix (handled separately).
    if ($s -notmatch '^\(t\):\s*') {
      $s = $s -replace '^[^:\r\n]{1,40}:\s+', ''
    }
  }

  if ($StripThoughtPrefix) {
    $s = $s -replace '^\(t\):\s*', ''
  }

  if ($NormalizePunctuation) {
    # Collapse whitespace and make ellipsis usage robust against formatting differences
    # introduced during earlier imports (e.g. "Uh ..." vs "Uh …" vs "Uh...").
    $s = $s -replace "[\u2013\u2014]", '-'
    $s = $s -replace '\.\.{2,}', $script:Ellipsis
    $s = $s -replace "\\s*${script:Ellipsis}\\s*", $script:Ellipsis
    $s = $s -replace '\s+', ' '
  }

  return $s
}

function Get-UnityTextValuesFromStorySceneAsset {
  param([Parameter(Mandatory=$true)][string]$AssetPath)

  $lines = [System.IO.File]::ReadAllText($AssetPath, [System.Text.Encoding]::UTF8)

  # Quick filter: only parse StoryScene assets.
  if ($lines -notmatch 'm_EditorClassIdentifier:\s*Assembly-CSharp::StoryScene') {
    return @()
  }

  $result = New-Object System.Collections.Generic.List[string]
  $fileLines = $lines -split "\r?\n"

  for ($i = 0; $i -lt $fileLines.Count; $i++) {
    $line = $fileLines[$i]

    $m = [regex]::Match($line, '^\s*-\s*text:\s*(.*)$')
    if (-not $m.Success) { continue }

    $rest = $m.Groups[1].Value
    $baseIndent = ([regex]::Match($line, '^(\s*)-\s*text:').Groups[1].Value).Length

    if ($rest -eq '') {
      $result.Add('')
      continue
    }

    # Quoted string (Unity often wraps long strings across lines while still being within quotes).
    if ($rest.StartsWith('"')) {
      $acc = $rest
      while ($acc -notmatch '(?<!\\)"\s*$') {
        $i++
        if ($i -ge $fileLines.Count) { break }
        $cont = $fileLines[$i]
        # Continuation is typically indented; keep exact whitespace inside the quoted string.
        $acc += "\n" + ($cont.TrimStart())
      }

      # Remove outer quotes if possible.
      $raw = $acc
      if ($raw.Length -ge 2 -and $raw.StartsWith('"') -and $raw -match '(?<!\\)"\s*$') {
        $raw = $raw -replace '^"', ''
        $raw = $raw -replace '(?<!\\)"\s*$', ''
      }

      # Unescape common sequences (\uXXXX, \n, \" etc).
      $unescaped = [System.Text.RegularExpressions.Regex]::Unescape($raw)
      # Unity YAML sometimes breaks lines visually; collapse embedded newlines from YAML wrapping.
      $unescaped = ($unescaped -replace "\r?\n\s+", ' ')
      $result.Add($unescaped)
      continue
    }

    # Unquoted scalar. Unity YAML may wrap long plain scalars onto the next line(s)
    # by indenting continuation lines; treat those as part of the same string.
    $acc = $rest
    while ($true) {
      $peekIndex = $i + 1
      if ($peekIndex -ge $fileLines.Count) { break }

      $nextLine = $fileLines[$peekIndex]
      if ($nextLine -match '^\s*$') {
        $i = $peekIndex
        continue
      }

      $nextIndent = ([regex]::Match($nextLine, '^(\s*)').Groups[1].Value).Length
      if ($nextIndent -le $baseIndent) { break }

      # Stop if the continuation line is actually the start of a new YAML key.
      if ($nextLine -match '^\s*(alternativeText|character|actions|music|sound|background|nextScene):') { break }
      if ($nextLine -match '^\s*-\s*\w+:') { break }

      $acc += ' ' + ($nextLine.Trim())
      $i = $peekIndex
    }

    $result.Add($acc)
  }

  return $result.ToArray()
}

function Get-SourceLinesForChapter {
  param([Parameter(Mandatory=$true)][string]$SourcePath)

  $rawText = [System.IO.File]::ReadAllText($SourcePath, [System.Text.Encoding]::UTF8)
  $rawLines = $rawText -split "\r?\n"
  $out = New-Object System.Collections.Generic.List[string]

  for ($idx = 0; $idx -lt $rawLines.Count; $idx++) {
    $l = $rawLines[$idx]
    $s = ($l -replace "\r$", '').TrimEnd()
    if ($s.Trim() -eq '') { continue }

    # Drop header like "Chapter 9" / "Epilogue 3".
    if ($s -match '^(Chapter|Epilogue)\s+\d+\s*$') { continue }

    # Drop script directives.
    if ($s -match '^CHOICE OPTION\s+\d+:' ) { continue }
    if ($s -match '^CONTINUE HERE' ) { continue }
    if ($s -match '^(ILLUSTRATION|MINIGAME)\s*$') { continue }
    if ($s -match '^\(SCARLET POV' ) { continue }
    if ($s -match '^\(BACK TO' ) { continue }
    if ($s -match '^\(MC POV' ) { continue }
    if ($s -match '^\(.*NO SCARLET SPRITE.*\)$') { continue }
    if ($s -match '^\(.*DON\x27T SHOW.*\)$') { continue }
    if ($s -match '^TIME SKIP' ) { continue }
    if ($s -match '^THE END\b' ) { continue }
    if ($s -match '^HERE CHECK IF PLAYER' ) { continue }
    if ($s -match '^IF ENOUGH POINTS' ) { continue }
    if ($s -match '^IF NOT ENOUGH POINTS' ) { continue }
    if ($s -match '^JUMP TO EPILOGUE' ) { continue }

    # Some scripts encode the *second* choice label as a plain line immediately
    # followed by a JUMP directive. Treat that label line as a directive too.
    $peek = $idx + 1
    while ($peek -lt $rawLines.Count -and $rawLines[$peek].Trim() -eq '') { $peek++ }
    if ($peek -lt $rawLines.Count) {
      $nextNonEmpty = $rawLines[$peek].Trim()
      if ($nextNonEmpty -match '^JUMP TO EPILOGUE') { continue }
    }

    if ($SplitSourceSentences) {
      $isThought = $s -match '^\(t\):\s*'
      $hasSpeaker = $s -match '^[^:\r\n]{1,40}:\s+'

      # Only split long narration lines. Dialogue/thought lines tend to include
      # ellipses/dashes where splitting is risky.
      if (-not $isThought -and -not $hasSpeaker -and $s.Length -ge 160) {
        $parts = [regex]::Split($s, '(?<=[.!?])\s+(?=[A-Z({])')
        if ($parts.Count -gt 1) {
          foreach ($p in $parts) {
            $chunk = $p.Trim()
            if ($chunk -ne '') { $out.Add($chunk) }
          }
          continue
        }
      }
    }

    $out.Add($s)
  }

  return $out.ToArray()
}

function Test-SceneLinesAgainstSource {
  param(
    [Parameter(Mandatory=$true)][string[]]$SceneLines,
    [Parameter(Mandatory=$true)][string[]]$SourceLines,
    [switch]$RelaxThoughtPrefix
  )

  $sourceNorm = @()
  $sourceNormRelax = @()

  for ($i=0; $i -lt $SourceLines.Count; $i++) {
    $sourceNorm += (Get-NormalizedLine -Line $SourceLines[$i] -StripSpeakerPrefix -StripThoughtPrefix:$IgnoreThoughtPrefix -NormalizePunctuation:$NormalizePunctuation)
    $sourceNormRelax += (Get-NormalizedLine -Line $SourceLines[$i] -StripSpeakerPrefix -StripThoughtPrefix:$true -NormalizePunctuation:$NormalizePunctuation)
  }

  $unmatched = New-Object System.Collections.Generic.List[object]
  $thoughtPrefixMismatches = New-Object System.Collections.Generic.List[object]

  $posStrict = 0
  $posRelax = 0

  for ($i=0; $i -lt $SceneLines.Count; $i++) {
    $sceneRaw = $SceneLines[$i]
    $sceneStrict = (Get-NormalizedLine -Line $sceneRaw -StripSpeakerPrefix:$StripSpeakerPrefixesInAssets -StripThoughtPrefix:$IgnoreThoughtPrefix -NormalizePunctuation:$NormalizePunctuation)

    # Skip empty (should be none after our repairs).
    if ($sceneStrict -eq '') { continue }

    # 1) Strict match against speaker-stripped source, preserving (t):
    $found = $false
    for ($j=$posStrict; $j -lt $sourceNorm.Count; $j++) {
      if ($sourceNorm[$j] -eq $sceneStrict) { $posStrict = $j + 1; $found = $true; break }
    }

    if ($found) { continue }

    # 2) Relaxed thought prefix match (optional): strip (t): from source and scene and try again.
    $sceneRelax = (Get-NormalizedLine -Line $sceneRaw -StripSpeakerPrefix:$StripSpeakerPrefixesInAssets -StripThoughtPrefix:$true -NormalizePunctuation:$NormalizePunctuation)

    if ($RelaxThoughtPrefix) {
      $relFound = $false
      for ($j=$posRelax; $j -lt $sourceNormRelax.Count; $j++) {
        if ($sourceNormRelax[$j] -eq $sceneRelax) { $posRelax = $j + 1; $relFound = $true; break }
      }

      if ($relFound) {
        if ($sceneStrict -ne $sceneRelax) {
          $thoughtPrefixMismatches.Add([pscustomobject]@{ Index = $i; Scene = $sceneStrict; Expected = $sourceNormRelax[$posRelax-1] })
        }
        continue
      }
    }

    $unmatched.Add([pscustomobject]@{ Index = $i; Scene = $sceneStrict })
  }

  return [pscustomobject]@{
    Unmatched = $unmatched
    ThoughtPrefixMismatches = $thoughtPrefixMismatches
  }
}

function Get-ChapterSourceMap {
  param([Parameter(Mandatory=$true)][string]$StoryFolder)

  $map = @{}
  $files = Get-ChildItem -LiteralPath $StoryFolder -File -ErrorAction Stop

  foreach ($f in $files) {
    if ($f.Name -match '^Scarlet Chapter (\d+) Text$') {
      $map["Chapter$($Matches[1])"] = $f.FullName
      continue
    }
    if ($f.Name -match '^Scarlet Epilogue (\d+) Text$') {
      $map["Epilogue$($Matches[1])"] = $f.FullName
      continue
    }
  }

  return $map
}

$assetsStoryFolder = Join-Path $ProjectRoot 'had\Assets\Story'
$scarletScenesFolder = Join-Path $ProjectRoot 'had\Assets\Story\Scenes\Scarlet'

if (-not (Test-Path -LiteralPath $assetsStoryFolder)) { throw "Story folder not found: $assetsStoryFolder" }
if (-not (Test-Path -LiteralPath $scarletScenesFolder)) { throw "Scarlet scenes folder not found: $scarletScenesFolder" }

$sourceMap = Get-ChapterSourceMap -StoryFolder $assetsStoryFolder

$allKeys = @()
$allKeys += ($sourceMap.Keys | Where-Object { $_ -like 'Chapter*' } | Sort-Object { [int]($_ -replace '^Chapter','') })
$allKeys += ($sourceMap.Keys | Where-Object { $_ -like 'Epilogue*' } | Sort-Object { [int]($_ -replace '^Epilogue','') })

$overall = [pscustomobject]@{
  Chapters = 0
  Scenes = 0
  TotalSentences = 0
  SceneUnmatched = 0
  SourceMissing = 0
  ThoughtPrefixMismatches = 0
}

$anyMismatch = $false

foreach ($key in $allKeys) {
  $sourcePath = $sourceMap[$key]
  $sourceLines = @(Get-SourceLinesForChapter -SourcePath $sourcePath)

  if ($key -like 'Chapter*') {
    $num = [int]($key -replace '^Chapter','')
    $prefix = "S${num}_"
    $prefixX = "S${num}_X"

    $sceneFiles = Get-ChildItem -LiteralPath $scarletScenesFolder -Filter '*.asset' -File |
      Where-Object { $_.Name.StartsWith($prefix) -or $_.Name.StartsWith($prefixX) } |
      Sort-Object Name
  } else {
    $num = [int]($key -replace '^Epilogue','')
    $prefix = "SE${num}_"
    $sceneFiles = Get-ChildItem -LiteralPath $scarletScenesFolder -Filter '*.asset' -File |
      Where-Object { $_.Name.StartsWith($prefix) } |
      Sort-Object Name
  }

  $overall.Chapters++

  $chapterTotalSentences = 0
  $chapterUnmatched = 0
  $chapterThoughtMismatches = 0

  # Build a set for coverage test.
  $assetLineSet = New-Object 'System.Collections.Generic.HashSet[string]'

  $sceneReports = New-Object System.Collections.Generic.List[object]

  foreach ($sf in $sceneFiles) {
    $sceneLines = @(Get-UnityTextValuesFromStorySceneAsset -AssetPath $sf.FullName)
    if ($sceneLines.Count -eq 0) { continue }

    $overall.Scenes++
    $chapterTotalSentences += $sceneLines.Count

    foreach ($sl in $sceneLines) {
      $norm = Get-NormalizedLine -Line $sl -StripSpeakerPrefix:$StripSpeakerPrefixesInAssets -StripThoughtPrefix:$IgnoreThoughtPrefix -NormalizePunctuation:$NormalizePunctuation
      if ($norm -ne '') { [void]$assetLineSet.Add($norm) }
      if ($RelaxThoughtPrefix) {
        $rel = Get-NormalizedLine -Line $sl -StripSpeakerPrefix:$StripSpeakerPrefixesInAssets -StripThoughtPrefix:$true -NormalizePunctuation:$NormalizePunctuation
        if ($rel -ne '') { [void]$assetLineSet.Add($rel) }
      }
    }

    $check = Test-SceneLinesAgainstSource -SceneLines $sceneLines -SourceLines $sourceLines -RelaxThoughtPrefix:$RelaxThoughtPrefix

    $uCount = $check.Unmatched.Count
    $tCount = $check.ThoughtPrefixMismatches.Count

    $chapterUnmatched += $uCount
    $chapterThoughtMismatches += $tCount

    if ($uCount -gt 0 -or ($Strict -and $tCount -gt 0)) {
      $anyMismatch = $true
      $sceneReports.Add([pscustomobject]@{
        Scene = $sf.Name
        Unmatched = $check.Unmatched
        ThoughtPrefixMismatches = $check.ThoughtPrefixMismatches
      })
    }
  }

  $overall.TotalSentences += $chapterTotalSentences
  $overall.SceneUnmatched += $chapterUnmatched
  $overall.ThoughtPrefixMismatches += $chapterThoughtMismatches

  # Coverage: any source line not present in ANY asset line.
  $missingSource = New-Object System.Collections.Generic.List[object]
  for ($i=0; $i -lt $sourceLines.Count; $i++) {
    $srcStrict = Get-NormalizedLine -Line $sourceLines[$i] -StripSpeakerPrefix -StripThoughtPrefix:$IgnoreThoughtPrefix -NormalizePunctuation:$NormalizePunctuation
    $srcRelax = Get-NormalizedLine -Line $sourceLines[$i] -StripSpeakerPrefix -StripThoughtPrefix:$true -NormalizePunctuation:$NormalizePunctuation

    $ok = $assetLineSet.Contains($srcStrict)
    if (-not $ok -and $RelaxThoughtPrefix) { $ok = $assetLineSet.Contains($srcRelax) }

    if (-not $ok) {
      $missingSource.Add([pscustomobject]@{ LineNumber = $i + 1; Source = $sourceLines[$i] })
    }
  }

  $overall.SourceMissing += $missingSource.Count
  if ($missingSource.Count -gt 0) { $anyMismatch = $true }

  Write-Host "==== $key ===="
  Write-Host "Source: $([System.IO.Path]::GetFileName($sourcePath))"
  Write-Host "Scenes parsed: $($sceneFiles.Count), sentences parsed: $chapterTotalSentences"
  Write-Host "Scene lines unmatched (order/subsequence check): $chapterUnmatched"
  if ($RelaxThoughtPrefix) {
    Write-Host "Thought-prefix mismatches (matched only when stripping '(t):'): $chapterThoughtMismatches"
  }
  Write-Host "Source lines missing from ALL scenes: $($missingSource.Count)"

  if ($sceneReports.Count -gt 0 -and $ShowExamples -gt 0) {
    foreach ($sr in ($sceneReports | Select-Object -First $ShowExamples)) {
      Write-Host "-- Scene: $($sr.Scene)"
      foreach ($u in ($sr.Unmatched | Select-Object -First 5)) {
        Write-Host "   UNMATCHED[$($u.Index)]: $($u.Scene)"
      }
      if ($Strict -and $sr.ThoughtPrefixMismatches.Count -gt 0) {
        foreach ($t in ($sr.ThoughtPrefixMismatches | Select-Object -First 5)) {
          Write-Host "   THOUGHT-PREFIX[$($t.Index)]: $($t.Scene)"
        }
      }
    }
  }

  if ($missingSource.Count -gt 0 -and $ShowExamples -gt 0) {
    Write-Host "-- Missing source examples:"
    foreach ($ms in ($missingSource | Select-Object -First $ShowExamples)) {
      Write-Host "   MISSING[$($ms.LineNumber)]: $($ms.Source)"
    }
  }

  Write-Host ""
}

Write-Host "==== OVERALL ===="
Write-Host "Chapters/Epilogues: $($overall.Chapters)"
Write-Host "Scenes parsed:      $($overall.Scenes)"
Write-Host "Sentences parsed:   $($overall.TotalSentences)"
Write-Host "Unmatched lines:    $($overall.SceneUnmatched)"
if ($RelaxThoughtPrefix) {
  Write-Host "Thought mismatches:  $($overall.ThoughtPrefixMismatches)"
}
Write-Host "Missing source lines: $($overall.SourceMissing)"

if ($anyMismatch) {
  exit 1
}

exit 0
