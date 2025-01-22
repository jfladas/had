using UnityEngine;

[CreateAssetMenu(fileName = "NewChapterScene", menuName = "Data/New Chapter Scene")]
[System.Serializable]
public class ChapterScene : GameScene
{
    public Sprite background;
    public GameScene nextScene;
}