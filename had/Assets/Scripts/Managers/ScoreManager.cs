using UnityEngine;

public static class ScoreManager
{
    private const string CURRENT_SCORE_KEY = "CurrentTotalScore";
    private const string MINIGAME_PLAYED_PREFIX = "MinigameLevel_";
    private const string CURRENT_SCENE_KEY = "CurrentSceneName";
    private const string CURRENT_SENTENCE_INDEX_KEY = "CurrentSentenceIndex";

    public static int GetCurrentScore()
    {
        return PlayerPrefs.GetInt(CURRENT_SCORE_KEY, 0);
    }

    public static void SetCurrentScore(int score)
    {
        PlayerPrefs.SetInt(CURRENT_SCORE_KEY, score);
        PlayerPrefs.Save();
    }

    public static void AddToCurrentScore(int points)
    {
        int currentScore = GetCurrentScore();
        SetCurrentScore(currentScore + points);
    }

    public static bool HasMinigameLevelBeenPlayed(int level)
    {
        string key = MINIGAME_PLAYED_PREFIX + level;
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static void SetMinigameLevelPlayed(int level, bool played = true)
    {
        string key = MINIGAME_PLAYED_PREFIX + level;
        PlayerPrefs.SetInt(key, played ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool TryAddMinigameScore(int level, int points)
    {
        if (HasMinigameLevelBeenPlayed(level))
        {
            SetMinigameLevelPlayed(level, true);
            return false;
        }

        AddToCurrentScore(points);
        SetMinigameLevelPlayed(level, true);
        return true;
    }

    public static void ResetMinigameData()
    {
        SetCurrentScore(0);

        for (int i = 0; i <= 10; i++)
        {
            SetMinigameLevelPlayed(i, false);
        }
    }

    public static void DeleteAllPlayerData()
    {
        SetCurrentScore(0);
        for (int i = 0; i <= 10; i++)
        {
            SetMinigameLevelPlayed(i, false);
        }

        PlayerPrefs.DeleteKey("PlayerName");
        ClearGameState();

        string[] chapterKeys = {
            "TheEnd", "AChapter15", "AChapter14", "AChapter13", "AChapter12",
            "AChapter11", "AChapter10", "AChapter9", "AChapter8", "AChapter7",
            "Chapter6", "Chapter5", "Chapter4", "Chapter3", "Chapter2", "Chapter1",
            "A1", "A2", "A3", "A4", "A5", "A6"
        };

        foreach (string key in chapterKeys)
        {
            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
    }

    public static string GetCurrentSceneName()
    {
        return PlayerPrefs.GetString(CURRENT_SCENE_KEY, "");
    }

    public static void SetCurrentSceneName(string sceneName)
    {
        PlayerPrefs.SetString(CURRENT_SCENE_KEY, sceneName);
        PlayerPrefs.Save();
    }

    public static int GetCurrentSentenceIndex()
    {
        return PlayerPrefs.GetInt(CURRENT_SENTENCE_INDEX_KEY, -1);
    }

    public static void SetCurrentSentenceIndex(int sentenceIndex)
    {
        PlayerPrefs.SetInt(CURRENT_SENTENCE_INDEX_KEY, sentenceIndex);
        PlayerPrefs.Save();
    }

    public static void SaveGameState(string sceneName, int sentenceIndex)
    {
        SetCurrentSceneName(sceneName);
        SetCurrentSentenceIndex(sentenceIndex);
    }

    public static void ClearGameState()
    {
        PlayerPrefs.DeleteKey(CURRENT_SCENE_KEY);
        PlayerPrefs.DeleteKey(CURRENT_SENTENCE_INDEX_KEY);
        PlayerPrefs.Save();
    }
}
