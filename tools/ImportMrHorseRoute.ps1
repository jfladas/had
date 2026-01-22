param(
    [string]$InputPath = "had/Assets/Story/Mr. Horse Story Text",
    [string]$OutputDir = "had/Assets/Story/Scenes/MrHorse",
    [int]$MinigameLevelStart = 15,
    # Numeric level value stored in MinigameScene assets. Kept separate from the
    # minigame index used in asset names (e.g. H9_M16) so we can restart levels at 1.
    [int]$MinigameLevelValueStart = 1
)

$ErrorActionPreference = 'Stop'

function New-GuidString {
    return ([guid]::NewGuid().ToString('N'))
}

function Ensure-Dir([string]$path) {
    if (!(Test-Path -LiteralPath $path)) {
        New-Item -ItemType Directory -Path $path | Out-Null
    }
}

function Read-AllLines([string]$path) {
    # Preserve unicode punctuation etc.
    return Get-Content -LiteralPath $path -Encoding UTF8
}

function Write-TextFile([string]$path, [string]$content) {
    $dir = Split-Path -Parent $path
    Ensure-Dir $dir
    Set-Content -LiteralPath $path -Value $content -Encoding UTF8
}

function Write-Meta([string]$metaPath, [string]$guid) {
    $meta = @" 
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@.TrimStart()
    Write-TextFile $metaPath $meta
}

function Escape-YamlScalar([string]$s) {
    if ($null -eq $s) { return "" }

    # Normalize Windows CRLF safety
    $s = $s -replace "\r\n?", "\n"

    # Use double-quoted YAML scalar when needed.
    $needsQuotes = $false
    if ($s -match "^\s" -or $s -match "\s$" -or $s -match ":\s") { $needsQuotes = $true }
    if ($s -match "\n") { $needsQuotes = $true }
    if ($s -match '[\[\]{}#,>&*!|%@$`"]') { $needsQuotes = $true }
    if ($s -match "^(yes|no|true|false|null|~)$") { $needsQuotes = $true }
    if ($s -match "^-\s") { $needsQuotes = $true }

    if (!$needsQuotes) { return $s }

    $escaped = $s.Replace('\\', '\\\\').Replace('"', '\\"')
    $escaped = $escaped.Replace("`n", "\\n")
    return '"' + $escaped + '"'
}

# Script GUIDs (Unity .cs meta GUIDs)
$StorySceneScriptGuid = "51fb4c6070f247e48b4377b5728b8182"
$ChooseSceneScriptGuid = "e9b1b52c21e349d4198396f2e1d2dd0f"
$ChapterSceneScriptGuid = "0e4a526d5bd70df4d9034bcf1a1de57a"
$MinigameSceneScriptGuid = "9cc4303f8f262e0429d6312637f3b265"
$CharacterScriptGuid = "a0e6a862dae1ccf488ae4d4570f9f88c"

# Character asset GUIDs
$Char_None = "221cb615d975cab419307e866ebd5c5c"
$Char_Think = "4849dc22c0fc77f45b58cdfcf9fc156e"
$Char_Me = "d69d53562766de143bf061768e3de597"
$Char_Unknown = "e98f6dd1b5f6d3e4e87cd47b6a20f514"
$Char_MrHorse = "3db0785a05cdb804ead5133abfe270c1"

# Background sprite GUIDs (resolved from meta files for safety)
$Bg_Bunker = $null
$Bg_Street = $null
$Bg_Fields = $null
$Bg_White = $null
$Bg_Apartment = $null
$Bg_Gym = $null
$Bg_RoyalPalace = $null
$Bg_Minigame = $null
$Bg_TBC = $null

# Chapter background sprites (c7h.png etc)
$ChapterBgGuids = @{}

function Resolve-GuidFromMeta([string]$metaPath) {
    if (!(Test-Path -LiteralPath $metaPath)) { return $null }
    $line = Select-String -LiteralPath $metaPath -Pattern '^guid:\s*(.+)$' | Select-Object -First 1
    if ($null -eq $line) { return $null }
    return ($line.Matches[0].Groups[1].Value).Trim()
}

# Resolve background GUIDs from actual meta files so we don't hardcode wrong values.
$Bg_Bunker = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/bunker.PNG.meta"
$Bg_Street = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/street.PNG.meta"
$Bg_Fields = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/fields.png.meta"
$Bg_White = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/white.png.meta"
$Bg_Apartment = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/apartment.PNG.meta"
$Bg_Gym = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/gym.png.meta"
$Bg_RoyalPalace = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/royalpalace.png.meta"
$Bg_Minigame = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/minigame.png.meta"
$Bg_TBC = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/Chapters/tbc.png.meta"

$ChapterBgGuids[7] = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/Chapters/c7h.png.meta"
$ChapterBgGuids[8] = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/Chapters/c8h.png.meta"
$ChapterBgGuids[9] = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/Chapters/c9h.png.meta"
$ChapterBgGuids[10] = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/Chapters/c10h.png.meta"
$ChapterBgGuids[11] = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/Chapters/c11h.png.meta"
$ChapterBgGuids[12] = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/Chapters/c12h.png.meta"
$ChapterBgGuids[13] = Resolve-GuidFromMeta "had/Assets/Sprites/Backgrounds/Chapters/c13h.png.meta"

foreach ($k in @('Bg_Bunker','Bg_Street','Bg_Fields','Bg_White','Bg_Apartment','Bg_Gym','Bg_RoyalPalace','Bg_Minigame','Bg_TBC')) {
    if ([string]::IsNullOrEmpty((Get-Variable -Name $k -ValueOnly))) {
        throw "Failed to resolve GUID for $k"
    }
}
foreach ($c in 7..13) {
    if ([string]::IsNullOrEmpty($ChapterBgGuids[$c])) {
        throw "Failed to resolve chapter background GUID for chapter $c (c${c}h.png.meta)"
    }
}

function Parse-SpeakerLine([string]$line) {
    # Returns @{ speaker = <string or null>; text = <string> ; kind = 'dialogue'|'thought'|'narration' }
    $trim = $line.Trim()

    if ($trim -eq "" ) { return $null }

    # Skip directives
    if ($trim -match '^>\s*CONTINUE HERE AFTER CHOICE OPTIONS') { return @{ kind = 'directive'; directive = 'continue' } }
    if ($trim -match '^CHOICE OPTION\s+\d+\s*:') {
        $label = ($trim -replace '^CHOICE OPTION\s+\d+\s*:\s*', '')
        return @{ kind = 'directive'; directive = 'choice'; label = $label }
    }
    if ($trim -match '^-MINIGAME-$') { return @{ kind = 'directive'; directive = 'minigame' } }
    if ($trim -match '^-ILLUSTRATION\s+.+-$') { return @{ kind = 'directive'; directive = 'illustration' } }
    if ($trim -match '^-$') { return $null }

    # Thoughts
    if ($trim -match '^\(t\):\s*(.+)$') {
        return @{ kind = 'thought'; speaker = '(t)'; text = $Matches[1] }
    }

    # Speaker format: "Name: text"
    $m = [regex]::Match($trim, '^(?<sp>[^:]{1,40}):\s*(?<tx>.*)$')
    if ($m.Success) {
        $sp = $m.Groups['sp'].Value.Trim()
        $tx = $m.Groups['tx'].Value
        return @{ kind = 'dialogue'; speaker = $sp; text = $tx }
    }

    return @{ kind = 'narration'; speaker = $null; text = $trim }
}

function TryResolve-CharacterGuidByAssetName([string]$speakerName) {
    if ([string]::IsNullOrEmpty($speakerName)) { return $null }

    $candidateMeta = "had/Assets/Story/Chars/$speakerName.asset.meta"
    $g = Resolve-GuidFromMeta $candidateMeta
    if (![string]::IsNullOrEmpty($g)) { return $g }

    return $null
}

function Get-CharacterGuidForSpeaker([string]$speaker) {
    if ([string]::IsNullOrEmpty($speaker)) { return $Char_None }

    switch ($speaker) {
        'Me' { return $Char_Me }
        'Mr. Horse' { return $Char_MrHorse }
        'Horse' { return $Char_MrHorse }
        '???' { return $Char_Unknown }
        'Angry Voice' {
            $resolved = TryResolve-CharacterGuidByAssetName 'Angry Voice'
            if (![string]::IsNullOrEmpty($resolved)) { return $resolved }
            return $Char_None
        }
        'AV' {
            $resolved = TryResolve-CharacterGuidByAssetName 'Angry Voice'
            if (![string]::IsNullOrEmpty($resolved)) { return $resolved }
            return $Char_None
        }
        'Compliant Voice' {
            $resolved = TryResolve-CharacterGuidByAssetName 'Compliant Voice'
            if (![string]::IsNullOrEmpty($resolved)) { return $resolved }
            return $Char_None
        }
        'CV' {
            $resolved = TryResolve-CharacterGuidByAssetName 'Compliant Voice'
            if (![string]::IsNullOrEmpty($resolved)) { return $resolved }
            return $Char_None
        }
        default {
            # Prefer existing character assets; otherwise fall back to NONE.
            $resolved = TryResolve-CharacterGuidByAssetName $speaker
            if (![string]::IsNullOrEmpty($resolved)) { return $resolved }
            return $Char_None
        }
    }
}

function Make-SentenceYaml([string]$text, [string]$characterGuid, [bool]$showMrHorse, [int]$mrHorseSpriteIndex) {
    $yamlText = Escape-YamlScalar $text

    $actionsYaml = "    actions: []`n"
    if ($showMrHorse) {
        # type: 1 == SHOW
        $actionsYaml = (@"
    actions:
    - character: {fileID: 11400000, guid: $Char_MrHorse, type: 2}
      spriteIndex: $mrHorseSpriteIndex
      type: 1
      position: {x: 0, y: 0}
      speed: 0
      targetScale: 1
"@) + "`n"
    }

    return (
        "  - text: $yamlText`n" +
        "    alternativeText: `n" +
        "    character: {fileID: 11400000, guid: $characterGuid, type: 2}`n" +
        $actionsYaml +
        "    music: {fileID: 0}`n" +
        "    sound: {fileID: 0}`n"
    )
}

function Write-StorySceneAsset([string]$path, [string]$name, [string]$bgGuid, [string]$nextGuid, [array]$sentencesYaml) {
    $nextLine = "nextScene: {fileID: 0}"
    if (![string]::IsNullOrEmpty($nextGuid)) {
        $nextLine = "nextScene: {fileID: 11400000, guid: $nextGuid, type: 2}"
    }

    $sentencesBlock = "sentences: []"
    if ($sentencesYaml.Count -gt 0) {
        $sentencesBlock = "sentences:`n" + ($sentencesYaml -join "")
    }

    $yaml = @" 
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $StorySceneScriptGuid, type: 3}
  m_Name: $name
  m_EditorClassIdentifier: 
  $sentencesBlock
  background: {fileID: 21300000, guid: $bgGuid, type: 3}
  $nextLine
"@.TrimStart()

    Write-TextFile $path $yaml
}

function Write-ChooseSceneAsset([string]$path, [string]$name, [array]$labels, [string]$bgGuid) {
    $labelsYaml = "labels: []"
    if ($labels.Count -gt 0) {
        $labelsYaml = "labels:`n" + ($labels -join "")
    }

    $bgLine = "background: {fileID: 0}"
    if (![string]::IsNullOrEmpty($bgGuid)) {
        $bgLine = "background: {fileID: 21300000, guid: $bgGuid, type: 3}"
    }

    $yaml = @" 
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $ChooseSceneScriptGuid, type: 3}
  m_Name: $name
  m_EditorClassIdentifier: 
  $bgLine
  $labelsYaml
"@.TrimStart()

    Write-TextFile $path $yaml
}

function Write-MinigameSceneAsset([string]$path, [string]$name, [int]$level, [string]$nextGuid) {
    $nextLine = "nextScene: {fileID: 0}"
    if (![string]::IsNullOrEmpty($nextGuid)) {
        $nextLine = "nextScene: {fileID: 11400000, guid: $nextGuid, type: 2}"
    }

    $yaml = @" 
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $MinigameSceneScriptGuid, type: 3}
  m_Name: $name
  m_EditorClassIdentifier: 
  background: {fileID: 21300000, guid: $Bg_Minigame, type: 3}
  level: $level
  $nextLine
"@.TrimStart()

    Write-TextFile $path $yaml
}

function Write-ChapterSceneAsset([string]$path, [string]$name, [string]$chapterBgGuid, [string]$nextGuid) {
    $yaml = @" 
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $ChapterSceneScriptGuid, type: 3}
  m_Name: $name
  m_EditorClassIdentifier: 
  background: {fileID: 21300000, guid: $chapterBgGuid, type: 3}
  nextScene: {fileID: 11400000, guid: $nextGuid, type: 2}
"@.TrimStart()

    Write-TextFile $path $yaml
}

# ------------ Build story structure ------------

$inputFull = Join-Path (Get-Location) $InputPath
if (!(Test-Path -LiteralPath $inputFull)) {
    throw "InputPath not found: $inputFull"
}

Ensure-Dir $OutputDir

$lines = Read-AllLines $inputFull

# Parse chapters
$chapters = @{} # chapterNumber -> array of parsed entries
$currentChapter = $null
foreach ($line in $lines) {
    $t = $line.Trim()
    if ($t -match '^Chapter\s+(\d+)\s*$') {
        $currentChapter = [int]$Matches[1]
        if ($currentChapter -lt 7 -or $currentChapter -gt 13) {
            continue
        }
        if (!$chapters.ContainsKey($currentChapter)) {
            $chapters[$currentChapter] = New-Object System.Collections.Generic.List[object]
        }
        continue
    }

    if ($null -eq $currentChapter) { continue }
    if ($currentChapter -lt 7 -or $currentChapter -gt 13) { continue }

    $parsed = Parse-SpeakerLine $line
    if ($null -eq $parsed) { continue }
    $chapters[$currentChapter].Add($parsed) | Out-Null
}

if ($chapters.Keys.Count -eq 0) {
    throw "No chapters 7-13 found in input." 
}

# Reuse existing GUIDs for already-checked-in assets
$existingAssetGuids = @{}
Get-ChildItem -LiteralPath $OutputDir -Filter '*.asset.meta' | ForEach-Object {
    $g = Resolve-GuidFromMeta $_.FullName
    if (![string]::IsNullOrEmpty($g)) {
        $base = [IO.Path]::GetFileNameWithoutExtension([IO.Path]::GetFileNameWithoutExtension($_.Name))
        $existingAssetGuids[$base] = $g
    }
}

function Get-OrCreateAssetGuid([string]$assetName) {
    if ($existingAssetGuids.ContainsKey($assetName)) { return $existingAssetGuids[$assetName] }
    $g = New-GuidString
    $existingAssetGuids[$assetName] = $g
    return $g
}

# Helper: choose chapter default background
function Get-ChapterDefaultBg([int]$chapter, [int]$segmentIndex) {
    switch ($chapter) {
        7 { return $Bg_Bunker }
        8 {
            if ($segmentIndex -eq 0) { return $Bg_Street }
            return $Bg_Fields
        }
        9 { return $Bg_Fields }
        10 {
            if ($segmentIndex -eq 0) { return $Bg_White }
            return $Bg_Apartment
        }
        11 { return $Bg_Gym }
        12 {
            if ($segmentIndex -eq 0) { return $Bg_Apartment }
            return $Bg_RoyalPalace
        }
        13 { return $Bg_RoyalPalace }
        default { return $Bg_Bunker }
    }
}

# Build assets per chapter with simple splitting:
# - one main StoryScene chain
# - each choice becomes a ChooseScene with per-option StoryScene chains
# - minigames become MinigameScene nodes
$minigameLevel = $MinigameLevelStart

# Map chapter -> chapter scene guid
$chapterSceneGuids = @{}
foreach ($ch in 7..13) {
    $chapterSceneGuids[$ch] = Get-OrCreateAssetGuid "HChapter$ch"
}

# TBC scene guid (existing)
$tbcGuid = Resolve-GuidFromMeta "had/Assets/Story/Scenes/TBC.asset.meta"
if ([string]::IsNullOrEmpty($tbcGuid)) { throw "Missing TBC.asset.meta" }

# Create/update ChapterScenes
foreach ($ch in 7..13) {
    $name = "HChapter$ch"
    $assetPath = Join-Path $OutputDir "$name.asset"
    $metaPath = "$assetPath.meta"
    $guid = Get-OrCreateAssetGuid $name

    if (!(Test-Path -LiteralPath $metaPath)) {
        Write-Meta $metaPath $guid
    }

    # next points to first story scene (H{ch}_1)
    $firstStoryName = "H${ch}_1"
    $firstStoryGuid = Get-OrCreateAssetGuid $firstStoryName

    Write-ChapterSceneAsset $assetPath $name $ChapterBgGuids[$ch] $firstStoryGuid
}

# Generate story content
foreach ($ch in 7..13) {
    $entries = $chapters[$ch]
    if ($null -eq $entries) { continue }

    $sceneIndex = 1
    $choiceIndex = 1
    $segmentIndex = 0

    # Build a queue of nodes in the chapter.
    $nodes = New-Object System.Collections.Generic.List[object]

    $currentSentences = New-Object System.Collections.Generic.List[object]

    $inChoice = $false
    $currentChoice = $null

    function Flush-StoryBlock([System.Collections.Generic.List[object]]$sentences, [bool]$force = $false) {
        if ($sentences.Count -eq 0) { return }
        $nodes.Add(@{ type = 'story'; sentences = $sentences.ToArray(); segment = $segmentIndex }) | Out-Null
        $sentences.Clear() | Out-Null
    }

    foreach ($e in $entries) {
        if ($e.kind -eq 'directive') {
            if ($e.directive -eq 'illustration') {
                continue
            }

            if ($e.directive -eq 'minigame') {
                if (!$inChoice) {
                    Flush-StoryBlock $currentSentences
                    $nodes.Add(@{ type = 'minigame'; level = $minigameLevel; levelValue = ($MinigameLevelValueStart + ($minigameLevel - $MinigameLevelStart)); segment = $segmentIndex }) | Out-Null
                } else {
                    $opt = $currentChoice.options[$currentChoice.options.Count - 1]
                    if ($opt.sentences.Count -gt 0) {
                        $opt.nodes.Add(@{ type = 'story'; sentences = $opt.sentences.ToArray(); segment = $opt.optionSegment }) | Out-Null
                        $opt.sentences.Clear() | Out-Null
                    }
                    $opt.nodes.Add(@{ type = 'minigame'; level = $minigameLevel; levelValue = ($MinigameLevelValueStart + ($minigameLevel - $MinigameLevelStart)); segment = $segmentIndex }) | Out-Null
                }
                $minigameLevel++
                $segmentIndex++
                continue
            }

            if ($e.directive -eq 'choice') {
                if (!$inChoice) {
                    Flush-StoryBlock $currentSentences
                    $currentChoice = @{ type = 'choice'; index = $choiceIndex; options = New-Object System.Collections.Generic.List[object] }
                    $choiceIndex++
                    $inChoice = $true
                } else {
                    # New option within the current choice block.
                    $opt = $currentChoice.options[$currentChoice.options.Count - 1]
                    if ($opt.sentences.Count -gt 0) {
                        $opt.nodes.Add(@{ type = 'story'; sentences = $opt.sentences.ToArray(); segment = $opt.optionSegment }) | Out-Null
                        $opt.sentences.Clear() | Out-Null
                    }
                }

                $currentChoice.options.Add(@{ label = $e.label; nodes = New-Object System.Collections.Generic.List[object]; sentences = New-Object System.Collections.Generic.List[object]; optionSegment = $segmentIndex }) | Out-Null
                continue
            }

            if ($e.directive -eq 'continue') {
                # end choice
                if ($inChoice -and $null -ne $currentChoice) {
                    # flush last option sentences
                    $opt = $currentChoice.options[$currentChoice.options.Count - 1]
                    if ($opt.sentences.Count -gt 0) {
                        $opt.nodes.Add(@{ type = 'story'; sentences = $opt.sentences.ToArray(); segment = $opt.optionSegment }) | Out-Null
                        $opt.sentences.Clear() | Out-Null
                    }
                    $nodes.Add($currentChoice) | Out-Null
                    $currentChoice = $null
                    $inChoice = $false
                }
                continue
            }
        }

        # Normal content line
        if (!$inChoice) {
            $currentSentences.Add($e) | Out-Null
            if ($currentSentences.Count -ge 35) {
                Flush-StoryBlock $currentSentences
            }
        } else {
            # inside choice: if we encounter another CHOICE OPTION directive it was handled above
            $opt = $currentChoice.options[$currentChoice.options.Count - 1]
            $opt.sentences.Add($e) | Out-Null
            if ($opt.sentences.Count -ge 30) {
                $opt.nodes.Add(@{ type = 'story'; sentences = $opt.sentences.ToArray(); segment = $opt.optionSegment }) | Out-Null
                $opt.sentences.Clear() | Out-Null
            }
        }

        # Detect subsequent CHOICE OPTION lines while inChoice handled by directive earlier
    }

    # Flush remaining
    if ($inChoice -and $null -ne $currentChoice) {
        $opt = $currentChoice.options[$currentChoice.options.Count - 1]
        if ($opt.sentences.Count -gt 0) {
            $opt.nodes.Add(@{ type = 'story'; sentences = $opt.sentences.ToArray(); segment = $opt.optionSegment }) | Out-Null
            $opt.sentences.Clear() | Out-Null
        }
        $nodes.Add($currentChoice) | Out-Null
        $inChoice = $false
        $currentChoice = $null
    }
    Flush-StoryBlock $currentSentences

    # Build assets for nodes with GUIDs and next links
    # We'll pre-create a linear list of asset names for mainline; choice expands.

    $mainAssetNames = New-Object System.Collections.Generic.List[string]

    foreach ($n in $nodes) {
        if ($n.type -eq 'story') {
            $mainAssetNames.Add("H${ch}_${sceneIndex}") | Out-Null
            $sceneIndex++
        } elseif ($n.type -eq 'minigame') {
            $mainAssetNames.Add("H${ch}_M$($n.level)") | Out-Null
        } elseif ($n.type -eq 'choice') {
            $mainAssetNames.Add("H${ch}_X$($n.index)") | Out-Null
        }
    }

    # Assign GUIDs for all main assets
    foreach ($name in $mainAssetNames) { [void](Get-OrCreateAssetGuid $name) }

    # Also assign GUIDs for option scenes
    foreach ($n in $nodes) {
        if ($n.type -ne 'choice') { continue }
        $optNum = 1
        foreach ($opt in $n.options) {
            $optSceneNum = 1
            foreach ($optNode in $opt.nodes) {
                if ($optNode.type -eq 'story') {
                    $optName = "H${ch}_X$($n.index)_$optNum`_$optSceneNum"
                    [void](Get-OrCreateAssetGuid $optName)
                    $optSceneNum++
                } elseif ($optNode.type -eq 'minigame') {
                    $optName = "H${ch}_X$($n.index)_$optNum`_M$($optNode.level)"
                    [void](Get-OrCreateAssetGuid $optName)
                }
            }
            $optNum++
        }
    }

    # Write metas for any missing
    foreach ($kv in $existingAssetGuids.GetEnumerator()) {
        $assetName = $kv.Key
        if (!$assetName.StartsWith('H')) { continue }
        if ($assetName -notmatch "^H(Chapter\d+|\d+_|\d+_X|\d+_M)") { continue }
        $assetPath = Join-Path $OutputDir "$assetName.asset"
        $metaPath = "$assetPath.meta"
        if (!(Test-Path -LiteralPath $metaPath) -and ((Test-Path -LiteralPath $assetPath) -or ($assetName -match '^HChapter'))) {
            Write-Meta $metaPath $kv.Value
        }
    }

    # Now actually write content assets.
    $mainIdx = 0
    for ($i = 0; $i -lt $nodes.Count; $i++) {
        $n = $nodes[$i]
        $assetName = $mainAssetNames[$mainIdx]
        $mainIdx++

        # Determine what the next scene guid is for this node
        $nextGuid = $null
        if ($i -lt $nodes.Count - 1) {
            $nextGuid = Get-OrCreateAssetGuid $mainAssetNames[$mainIdx]
        } else {
            # end of chapter -> next ChapterScene or TBC
            if ($ch -lt 13) {
                $nextGuid = $chapterSceneGuids[$ch + 1]
            } else {
                $nextGuid = $tbcGuid
            }
        }

        $assetGuid = Get-OrCreateAssetGuid $assetName
        $assetPath = Join-Path $OutputDir "$assetName.asset"
        $metaPath = "$assetPath.meta"
        if (!(Test-Path -LiteralPath $metaPath)) { Write-Meta $metaPath $assetGuid }

        if ($n.type -eq 'story') {
            $bg = Get-ChapterDefaultBg $ch $n.segment
            $sentencesYaml = New-Object System.Collections.Generic.List[string]
            $mrHorseShown = $false
            foreach ($s in $n.sentences) {
                if ($s.kind -eq 'thought') {
                    $cg = $Char_Think
                    $sentencesYaml.Add((Make-SentenceYaml $s.text $cg $false 0)) | Out-Null
                    continue
                }

                $speaker = $null
                if ($s.kind -eq 'dialogue') { $speaker = $s.speaker }
                $cg = Get-CharacterGuidForSpeaker $speaker
                $showHorse = $false
                if (!$mrHorseShown -and ($cg -eq $Char_MrHorse)) {
                    $showHorse = $true
                    $mrHorseShown = $true
                }
                $sentencesYaml.Add((Make-SentenceYaml $s.text $cg $showHorse 0)) | Out-Null
            }
            Write-StorySceneAsset $assetPath $assetName $bg $nextGuid $sentencesYaml
        }
        elseif ($n.type -eq 'minigame') {
            Write-MinigameSceneAsset $assetPath $assetName $n.levelValue $nextGuid
        }
        elseif ($n.type -eq 'choice') {
            # Create option start guids
            $labels = New-Object System.Collections.Generic.List[string]
            $optNum = 1
            foreach ($opt in $n.options) {
                $optFirstName = "H${ch}_X$($n.index)_$optNum`_1"
                $optFirstGuid = Get-OrCreateAssetGuid $optFirstName
                $labels.Add("  - text: $(Escape-YamlScalar $opt.label)`n    nextScene: {fileID: 11400000, guid: $optFirstGuid, type: 2}`n") | Out-Null

                # Write option chain
                $optNodes = $opt.nodes
                $optSceneNum = 1
                for ($j = 0; $j -lt $optNodes.Count; $j++) {
                    $on = $optNodes[$j]
                    $onName = $null
                    if ($on.type -eq 'story') {
                        $onName = "H${ch}_X$($n.index)_$optNum`_$optSceneNum"
                        $optSceneNum++
                    } elseif ($on.type -eq 'minigame') {
                        $onName = "H${ch}_X$($n.index)_$optNum`_M$($on.level)"
                    }

                    $onGuid = Get-OrCreateAssetGuid $onName
                    $onPath = Join-Path $OutputDir "$onName.asset"
                    $onMeta = "$onPath.meta"
                    if (!(Test-Path -LiteralPath $onMeta)) { Write-Meta $onMeta $onGuid }

                    $onNextGuid = $null
                    if ($j -lt $optNodes.Count - 1) {
                        # next within option
                        $peek = $optNodes[$j + 1]
                        if ($peek.type -eq 'story') {
                            $onNextName = "H${ch}_X$($n.index)_$optNum`_$optSceneNum"
                            $onNextGuid = Get-OrCreateAssetGuid $onNextName
                        } elseif ($peek.type -eq 'minigame') {
                            $onNextName = "H${ch}_X$($n.index)_$optNum`_M$($peek.level)"
                            $onNextGuid = Get-OrCreateAssetGuid $onNextName
                        }
                    } else {
                        # end of option -> converge back to main flow (nextGuid from choice node)
                        $onNextGuid = $nextGuid
                    }

                    if ($on.type -eq 'story') {
                        $bg = Get-ChapterDefaultBg $ch $on.segment
                        $sentencesYaml = New-Object System.Collections.Generic.List[string]
                        $mrHorseShown = $false
                        foreach ($s in $on.sentences) {
                            if ($s.kind -eq 'thought') {
                                $sentencesYaml.Add((Make-SentenceYaml $s.text $Char_Think $false 0)) | Out-Null
                                continue
                            }

                            $speaker = $null
                            if ($s.kind -eq 'dialogue') { $speaker = $s.speaker }
                            $cg = Get-CharacterGuidForSpeaker $speaker
                            $showHorse = $false
                            if (!$mrHorseShown -and ($cg -eq $Char_MrHorse)) {
                                $showHorse = $true
                                $mrHorseShown = $true
                            }
                            $sentencesYaml.Add((Make-SentenceYaml $s.text $cg $showHorse 0)) | Out-Null
                        }
                        Write-StorySceneAsset $onPath $onName $bg $onNextGuid $sentencesYaml
                    } else {
                        Write-MinigameSceneAsset $onPath $onName $on.levelValue $onNextGuid
                    }
                }

                $optNum++
            }

            $choiceBg = Get-ChapterDefaultBg $ch $n.segment
            Write-ChooseSceneAsset $assetPath $assetName $labels $choiceBg
        }
    }
}

Write-Host "MrHorse import complete. Wrote/updated assets in $OutputDir"