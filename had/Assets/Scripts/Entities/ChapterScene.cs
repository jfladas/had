using UnityEngine;

[CreateAssetMenu(fileName = "NewChapterScene", menuName = "Data/New Chapter Scene")]
[System.Serializable]
public class ChapterScene : GameScene
{
    public Sprite background;
    public GameScene nextScene;
    public GameScene failScene;

    public static void SetChapterDone(string chapterKey, bool done)
    {
        PlayerPrefs.SetInt(chapterKey, done ? 1 : 0);

        if (done)
        {
            long ticks = System.DateTime.UtcNow.Ticks;
            PlayerPrefs.SetString(ProgressKeys.GetCheckpointCompletedAtKey(chapterKey), ticks.ToString());
            PlayerPrefs.SetString(ProgressKeys.LastCompletedCheckpointKey, chapterKey);
            PlayerPrefs.SetString(ProgressKeys.LastCompletedCheckpointAtUtcTicks, ticks.ToString());
        }

        PlayerPrefs.Save();
    }

    public static bool IsChapterDone(string chapterKey)
    {
        return PlayerPrefs.GetInt(chapterKey, 0) == 1;
    }

    public static bool TryGetChapterDoneAtUtcTicks(string chapterKey, out long ticks)
    {
        ticks = 0;
        if (string.IsNullOrEmpty(chapterKey))
            return false;

        string raw = PlayerPrefs.GetString(ProgressKeys.GetCheckpointCompletedAtKey(chapterKey), "");
        return long.TryParse(raw, out ticks);
    }
}