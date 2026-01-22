#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RouteAssetTools
{
    private const string ScarletCharacterAssetPath = "Assets/Story/Chars/Scarlet.asset";
    private const string MrHorseCharacterAssetPath = "Assets/Story/Chars/Mr. Horse.asset";

    private const string ScarletScenesFolder = "Assets/Story/Scenes/Scarlet";
    private const string MrHorseScenesFolder = "Assets/Story/Scenes/MrHorse";

    private const string CharacterSpritesFolder = "Assets/Sprites/Characters";
    private const string BackgroundsFolder = "Assets/Sprites/Backgrounds";
    private const string ChapterCardsFolder = "Assets/Sprites/Backgrounds/Chapters";

    [MenuItem("Tools/Story/Characters/Rebuild Scarlet Sprites")]
    public static void RebuildScarletSprites()
    {
        var spritePaths = BuildScarletSpritePaths();
        SetCharacterSprites(ScarletCharacterAssetPath, spritePaths);
    }

    [MenuItem("Tools/Story/Characters/Rebuild Mr. Horse Sprites")]
    public static void RebuildMrHorseSprites()
    {
        var spritePaths = BuildMrHorseSpritePaths();
        SetCharacterSprites(MrHorseCharacterAssetPath, spritePaths);
    }

    [MenuItem("Tools/Story/Backgrounds/Reevaluate Scarlet + MrHorse Scene Backgrounds")]
    public static void ReevaluateRouteStorySceneBackgrounds()
    {
        var backgroundLookup = LoadBackgroundSpritesByKey();
        var chapterLookup = LoadSpritesByKey(ChapterCardsFolder);

        ReevaluateStorySceneBackgroundsInFolder(ScarletScenesFolder, "scarlet", backgroundLookup);
        ReevaluateStorySceneBackgroundsInFolder(MrHorseScenesFolder, "mrhorse", backgroundLookup);

        ReevaluateChapterSceneBackgroundsInFolder(ScarletScenesFolder, "scarlet", chapterLookup);
        ReevaluateChapterSceneBackgroundsInFolder(MrHorseScenesFolder, "mrhorse", chapterLookup);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Story/Run Route Asset Fixes (Sprites + Backgrounds)")]
    public static void RunAllRouteFixes()
    {
        RebuildScarletSprites();
        RebuildMrHorseSprites();
        ReevaluateRouteStorySceneBackgrounds();
        Debug.Log("[RouteAssetTools] Done.");
    }

    private static List<string> BuildScarletSpritePaths()
    {
        // Index order requested by user:
        // 0-8: base, 9-17: dress, 18-26: scar dress
        var baseEmotions = new[]
        {
            "neutral", "angry", "closedeyes", "sad", "surprised", "blush", "cheeky", "smile", "laugh"
        };

        var paths = new List<string>(27);

        foreach (var emotion in baseEmotions)
            paths.Add($"{CharacterSpritesFolder}/scarlet_{emotion}.png");

        foreach (var emotion in baseEmotions)
            paths.Add($"{CharacterSpritesFolder}/scarlet_dress_{emotion}.png");

        foreach (var emotion in baseEmotions)
            paths.Add($"{CharacterSpritesFolder}/scarlet_dress_scar_{emotion}.png");

        return paths;
    }

    private static List<string> BuildMrHorseSpritePaths()
    {
        // Index order requested by user
        var emotions = new[]
        {
            "neutral", "smile", "laugh", "sad", "angry", "blush", "cheeky", "surprised", "closedeyes"
        };

        return emotions.Select(e => $"{CharacterSpritesFolder}/mrhorse_{e}.png").ToList();
    }

    private static void SetCharacterSprites(string characterAssetPath, List<string> spritePaths)
    {
        var character = AssetDatabase.LoadAssetAtPath<Character>(characterAssetPath);
        if (character == null)
        {
            Debug.LogError($"[RouteAssetTools] Character asset not found or wrong type: {characterAssetPath}");
            return;
        }

        var sprites = new List<Sprite>(spritePaths.Count);
        var missing = 0;

        foreach (var spritePath in spritePaths)
        {
            var sprite = LoadSprite(spritePath);
            if (sprite == null)
                missing++;
            sprites.Add(sprite);
        }

        Undo.RecordObject(character, "Rebuild character sprites");
        character.sprites = sprites;
        EditorUtility.SetDirty(character);

        AssetDatabase.SaveAssets();

        Debug.Log($"[RouteAssetTools] Updated {characterAssetPath}: sprites={sprites.Count}, missing={missing}.");
    }

    private static Sprite LoadSprite(string assetPath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
            return sprite;

        // Fallback: sometimes Unity stores the Sprite as a sub-asset.
        var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        sprite = all.OfType<Sprite>().FirstOrDefault();
        if (sprite != null)
            return sprite;

        Debug.LogWarning($"[RouteAssetTools] Sprite not found at path: {assetPath}");
        return null;
    }

    private static Dictionary<string, Sprite> LoadBackgroundSpritesByKey()
    {
        return LoadSpritesByKey(BackgroundsFolder);
    }

    private static Dictionary<string, Sprite> LoadSpritesByKey(string folder)
    {
        var dict = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogError($"[RouteAssetTools] Sprite folder not found: {folder}");
            return dict;
        }

        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                continue;

            var key = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (!dict.ContainsKey(key))
                dict.Add(key, sprite);
        }

        return dict;
    }

    private static void ReevaluateChapterSceneBackgroundsInFolder(
        string folder,
        string routeKey,
        Dictionary<string, Sprite> chapterCards)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[RouteAssetTools] Folder not found: {folder}");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:ChapterScene", new[] { folder });
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        var changed = 0;
        var noMatch = 0;

        foreach (var path in paths)
        {
            var scene = AssetDatabase.LoadAssetAtPath<ChapterScene>(path);
            if (scene == null)
                continue;

            var desired = PickChapterCard(scene.name, routeKey, chapterCards);
            if (desired == null)
            {
                noMatch++;
                continue;
            }

            if (scene.background == desired)
                continue;

            Undo.RecordObject(scene, "Reevaluate chapter background");
            scene.background = desired;
            EditorUtility.SetDirty(scene);
            changed++;
        }

        Debug.Log($"[RouteAssetTools] {routeKey}: ChapterScene backgrounds in {folder}: changed={changed}, noMatch={noMatch}.");
    }

    private static Sprite PickChapterCard(string chapterSceneName, string routeKey, Dictionary<string, Sprite> chapterCards)
    {
        if (string.IsNullOrEmpty(chapterSceneName) || chapterCards.Count == 0)
            return null;

        var n = chapterSceneName.ToLowerInvariant();

        // Special cards
        if (n.Contains("tbc") && chapterCards.TryGetValue("tbc", out var tbc))
            return tbc;

        if (n.Contains("end") && chapterCards.TryGetValue("end", out var end))
            return end;

        if (n.Contains("epilogue"))
        {
            var key = routeKey == "mrhorse" ? "eh" : "es";
            return chapterCards.TryGetValue(key, out var epi) ? epi : null;
        }

        // Standard chapter cards: c7s/c7h ... c15s/c15h
        var marker = "chapter";
        var idx = n.IndexOf(marker, StringComparison.Ordinal);
        if (idx >= 0)
        {
            idx += marker.Length;
            var digits = new string(n.Skip(idx).TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var chapterNumber) && chapterNumber > 0)
            {
                var suffix = routeKey == "mrhorse" ? "h" : "s";
                var key = $"c{chapterNumber}{suffix}";
                return chapterCards.TryGetValue(key, out var card) ? card : null;
            }
        }

        return null;
    }

    private static void ReevaluateStorySceneBackgroundsInFolder(
        string folder,
        string routeKey,
        Dictionary<string, Sprite> backgrounds)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[RouteAssetTools] Folder not found: {folder}");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:StoryScene", new[] { folder });
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        var changed = 0;
        var unchanged = 0;
        var skippedNoMatch = 0;

        foreach (var path in paths)
        {
            var scene = AssetDatabase.LoadAssetAtPath<StoryScene>(path);
            if (scene == null)
                continue;

            var text = ExtractSceneText(scene);
            var best = PickBackgroundForText(text, routeKey, backgrounds, out var bestScore);
            if (best == null)
            {
                skippedNoMatch++;
                continue;
            }

            var currentKey = GetBackgroundKey(scene.background);
            var bestKey = GetBackgroundKey(best);

            var shouldReplace = scene.background == null || IsPlaceholderBackground(currentKey);
            if (!shouldReplace)
            {
                // If it looks like a strong match, allow replacement.
                shouldReplace = bestScore >= 3 && !string.Equals(currentKey, bestKey, StringComparison.OrdinalIgnoreCase);
            }

            if (!shouldReplace)
            {
                unchanged++;
                continue;
            }

            if (scene.background == best)
            {
                unchanged++;
                continue;
            }

            Undo.RecordObject(scene, "Reevaluate scene background");
            scene.background = best;
            EditorUtility.SetDirty(scene);
            changed++;
        }

        Debug.Log($"[RouteAssetTools] {routeKey}: StoryScene backgrounds in {folder}: changed={changed}, unchanged={unchanged}, noMatch={skippedNoMatch}.");
    }

    private static string ExtractSceneText(StoryScene scene)
    {
        if (scene.sentences == null || scene.sentences.Count == 0)
            return string.Empty;

        // Keep it simple and fast.
        return string.Join("\n", scene.sentences.Select(s => s.text ?? string.Empty));
    }

    private static Sprite PickBackgroundForText(
        string text,
        string routeKey,
        Dictionary<string, Sprite> backgrounds,
        out int score)
    {
        score = 0;
        if (string.IsNullOrWhiteSpace(text) || backgrounds.Count == 0)
            return null;

        var t = text.ToLowerInvariant();

        // Keyword -> background key
        // Extend this list as needed when new backgrounds are added.
        var rules = new (string keyword, string bgKey, int weight)[]
        {
            ("bunker", "bunker", 3),
            ("subway", "subway", 3),
            ("apartment", "apartment", 3),
            ("office", "office", 3),
            ("park", "park", 3),
            ("hospital", "hospital", 3),
            ("library", "library", 3),
            ("corridor", "corridor", 3),
            ("breakroom", "breakroom", 3),
            ("company", "company", 3),
            ("street", "street", 2),
            ("alley", "alley", 3),
            ("fields", "fields", 3),
            ("fountain", "fountain", 3),
            ("tailor", "tailorshop", 3),
            ("tearoom", "tearoom", 3),
            ("gym", "gym", 3),
            ("mirror", "mirror", 3),
            ("concert", "concert", 3),
            ("backstage", "backstage", 3),
            ("palace", "royalpalace", 3),
            ("castle", "royalpalace", 2),
            ("gang", "gang_base", 2),
        };

        // Route-specific gentle defaults.
        var defaultKey = routeKey == "scarlet" ? "scarletroom" : "room";

        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (keyword, bgKey, weight) in rules)
        {
            if (!t.Contains(keyword))
                continue;

            scores.TryGetValue(bgKey, out var s);
            scores[bgKey] = s + weight;
        }

        // Handle "room" mentions; Scarlet prefers scarletroom.
        if (t.Contains("room"))
        {
            var key = routeKey == "scarlet" ? "scarletroom" : "room";
            scores.TryGetValue(key, out var s);
            scores[key] = s + 1;
        }

        if (scores.Count == 0)
        {
            if (backgrounds.TryGetValue(defaultKey, out var d))
            {
                score = 1;
                return d;
            }

            return null;
        }

        var best = scores.OrderByDescending(kvp => kvp.Value).First();
        score = best.Value;
        return backgrounds.TryGetValue(best.Key, out var sprite) ? sprite : null;
    }

    private static bool IsPlaceholderBackground(string backgroundKey)
    {
        if (string.IsNullOrEmpty(backgroundKey))
            return true;

        return backgroundKey is "black" or "white" or "minigame";
    }

    private static string GetBackgroundKey(Sprite sprite)
    {
        if (sprite == null)
            return "";

        var path = AssetDatabase.GetAssetPath(sprite);
        if (string.IsNullOrEmpty(path))
            return sprite.name.ToLowerInvariant();

        return Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
    }
}
#endif
