using UnityEngine;

[CreateAssetMenu(fileName = "NewMinigameScene", menuName = "Data/New Minigame Scene")]
[System.Serializable]
public class MinigameScene : GameScene
{
    public Sprite background;
    public int level;
    public GameScene nextScene;

}
