using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartController : MonoBehaviour
{
    public Button startButton;
    public TMP_InputField playerNameInput;
    public Character meCharacter;

    void Start()
    {
        startButton.onClick.AddListener(StartGame);
    }

    void StartGame()
    {
        string playerName = playerNameInput.text;
        if (playerName != "")
        {
            meCharacter.characterName = playerName;
        }
        else
        {
            meCharacter.characterName = "Me";
        }
        SceneManager.LoadScene("MainScene");
    }
}