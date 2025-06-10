using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    public GameObject circlePrefab;
    public GameObject horizontalLinePrefab;
    public GameObject verticalLinePrefab;
    public RectTransform canvasTransform;
    public TMPro.TMP_Text scoreText;

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

        float minigameDuration;
        float spawnInterval;
        float lineSpeed;
        float circleGrowDuration;


        switch (minigameScene.level)
        {
            case 0:
                minigameDuration = 15f;
                spawnInterval = 1.5f;
                lineSpeed = 3000f;
                circleGrowDuration = 1f;
                break;
            case 1:
                minigameDuration = 20f;
                spawnInterval = 1f;
                lineSpeed = 1000f;
                circleGrowDuration = 3f;
                break;
            case 2:
                minigameDuration = 15f;
                spawnInterval = 0.8f;
                lineSpeed = 1500f;
                circleGrowDuration = 2f;
                break;
            case 3:
                minigameDuration = 10f;
                spawnInterval = 0.5f;
                lineSpeed = 2000f;
                circleGrowDuration = 1.5f;
                break;
            default:
                minigameDuration = 20f;
                spawnInterval = 1f;
                lineSpeed = 1000f;
                circleGrowDuration = 3f;
                break;
        }

        UpdateScoreText();

        float elapsedTime = 0f;
        float circleSpawnTimer = 0f; // Move this outside the while loop to persist across rounds

        while (elapsedTime < minigameDuration)
        {
            // Add a small gap to prevent double-click/press
            yield return new WaitForSeconds(0.1f);

            activeHorizontalLine = Instantiate(horizontalLinePrefab, canvasTransform);
            RectTransform hLineRect = activeHorizontalLine.GetComponent<RectTransform>();
            hLineRect.anchoredPosition = new Vector2(0, canvasTransform.rect.height / 2);

            StartCoroutine(AnimateHorizontalLine(lineSpeed));

            float horizontalElapsed = 0f;
            isLineMoving = true;

            // Only use horizontalElapsed and circleSpawnTimer here
            while (isLineMoving && (elapsedTime + horizontalElapsed) < minigameDuration)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    StopHorizontalLine();
                    yield return null;
                    break;
                }

                // This timer now persists across all rounds
                circleSpawnTimer += Time.deltaTime;
                if (circleSpawnTimer >= spawnInterval)
                {
                    SpawnCircle(circleGrowDuration);
                    circleSpawnTimer = 0f;
                }

                horizontalElapsed += Time.deltaTime;
                yield return null;
            }

            // If time is up after horizontal, break before vertical
            if (elapsedTime + horizontalElapsed >= minigameDuration)
            {
                elapsedTime += horizontalElapsed;
                if (activeHorizontalLine != null)
                    Destroy(activeHorizontalLine);
                break;
            }

            // Add a small gap to prevent double-click/press
            yield return new WaitForSeconds(0.1f);

            activeVerticalLine = Instantiate(verticalLinePrefab, canvasTransform);
            RectTransform vLineRect = activeVerticalLine.GetComponent<RectTransform>();
            vLineRect.anchoredPosition = new Vector2(-canvasTransform.rect.width / 2, 0);

            StartCoroutine(AnimateVerticalLine(lineSpeed));

            isVerticalLineMoving = true;
            float verticalElapsed = 0f;
            while (isVerticalLineMoving && (elapsedTime + horizontalElapsed + verticalElapsed) < minigameDuration)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    StopVerticalLine();
                    yield return null;
                    break;
                }

                // Circles should also spawn during the vertical line phase
                circleSpawnTimer += Time.deltaTime;
                if (circleSpawnTimer >= spawnInterval)
                {
                    SpawnCircle(circleGrowDuration);
                    circleSpawnTimer = 0f;
                }

                verticalElapsed += Time.deltaTime;
                yield return null;
            }

            // If time is up after vertical, update elapsedTime and break
            if (elapsedTime + horizontalElapsed + verticalElapsed >= minigameDuration)
            {
                elapsedTime += horizontalElapsed + verticalElapsed;
            }
            else
            {
                elapsedTime += horizontalElapsed + verticalElapsed;
            }

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

            if (activeHorizontalLine != null)
            {
                Destroy(activeHorizontalLine);
            }
            if (activeVerticalLine != null)
            {
                Destroy(activeVerticalLine);
            }
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

    private IEnumerator AnimateHorizontalLine(float speed = 1000f)
    {
        isLineMoving = true;
        RectTransform lineTransform = activeHorizontalLine != null ? activeHorizontalLine.GetComponent<RectTransform>() : null;
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

    private IEnumerator AnimateVerticalLine(float speed = 1000f)
    {
        isVerticalLineMoving = true;
        RectTransform lineTransform = activeVerticalLine != null ? activeVerticalLine.GetComponent<RectTransform>() : null;
        int direction = 1;

        while (isVerticalLineMoving)
        {
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

    private void SpawnCircle(float growDuration = 3f)
    {
        Vector2 randomPosition = new Vector2(
            Random.Range(0, canvasTransform.rect.width) - canvasTransform.rect.width / 2,
            Random.Range(0, canvasTransform.rect.height) - canvasTransform.rect.height / 2
        );

        GameObject circle = Instantiate(circlePrefab, canvasTransform);
        circle.GetComponent<RectTransform>().anchoredPosition = randomPosition;
        activeCircles.Add(circle);

        StartCoroutine(AnimateCircle(circle, growDuration));
    }

    private IEnumerator AnimateCircle(GameObject circle, float growDuration = 3f)
    {
        if (circle == null) yield break;

        RectTransform rectTransform = circle.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = circle.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = circle.AddComponent<CanvasGroup>();
        }

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

    // Restore correct PlayAudio for StoryScene.Sentence
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
