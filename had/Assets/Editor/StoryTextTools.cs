#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class StoryTextTools
{
    private const string ScenesRoot = "Assets/Story/Scenes";
    private const string PlayerNameToken = "{playerName}";

    [MenuItem("Tools/Story/Text/Ensure alternativeText for {playerName}")]
    public static void EnsureAlternativeTextForPlayerName()
    {
        RunPlayerNameAlternativeTextPass(writeFixes: true);
    }

    [MenuItem("Tools/Story/Text/Validate alternativeText for {playerName}")]
    public static void ValidateAlternativeTextForPlayerName()
    {
        RunPlayerNameAlternativeTextPass(writeFixes: false);
    }

    private static void RunPlayerNameAlternativeTextPass(bool writeFixes)
    {
        if (!AssetDatabase.IsValidFolder(ScenesRoot))
        {
            Debug.LogError($"[StoryTextTools] Folder not found: {ScenesRoot}");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:StoryScene", new[] { ScenesRoot });
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var changedScenes = 0;
        var changedSentences = 0;
        var issues = 0;
        var touched = new List<string>();

        foreach (var path in paths)
        {
            var scene = AssetDatabase.LoadAssetAtPath<StoryScene>(path);
            if (scene == null || scene.sentences == null || scene.sentences.Count == 0)
                continue;

            var sentences = scene.sentences;
            var didChangeScene = false;

            for (var i = 0; i < sentences.Count; i++)
            {
                var s = sentences[i];
                if (string.IsNullOrEmpty(s.text) || !s.text.Contains(PlayerNameToken))
                    continue;

                if (!string.IsNullOrEmpty(s.alternativeText))
                    continue;

                issues++;

                if (!writeFixes)
                    continue;

                // Fallback: ensure alternativeText exists and does NOT still contain the token.
                s.alternativeText = s.text.Replace(PlayerNameToken, "Me");
                sentences[i] = s;
                didChangeScene = true;
                changedSentences++;
            }

            if (!didChangeScene)
                continue;

            Undo.RecordObject(scene, "Ensure alternativeText for {playerName}");
            scene.sentences = sentences;
            EditorUtility.SetDirty(scene);

            changedScenes++;
            touched.Add(scene.name);
        }

        if (writeFixes)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        var mode = writeFixes ? "FIX" : "CHECK";
        Debug.Log($"[StoryTextTools] {mode} alternativeText for {{playerName}}: issues={issues}, scenes changed={changedScenes}, sentences changed={changedSentences}.");
        if (touched.Count > 0)
        {
            Debug.Log($"[StoryTextTools] Touched scenes: {string.Join(", ", touched.Take(50))}{(touched.Count > 50 ? " ..." : "")}");
        }
    }
}
#endif
