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

# Speakers known to appear in Scarlet route scripts/assets.
# Note: Escaped for regex alternation.
$speakerPattern = '(?:Me|Scarlet|Mr\.\s*Moon|Miss\s*Moon|Ray|Seth|Lu|Amelie|Amon|Attendant|\?\?\?)'

function Escape-YamlDoubleQuoted {
  param([Parameter(Mandatory = $true)][string]$Text)

  # Minimal escaping for Unity-style YAML double-quoted scalars.
  $s = $Text
  $s = $s -replace '\\', '\\\\'
  $s = $s -replace '"', '\\"'
  return $s
}

function Needs-YamlQuoting {
  param([Parameter(Mandatory = $true)][string]$Unquoted)

  $v = $Unquoted

  if ($v -match '^\s*[\{\[]') { return $true } # flow mapping/sequence starters
  if ($v -match ':\s') { return $true }          # "key: value" ambiguity
  if ($v -match '^\s*[\-\?\!\*\&]') { return $true } # YAML indicators
  if ($v -match '#') { return $true }            # could become comment

  return $false
}

$assets = Get-ChildItem -LiteralPath $scarletScenesFolder -Filter '*.asset' -File -ErrorAction Stop

$changedCount = 0
foreach ($asset in $assets) {
  $path = $asset.FullName
  $raw = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
  $lines = $raw -split "\r?\n"

  $changed = $false

  for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]

    $mText = [regex]::Match($line, '^(\s*-\s*text:\s*)(.*)$')
    $mAlt = [regex]::Match($line, '^(\s*alternativeText:\s*)(.*)$')
    if (-not $mText.Success -and -not $mAlt.Success) { continue }

    $prefix = $null
    $value = $null
    if ($mText.Success) { $prefix = $mText.Groups[1].Value; $value = $mText.Groups[2].Value }
    else { $prefix = $mAlt.Groups[1].Value; $value = $mAlt.Groups[2].Value }

    if ($value -eq '') { continue }

    # Detect quoting.
    $isQuoted = $value.TrimStart().StartsWith('"')

    $newValue = $value

    if ($isQuoted) {
      # Replace inside the opening quote only.
      $newValue = [regex]::Replace(
        $newValue,
        '^(\s*")' + $speakerPattern + ':\s*',
        '$1',
        'IgnoreCase'
      )
    } else {
      $newValue = [regex]::Replace(
        $newValue,
        '^\s*' + $speakerPattern + ':\s*',
        '',
        'IgnoreCase'
      )

      # If we changed it and it now needs quoting, quote it.
      if ($newValue -ne $value) {
        $trimmed = $newValue.Trim()
        if (Needs-YamlQuoting -Unquoted $trimmed) {
          $escaped = Escape-YamlDoubleQuoted -Text $trimmed
          $newValue = '"' + $escaped + '"'
        } else {
          # Keep a single leading space removed by regex; normalize to same style as existing assets.
          $newValue = $trimmed
        }
      }
    }

    if ($newValue -ne $value) {
      $lines[$i] = $prefix + $newValue
      $changed = $true
    }
  }

  if ($changed) {
    $changedCount++
    if (-not $WhatIf) {
      $outText = ($lines -join "`r`n")
      [System.IO.File]::WriteAllText($path, $outText, [System.Text.Encoding]::UTF8)
    }
  }
}

Write-Host "Processed $($assets.Count) assets. Changed: $changedCount."
