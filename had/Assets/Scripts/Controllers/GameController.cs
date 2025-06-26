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

    [Header("Gallery Illustrations")]
    public Image illustrationA1;
    public Image illustrationA2;
    public Image illustrationA3;
    public Image illustrationA4;
    public Image illustrationA5;
    public Image illustrationA6;
    public Image illustrationLocked;

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
            ScoreManager.SaveGameState(currentScene.name, 0);
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

        HideAllIllustrations();
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
                            ScoreManager.SaveGameState(currentScene.name, bottomBar.GetSentenceIndex());
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
            HideAllIllustrations();
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
            ScoreManager.SaveGameState(scene.name, 0);
            state = State.IDLE;
        }
        else if (scene is ChooseScene)
        {
            ChooseScene chooseScene = scene as ChooseScene;
            HandleGalleryIllustrations(chooseScene.name);
            state = State.CHOOSE;
            chooseController.SetupChoose(chooseScene);
            ScoreManager.ClearGameState();
        }
        else if (scene is ChapterScene)
        {
            ChapterScene.SetChapterDone(scene.name, true);
            StartCoroutine(DisplayChapterScene(scene as ChapterScene));
            ScoreManager.ClearGameState();
        }
        else if (scene is MinigameScene)
        {
            HideAllIllustrations();
            if (minigameController != null)
            {
                StartCoroutine(minigameController.PlayMinigame(scene as MinigameScene));
            }
            ScoreManager.ClearGameState();
        }
    }

    private IEnumerator SwitchSceneFromSaved(GameScene scene, int savedSentenceIndex)
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

        if (scene is StoryScene)
        {
            HideAllIllustrations();
            menuButton?.gameObject.SetActive(true);
            StoryScene storyScene = scene as StoryScene;
            spriteSwitcher.SwitchImage(storyScene.background);

            int targetSentenceIndex = savedSentenceIndex >= 0 ? savedSentenceIndex : 0;
            PlayAudio(storyScene.sentences[targetSentenceIndex]);
            yield return new WaitForSeconds(0.5f);
            bottomBar.ClearText();
            bottomBar.Show();
            string charName = storyScene.sentences[targetSentenceIndex].character.characterName;
            if (charName != "" && charName != "...")
            {
                nameBar.Show();
            }
            yield return new WaitForSeconds(0.5f);

            if (savedSentenceIndex >= 0)
            {
                bottomBar.PlaySceneFromSentence(storyScene, playerName, targetSentenceIndex);
            }
            else
            {
                bottomBar.PlayScene(storyScene, playerName);
            }
            state = State.IDLE;
        }
        else if (scene is ChooseScene)
        {
            ChooseScene chooseScene = scene as ChooseScene;
            HandleGalleryIllustrations(chooseScene.name);
            state = State.CHOOSE;
            chooseController.SetupChoose(chooseScene);
            ScoreManager.ClearGameState();
        }
        else if (scene is ChapterScene)
        {
            ChapterScene.SetChapterDone(scene.name, true);
            StartCoroutine(DisplayChapterScene(scene as ChapterScene));
            ScoreManager.ClearGameState();
        }
        else if (scene is MinigameScene)
        {
            HideAllIllustrations();
            if (minigameController != null)
            {
                StartCoroutine(minigameController.PlayMinigame(scene as MinigameScene));
            }
            ScoreManager.ClearGameState();
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

    private void HandleGalleryIllustrations(string sceneName)
    {
        HideAllIllustrations();

        switch (sceneName)
        {
            case "GalleryA1":
                if (ChapterScene.IsChapterDone("A1"))
                {
                    if (illustrationA1 != null)
                        illustrationA1.gameObject.SetActive(true);
                }
                else
                {
                    if (illustrationLocked != null)
                        illustrationLocked.gameObject.SetActive(true);
                }
                break;
            case "GalleryA2":
                if (ChapterScene.IsChapterDone("A2"))
                {
                    if (illustrationA2 != null)
                        illustrationA2.gameObject.SetActive(true);
                }
                else
                {
                    if (illustrationLocked != null)
                        illustrationLocked.gameObject.SetActive(true);
                }
                break;
            case "GalleryA3":
                if (ChapterScene.IsChapterDone("A3"))
                {
                    if (illustrationA3 != null)
                        illustrationA3.gameObject.SetActive(true);
                }
                else
                {
                    if (illustrationLocked != null)
                        illustrationLocked.gameObject.SetActive(true);
                }
                break;
            case "GalleryA4":
                if (ChapterScene.IsChapterDone("A4"))
                {
                    if (illustrationA4 != null)
                        illustrationA4.gameObject.SetActive(true);
                }
                else
                {
                    if (illustrationLocked != null)
                        illustrationLocked.gameObject.SetActive(true);
                }
                break;
            case "GalleryA5":
                if (ChapterScene.IsChapterDone("A5"))
                {
                    if (illustrationA5 != null)
                        illustrationA5.gameObject.SetActive(true);
                }
                else
                {
                    if (illustrationLocked != null)
                        illustrationLocked.gameObject.SetActive(true);
                }
                break;
            case "GalleryA6":
                if (ChapterScene.IsChapterDone("A6"))
                {
                    if (illustrationA6 != null)
                        illustrationA6.gameObject.SetActive(true);
                }
                else
                {
                    if (illustrationLocked != null)
                        illustrationLocked.gameObject.SetActive(true);
                }
                break;
        }
    }

    private void HideAllIllustrations()
    {
        if (illustrationA1 != null) illustrationA1.gameObject.SetActive(false);
        if (illustrationA2 != null) illustrationA2.gameObject.SetActive(false);
        if (illustrationA3 != null) illustrationA3.gameObject.SetActive(false);
        if (illustrationA4 != null) illustrationA4.gameObject.SetActive(false);
        if (illustrationA5 != null) illustrationA5.gameObject.SetActive(false);
        if (illustrationA6 != null) illustrationA6.gameObject.SetActive(false);
        if (illustrationLocked != null) illustrationLocked.gameObject.SetActive(false);
    }

    private void PlayAudio(StoryScene.Sentence sentence)
    {
        audioController.PlayAudio(sentence.music, sentence.sound);
    }

    private void OnMenuButtonClicked()
    {
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
        if (savedScene is StoryScene && bottomBar != null && savedSentenceIndex >= 0)
        {
            chooseController.HideChoose();
            StartCoroutine(SwitchSceneFromSaved(savedScene, savedSentenceIndex));
        }
        else if (savedScene != null)
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
