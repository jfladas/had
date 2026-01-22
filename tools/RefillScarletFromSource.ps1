param(
  [string]$ScarletScenesRoot = 'C:\Users\lukas\Code\had\had\Assets\Story\Scenes\Scarlet',
  [string]$ScarletSourceRoot = 'C:\Users\lukas\Code\had\had\Assets\Story'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Normalize([string]$s) {
  if ($null -eq $s) { return '' }
  $s = $s.Trim()
  if ($s.StartsWith('"') -and $s.EndsWith('"') -and $s.Length -ge 2) {
    $s = $s.Substring(1, $s.Length - 2)
  }
  $s = $s -replace '\\"', '"'
  $s = $s -replace '\s+', ' '
  return $s.Trim()
}

function Load-SourceLines([string]$path) {
  $raw = Get-Content -LiteralPath $path
  $out = New-Object System.Collections.Generic.List[string]

  foreach ($line in $raw) {
    $t = $line.Trim()
    if ($t -eq '') { continue }

    if ($t -match '^(Chapter|Epilogue)\b') { continue }

    if ($t -match '^(CHOICE OPTION|MINIGAME|ILLUSTRATION|TIME SKIP|HERE CHECK|IF ENOUGH|CONTINUE HERE|\(SCARLET POV|\(BACK TO|/\*|\*/|THE END)') { continue }

    if ($t.StartsWith('(t):')) {
      $t = $t.Substring(4).Trim()
    }

    if ($t -match '^([^:]{1,40}):\s+(.*)$') {
      $speaker = $Matches[1]
      $rest = $Matches[2]
      if ($speaker -notmatch '^(CHOICE OPTION|Chapter|Epilogue)$') {
        $t = $rest
      }
    }

    $out.Add($t)
  }

  return ,$out.ToArray()
}

function Quote-IfNeeded([string]$t) {
  if ($null -eq $t) { return '' }

  # YAML safety: quote if ": " occurs, starts with '{', starts with '?', starts with '-', contains quotes
  if ($t -match ':\s' -or $t.StartsWith('{') -or $t.StartsWith('?') -or $t.StartsWith('-') -or $t.Contains('"')) {
    $escaped = $t.Replace('"', '\\"')
    return '"' + $escaped + '"'
  }

  return $t
}

function Fill-EmptyTextInFile([string]$assetPath, [string[]]$sourceLines) {
  $lines = Get-Content -LiteralPath $assetPath
  if (-not ($lines | Select-String -Pattern 'm_EditorClassIdentifier: Assembly-CSharp::StoryScene' -Quiet)) {
    return 0
  }

  $sentenceIdx = @()
  $sentenceText = @()

  for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^\s*-\s*text:\s*(.*)$') {
      $sentenceIdx += $i
      $val = $Matches[1]
      if ([string]::IsNullOrWhiteSpace($val)) {
        $sentenceText += $null
      } else {
        $sentenceText += (Normalize $val)
      }
    }
  }

  if ($sentenceIdx.Count -eq 0) { return 0 }

  $anchors = @()
  foreach ($t in $sentenceText) {
    if ($t) { $anchors += $t }
  }

  if ($anchors.Count -eq 0) { return 0 }

  $srcNorm = $sourceLines | ForEach-Object { Normalize $_ }

  $pos = 0
  $matched = @()

  foreach ($a in $anchors) {
    for ($j = $pos; $j -lt $srcNorm.Count; $j++) {
      if ($srcNorm[$j] -eq (Normalize $a)) {
        $matched += $j
        $pos = $j + 1
        break
      }
    }
  }

  if ($matched.Count -eq 0) { return 0 }

  $spanStart = $matched[0]
  $spanEnd = $matched[$matched.Count - 1]
  if ($spanEnd -lt $spanStart) { $spanEnd = $spanStart }

  $ptr = $spanStart
  $filled = 0

  for ($k = 0; $k -lt $sentenceIdx.Count; $k++) {
    $idx = $sentenceIdx[$k]
    $t = $sentenceText[$k]

    if ($t) {
      while ($ptr -le $spanEnd -and (Normalize $sourceLines[$ptr]) -ne (Normalize $t)) {
        $ptr++
      }
      if ($ptr -le $spanEnd) { $ptr++ }
      continue
    }

    if ($ptr -le $spanEnd) {
      $newText = $sourceLines[$ptr]
      $ptr++
      $indent = [regex]::Match($lines[$idx], '^(\s*)-').Groups[1].Value
      $lines[$idx] = $indent + '- text: ' + (Quote-IfNeeded $newText)
      $filled++
    }
  }

  if ($filled -gt 0) {
    Set-Content -LiteralPath $assetPath -Value $lines -Encoding UTF8
  }

  return $filled
}

function Fill-EmptyTextInFileExpandedSpan([string]$assetPath, [string[]]$sourceLines, [int]$contextWindow = 80) {
  $lines = Get-Content -LiteralPath $assetPath
  if (-not ($lines | Select-String -Pattern 'm_EditorClassIdentifier: Assembly-CSharp::StoryScene' -Quiet)) {
    return 0
  }

  $sentenceIdx = @()
  $sentenceText = @()

  for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^\s*-\s*text:\s*(.*)$') {
      $sentenceIdx += $i
      $val = $Matches[1]
      if ([string]::IsNullOrWhiteSpace($val)) {
        $sentenceText += $null
      } else {
        $sentenceText += (Normalize $val)
      }
    }
  }

  if ($sentenceIdx.Count -eq 0) { return 0 }
  if (-not ($sentenceText | Where-Object { $_ -eq $null })) { return 0 }

  $anchors = @()
  foreach ($t in $sentenceText) { if ($t) { $anchors += $t } }
  if ($anchors.Count -eq 0) { return 0 }

  $srcNorm = $sourceLines | ForEach-Object { Normalize $_ }

  $pos = 0
  $matched = @()
  foreach ($a in $anchors) {
    for ($j = $pos; $j -lt $srcNorm.Count; $j++) {
      if ($srcNorm[$j] -eq (Normalize $a)) {
        $matched += $j
        $pos = $j + 1
        break
      }
    }
  }

  if ($matched.Count -eq 0) { return 0 }

  $spanStart = [Math]::Max(0, $matched[0] - $contextWindow)
  $spanEnd = [Math]::Min($srcNorm.Count - 1, $matched[$matched.Count - 1] + $contextWindow)
  $ptr = $spanStart

  $filled = 0
  for ($k = 0; $k -lt $sentenceIdx.Count; $k++) {
    $idx = $sentenceIdx[$k]
    $t = $sentenceText[$k]

    if ($t) {
      # Advance within the span until we match this anchor.
      while ($ptr -le $spanEnd -and (Normalize $sourceLines[$ptr]) -ne (Normalize $t)) {
        $ptr++
      }
      if ($ptr -le $spanEnd) { $ptr++ }
      continue
    }

    # Fill from the next available line within the span.
    if ($ptr -le $spanEnd) {
      $newText = $sourceLines[$ptr]
      $ptr++
      $indent = [regex]::Match($lines[$idx], '^(\s*)-').Groups[1].Value
      $lines[$idx] = $indent + '- text: ' + (Quote-IfNeeded $newText)
      $filled++
    }
  }

  if ($filled -gt 0) {
    Set-Content -LiteralPath $assetPath -Value $lines -Encoding UTF8
  }

  return $filled
}

function Fill-EmptyTextInFileSequential([string]$assetPath, [string[]]$sourceLines, [ref]$pointer) {
  $lines = Get-Content -LiteralPath $assetPath
  if (-not ($lines | Select-String -Pattern 'm_EditorClassIdentifier: Assembly-CSharp::StoryScene' -Quiet)) {
    return 0
  }

  $srcNorm = $sourceLines | ForEach-Object { Normalize $_ }

  $sentenceIdx = @()
  $sentenceText = @()
  for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^\s*-\s*text:\s*(.*)$') {
      $sentenceIdx += $i
      $val = $Matches[1]
      if ([string]::IsNullOrWhiteSpace($val)) {
        $sentenceText += $null
      } else {
        $sentenceText += (Normalize $val)
      }
    }
  }

  if ($sentenceIdx.Count -eq 0) { return 0 }

  $filled = 0

  $maxLookahead = 300

  for ($k = 0; $k -lt $sentenceIdx.Count; $k++) {
    $idx = $sentenceIdx[$k]
    $t = $sentenceText[$k]

    if ($t) {
      # Try to match this anchor within a bounded lookahead window.
      $anchor = Normalize $t
      $start = [Math]::Max(0, $pointer.Value)
      $end = [Math]::Min($srcNorm.Count - 1, $start + $maxLookahead)
      $found = $false
      for ($p = $start; $p -le $end; $p++) {
        if ($srcNorm[$p] -eq $anchor) {
          $pointer.Value = $p + 1
          $found = $true
          break
        }
      }
      if (-not $found) {
        # Do not advance the pointer; keep it available for filling empties.
      }
      continue
    }

    if ($pointer.Value -lt $sourceLines.Count) {
      $newText = $sourceLines[$pointer.Value]
      $pointer.Value++
      $indent = [regex]::Match($lines[$idx], '^(\s*)-').Groups[1].Value
      $lines[$idx] = $indent + '- text: ' + (Quote-IfNeeded $newText)
      $filled++
    }
  }

  if ($filled -gt 0) {
    Set-Content -LiteralPath $assetPath -Value $lines -Encoding UTF8
  }

  return $filled
}

function Get-NaturalSortKey([string]$name) {
  return ([regex]::Replace($name, '\d+', { param($m) $m.Value.PadLeft(6, '0') }))
}

$groups = @(
  @{ Source = 'Scarlet Chapter 7 Text'; Pattern = 'S7*.asset' },
  @{ Source = 'Scarlet Chapter 8 Text'; Pattern = 'S8*.asset' },
  @{ Source = 'Scarlet Chapter 9 Text'; Pattern = 'S9*.asset' },
  @{ Source = 'Scarlet Chapter 10 Text'; Pattern = 'S10*.asset' },
  @{ Source = 'Scarlet Chapter 11 Text'; Pattern = 'S11*.asset' },
  @{ Source = 'Scarlet Chapter 12 Text'; Pattern = 'S12*.asset' },
  @{ Source = 'Scarlet Chapter 13 Text'; Pattern = 'S13*.asset' },
  @{ Source = 'Scarlet Chapter 14 Text'; Pattern = 'S14*.asset' },
  @{ Source = 'Scarlet Chapter 15 Text'; Pattern = 'S15*.asset' },
  @{ Source = 'Scarlet Epilogue 1 Text'; Pattern = 'SE1*.asset' },
  @{ Source = 'Scarlet Epilogue 2 Text'; Pattern = 'SE2*.asset' },
  @{ Source = 'Scarlet Epilogue 3 Text'; Pattern = 'SE3*.asset' }
)

foreach ($g in $groups) {
  $srcPath = Join-Path $ScarletSourceRoot $g.Source
  if (-not (Test-Path -LiteralPath $srcPath)) {
    Write-Output "SKIP: missing source '$srcPath'"
    continue
  }

  $sourceLines = Load-SourceLines $srcPath
  $assets = Get-ChildItem -LiteralPath $ScarletScenesRoot -Filter $g.Pattern -File |
    Where-Object { Select-String -LiteralPath $_.FullName -Pattern 'm_EditorClassIdentifier: Assembly-CSharp::StoryScene' -Quiet } |
    Sort-Object { Get-NaturalSortKey $_.Name }

  $totalFilled = 0
  $touched = 0
  $ptr = 0

  foreach ($a in $assets) {
    $filled = Fill-EmptyTextInFileSequential $a.FullName $sourceLines ([ref]$ptr)
    if ($filled -gt 0) {
      $touched++
      $totalFilled += $filled
    }
  }

  # Second pass: some branch scenes have too few anchors; try an expanded-span fill.
  $secondPassFilled = 0
  $secondPassTouched = 0
  foreach ($a in $assets) {
    if (Select-String -LiteralPath $a.FullName -Pattern '^\s*-\s*text:\s*$' -Quiet) {
      $f2 = Fill-EmptyTextInFileExpandedSpan $a.FullName $sourceLines 120
      if ($f2 -gt 0) {
        $secondPassTouched++
        $secondPassFilled += $f2
      }
    }
  }

  $sumFilled = $totalFilled + $secondPassFilled
  $sumTouched = ($touched + $secondPassTouched)
  Write-Output ("{0}: filled {1} empty text entries across {2} files (seq {3}/{4} lines; 2nd-pass {5} fills in {6} files)" -f $g.Source, $sumFilled, $sumTouched, $ptr, $sourceLines.Count, $secondPassFilled, $secondPassTouched)
}
