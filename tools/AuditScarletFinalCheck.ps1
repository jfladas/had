[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [string]$ProjectRoot,

  [int]$ShowExamples = 10,

  [int]$FromChapter = 7,
  [int]$ToChapter = 15,
  [int]$FromEpilogue = 1,
  [int]$ToEpilogue = 3,

  [switch]$StripSpeakerPrefixesInAssets,
  [switch]$IgnoreThoughtPrefix,
  [switch]$NormalizePunctuation,

  [switch]$CheckOrder,

  [switch]$AllowSubstringCoverage,
  [int]$MinSubstringLength = 25,

  [switch]$Strict
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Ellipsis = [string][char]0x2026
$EmDash = [string][char]0x2014

function Normalize-Punctuation {
  param([Parameter(Mandatory = $true)][string]$Text)

  $s = $Text

  # Normalize punctuation variants.
  $s = $s -replace '\.\.\.', $Ellipsis
  # Treat stutter/interruption hyphens as equivalent to em-dash.
  # Examples: "Y-Your", "S-Scarlet", "I didn't-", "I- I".
  $s = [regex]::Replace($s, '\b([A-Za-z])\-\s*([A-Za-z])', { param($m) $m.Groups[1].Value + $EmDash + $m.Groups[2].Value })
  $s = [regex]::Replace($s, '\b([A-Za-z])\-\s+([A-Za-z])', { param($m) $m.Groups[1].Value + $EmDash + ' ' + $m.Groups[2].Value })
  $s = $s -replace '\-\s*$', $EmDash
  $s = $s -replace '\u201C|\u201D', '"'
  $s = $s -replace '\u2018|\u2019', "'"
  $s = $s -replace '\u2014', $EmDash

  # Normalize whitespace around ellipsis and em-dash.
  $s = $s -replace "\s*$Ellipsis\s*", " $Ellipsis "
  $s = $s -replace "\s*$EmDash\s*", " $EmDash "

  # Collapse whitespace.
  $s = ($s -replace '\s+', ' ').Trim()

  return $s
}

function Get-NormalizedLine {
  param(
    [Parameter(Mandatory = $true)][string]$Line,
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
    $s = Normalize-Punctuation -Text $s
  } else {
    $s = ($s -replace '\s+', ' ').Trim()
  }

  return $s
}

function Get-UnityTextValuesFromStorySceneAsset {
  param([Parameter(Mandatory = $true)][string]$AssetPath)

  $raw = [System.IO.File]::ReadAllText($AssetPath, [System.Text.Encoding]::UTF8)

  # Only parse StoryScene assets.
  if ($raw -notmatch 'm_EditorClassIdentifier:\s*Assembly-CSharp::StoryScene') {
    return @()
  }

  $result = New-Object System.Collections.Generic.List[string]
  $fileLines = $raw -split "\r?\n"

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

    # Quoted string, possibly wrapped.
    if ($rest.StartsWith('"')) {
      $acc = $rest
      while ($acc -notmatch '(?<!\\)"\s*$') {
        $i++
        if ($i -ge $fileLines.Count) { break }
        $cont = $fileLines[$i]
        $acc += "\n" + ($cont.TrimStart())
      }

      $rawQuoted = $acc
      if ($rawQuoted.Length -ge 2 -and $rawQuoted.StartsWith('"') -and $rawQuoted -match '(?<!\\)"\s*$') {
        $rawQuoted = $rawQuoted -replace '^"', ''
        $rawQuoted = $rawQuoted -replace '(?<!\\)"\s*$', ''
      }

      $unescaped = [System.Text.RegularExpressions.Regex]::Unescape($rawQuoted)
      $unescaped = ($unescaped -replace "\r?\n\s+", ' ')
      $result.Add($unescaped)
      continue
    }

    # Unquoted scalar, possibly wrapped by indentation.
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
  param([Parameter(Mandatory = $true)][string]$SourcePath)

  $rawText = [System.IO.File]::ReadAllText($SourcePath, [System.Text.Encoding]::UTF8)
  $rawLines = $rawText -split "\r?\n"
  $out = New-Object System.Collections.Generic.List[string]
  $skipNextChoiceText = 0
  $lastAddedWasNonSpeakerChoiceText = $false

  foreach ($l in $rawLines) {
    $s = ($l -replace "\r$", '').TrimEnd()
    if ($s.Trim() -eq '') { continue }

    # Drop header like "Chapter 9" / "Epilogue 3".
    if ($s -match '^(Chapter|Epilogue)\s+\d+\s*$') { continue }

    # Drop script directives / notes.
    # Some files use:
    #   CHOICE OPTION 1:
    #   <option text>
    # In that case, the next non-empty line is a choice label, not story dialogue.
    if ($s -match '^CHOICE OPTION\s+\d+:\s*$') { $skipNextChoiceText = 1; continue }
    if ($s -match '^CHOICE OPTION\s+\d+:') { continue }
    if ($s -match '^CONTINUE HERE') { continue }
    if ($s -match '^(ILLUSTRATION|MINIGAME)\s*$') { continue }
    if ($s -match '^\(SCARLET POV') { continue }
    if ($s -match '^\(BACK TO') { continue }
    if ($s -match '^\(MC POV') { continue }
    if ($s -match '^\(.*NO SCARLET SPRITE.*\)$') { continue }
    if ($s -match '^\(.*DON\x27T SHOW.*\)$') { continue }
    if ($s -match '^TIME SKIP') { continue }
    if ($s -match '^THE END\b') { continue }
    if ($s -match '^HERE CHECK IF PLAYER') { continue }
    if ($s -match '^IF ENOUGH POINTS') { continue }
    if ($s -match '^IF NOT ENOUGH POINTS') { continue }
    if ($s -match '^JUMP TO EPILOGUE') {
      # Some scripts omit a second "CHOICE OPTION N:" line and instead place the
      # second option text directly before a JUMP marker. In that case, drop the
      # immediately preceding non-speaker line.
      if ($lastAddedWasNonSpeakerChoiceText -and $out.Count -gt 0) {
        $out.RemoveAt($out.Count - 1)
        $lastAddedWasNonSpeakerChoiceText = $false
      }
      continue
    }

    if ($skipNextChoiceText -gt 0) {
      $skipNextChoiceText--
      continue
    }

    $out.Add($s)
    $lastAddedWasNonSpeakerChoiceText = (
      ($s -notmatch '^\(t\):\s*') -and
      ($s -notmatch '^[^:\r\n]{1,40}:\s+')
    )
  }

  return $out.ToArray()
}

function Test-SceneLinesAgainstSource {
  param(
    [Parameter(Mandatory = $true)][string[]]$SceneLines,
    [Parameter(Mandatory = $true)][string[]]$SourceLines,
    [switch]$StripSpeakerPrefixesInAssets,
    [switch]$IgnoreThoughtPrefix,
    [switch]$NormalizePunctuation
  )

  $sourceNorm = @()
  for ($i = 0; $i -lt $SourceLines.Count; $i++) {
    $sourceNorm += (Get-NormalizedLine -Line $SourceLines[$i] -StripSpeakerPrefix -StripThoughtPrefix:$IgnoreThoughtPrefix -NormalizePunctuation:$NormalizePunctuation)
  }

  $unmatched = New-Object System.Collections.Generic.List[object]
  $pos = 0

  for ($i = 0; $i -lt $SceneLines.Count; $i++) {
    $sceneRaw = $SceneLines[$i]
    $sceneNorm = (Get-NormalizedLine -Line $sceneRaw -StripSpeakerPrefix:$StripSpeakerPrefixesInAssets -StripThoughtPrefix:$IgnoreThoughtPrefix -NormalizePunctuation:$NormalizePunctuation)

    if ($sceneNorm -eq '') { continue }

    $found = $false
    for ($j = $pos; $j -lt $sourceNorm.Count; $j++) {
      if ($sourceNorm[$j] -eq $sceneNorm) { $pos = $j + 1; $found = $true; break }
    }

    if (-not $found) {
      $unmatched.Add([pscustomobject]@{ Index = $i; Scene = $sceneNorm })
    }
  }

  return [pscustomobject]@{ Unmatched = $unmatched }
}

function Get-ChapterSourceMap {
  param([Parameter(Mandatory = $true)][string]$StoryFolder)

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

$keys = New-Object System.Collections.Generic.List[string]
for ($c = $FromChapter; $c -le $ToChapter; $c++) { $keys.Add("Chapter$c") }
for ($e = $FromEpilogue; $e -le $ToEpilogue; $e++) { $keys.Add("Epilogue$e") }

$overall = [pscustomobject]@{
  Chapters = 0
  Scenes = 0
  TotalSentences = 0
  OrderUnmatched = 0
  SourceMissing = 0
}

$anyMismatch = $false

foreach ($key in $keys) {
  if (-not $sourceMap.ContainsKey($key)) {
    Write-Host "==== $key ===="
    Write-Host "Source: (missing source file)"
    Write-Host ""
    $anyMismatch = $true
    continue
  }

  $sourcePath = $sourceMap[$key]
  $sourceLines = @(Get-SourceLinesForChapter -SourcePath $sourcePath)

  if ($key -like 'Chapter*') {
    $num = [int]($key -replace '^Chapter', '')
    $prefix = "S${num}_"
    $prefixX = "S${num}_X"

    $sceneFiles = Get-ChildItem -LiteralPath $scarletScenesFolder -Filter '*.asset' -File |
      Where-Object { $_.Name.StartsWith($prefix) -or $_.Name.StartsWith($prefixX) } |
      Sort-Object Name
  } else {
    $num = [int]($key -replace '^Epilogue', '')
    $prefix = "SE${num}_"
    $sceneFiles = Get-ChildItem -LiteralPath $scarletScenesFolder -Filter '*.asset' -File |
      Where-Object { $_.Name.StartsWith($prefix) } |
      Sort-Object Name
  }

  $overall.Chapters++

  $chapterTotalSentences = 0
  $chapterOrderUnmatched = 0

  # Coverage set over ALL scenes for this chapter.
  $assetLineSet = New-Object 'System.Collections.Generic.HashSet[string]'
  $assetNormLines = New-Object System.Collections.Generic.List[string]

  $sceneReports = New-Object System.Collections.Generic.List[object]

  foreach ($sf in $sceneFiles) {
    $sceneLines = @(Get-UnityTextValuesFromStorySceneAsset -AssetPath $sf.FullName)
    if ($sceneLines.Count -eq 0) { continue }

    $overall.Scenes++
    $chapterTotalSentences += $sceneLines.Count

    foreach ($sl in $sceneLines) {
      $norm = Get-NormalizedLine -Line $sl -StripSpeakerPrefix:$StripSpeakerPrefixesInAssets -StripThoughtPrefix:$IgnoreThoughtPrefix -NormalizePunctuation:$NormalizePunctuation
      if ($norm -ne '') {
        [void]$assetLineSet.Add($norm)
        $assetNormLines.Add($norm)
      }
    }

    if ($CheckOrder) {
      $check = Test-SceneLinesAgainstSource -SceneLines $sceneLines -SourceLines $sourceLines -StripSpeakerPrefixesInAssets:$StripSpeakerPrefixesInAssets -IgnoreThoughtPrefix:$IgnoreThoughtPrefix -NormalizePunctuation:$NormalizePunctuation
      $uCount = $check.Unmatched.Count
      $chapterOrderUnmatched += $uCount

      if ($uCount -gt 0) {
        $anyMismatch = $true
        $sceneReports.Add([pscustomobject]@{ Scene = $sf.Name; Unmatched = $check.Unmatched })
      }
    }
  }

  $overall.TotalSentences += $chapterTotalSentences
  $overall.OrderUnmatched += $chapterOrderUnmatched

  # Coverage: any source line not present in ANY asset line.
  $missingSource = New-Object System.Collections.Generic.List[object]
  for ($i = 0; $i -lt $sourceLines.Count; $i++) {
    $probe = Get-NormalizedLine -Line $sourceLines[$i] -StripSpeakerPrefix -StripThoughtPrefix:$IgnoreThoughtPrefix -NormalizePunctuation:$NormalizePunctuation

    $ok = $assetLineSet.Contains($probe)

    if (-not $ok -and $AllowSubstringCoverage -and $probe -and $probe.Length -ge $MinSubstringLength) {
      foreach ($al in $assetNormLines) {
        if ($al.Contains($probe)) { $ok = $true; break }
        if ($al.Length -ge $MinSubstringLength -and $probe.Contains($al)) { $ok = $true; break }
      }
    }

    if (-not $ok) {
      $missingSource.Add([pscustomobject]@{ LineNumber = $i + 1; Source = $sourceLines[$i] })
    }
  }

  $overall.SourceMissing += $missingSource.Count
  if ($missingSource.Count -gt 0) { $anyMismatch = $true }

  Write-Host "==== $key ===="
  Write-Host "Source: $([System.IO.Path]::GetFileName($sourcePath))"
  Write-Host "Scenes parsed: $($sceneFiles.Count), sentences parsed: $chapterTotalSentences"
  if ($CheckOrder) {
    Write-Host "Scene lines unmatched (order/subsequence check): $chapterOrderUnmatched"
  } else {
    Write-Host "Scene lines unmatched (order/subsequence check): (skipped)"
  }
  Write-Host "Source lines missing from ALL scenes: $($missingSource.Count)"

  if ($sceneReports.Count -gt 0 -and $ShowExamples -gt 0) {
    foreach ($sr in ($sceneReports | Select-Object -First $ShowExamples)) {
      Write-Host "-- Scene: $($sr.Scene)"
      foreach ($u in ($sr.Unmatched | Select-Object -First 5)) {
        Write-Host "   UNMATCHED[$($u.Index)]: $($u.Scene)"
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
Write-Host "Order unmatched:    $($overall.OrderUnmatched)"
Write-Host "Missing source lines: $($overall.SourceMissing)"

if ($anyMismatch) {
  exit 1
}

exit 0
