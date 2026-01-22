using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartController : MonoBehaviour
{
    // Static field to communicate the starting scene to GameController
    public static GameScene overrideStartingScene = null;

    public Button startButton;
    public TMP_InputField playerNameInput;
    public Character meCharacter;
    public Image fadeImage;
    private Animator animator;
    private AsyncOperation asyncLoad;

    [System.Serializable]
    public struct ContinueCheckpoint
    {
        [Tooltip("PlayerPrefs key that marks this checkpoint as completed (usually the ChapterScene asset name, e.g. AChapter7, SChapter7, HChapter7).")]
        public string chapterKey;

        [Tooltip("Scene to start from when this checkpoint is the most recent completed one.")]
        public GameScene scene;
    }

    [Header("Continue / Route Checkpoints")]
    [Tooltip("Optional ordered list of checkpoints (newest -> oldest). If set, this list is used to determine where to continue from. This is the recommended way to add new routes like Scarlet/MrHorse.")]
    public List<ContinueCheckpoint> continueCheckpoints;

    [Header("Chapter Scenes")]
    [Header("Scarlet Scenes")]
    public GameScene sEpilogue3Scene;
    public GameScene sEpilogue2Scene;
    public GameScene sEpilogue1Scene;
    public GameScene sChapter15Scene;
    public GameScene sChapter14Scene;
    public GameScene sChapter13Scene;
    public GameScene sChapter12Scene;
    public GameScene sChapter11Scene;
    public GameScene sChapter10Scene;
    public GameScene sChapter9Scene;
    public GameScene sChapter8Scene;
    public GameScene sChapter7Scene;

    [Header("Aleph Scenes")]
    public GameScene aChapter15Scene;
    public GameScene aChapter14Scene;
    public GameScene aChapter13Scene;
    public GameScene aChapter12Scene;
    public GameScene aChapter11Scene;
    public GameScene aChapter10Scene;
    public GameScene aChapter9Scene;
    public GameScene aChapter8Scene;
    public GameScene aChapter7Scene;

    [Header("Global Scenes")]
    public GameScene chapter6Scene;
    public GameScene chapter5Scene;
    public GameScene chapter4Scene;
    public GameScene chapter3Scene;
    public GameScene chapter2Scene;
    public GameScene chapter1Scene;
    public GameScene startScene;

    void Start()
    {
        startButton.onClick.AddListener(StartGame);
        animator = GetComponent<Animator>();
        StartCoroutine(PreloadMainScene());
        LoadSavedPlayerName();
    }

    private void LoadSavedPlayerName()
    {
        string savedName = PlayerPrefs.GetString(ProgressKeys.PlayerName, "");
        if (!string.IsNullOrEmpty(savedName))
        {
            playerNameInput.text = savedName;
        }
    }

    private IEnumerator PreloadMainScene()
    {
        asyncLoad = SceneManager.LoadSceneAsync("MainScene");
        asyncLoad.allowSceneActivation = false;
        yield return asyncLoad;
    }

    void StartGame()
    {
        string playerName = playerNameInput.text;
        if (playerName != "")
        {
            meCharacter.characterName = playerName;
            PlayerPrefs.SetString(ProgressKeys.PlayerName, playerName);
            PlayerPrefs.Save();
        }
        else
        {
            meCharacter.characterName = "Me";
        }

        GameScene startingScene = DetermineStartingScene();
        if (startingScene != null)
        {
            overrideStartingScene = startingScene;
        }

        animator.SetTrigger("FadeOut");
        StartCoroutine(WaitAndActivateScene());
    }

    private IEnumerator WaitAndActivateScene()
    {
        yield return new WaitForSeconds(0.5f);
        asyncLoad.allowSceneActivation = true;
    }

    private GameScene DetermineStartingScene()
    {
        if (ChapterScene.IsChapterDone("TheEnd"))
        {
            ScoreManager.ResetMinigameData();
            return startScene;
        }

        var checkpointsToUse = BuildContinueCheckpointList();

        // Prefer explicit "most recent checkpoint" if present.
        string lastKey = PlayerPrefs.GetString(ProgressKeys.LastCompletedCheckpointKey, "");
        if (!string.IsNullOrEmpty(lastKey) && ChapterScene.IsChapterDone(lastKey))
        {
            for (int i = 0; i < checkpointsToUse.Count; i++)
            {
                var cp = checkpointsToUse[i];
                if (cp.scene == null)
                    continue;
                if (!string.IsNullOrEmpty(cp.chapterKey) && cp.chapterKey == lastKey)
                    return cp.scene;
            }
        }

        // Otherwise pick the most recently completed checkpoint among all routes.
        long bestTicks = 0;
        GameScene bestScene = null;
        int bestFallbackRank = int.MinValue;

        for (int i = 0; i < checkpointsToUse.Count; i++)
        {
            var cp = checkpointsToUse[i];
            if (cp.scene == null)
                continue;
            if (string.IsNullOrEmpty(cp.chapterKey))
                continue;
            if (!ChapterScene.IsChapterDone(cp.chapterKey))
                continue;

            if (ChapterScene.TryGetChapterDoneAtUtcTicks(cp.chapterKey, out long ticks) && ticks > 0)
            {
                if (ticks > bestTicks)
                {
                    bestTicks = ticks;
                    bestScene = cp.scene;
                }
                continue;
            }

            // Fallback for older saves that don't have timestamps yet:
            // choose the most advanced checkpoint by a simple rank.
            int rank = GetCheckpointRank(cp.chapterKey);
            if (rank > bestFallbackRank)
            {
                bestFallbackRank = rank;
                if (bestScene == null)
                    bestScene = cp.scene;
            }
        }

        if (bestScene != null)
            return bestScene;

        return startScene;
    }

    private List<ContinueCheckpoint> BuildContinueCheckpointList()
    {
        // Some scenes may still have continueCheckpoints populated with an older list
        // (e.g., Aleph-only). Merge it with legacy defaults so new routes (Scarlet)
        // always work without needing manual inspector updates.
        var merged = new List<ContinueCheckpoint>(64);
        var seenKeys = new HashSet<string>();

        if (continueCheckpoints != null)
        {
            for (int i = 0; i < continueCheckpoints.Count; i++)
            {
                var cp = continueCheckpoints[i];
                if (cp.scene == null)
                    continue;
                if (!string.IsNullOrEmpty(cp.chapterKey) && seenKeys.Contains(cp.chapterKey))
                    continue;

                merged.Add(cp);
                if (!string.IsNullOrEmpty(cp.chapterKey))
                    seenKeys.Add(cp.chapterKey);
            }
        }

        var legacy = BuildLegacyContinueCheckpoints();
        for (int i = 0; i < legacy.Count; i++)
        {
            var cp = legacy[i];
            if (cp.scene == null)
                continue;
            if (!string.IsNullOrEmpty(cp.chapterKey) && seenKeys.Contains(cp.chapterKey))
                continue;

            merged.Add(cp);
            if (!string.IsNullOrEmpty(cp.chapterKey))
                seenKeys.Add(cp.chapterKey);
        }

        return merged;
    }

    private int GetCheckpointRank(string chapterKey)
    {
        // Higher = more advanced.
        // Epilogues should outrank chapters.
        if (string.IsNullOrEmpty(chapterKey))
            return int.MinValue;

        if (chapterKey == "TheEnd")
            return 1_000_000;

        // Route epilogues.
        if (chapterKey.StartsWith("SEpilogue"))
        {
            if (int.TryParse(chapterKey.Substring("SEpilogue".Length), out int e))
                return 900_000 + e;
        }
        if (chapterKey.StartsWith("AEpilogue"))
        {
            if (int.TryParse(chapterKey.Substring("AEpilogue".Length), out int e))
                return 900_000 + e;
        }

        // Route chapters like SChapter15 / AChapter7.
        if (chapterKey.Length >= 9 && (chapterKey[0] == 'S' || chapterKey[0] == 'A' || chapterKey[0] == 'H') && chapterKey.Substring(1).StartsWith("Chapter"))
        {
            if (int.TryParse(chapterKey.Substring(1 + "Chapter".Length), out int c))
                return 800_000 + c;
        }

        // Global chapters.
        if (chapterKey.StartsWith("Chapter"))
        {
            if (int.TryParse(chapterKey.Substring("Chapter".Length), out int c))
                return 700_000 + c;
        }

        return 0;
    }

    private List<ContinueCheckpoint> BuildLegacyContinueCheckpoints()
    {
        return new List<ContinueCheckpoint>
        {
            // Scarlet route (new)
            new ContinueCheckpoint { chapterKey = "SEpilogue3", scene = sEpilogue3Scene },
            new ContinueCheckpoint { chapterKey = "SEpilogue2", scene = sEpilogue2Scene },
            new ContinueCheckpoint { chapterKey = "SEpilogue1", scene = sEpilogue1Scene },
            new ContinueCheckpoint { chapterKey = "SChapter15", scene = sChapter15Scene },
            new ContinueCheckpoint { chapterKey = "SChapter14", scene = sChapter14Scene },
            new ContinueCheckpoint { chapterKey = "SChapter13", scene = sChapter13Scene },
            new ContinueCheckpoint { chapterKey = "SChapter12", scene = sChapter12Scene },
            new ContinueCheckpoint { chapterKey = "SChapter11", scene = sChapter11Scene },
            new ContinueCheckpoint { chapterKey = "SChapter10", scene = sChapter10Scene },
            new ContinueCheckpoint { chapterKey = "SChapter9", scene = sChapter9Scene },
            new ContinueCheckpoint { chapterKey = "SChapter8", scene = sChapter8Scene },
            new ContinueCheckpoint { chapterKey = "SChapter7", scene = sChapter7Scene },

            // Aleph route (existing)
            new ContinueCheckpoint { chapterKey = "AChapter15", scene = aChapter15Scene },
            new ContinueCheckpoint { chapterKey = "AChapter14", scene = aChapter14Scene },
            new ContinueCheckpoint { chapterKey = "AChapter13", scene = aChapter13Scene },
            new ContinueCheckpoint { chapterKey = "AChapter12", scene = aChapter12Scene },
            new ContinueCheckpoint { chapterKey = "AChapter11", scene = aChapter11Scene },
            new ContinueCheckpoint { chapterKey = "AChapter10", scene = aChapter10Scene },
            new ContinueCheckpoint { chapterKey = "AChapter9", scene = aChapter9Scene },
            new ContinueCheckpoint { chapterKey = "AChapter8", scene = aChapter8Scene },
            new ContinueCheckpoint { chapterKey = "AChapter7", scene = aChapter7Scene },

            // Global chapters
            new ContinueCheckpoint { chapterKey = "Chapter6", scene = chapter6Scene },
            new ContinueCheckpoint { chapterKey = "Chapter5", scene = chapter5Scene },
            new ContinueCheckpoint { chapterKey = "Chapter4", scene = chapter4Scene },
            new ContinueCheckpoint { chapterKey = "Chapter3", scene = chapter3Scene },
            new ContinueCheckpoint { chapterKey = "Chapter2", scene = chapter2Scene },
            new ContinueCheckpoint { chapterKey = "Chapter1", scene = chapter1Scene },
        };
    }
}