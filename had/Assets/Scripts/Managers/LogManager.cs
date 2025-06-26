using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LogEntry
{
    public string characterName;
    public string text;
    public bool isThought;

    public LogEntry(string characterName, string text, bool isThought = false)
    {
        this.characterName = characterName;
        this.text = text;
        this.isThought = isThought;
    }

    public string FormatForDisplay()
    {
        if (string.IsNullOrEmpty(characterName))
        {
            return isThought ? $"<i>{text}</i>" : text;
        }
        else
        {
            return isThought ? $"<i>{text}</i>" : $"{characterName}: \"{text}\"";
        }
    }
}

public static class LogManager
{
    private const string LOG_DATA_KEY = "DialogueLog";
    private const int MAX_CHARACTER_LIMIT = 800;

    public static void AddLogEntry(string characterName, string text)
    {
        bool isThought = characterName == "...";

        string displayCharacterName = (string.IsNullOrEmpty(characterName) || characterName == "...") ? "" : characterName;

        LogEntry newEntry = new LogEntry(displayCharacterName, text, isThought);

        List<LogEntry> currentLog = GetLogEntries();

        foreach (LogEntry existingEntry in currentLog)
        {
            if (existingEntry.characterName == newEntry.characterName &&
                existingEntry.text == newEntry.text &&
                existingEntry.isThought == newEntry.isThought)
            {
                return;
            }
        }

        currentLog.Add(newEntry);

        EnforceCharacterLimit(currentLog);

        SaveLogEntries(currentLog);
    }

    public static List<LogEntry> GetLogEntries()
    {
        string logJson = PlayerPrefs.GetString(LOG_DATA_KEY, "");
        if (string.IsNullOrEmpty(logJson))
        {
            return new List<LogEntry>();
        }

        try
        {
            LogEntryList logList = JsonUtility.FromJson<LogEntryList>(logJson);
            return logList?.entries ?? new List<LogEntry>();
        }
        catch
        {
            return new List<LogEntry>();
        }
    }

    public static string GetFormattedLogText()
    {
        List<LogEntry> entries = GetLogEntries();
        if (entries.Count == 0)
        {
            return "No dialogue recorded yet.";
        }

        List<string> formattedEntries = new List<string>();
        foreach (LogEntry entry in entries)
        {
            formattedEntries.Add(entry.FormatForDisplay());
        }

        return string.Join("\n\n", formattedEntries);
    }

    private static void EnforceCharacterLimit(List<LogEntry> logEntries)
    {
        int totalCharacters = CalculateLogLength(logEntries);

        while (totalCharacters > MAX_CHARACTER_LIMIT && logEntries.Count > 0)
        {
            logEntries.RemoveAt(0);
            totalCharacters = CalculateLogLength(logEntries);
        }
    }

    private static int CalculateLogLength(List<LogEntry> logEntries)
    {
        int totalLength = 0;
        foreach (LogEntry entry in logEntries)
        {
            string formattedEntry = entry.FormatForDisplay();

            foreach (char c in formattedEntry)
            {
                if (c == '\n')
                {
                    totalLength += 15;
                }
                else
                {
                    totalLength += 1;
                }
            }

            totalLength += 30;
        }
        return totalLength;
    }

    private static void SaveLogEntries(List<LogEntry> logEntries)
    {
        LogEntryList logList = new LogEntryList { entries = logEntries };
        string logJson = JsonUtility.ToJson(logList);
        PlayerPrefs.SetString(LOG_DATA_KEY, logJson);
        PlayerPrefs.Save();
    }

    public static void ClearLog()
    {
        PlayerPrefs.DeleteKey(LOG_DATA_KEY);
        PlayerPrefs.Save();
    }
}

[System.Serializable]
public class LogEntryList
{
    public List<LogEntry> entries = new List<LogEntry>();
}
