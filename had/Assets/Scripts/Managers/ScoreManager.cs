using UnityEngine;

public static class ScoreManager
{
    private const string CURRENT_SCORE_KEY = "CurrentTotalScore";
    private const string MINIGAME_PLAYED_PREFIX = "MinigameLevel_";

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

        string[] chapterKeys = {
            "TheEnd", "AChapter15", "AChapter14", "AChapter13", "AChapter12",
            "AChapter11", "AChapter10", "AChapter9", "AChapter8", "AChapter7",
            "Chapter6", "Chapter5", "Chapter4", "Chapter3", "Chapter2", "Chapter1"
        };

        foreach (string key in chapterKeys)
        {
            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
    }

    public static string GetDebugInfo()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Current Total Score: {GetCurrentScore()}");
        sb.AppendLine("Completed Minigame Levels:");

        for (int i = 0; i <= 10; i++)
        {
            if (HasMinigameLevelBeenPlayed(i))
            {
                sb.AppendLine($"  Level {i}: Completed");
            }
        }

        return sb.ToString();
    }
}
