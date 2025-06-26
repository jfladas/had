using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{
    public GameScene currentScene;
    public BottomBarController bottomBar;
    public NameBarController nameBar;
    public SpriteSwitcher spriteSwitcher;
    public ChooseController chooseController;
    public AudioController audioController;
    public Character player;
    private string playerName;

    public MinigameController minigameController;
    public Button menuButton;
    public Image menuImage;
    public GameScene menuScene;
    public Button closeButton;

    private int score = 0;

    private State state = State.IDLE;

    private enum State
    {
        IDLE, ANIMATE, CHOOSE
    }

    private GameScene savedScene;
    private int savedSentenceIndex;

    void Start()
    {
        score = ScoreManager.GetCurrentScore();

        if (StartController.overrideStartingScene != null)
        {
            currentScene = StartController.overrideStartingScene;
            StartController.overrideStartingScene = null;
        }

        playerName = player.characterName;
        if (currentScene is StoryScene)
        {
            StoryScene storyScene = currentScene as StoryScene;
            bottomBar.PlayScene(storyScene, playerName);
            spriteSwitcher.SetImage(storyScene.background);
            PlayAudio(storyScene.sentences[0]);
        }
        else if (currentScene is ChapterScene)
        {
            ChapterScene chapterScene = currentScene as ChapterScene;
            StartCoroutine(DisplayChapterScene(chapterScene));
        }

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuButtonClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);

        if (minigameController != null)
        {
            minigameController.OnMinigameComplete += OnMinigameComplete;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (state == State.IDLE)
            {
                if (currentScene is StoryScene)
                {
                    if (bottomBar.IsCompleted())
                    {
                        if (bottomBar.IsLastSentence())
                        {
                            PlayScene((currentScene as StoryScene).nextScene);
                        }
                        else
                        {
                            bottomBar.PlayNextSentence(playerName);
                            PlayAudio((currentScene as StoryScene).sentences[bottomBar.GetSentenceIndex()]);
                        }
                    }
                    else
                    {
                        bottomBar.SkipToFullSentence(playerName);
                    }
                }
            }
        }
    }

    public void PlayScene(GameScene scene)
    {
        StartCoroutine(SwitchScene(scene));
    }

    private IEnumerator SwitchScene(GameScene scene)
    {
        state = State.ANIMATE;
        var previousScene = currentScene;
        currentScene = scene;
        bottomBar.Hide();
        nameBar.Hide();
        menuButton?.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        if (previousScene is ChooseScene && scene is ChooseScene)
        {
            yield return new WaitForSeconds(0.5f);
        }
        if (scene is ChapterScene)
        {
            closeButton?.gameObject.SetActive(false);
            if (menuImage != null && menuImage.gameObject.activeSelf)
            {
                menuImage.gameObject.SetActive(false);
            }
            if (bottomBar != null && bottomBar.spritesPrefab != null)
            {
                foreach (Transform child in bottomBar.spritesPrefab.transform)
                {
                    GameObject.Destroy(child.gameObject);
                }
                if (bottomBar.sprites != null)
                {
                    bottomBar.sprites.Clear();
                }
            }
        }
        if (scene is StoryScene)
        {
            menuButton?.gameObject.SetActive(true);
            StoryScene storyScene = scene as StoryScene;
            spriteSwitcher.SwitchImage(storyScene.background);
            PlayAudio(storyScene.sentences[0]);
            yield return new WaitForSeconds(0.5f);
            bottomBar.ClearText();
            bottomBar.Show();
            string charName = storyScene.sentences[0].character.characterName;
            if (charName != "" && charName != "...")
            {
                nameBar.Show();
            }
            yield return new WaitForSeconds(0.5f);
            bottomBar.PlayScene(storyScene, playerName);
            state = State.IDLE;
        }
        else if (scene is ChooseScene)
        {
            state = State.CHOOSE;
            chooseController.SetupChoose(scene as ChooseScene);
        }
        else if (scene is ChapterScene)
        {
            ChapterScene.SetChapterDone(scene.name, true);
            StartCoroutine(DisplayChapterScene(scene as ChapterScene));
        }
        else if (scene is MinigameScene)
        {
            if (minigameController != null)
            {
                StartCoroutine(minigameController.PlayMinigame(scene as MinigameScene));
            }
        }
    }

    private IEnumerator DisplayChapterScene(ChapterScene chapterScene)
    {
        if (chapterScene.name == "Home")
        {
            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene("StartScene");
            yield break;
        }

        if (chapterScene.name == "Delete")
        {
            yield return new WaitForSeconds(0.5f);
            ScoreManager.DeleteAllPlayerData();
            SceneManager.LoadScene("StartScene");
            yield break;
        }

        bottomBar.Hide();
        nameBar.Hide();
        spriteSwitcher.SwitchImage(chapterScene.background);
        if (chapterScene.name != "Start")
        {
            float waitTime = 0f;
            bool proceed = false;
            while (waitTime < 5f && !proceed)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    proceed = true;
                }
                waitTime += Time.deltaTime;
                yield return null;
            }
        }
        PlayScene(chapterScene.nextScene);
    }

    private void PlayAudio(StoryScene.Sentence sentence)
    {
        audioController.PlayAudio(sentence.music, sentence.sound);
    }

    private void OnMenuButtonClicked()
    {
        //Time.timeScale = 0f;
        menuImage?.gameObject.SetActive(true);
        closeButton?.gameObject.SetActive(true);
        menuButton?.gameObject.SetActive(false);
        savedScene = currentScene;
        if (currentScene is StoryScene && bottomBar != null)
            savedSentenceIndex = bottomBar.GetSentenceIndex();
        else
            savedSentenceIndex = -1;
        StartCoroutine(SwitchScene(menuScene));
    }

    private void OnCloseButtonClicked()
    {
        menuImage?.gameObject.SetActive(false);
        closeButton?.gameObject.SetActive(false);
        menuButton?.gameObject.SetActive(true);
        //Time.timeScale = 1f;
        if (savedScene is StoryScene && bottomBar != null && savedSentenceIndex >= 0)
        {
            chooseController.HideChoose();
            StartCoroutine(SwitchScene(savedScene));
        }
    }

    private void OnMinigameComplete(GameScene nextScene)
    {
        if (nextScene != null)
        {
            PlayScene(nextScene);
        }
    }

    private IEnumerator HideReplayMessageAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        if (bottomBar != null)
        {
            bottomBar.Hide();
        }
    }
}
