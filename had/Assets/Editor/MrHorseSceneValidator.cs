#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MrHorseSceneValidator
{
    private const string MrHorseFolder = "Assets/Story/Scenes/MrHorse";

    private static readonly Regex NextSceneGuidRegex =
        new Regex(@"\n\s{2}nextScene:\s*\{[^}]*guid:\s*([0-9a-fA-F]{32})[^}]*\}", RegexOptions.Compiled);

    private static readonly Regex BackgroundGuidRegex =
        new Regex(@"\n\s{2}background:\s*\{[^}]*guid:\s*([0-9a-fA-F]{32})[^}]*\}", RegexOptions.Compiled);

    [MenuItem("Tools/Story/Validate MrHorse StoryScenes")]
    public static void ValidateMrHorseStoryScenes()
    {
        if (!AssetDatabase.IsValidFolder(MrHorseFolder))
        {
            Debug.LogError($"Folder not found: {MrHorseFolder}");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:StoryScene", new[] { MrHorseFolder });
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        Debug.Log($"[MrHorseSceneValidator] Found {paths.Length} StoryScene assets under {MrHorseFolder}.");

        var missingBackground = 0;
        var missingNext = 0;

        foreach (var path in paths)
        {
            var scene = AssetDatabase.LoadAssetAtPath<StoryScene>(path);
            if (scene == null)
            {
                var main = AssetDatabase.LoadMainAssetAtPath(path);
                Debug.LogWarning($"[MrHorseSceneValidator] Could not load as StoryScene: {path} (main type: {main?.GetType().Name ?? "null"})");
                continue;
            }

            var bgMissing = scene.background == null;
            var nextMissing = scene.nextScene == null;

            if (!bgMissing && !nextMissing)
                continue;

            if (bgMissing) missingBackground++;
            if (nextMissing) missingNext++;

            var diskInfo = TryReadYamlHints(path, scene);
            Debug.LogWarning(
                $"[MrHorseSceneValidator] {path} missing: " +
                $"background={(bgMissing ? "NULL" : "OK")}, nextScene={(nextMissing ? "NULL" : "OK")}. " +
                diskInfo);
        }

        Debug.Log($"[MrHorseSceneValidator] Done. Missing background: {missingBackground}, missing nextScene: {missingNext}.");
    }

    [MenuItem("Tools/Story/Repair MrHorse StoryScenes (background/nextScene)")]
    public static void RepairMrHorseStoryScenes()
    {
        if (!AssetDatabase.IsValidFolder(MrHorseFolder))
        {
            Debug.LogError($"Folder not found: {MrHorseFolder}");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:StoryScene", new[] { MrHorseFolder });
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        Debug.Log($"[MrHorseSceneValidator] Repair: Found {paths.Length} StoryScene assets under {MrHorseFolder}.");

        var fixedBackground = 0;
        var fixedNext = 0;
        var warnings = 0;

        foreach (var path in paths)
        {
            var scene = AssetDatabase.LoadAssetAtPath<StoryScene>(path);
            if (scene == null)
                continue;

            string text;
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                {
                    warnings++;
                    Debug.LogWarning($"[MrHorseSceneValidator] Repair: File missing on disk: {path}");
                    continue;
                }

                text = File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                warnings++;
                Debug.LogWarning($"[MrHorseSceneValidator] Repair: Failed reading YAML for {path}: {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            var changed = false;

            if (scene.nextScene == null)
            {
                var match = NextSceneGuidRegex.Match(text);
                if (match.Success)
                {
                    var nextGuid = match.Groups[1].Value;
                    var nextPath = AssetDatabase.GUIDToAssetPath(nextGuid);
                    var nextAsset = string.IsNullOrEmpty(nextPath) ? null : AssetDatabase.LoadAssetAtPath<GameScene>(nextPath);
                    if (nextAsset != null)
                    {
                        Undo.RecordObject(scene, "Repair MrHorse nextScene");
                        scene.nextScene = nextAsset;
                        fixedNext++;
                        changed = true;
                    }
                    else
                    {
                        warnings++;
                        Debug.LogWarning($"[MrHorseSceneValidator] Repair: {path} nextScene guid={nextGuid} resolvedPath='{nextPath}', loadAsGameScene={(nextAsset != null ? "yes" : "no")}");
                    }
                }
                else
                {
                    warnings++;
                    Debug.LogWarning($"[MrHorseSceneValidator] Repair: {path} has no parsable nextScene GUID in YAML.");
                }
            }

            if (scene.background == null)
            {
                var match = BackgroundGuidRegex.Match(text);
                if (match.Success)
                {
                    var bgGuid = match.Groups[1].Value;
                    var bgPath = AssetDatabase.GUIDToAssetPath(bgGuid);
                    var sprite = string.IsNullOrEmpty(bgPath)
                        ? null
                        : AssetDatabase.LoadAllAssetsAtPath(bgPath).OfType<Sprite>().FirstOrDefault();

                    if (sprite != null)
                    {
                        Undo.RecordObject(scene, "Repair MrHorse background");
                        scene.background = sprite;
                        fixedBackground++;
                        changed = true;
                    }
                    else
                    {
                        warnings++;
                        Debug.LogWarning($"[MrHorseSceneValidator] Repair: {path} background guid={bgGuid} resolvedPath='{bgPath}', spriteFound={(sprite != null ? "yes" : "no")}. (Texture may not be imported as Sprite)");
                    }
                }
                else
                {
                    warnings++;
                    Debug.LogWarning($"[MrHorseSceneValidator] Repair: {path} has no parsable background GUID in YAML.");
                }
            }

            if (changed)
                EditorUtility.SetDirty(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MrHorseSceneValidator] Repair done. Fixed nextScene: {fixedNext}, fixed background: {fixedBackground}, warnings: {warnings}.");
    }

    private static string TryReadYamlHints(string assetPath, StoryScene loadedScene)
    {
        try
        {
            var fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
                return "(could not read yaml: file missing on disk)";

            var text = File.ReadAllText(fullPath);
            var hasBackgroundLine = text.Contains("\n  background:", StringComparison.Ordinal);
            var hasNextLine = text.Contains("\n  nextScene:", StringComparison.Ordinal);
            var scriptLine = text.Split('\n').FirstOrDefault(l => l.Contains("m_Script:", StringComparison.Ordinal))?.Trim();

            var sentenceCount = loadedScene?.sentences == null ? -1 : loadedScene.sentences.Count;

            var nextMatch = NextSceneGuidRegex.Match(text);
            var nextGuid = nextMatch.Success ? nextMatch.Groups[1].Value : "";
            var nextPath = string.IsNullOrEmpty(nextGuid) ? "" : AssetDatabase.GUIDToAssetPath(nextGuid);
            var nextResolves = !string.IsNullOrEmpty(nextPath) && AssetDatabase.LoadAssetAtPath<GameScene>(nextPath) != null;

            var bgMatch = BackgroundGuidRegex.Match(text);
            var bgGuid = bgMatch.Success ? bgMatch.Groups[1].Value : "";
            var bgPath = string.IsNullOrEmpty(bgGuid) ? "" : AssetDatabase.GUIDToAssetPath(bgGuid);
            var bgResolves = !string.IsNullOrEmpty(bgPath) && AssetDatabase.LoadAllAssetsAtPath(bgPath).OfType<Sprite>().Any();

            return $"(sentences={sentenceCount}, yaml: backgroundLine={(hasBackgroundLine ? "yes" : "no")}, nextSceneLine={(hasNextLine ? "yes" : "no")}, " +
                   $"bgGuid={(string.IsNullOrEmpty(bgGuid) ? "-" : bgGuid)}, bgResolves={(bgResolves ? "yes" : "no")}, " +
                   $"nextGuid={(string.IsNullOrEmpty(nextGuid) ? "-" : nextGuid)}, nextResolves={(nextResolves ? "yes" : "no")}, {scriptLine})";
        }
        catch (Exception ex)
        {
            return $"(could not read yaml: {ex.GetType().Name}: {ex.Message})";
        }
    }
}
#endif
