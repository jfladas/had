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
}
