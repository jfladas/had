using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Data/New Character")]
[System.Serializable]
public class Character : ScriptableObject
{
    public string characterName;
    public List<Sprite> sprites;
    public SpriteController prefab;

    // Runtime-only name (e.g. the player's chosen name). Never serialized, so the asset itself stays unchanged.
    [System.NonSerialized]
    public string runtimeName;

    public string DisplayName => string.IsNullOrEmpty(runtimeName) ? characterName : runtimeName;
}
