using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public GameObject circlePrefab;
    public RectTransform canvasTransform;
    private List<GameObject> activeCircles = new List<GameObject>();

    private State state = State.IDLE;

    private enum State
    {
        IDLE, ANIMATE, CHOOSE
    }

    void Start()
    {
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
        currentScene = scene;
        bottomBar.Hide();
        nameBar.Hide();
        yield return new WaitForSeconds(0.5f);
        if (scene is StoryScene)
        {
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
            StartCoroutine(DisplayChapterScene(scene as ChapterScene));
        }
        else if (scene is MinigameScene)
        {
            StartCoroutine(PlayMinigame(scene as MinigameScene));
        }
    }

    private IEnumerator DisplayChapterScene(ChapterScene chapterScene)
    {
        bottomBar.Hide();
        nameBar.Hide();
        spriteSwitcher.SwitchImage(chapterScene.background);
        yield return new WaitForSeconds(0.5f);
        PlayScene(chapterScene.nextScene);
    }

    private IEnumerator PlayMinigame(MinigameScene minigameScene)
    {
        state = State.ANIMATE;
        bottomBar.Hide();
        nameBar.Hide();
        spriteSwitcher.SwitchImage(minigameScene.background);

        float minigameDuration = 10f; // Duration of the minigame
        float spawnInterval = 1f; // Interval between spawning circles
        float elapsedTime = 0f;

        while (elapsedTime < minigameDuration)
        {
            SpawnCircle();
            yield return new WaitForSeconds(spawnInterval);
            elapsedTime += spawnInterval;
        }

        // Clean up remaining circles
        foreach (var circle in activeCircles)
        {
            Destroy(circle);
        }
        activeCircles.Clear();

        PlayScene(minigameScene.nextScene);
    }

    private void SpawnCircle()
    {
        Vector2 randomPosition = new Vector2(
            Random.Range(0, canvasTransform.rect.width) - canvasTransform.rect.width / 2,
            Random.Range(0, canvasTransform.rect.height) - canvasTransform.rect.height / 2
        );

        GameObject circle = Instantiate(circlePrefab, canvasTransform);
        circle.GetComponent<RectTransform>().anchoredPosition = randomPosition;
        activeCircles.Add(circle);

        StartCoroutine(AnimateCircle(circle));
    }

    private IEnumerator AnimateCircle(GameObject circle)
    {
        if (circle == null) yield break;

        RectTransform rectTransform = circle.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = circle.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = circle.AddComponent<CanvasGroup>();
        }

        float growDuration = 2f; // Time for the circle to grow
        float fadeDuration = 1f; // Time for the circle to fade out
        float elapsedTime = 0f;

        Vector2 initialScale = Vector2.zero;
        Vector2 targetScale = new Vector2(10f, 10f); // Final size of the circle

        // Grow the circle
        while (elapsedTime < growDuration)
        {
            if (circle == null) yield break;

            rectTransform.localScale = Vector2.Lerp(initialScale, targetScale, elapsedTime / growDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;

        // Fade out the circle
        while (elapsedTime < fadeDuration)
        {
            if (circle == null) yield break;

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (circle != null)
        {
            Destroy(circle);
            activeCircles.Remove(circle);
        }
    }

    private void PlayAudio(StoryScene.Sentence sentence)
    {
        audioController.PlayAudio(sentence.music, sentence.sound);
    }
}
