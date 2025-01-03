using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Data/New Character")]
[System.Serializable]
public class Character : ScriptableObject
{
    public string characterName;
    public List<Sprite> sprites;
    public SpriteController prefab;
}
