using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartController : MonoBehaviour
{
    // Static field to communicate the starting scene to GameController
    public static GameScene overrideStartingScene = null;

    public Button startButton;
    public TMP_InputField playerNameInput;
    public Character meCharacter;
    public Image fadeImage;
    private Animator animator;
    private AsyncOperation asyncLoad;

    [Header("Chapter Scenes")]
    public GameScene aChapter15Scene;
    public GameScene aChapter14Scene;
    public GameScene aChapter13Scene;
    public GameScene aChapter12Scene;
    public GameScene aChapter11Scene;
    public GameScene aChapter10Scene;
    public GameScene aChapter9Scene;
    public GameScene aChapter8Scene;
    public GameScene aChapter7Scene;
    public GameScene chapter6Scene;
    public GameScene chapter5Scene;
    public GameScene chapter4Scene;
    public GameScene chapter3Scene;
    public GameScene chapter2Scene;
    public GameScene chapter1Scene;
    public GameScene chapter0Scene; // Default starting scene

    void Start()
    {
        startButton.onClick.AddListener(StartGame);
        animator = GetComponent<Animator>();
        StartCoroutine(PreloadMainScene());
        LoadSavedPlayerName();
    }

    private void LoadSavedPlayerName()
    {
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(savedName))
        {
            playerNameInput.text = savedName;
        }
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
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
        }
        else
        {
            meCharacter.characterName = "Me";
        }

        GameScene startingScene = DetermineStartingScene();
        if (startingScene != null)
        {
            overrideStartingScene = startingScene;
        }

        animator.SetTrigger("FadeOut");
        StartCoroutine(WaitAndActivateScene());
    }

    private IEnumerator WaitAndActivateScene()
    {
        yield return new WaitForSeconds(0.5f);
        asyncLoad.allowSceneActivation = true;
    }

    private GameScene DetermineStartingScene()
    {
        if (ChapterScene.IsChapterDone("TheEnd"))
            return chapter0Scene;

        if (aChapter15Scene != null && ChapterScene.IsChapterDone("AChapter15"))
            return aChapter15Scene;

        if (aChapter14Scene != null && ChapterScene.IsChapterDone("AChapter14"))
            return aChapter14Scene;

        if (aChapter13Scene != null && ChapterScene.IsChapterDone("AChapter13"))
            return aChapter13Scene;

        if (aChapter12Scene != null && ChapterScene.IsChapterDone("AChapter12"))
            return aChapter12Scene;

        if (aChapter11Scene != null && ChapterScene.IsChapterDone("AChapter11"))
            return aChapter11Scene;

        if (aChapter10Scene != null && ChapterScene.IsChapterDone("AChapter10"))
            return aChapter10Scene;

        if (aChapter9Scene != null && ChapterScene.IsChapterDone("AChapter9"))
            return aChapter9Scene;

        if (aChapter8Scene != null && ChapterScene.IsChapterDone("AChapter8"))
            return aChapter8Scene;

        if (aChapter7Scene != null && ChapterScene.IsChapterDone("AChapter7"))
            return aChapter7Scene;

        if (chapter6Scene != null && ChapterScene.IsChapterDone("Chapter6"))
            return chapter6Scene;

        if (chapter5Scene != null && ChapterScene.IsChapterDone("Chapter5"))
            return chapter5Scene;

        if (chapter4Scene != null && ChapterScene.IsChapterDone("Chapter4"))
            return chapter4Scene;

        if (chapter3Scene != null && ChapterScene.IsChapterDone("Chapter3"))
            return chapter3Scene;

        if (chapter2Scene != null && ChapterScene.IsChapterDone("Chapter2"))
            return chapter2Scene;

        if (chapter1Scene != null && ChapterScene.IsChapterDone("Chapter1"))
            return chapter1Scene;

        return chapter0Scene;
    }
}