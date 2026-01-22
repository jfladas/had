[CmdletBinding(SupportsShouldProcess=$true)]
param(
  [Parameter(Mandatory=$true)]
  [string]$ProjectRoot,

  [switch]$IncludeAllStoryScenes
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$encodingNoBom = New-Object System.Text.UTF8Encoding($false)
$enc1252 = [System.Text.Encoding]::GetEncoding(1252)
$encUtf8 = [System.Text.Encoding]::UTF8

function Repair-MojibakeWindows1252ToUtf8 {
  param([Parameter(Mandatory=$true)][string]$Text)
  # Reverse common mojibake where UTF-8 bytes were decoded as Windows-1252 and then saved.
  return $encUtf8.GetString($enc1252.GetBytes($Text))
}

function Replace-All {
  param(
    [Parameter(Mandatory=$true)][string]$Text,
    [Parameter(Mandatory=$true)][hashtable]$Map
  )

  $out = $Text
  foreach ($key in $Map.Keys) {
    $out = $out.Replace([string]$key, [string]$Map[$key])
  }
  return $out
}

$targetFolder = if ($IncludeAllStoryScenes) {
  Join-Path $ProjectRoot 'had\Assets\Story\Scenes'
} else {
  Join-Path $ProjectRoot 'had\Assets\Story\Scenes\Scarlet'
}

if (-not (Test-Path -LiteralPath $targetFolder)) {
  throw "Target folder not found: $targetFolder"
}

$files = Get-ChildItem -LiteralPath $targetFolder -Filter '*.asset' -File -Recurse

# After repairing mojibake, align to Scarlet source conventions:
# - straight apostrophes
# - no en-dash / em-dash (source uses '-')
$unicodeToSource = @{
  ([string][char]0x2019) = "'"  # ’
  ([string][char]0x2018) = "'"  # ‘
  ([string][char]0x2014) = '-'   # —
  ([string][char]0x2013) = '-'   # –
}

# Unity often stores typographic punctuation via escapes inside quoted YAML scalars.
$escapesToSource = @{
  '\u2019' = "'"
  '\u2018' = "'"
  '\u2014' = '-'
  '\u2013' = '-'
}

$changedFiles = 0
$totalReplacements = 0

foreach ($f in $files) {
  $before = [System.IO.File]::ReadAllText($f.FullName, $encodingNoBom)
  $after = $before

  # Only run mojibake repair if the file contains common markers.
  $containsMojibakeMarker = $after.Contains([string][char]0x00E2) -or $after.Contains([string][char]0x00C3)
  if ($containsMojibakeMarker) {
    $repaired = Repair-MojibakeWindows1252ToUtf8 -Text $after

    # Heuristic: accept repair if it reduces obvious mojibake markers.
    $badBefore = ([regex]::Matches($after, '[\u00C3\u00E2]').Count)
    $badAfter  = ([regex]::Matches($repaired, '[\u00C3\u00E2]').Count)
    if ($badAfter -lt $badBefore) {
      $after = $repaired
    }
  }

  $after = Replace-All -Text $after -Map $escapesToSource
  $after = Replace-All -Text $after -Map $unicodeToSource

  if ($after -ne $before) {
    if ($PSCmdlet.ShouldProcess($f.FullName, 'Fix mojibake/punctuation in StoryScene asset')) {
      [System.IO.File]::WriteAllText($f.FullName, $after, $encodingNoBom)
    }
    $changedFiles++

    # Rough replacement count estimate for reporting
    $delta = ($before.Length - $after.Length)
    $totalReplacements += [Math]::Abs($delta)
  }
}

Write-Output "Fixed files: $changedFiles / $($files.Count)"
Write-Output "(Note) totalReplacements is a rough estimate: $totalReplacements"
