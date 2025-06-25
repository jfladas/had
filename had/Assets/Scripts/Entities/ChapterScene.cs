using UnityEngine;

[CreateAssetMenu(fileName = "NewChapterScene", menuName = "Data/New Chapter Scene")]
[System.Serializable]
public class ChapterScene : GameScene
{
    public Sprite background;
    public GameScene nextScene;

    public static void SetChapterDone(string chapterKey, bool done)
    {
        PlayerPrefs.SetInt(chapterKey, done ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool IsChapterDone(string chapterKey)
    {
        return PlayerPrefs.GetInt(chapterKey, 0) == 1;
    }
}