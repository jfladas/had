#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class StorySceneActionFixer
{
    [MenuItem("Tools/Story/Fix Scarlet SHOW->NONE (when already visible)")]
    public static void FixScarletShowWhenAlreadyVisible()
    {
        FixShowWhenAlreadyVisible(new[] { "Assets/Story/Scenes/Scarlet" });
    }

    [MenuItem("Tools/Story/Fix All SHOW->NONE (when already visible)")]
    public static void FixAllShowWhenAlreadyVisible()
    {
        FixShowWhenAlreadyVisible(new[] { "Assets/Story/Scenes" });
    }

    private static void FixShowWhenAlreadyVisible(string[] searchFolders)
    {
        string[] guids = AssetDatabase.FindAssets("t:StoryScene", searchFolders);
        int changedAssets = 0;
        int changedActions = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StoryScene scene = AssetDatabase.LoadAssetAtPath<StoryScene>(path);
            if (scene == null || scene.sentences == null)
            {
                continue;
            }

            bool changed = false;
            var visible = new HashSet<Character>();
            var sentences = scene.sentences;

            for (int i = 0; i < sentences.Count; i++)
            {
                var sentence = sentences[i];
                if (sentence.actions == null)
                {
                    continue;
                }

                for (int j = 0; j < sentence.actions.Count; j++)
                {
                    var action = sentence.actions[j];
                    if (action.character == null)
                    {
                        continue;
                    }

                    if (action.type == StoryScene.Sentence.Action.Type.SHOW)
                    {
                        if (visible.Contains(action.character))
                        {
                            action.type = StoryScene.Sentence.Action.Type.NONE;
                            sentence.actions[j] = action;
                            changed = true;
                            changedActions++;
                        }
                        else
                        {
                            visible.Add(action.character);
                        }
                    }
                    else if (action.type == StoryScene.Sentence.Action.Type.HIDE)
                    {
                        visible.Remove(action.character);
                    }
                }

                sentences[i] = sentence;
            }

            if (!changed)
            {
                continue;
            }

            Undo.RecordObject(scene, "Fix StoryScene action types");
            scene.sentences = sentences;
            EditorUtility.SetDirty(scene);
            changedAssets++;
        }

        if (changedAssets > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"Fixed SHOW->NONE in {changedActions} actions across {changedAssets} StoryScenes.");
    }
}
#endif
