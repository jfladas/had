using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChooseController : MonoBehaviour
{
    public ChooseLabelController label;
    public GameController gameController;
    private RectTransform rectTransform;
    private Animator animator;
    private float labelHeight = -1;

    void Start()
    {
        animator = GetComponent<Animator>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetupChoose(ChooseScene scene)
    {
        DestroyLabels();
        animator.SetTrigger("Show");
        for (int index = 0; index < scene.labels.Count; index++)
        {
            ChooseLabelController newLabel = Instantiate(label.gameObject, transform).GetComponent<ChooseLabelController>();

            if (labelHeight == -1)
            {
                labelHeight = newLabel.GetHeight();
            }

            newLabel.Setup(scene.labels[index], this, CalculateLabelPosition(index, scene.labels.Count));

            RectTransform labelRectTransform = newLabel.GetComponent<RectTransform>();
            labelRectTransform.anchorMin = new Vector2(0, 0.5f);
            labelRectTransform.anchorMax = new Vector2(1, 0.5f);
            labelRectTransform.pivot = new Vector2(0.5f, 0.5f);
            labelRectTransform.offsetMin = new Vector2(100, labelRectTransform.offsetMin.y);
            labelRectTransform.offsetMax = new Vector2(-100, labelRectTransform.offsetMax.y);

            TMP_Text labelText = newLabel.GetComponentInChildren<TMP_Text>();
            labelText.alignment = TextAlignmentOptions.Center;
        }

        Vector2 size = rectTransform.sizeDelta;
        size.y = (scene.labels.Count + 2) * labelHeight;
        rectTransform.sizeDelta = size;
    }

    public void PerformChoose(StoryScene scene)
    {
        gameController.PlayScene(scene);
        animator.SetTrigger("Hide");
    }

    private float CalculateLabelPosition(int labelIndex, int labelCount)
    {
        if (labelCount % 2 == 0)
        {
            if (labelIndex < labelCount / 2)
            {
                return labelHeight * (labelCount / 2 - labelIndex - 1) + labelHeight / 2;
            }
            else
            {
                return -1 * (labelHeight * (labelIndex - labelCount / 2) + labelHeight / 2);
            }
        }
        else
        {
            if (labelIndex < labelCount / 2)
            {
                return labelHeight * (labelCount / 2 - labelIndex);
            }
            else if (labelIndex > labelCount / 2)
            {
                return -1 * (labelHeight * (labelIndex - labelCount / 2));
            }
            else
            {
                return 0;
            }
        }
    }

    private void DestroyLabels()
    {
        foreach (Transform childTransform in transform)
        {
            Destroy(childTransform.gameObject);
        }
    }
}
