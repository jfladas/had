using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStoryScene", menuName = "Data/New Story Scene")]
[System.Serializable]
public class StoryScene : GameScene
{
    public List<Sentence> sentences;
    public Sprite background;
    public GameScene nextScene;

    [System.Serializable]
    public struct Sentence
    {
        public string text;
        public Character character;
        public List<Action> actions;
        public AudioClip music;
        public AudioClip sound;

        [System.Serializable]
        public struct Action
        {
            public Character character;
            public int spriteIndex;
            public Type type;
            public Vector2 position;
            public float speed;

            [System.Serializable]
            public enum Type
            {
                NONE, SHOW, MOVE, HIDE
            }
        }
    }
}

public class GameScene : ScriptableObject { }
