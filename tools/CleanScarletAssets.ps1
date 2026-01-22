[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [string]$ProjectRoot,

  [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scarletScenesFolder = Join-Path $ProjectRoot 'had\Assets\Story\Scenes\Scarlet'
if (-not (Test-Path -LiteralPath $scarletScenesFolder)) {
  throw "Scarlet scenes folder not found: $scarletScenesFolder"
}

function Escape-YamlDoubleQuoted {
  param([Parameter(Mandatory = $true)][string]$Text)

  $s = $Text
  $s = $s -replace '\\', '\\\\'
  $s = $s -replace '"', '\\"'
  return $s
}

function Needs-YamlQuoting {
  param([Parameter(Mandatory = $true)][string]$Unquoted)

  $v = $Unquoted

  if ($v -match '^\s*[\{\[]') { return $true } # flow mapping/sequence starters
  if ($v -match ':\s') { return $true }          # mapping ambiguity
  if ($v -match '^\s*[\-\?\!\*\&]') { return $true } # YAML indicators
  if ($v -match '#') { return $true }            # could become comment

  return $false
}

function Strip-ThoughtPrefix {
  param(
    [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value,
    [switch]$NormalizeYamlQuoting
  )

  $v = $Value

  # Remove literal carriage returns that sometimes end up embedded in the scalar.
  $v = $v -replace "`r", ''

  $trimLeft = $v.TrimStart()
  $isQuoted = $trimLeft.StartsWith('"')

  if ($isQuoted) {
    $v2 = [regex]::Replace($v, '^(\s*")\(t\):\s*', '$1')
    return $v2
  }

  $v2 = [regex]::Replace($v, '^\s*\(t\):\s*', '')
  if ($NormalizeYamlQuoting -and $v2 -ne $v) {
    $trimmed = $v2.Trim()
    if (Needs-YamlQuoting -Unquoted $trimmed) {
      $escaped = Escape-YamlDoubleQuoted -Text $trimmed
      return '"' + $escaped + '"'
    }

    return $trimmed
  }

  return $v2
}

$assets = Get-ChildItem -LiteralPath $scarletScenesFolder -Filter '*.asset' -File -ErrorAction Stop

$changedFiles = 0
$removedBlankLines = 0
$removedThoughtPrefixes = 0
$fixedEmbeddedCR = 0

foreach ($asset in $assets) {
  $path = $asset.FullName
  $raw = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

  # Fix rare CRCRLF sequences (embedded CR before newline).
  $raw2 = $raw -replace "`r`r`n", "`r`n"
  if ($raw2 -ne $raw) { $fixedEmbeddedCR++ }

  $lines = $raw2 -split "\r?\n"

  $outLines = New-Object System.Collections.Generic.List[string]
  $changed = $false

  for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]

    $mText = [regex]::Match($line, '^(\s*-\s*text:\s*)(.*)$')
    $mAlt = [regex]::Match($line, '^(\s*alternativeText:\s*)(.*)$')

    if ($mText.Success -or $mAlt.Success) {
      $prefix = if ($mText.Success) { $mText.Groups[1].Value } else { $mAlt.Groups[1].Value }
      $value = if ($mText.Success) { $mText.Groups[2].Value } else { $mAlt.Groups[2].Value }

      $before = $value
      $after = Strip-ThoughtPrefix -Value $value -NormalizeYamlQuoting

      if ($after -ne $before) {
        $removedThoughtPrefixes++
        $changed = $true
        $line = $prefix + $after
      } else {
        # Still strip embedded CRs even if no (t): change.
        $v2 = $value -replace "`r", ''
        if ($v2 -ne $value) {
          $changed = $true
          $line = $prefix + $v2
        }
      }

      $outLines.Add($line)

      # Remove blank line(s) immediately after alternativeText lines.
      if ($mAlt.Success) {
        while (($i + 1) -lt $lines.Count -and $lines[$i + 1].Trim() -eq '') {
          $i++
          $removedBlankLines++
          $changed = $true
        }
      }

      continue
    }

    $outLines.Add($line)
  }

  if ($changed) {
    $changedFiles++
    if (-not $WhatIf) {
      $outText = ($outLines -join "`r`n")
      # Preserve final newline similar to Unity serialization style.
      if (-not $outText.EndsWith("`r`n")) { $outText += "`r`n" }
      [System.IO.File]::WriteAllText($path, $outText, [System.Text.Encoding]::UTF8)
    }
  }
}

Write-Host "Processed $($assets.Count) assets. Changed files: $changedFiles."
Write-Host "Removed blank lines after alternativeText: $removedBlankLines."
Write-Host "Thought prefixes removed: $removedThoughtPrefixes."
Write-Host "Files with CRCRLF fixed: $fixedEmbeddedCR."
