using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinigameController : MonoBehaviour
{
    [Header("Minigame Prefabs")]
    public GameObject circlePrefab;
    public GameObject specialCirclePrefab;
    public GameObject specialCirclePrefab2;
    public GameObject horizontalLinePrefab;
    public GameObject verticalLinePrefab;

    [Header("UI References")]
    public RectTransform canvasTransform;
    public TMPro.TMP_Text scoreText;
    public TMPro.TMP_Text timerText;
    public Image timerImage;
    public Image minigameEndImage;

    [Header("Game References")]
    public BottomBarController bottomBar;
    public NameBarController nameBar;
    public SpriteSwitcher spriteSwitcher;
    private List<GameObject> activeCircles = new List<GameObject>();
    private GameObject activeHorizontalLine;
    private GameObject activeVerticalLine;
    private bool isLineMoving = false;
    private bool isVerticalLineMoving = false;

    private int score = 0;
    private int sessionScore = 0;
    private int currentMinigameLevel = -1;
    private bool isReplayingMinigame = false;

    public System.Action<GameScene> OnMinigameComplete;

    private class CircleColliderData : MonoBehaviour
    {
        public float radius;
        public bool isSpecial = false;
    }

    public IEnumerator PlayMinigame(MinigameScene minigameScene)
    {
        bottomBar.Hide();
        nameBar.Hide();
        spriteSwitcher.SwitchImage(minigameScene.background);

        currentMinigameLevel = minigameScene.level;
        isReplayingMinigame = ScoreManager.HasMinigameLevelBeenPlayed(currentMinigameLevel);

        sessionScore = 0;
        score = ScoreManager.GetCurrentScore();

        if (isReplayingMinigame)
        {
            if (bottomBar != null)
            {
                bottomBar.barText.text = $"Replaying Level {currentMinigameLevel} - Points won't be added to total score";
                bottomBar.Show();
                StartCoroutine(HideReplayMessageAfterDelay());
            }
        }

        minigameEndImage?.gameObject.SetActive(false);
        scoreText?.gameObject.SetActive(true);
        timerText?.gameObject.SetActive(true);
        timerImage?.gameObject.SetActive(true);

        float minigameDuration;
        float spawnInterval;
        float lineSpeed;
        float circleGrowDuration;
        float circleRadius;

        switch (minigameScene.level)
        {
            case 0:
                minigameDuration = 15f;
                spawnInterval = 0.8f;
                lineSpeed = 3000f;
                circleGrowDuration = 0.5f;
                circleRadius = 80f;
                break;
            case 1:
            default:
                minigameDuration = 20f;
                spawnInterval = 1f;
                lineSpeed = 1000f;
                circleGrowDuration = 3f;
                circleRadius = 40f;
                break;
            case 2:
                minigameDuration = 15f;
                spawnInterval = 0.8f;
                lineSpeed = 1500f;
                circleGrowDuration = 2f;
                circleRadius = 60f;
                break;
            case 3:
                minigameDuration = 10f;
                spawnInterval = 0.8f;
                lineSpeed = 2000f;
                circleGrowDuration = 2f;
                circleRadius = 60f;
                break;
            case 4:
                minigameDuration = 10f;
                spawnInterval = 0.5f;
                lineSpeed = 2000f;
                circleGrowDuration = 1.5f;
                circleRadius = 80f;
                break;
        }

        UpdateScoreText(score);

        float elapsedTime = 0f;
        float circleSpawnTimer = 0f;

        UpdateTimerText(minigameDuration);

        float spawnIntervalMin = spawnInterval * 0.8f;
        float spawnIntervalMax = spawnInterval * 1.2f;
        float growDurationMin = circleGrowDuration * 0.8f;
        float growDurationMax = circleGrowDuration * 1.2f;
        float radiusMin = circleRadius * 0.8f;
        float radiusMax = circleRadius * 1.2f;

        float nextSpawnInterval = Random.Range(spawnIntervalMin, spawnIntervalMax);

        if (minigameScene.level == 1 && !isReplayingMinigame)
        {
            yield return StartCoroutine(PlayTutorial(lineSpeed, radiusMin, growDurationMin, growDurationMax));
        }

        UpdateScoreText(score + sessionScore);

        while (elapsedTime < minigameDuration)
        {
            yield return new WaitForSeconds(0.1f);

            activeHorizontalLine = Instantiate(horizontalLinePrefab, canvasTransform);
            RectTransform hLineRect = activeHorizontalLine.GetComponent<RectTransform>();
            hLineRect.anchoredPosition = new Vector2(0, canvasTransform.rect.height / 2);

            StartCoroutine(AnimateHorizontalLine(lineSpeed));

            float horizontalElapsed = 0f;
            isLineMoving = true;

            while (isLineMoving && (elapsedTime + horizontalElapsed) < minigameDuration)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    StopHorizontalLine();
                    yield return null;
                    break;
                }

                circleSpawnTimer += Time.deltaTime;
                if (circleSpawnTimer >= nextSpawnInterval)
                {
                    float thisGrowDuration = Random.Range(growDurationMin, growDurationMax);
                    float thisRadius = Random.Range(radiusMin, radiusMax);
                    SpawnCircle(thisGrowDuration, thisRadius);
                    circleSpawnTimer = 0f;
                    nextSpawnInterval = Random.Range(spawnIntervalMin, spawnIntervalMax);
                }

                horizontalElapsed += Time.deltaTime;
                UpdateTimerText(minigameDuration - (elapsedTime + horizontalElapsed));
                yield return null;
            }

            if (elapsedTime + horizontalElapsed >= minigameDuration)
            {
                elapsedTime += horizontalElapsed;
                if (activeHorizontalLine != null)
                    Destroy(activeHorizontalLine);
                break;
            }

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

                circleSpawnTimer += Time.deltaTime;
                if (circleSpawnTimer >= nextSpawnInterval)
                {
                    float thisGrowDuration = Random.Range(growDurationMin, growDurationMax);
                    float thisRadius = Random.Range(radiusMin, radiusMax);
                    SpawnCircle(thisGrowDuration, thisRadius);
                    circleSpawnTimer = 0f;
                    nextSpawnInterval = Random.Range(spawnIntervalMin, spawnIntervalMax);
                }

                verticalElapsed += Time.deltaTime;
                UpdateTimerText(minigameDuration - (elapsedTime + horizontalElapsed + verticalElapsed));
                yield return null;
            }

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
                if (activeHorizontalLine == null || activeVerticalLine == null)
                    yield break;
                RectTransform hLine = activeHorizontalLine.GetComponent<RectTransform>();
                RectTransform vLine = activeVerticalLine.GetComponent<RectTransform>();
                Vector2 intersection = new Vector2(
                    vLine.anchoredPosition.x,
                    hLine.anchoredPosition.y
                );

                List<GameObject> hitCircles = new List<GameObject>();
                foreach (var circle in new List<GameObject>(activeCircles))
                {
                    if (circle == null) continue;
                    RectTransform cRect = circle.GetComponent<RectTransform>();
                    float radius = cRect.sizeDelta.x * cRect.localScale.x / 2f;
                    bool isSpecial = false;
                    if (circle.TryGetComponent<CircleColliderData>(out var data))
                    {
                        radius = data.radius * cRect.localScale.x;
                        isSpecial = data.isSpecial;
                    }
                    Vector2 circleCenter = cRect.anchoredPosition;
                    if (Vector2.Distance(intersection, circleCenter) <= radius * 0.75f)
                    {
                        hitCircles.Add(circle);
                    }
                }
                foreach (var hitCircle in hitCircles)
                {
                    if (hitCircle == null) continue;
                    if (activeCircles.Contains(hitCircle))
                    {
                        int addScore = 100;
                        if (hitCircle.TryGetComponent<CircleColliderData>(out var data) && data.isSpecial)
                            addScore = 200;
                        activeCircles.Remove(hitCircle);
                        Destroy(hitCircle);
                        sessionScore += addScore;
                    }
                }
                if (hitCircles.Count > 0)
                {
                    UpdateScoreText(score + sessionScore);
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
            if (circle != null)
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

        bool pointsAdded = ScoreManager.TryAddMinigameScore(currentMinigameLevel, sessionScore);

        score = ScoreManager.GetCurrentScore();

        minigameEndImage?.gameObject.SetActive(true);
        scoreText?.gameObject.SetActive(true);

        UpdateScoreText(score);

        yield return new WaitForSeconds(1f);

        float waitTime = 0f;
        bool proceed = false;
        while (waitTime < 4f && !proceed)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                proceed = true;
            waitTime += Time.deltaTime;
            yield return null;
        }

        minigameEndImage?.gameObject.SetActive(false);
        scoreText?.gameObject.SetActive(false);
        timerText?.gameObject.SetActive(false);
        timerImage?.gameObject.SetActive(false);

        currentMinigameLevel = -1;
        sessionScore = 0;
        isReplayingMinigame = false;

        OnMinigameComplete?.Invoke(minigameScene.nextScene);
    }

    private IEnumerator PlayTutorial(float lineSpeed, float radiusMin, float growDurationMin, float growDurationMax)
    {
        Vector2 centerPos = Vector2.zero;
        float tutorialRadius = radiusMin;
        float tutorialGrowDuration = Random.Range(growDurationMin, growDurationMax);
        GameObject tutorialCircle = null;
        tutorialCircle = Instantiate(circlePrefab, canvasTransform);
        RectTransform tRect = tutorialCircle.GetComponent<RectTransform>();
        tRect.anchoredPosition = centerPos;
        tRect.sizeDelta = new Vector2(tutorialRadius * 2f, tutorialRadius * 2f);
        var tData = tutorialCircle.GetComponent<CircleColliderData>();
        if (tData == null) tData = tutorialCircle.AddComponent<CircleColliderData>();
        tData.radius = tutorialRadius;
        tData.isSpecial = false;
        activeCircles.Add(tutorialCircle);
        StartCoroutine(AnimateCircle(tutorialCircle, tutorialGrowDuration, true));

        activeHorizontalLine = Instantiate(horizontalLinePrefab, canvasTransform);
        RectTransform hLineRect = activeHorizontalLine.GetComponent<RectTransform>();
        hLineRect.anchoredPosition = new Vector2(0, canvasTransform.rect.height / 2);
        float hTargetY = 0f;
        isLineMoving = true;
        bool tutorialTap1 = false;
        bool tutorialTap2 = false;
        bool tutorialTap3 = false;
        bool tutorialTap4 = false;
        bool lineArrived = false;
        bool vLineArrived = false;

        while (!lineArrived)
        {
            float moveStep = lineSpeed * Time.deltaTime;
            hLineRect.anchoredPosition += new Vector2(0, -moveStep);
            if (hLineRect.anchoredPosition.y <= hTargetY)
            {
                hLineRect.anchoredPosition = new Vector2(0, hTargetY);
                isLineMoving = false;
                lineArrived = true;
            }
            yield return null;
        }

        bottomBar.barText.font = bottomBar.defaultFont;
        bottomBar.barText.text = "When your dimensional axis reaches the right position, give an impulse.";
        bottomBar.Show();
        nameBar.Show();
        nameBar.SetName("???");

        while (!tutorialTap1)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                tutorialTap1 = true;
            yield return null;
        }
        bottomBar.Hide();
        nameBar.Hide();
        yield return new WaitForSeconds(1f);

        activeVerticalLine = Instantiate(verticalLinePrefab, canvasTransform);
        RectTransform vLineRect = activeVerticalLine.GetComponent<RectTransform>();
        vLineRect.anchoredPosition = new Vector2(-canvasTransform.rect.width / 2, 0);
        float vTargetX = 0f;
        isVerticalLineMoving = true;
        while (!vLineArrived)
        {
            vLineRect.anchoredPosition += new Vector2(lineSpeed * Time.deltaTime, 0);
            if (vLineRect.anchoredPosition.x >= vTargetX)
            {
                vLineRect.anchoredPosition = new Vector2(vTargetX, 0);
                isVerticalLineMoving = false;
                vLineArrived = true;
            }
            yield return null;
        }

        bottomBar.barText.text = "Repeat this with the second axis and make them overlap in a specific spot. You can harness its power like this.";
        bottomBar.Show();
        nameBar.Show();
        nameBar.SetName("???");

        while (!tutorialTap2)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                tutorialTap2 = true;
            }
            yield return null;
        }
        bottomBar.Hide();
        nameBar.Hide();
        yield return new WaitForSeconds(1f);

        bottomBar.barText.text = "Aim for coloured circles, they possess triple the power of regular ones. There is not much time, I believe in you.";
        bottomBar.Show();
        nameBar.Show();
        nameBar.SetName("???");
        while (!tutorialTap3)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                tutorialTap3 = true;
            }
            yield return null;
        }
        bottomBar.Hide();
        nameBar.Hide();
        yield return new WaitForSeconds(1f);

        bottomBar.barText.text = "Ultimately, you must achieve a high enough level of total energy in order to restore the balance of the universe... They are depending on you!";
        bottomBar.Show();
        nameBar.Show();
        nameBar.SetName("???");
        while (!tutorialTap4)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                tutorialTap4 = true;
            }
            yield return null;
        }
        bottomBar.Hide();
        nameBar.Hide();

        if (activeHorizontalLine != null && activeVerticalLine != null)
        {
            if (activeHorizontalLine == null || activeVerticalLine == null)
                yield break;
            Vector2 intersection = new Vector2(
                vLineRect.anchoredPosition.x,
                hLineRect.anchoredPosition.y
            );
            List<GameObject> hitCircles = new List<GameObject>();
            foreach (var circle in new List<GameObject>(activeCircles))
            {
                if (circle == null) continue;
                RectTransform cRect = circle.GetComponent<RectTransform>();
                float radius = cRect.sizeDelta.x * cRect.localScale.x / 2f;
                if (circle.TryGetComponent<CircleColliderData>(out var data))
                    radius = data.radius * cRect.localScale.x;
                Vector2 circleCenter = cRect.anchoredPosition;
                if (Vector2.Distance(intersection, circleCenter) <= radius)
                    hitCircles.Add(circle);
            }
            foreach (var hitCircle in hitCircles)
            {
                if (hitCircle == null) continue;
                if (activeCircles.Contains(hitCircle))
                {
                    int addScore = 100;
                    if (hitCircle.TryGetComponent<CircleColliderData>(out var data) && data.isSpecial)
                        addScore = 300;
                    activeCircles.Remove(hitCircle);
                    Destroy(hitCircle);
                    sessionScore += addScore;
                }
            }
            if (hitCircles.Count > 0)
                UpdateScoreText(score + sessionScore);
        }
        if (activeHorizontalLine != null)
            Destroy(activeHorizontalLine);
        if (activeVerticalLine != null)
            Destroy(activeVerticalLine);
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

    private void SpawnCircle(float growDuration = 3f, float radius = 60f, bool noFade = false)
    {
        bool spawnSpecial = Random.value < 0.1f;
        GameObject prefab = circlePrefab;
        float actualRadius = radius;
        bool isSpecial = false;

        if (spawnSpecial)
        {
            prefab = Random.value < 0.5f ? specialCirclePrefab : specialCirclePrefab2;
            actualRadius = radius * 0.5f;
            isSpecial = true;
        }

        Vector2 randomPosition = new Vector2(
            Random.Range(0, canvasTransform.rect.width) - canvasTransform.rect.width / 2,
            Random.Range(0, canvasTransform.rect.height) - canvasTransform.rect.height / 2
        );

        GameObject circle = Instantiate(prefab, canvasTransform);
        circle.GetComponent<RectTransform>().anchoredPosition = randomPosition;
        var rect = circle.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(actualRadius * 2f, actualRadius * 2f);

        var data = circle.GetComponent<CircleColliderData>();
        if (data == null)
            data = circle.AddComponent<CircleColliderData>();
        data.radius = actualRadius;
        data.isSpecial = isSpecial;

        activeCircles.Add(circle);

        StartCoroutine(AnimateCircle(circle, growDuration, noFade));
    }

    private IEnumerator AnimateCircle(GameObject circle, float growDuration = 3f, bool noFade = false)
    {
        if (circle == null) yield break;

        RectTransform rectTransform = circle.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = circle.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = circle.AddComponent<CanvasGroup>();
        }

        float fadeDuration = growDuration * 0.5f;
        float elapsedTime = 0f;

        Vector2 initialScale = Vector2.zero;
        Vector2 targetScale = new Vector2(10, 10);

        float totalDuration = noFade ? growDuration : (growDuration + fadeDuration);
        while (elapsedTime < totalDuration)
        {
            if (circle == null) yield break;

            float growT = Mathf.Clamp01(elapsedTime / totalDuration);
            rectTransform.localScale = Vector2.Lerp(initialScale, targetScale, growT);

            if (noFade || elapsedTime < growDuration)
            {
                canvasGroup.alpha = 1f;
            }
            else
            {
                float fadeT = (elapsedTime - growDuration) / fadeDuration;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (!noFade && circle != null && activeCircles.Contains(circle))
        {
            Destroy(circle);
            activeCircles.Remove(circle);
        }
    }

    private void UpdateScoreText(int s)
    {
        if (scoreText != null)
        {
            scoreText.text = s.ToString();
        }
    }

    private void UpdateTimerText(float secondsLeft)
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(Mathf.Max(0, secondsLeft)).ToString();
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
