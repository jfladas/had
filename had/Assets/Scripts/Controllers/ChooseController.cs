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
            newLabel.Setup(scene.labels[index], this);
        }
    }

    public void PerformChoose(StoryScene scene)
    {
        gameController.PlayScene(scene);
        animator.SetTrigger("Hide");
    }

    private void DestroyLabels()
    {
        foreach (Transform childTransform in transform)
        {
            Destroy(childTransform.gameObject);
        }
    }
}
