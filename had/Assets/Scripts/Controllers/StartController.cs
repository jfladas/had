using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartController : MonoBehaviour
{
    public Button startButton;
    public TMP_InputField playerNameInput;
    public Character meCharacter;
    public Image fadeImage;
    private Animator animator;
    private AsyncOperation asyncLoad;

    void Start()
    {
        startButton.onClick.AddListener(StartGame);
        animator = GetComponent<Animator>();
        StartCoroutine(PreloadMainScene());
    }

    private IEnumerator PreloadMainScene()
    {
        asyncLoad = SceneManager.LoadSceneAsync("MainScene");
        asyncLoad.allowSceneActivation = false;
        yield return asyncLoad;
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
        animator.SetTrigger("FadeOut");
        StartCoroutine(WaitAndActivateScene());
    }

    private IEnumerator WaitAndActivateScene()
    {
        yield return new WaitForSeconds(0.5f);
        asyncLoad.allowSceneActivation = true;
    }
}