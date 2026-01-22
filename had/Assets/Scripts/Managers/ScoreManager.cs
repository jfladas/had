using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class ScoreManager
{
    private const string CURRENT_SCORE_KEY = "CurrentTotalScore";
    private const string MINIGAME_PLAYED_PREFIX = "MinigameLevel_";
    private const string MINIGAME_PLAYED_ID_PREFIX = "MinigameId_";
    private const string MINIGAME_PLAYED_IDS_KEY = "MinigamePlayedIds";
    private const string CURRENT_SCENE_KEY = "CurrentSceneName";
    private const string CURRENT_SENTENCE_INDEX_KEY = "CurrentSentenceIndex";
    private const int MAX_MINIGAME_LEVEL = 50;

    private static string SanitizeMinigameId(string id)
    {
        if (string.IsNullOrEmpty(id)) return "unknown";

        var sb = new StringBuilder(id.Length);
        foreach (char c in id)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }
        return sb.ToString();
    }

    private static void RegisterPlayedMinigameId(string sanitizedId)
    {
        // Keep a registry so we can reset/delete later.
        string existing = PlayerPrefs.GetString(MINIGAME_PLAYED_IDS_KEY, string.Empty);
        var set = new HashSet<string>();
        if (!string.IsNullOrEmpty(existing))
        {
            foreach (string part in existing.Split('|'))
            {
                if (!string.IsNullOrEmpty(part)) set.Add(part);
            }
        }
        if (set.Add(sanitizedId))
        {
            PlayerPrefs.SetString(MINIGAME_PLAYED_IDS_KEY, string.Join("|", set));
        }
    }

    public static bool HasMinigameBeenPlayed(string minigameId)
    {
        string id = SanitizeMinigameId(minigameId);
        string key = MINIGAME_PLAYED_ID_PREFIX + id;
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static void SetMinigamePlayed(string minigameId, bool played = true)
    {
        string id = SanitizeMinigameId(minigameId);
        string key = MINIGAME_PLAYED_ID_PREFIX + id;
        PlayerPrefs.SetInt(key, played ? 1 : 0);
        RegisterPlayedMinigameId(id);
        PlayerPrefs.Save();
    }

    public static bool TryAddMinigameScore(string minigameId, int points)
    {
        if (HasMinigameBeenPlayed(minigameId))
        {
            SetMinigamePlayed(minigameId, true);
            return false;
        }

        AddToCurrentScore(points);
        SetMinigamePlayed(minigameId, true);
        return true;
    }

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

        for (int i = 0; i <= MAX_MINIGAME_LEVEL; i++)
        {
            SetMinigameLevelPlayed(i, false);
        }

        // Also clear any id-based minigame flags we have registered.
        string existing = PlayerPrefs.GetString(MINIGAME_PLAYED_IDS_KEY, string.Empty);
        if (!string.IsNullOrEmpty(existing))
        {
            foreach (string id in existing.Split('|'))
            {
                if (string.IsNullOrEmpty(id)) continue;
                PlayerPrefs.SetInt(MINIGAME_PLAYED_ID_PREFIX + id, 0);
            }
        }
        PlayerPrefs.DeleteKey(MINIGAME_PLAYED_IDS_KEY);
        PlayerPrefs.Save();
    }

    public static void DeleteAllPlayerData()
    {
        SetCurrentScore(0);
        for (int i = 0; i <= MAX_MINIGAME_LEVEL; i++)
        {
            SetMinigameLevelPlayed(i, false);
        }

        // Clear any id-based minigame flags we have registered.
        string existing = PlayerPrefs.GetString(MINIGAME_PLAYED_IDS_KEY, string.Empty);
        if (!string.IsNullOrEmpty(existing))
        {
            foreach (string id in existing.Split('|'))
            {
                if (string.IsNullOrEmpty(id)) continue;
                PlayerPrefs.DeleteKey(MINIGAME_PLAYED_ID_PREFIX + id);
            }
        }
        PlayerPrefs.DeleteKey(MINIGAME_PLAYED_IDS_KEY);

        PlayerPrefs.DeleteKey(ProgressKeys.PlayerName);
        ClearGameState();

        LogManager.ClearLog();

        foreach (string key in ProgressKeys.AllProgressKeys)
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
