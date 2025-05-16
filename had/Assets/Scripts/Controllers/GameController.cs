using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Add this for Text
using TMPro; // Add this for TextMeshPro

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
    public GameObject horizontalLinePrefab;
    public GameObject verticalLinePrefab;
    public RectTransform canvasTransform;
    public TMPro.TMP_Text scoreText; // Changed from UnityEngine.UI.Text to TMPro.TMP_Text

    private List<GameObject> activeCircles = new List<GameObject>();
    private GameObject activeHorizontalLine;
    private GameObject activeVerticalLine;
    private bool isLineMoving = false;
    private bool isVerticalLineMoving = false;

    private int score = 0;

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

        float minigameDuration = 20f;
        float spawnInterval = 1f;
        float elapsedTime = 0f;
        float gapDuration = 0.5f;

        UpdateScoreText(); // Initialize score display

        while (elapsedTime < minigameDuration)
        {
            // Spawn and animate the horizontal line
            activeHorizontalLine = Instantiate(horizontalLinePrefab, canvasTransform);
            StartCoroutine(AnimateHorizontalLine());

            // Wait for horizontal line to stop
            float horizontalElapsed = 0f;
            isLineMoving = true;
            while (isLineMoving && elapsedTime < minigameDuration)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    StopHorizontalLine();
                    yield return null;
                    break;
                }
                SpawnCircle();
                yield return new WaitForSeconds(spawnInterval);
                elapsedTime += spawnInterval;
                horizontalElapsed += spawnInterval;
            }

            // Spawn and animate the vertical line (horizontal stays visible)
            activeVerticalLine = Instantiate(verticalLinePrefab, canvasTransform);
            StartCoroutine(AnimateVerticalLine());

            // Wait for vertical line to stop
            isVerticalLineMoving = true;
            while (isVerticalLineMoving && elapsedTime < minigameDuration)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    StopVerticalLine();
                    yield return null;
                    break;
                }
                yield return null;
            }

            // --- Intersection and scoring logic ---
            if (activeHorizontalLine != null && activeVerticalLine != null)
            {
                RectTransform hLine = activeHorizontalLine.GetComponent<RectTransform>();
                RectTransform vLine = activeVerticalLine.GetComponent<RectTransform>();
                Vector2 intersection = new Vector2(
                    vLine.anchoredPosition.x,
                    hLine.anchoredPosition.y
                );

                GameObject hitCircle = null;
                foreach (var circle in activeCircles)
                {
                    RectTransform cRect = circle.GetComponent<RectTransform>();
                    float radius = cRect.sizeDelta.x * cRect.localScale.x / 2f;
                    Vector2 circleCenter = cRect.anchoredPosition;
                    if (Vector2.Distance(intersection, circleCenter) <= radius)
                    {
                        hitCircle = circle;
                        break;
                    }
                }
                if (hitCircle != null)
                {
                    activeCircles.Remove(hitCircle);
                    Destroy(hitCircle);
                    score += 100;
                    UpdateScoreText();
                }
            }

            // Clean up both lines
            if (activeHorizontalLine != null)
            {
                Destroy(activeHorizontalLine);
            }
            if (activeVerticalLine != null)
            {
                Destroy(activeVerticalLine);
            }

            yield return new WaitForSeconds(gapDuration);
        }

        foreach (var circle in activeCircles)
        {
            Destroy(circle);
        }
        if (activeHorizontalLine != null)
        {
            Destroy(activeHorizontalLine);
        }
        if (activeVerticalLine != null)
        {
            Destroy(activeVerticalLine);
        }

        if (scoreText != null)
        {
            scoreText.text = "";
        }

        PlayScene(minigameScene.nextScene);
    }

    private IEnumerator AnimateHorizontalLine()
    {
        isLineMoving = true;
        RectTransform lineTransform = activeHorizontalLine != null ? activeHorizontalLine.GetComponent<RectTransform>() : null;
        float speed = 1000f;
        int direction = 1;

        while (isLineMoving)
        {
            if (activeHorizontalLine == null || lineTransform == null)
                yield break;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                StopHorizontalLine();
                yield break;
            }

            lineTransform.anchoredPosition += new Vector2(0, speed * direction * Time.deltaTime);

            if (lineTransform.anchoredPosition.y >= canvasTransform.rect.height / 2 && direction == 1)
            {
                direction = -1;
            }
            else if (lineTransform.anchoredPosition.y <= -canvasTransform.rect.height / 2 && direction == -1)
            {
                direction = 1;
            }

            yield return null;
        }
    }

    private IEnumerator AnimateVerticalLine()
    {
        isVerticalLineMoving = true;
        RectTransform lineTransform = activeVerticalLine != null ? activeVerticalLine.GetComponent<RectTransform>() : null;
        float speed = 1000f;
        int direction = 1;

        while (isVerticalLineMoving)
        {
            // Check if the line or its RectTransform has been destroyed
            if (activeVerticalLine == null || lineTransform == null)
                yield break;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                StopVerticalLine();
                yield break;
            }

            lineTransform.anchoredPosition += new Vector2(speed * direction * Time.deltaTime, 0);

            if (lineTransform.anchoredPosition.x >= canvasTransform.rect.width / 2 && direction == 1)
            {
                direction = -1;
            }
            else if (lineTransform.anchoredPosition.x <= -canvasTransform.rect.width / 2 && direction == -1)
            {
                direction = 1;
            }

            yield return null;
        }
    }

    private void StopHorizontalLine()
    {
        isLineMoving = false;
    }

    private void StopVerticalLine()
    {
        isVerticalLineMoving = false;
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

        float growDuration = 3f;
        float fadeDuration = 1f;
        float elapsedTime = 0f;

        Vector2 initialScale = Vector2.zero;
        Vector2 targetScale = new Vector2(10f, 10f);

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

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}
