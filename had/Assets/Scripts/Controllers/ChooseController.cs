using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChooseController : MonoBehaviour
{
    public ChooseLabelController label;
    public GameController gameController;
    public TextMeshProUGUI logTextDisplay;
    private RectTransform rectTransform;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetupChoose(ChooseScene scene)
    {
        if (scene.name == "Log")
        {
            SetupLogScene(scene);
        }
        else
        {
            SetupNormalChooseScene(scene);
            if (logTextDisplay != null)
            {
                logTextDisplay.gameObject.SetActive(false);
            }
        }
        animator.SetTrigger("Show");
    }

    private void SetupLogScene(ChooseScene scene)
    {
        for (int index = 0; index < scene.labels.Count; index++)
        {
            ChooseLabelController newLabel = Instantiate(label.gameObject, transform).GetComponent<ChooseLabelController>();
            newLabel.Setup(scene.labels[index], this);
        }

        if (logTextDisplay != null)
        {
            logTextDisplay.gameObject.SetActive(true);
            string logText = LogManager.GetFormattedLogText();
            if (string.IsNullOrEmpty(logText))
            {
                logText = "No dialogue recorded yet.";
            }
            logTextDisplay.text = logText;
        }
    }

    private void SetupNormalChooseScene(ChooseScene scene)
    {
        for (int index = 0; index < scene.labels.Count; index++)
        {
            if (IsChapterSelectionScene(scene.name) && !ShouldShowChapterLabel(scene.labels[index]))
            {
                continue;
            }

            ChooseLabelController newLabel = Instantiate(label.gameObject, transform).GetComponent<ChooseLabelController>();
            newLabel.Setup(scene.labels[index], this);
        }
    }

    public void PerformChoose(GameScene scene)
    {
        gameController.PlayScene(scene);
        HideChoose();
    }

    public void HideChoose()
    {
        if (logTextDisplay != null)
        {
            logTextDisplay.gameObject.SetActive(false);
        }

        animator.SetTrigger("Hide");
        StartCoroutine(DestroyLabelsAfterTimeout());
    }

    private IEnumerator DestroyLabelsAfterTimeout()
    {
        yield return new WaitForSeconds(0.75f);
        DestroyLabels();
    }

    private void DestroyLabels()
    {
        foreach (Transform childTransform in transform)
        {
            Destroy(childTransform.gameObject);
        }
    }

    private bool IsChapterSelectionScene(string sceneName)
    {
        return sceneName == "ChapterSelect1" || sceneName == "ChapterSelect2" || sceneName == "ChapterSelect3";
    }

    private bool ShouldShowChapterLabel(ChooseScene.ChooseLabel label)
    {
        if (label.text == "Back")
        {
            return true;
        }

        string chapterKey = GetChapterKeyFromText(label.text);

        if (string.IsNullOrEmpty(chapterKey))
        {
            return true;
        }

        return ChapterScene.IsChapterDone(chapterKey);
    }

    private string GetChapterKeyFromText(string chapterText)
    {
        switch (chapterText)
        {
            case "Prologue":
                return "Chapter0";
            case "Chapter 1":
                return "Chapter1";
            case "Chapter 2":
                return "Chapter2";
            case "Chapter 3":
                return "Chapter3";
            case "Chapter 4":
                return "Chapter4";
            case "Chapter 5":
                return "Chapter5";
            case "Chapter 6":
                return "Chapter6";
            case "Chapter 7":
                return "AChapter7";
            case "Chapter 8":
                return "AChapter8";
            case "Chapter 9":
                return "AChapter9";
            case "Chapter 10":
                return "AChapter10";
            case "Chapter 11":
                return "AChapter11";
            case "Chapter 12":
                return "AChapter12";
            case "Chapter 13":
                return "AChapter13";
            case "Chapter 14":
                return "AChapter14";
            case "Chapter 15":
                return "AChapter15";
            case "Epilogue (Ending 1)":
                return "AEpilogue1";
            case "Epilogue (Ending 2)":
                return "AEpilogue2";
            case "Epilogue (Ending 3)":
                return "AEpilogue3";
            default:
                return null;
        }
    }
}
