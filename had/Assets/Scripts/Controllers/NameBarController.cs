using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NameBarController : MonoBehaviour
{
    public TextMeshProUGUI personNameText;

    private Animator animator;
    private bool isHidden = false;

    public void SetName(string name)
    {
        personNameText.text = name;
    }

    private void ClearName()
    {
        personNameText.text = "";
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Hide()
    {
        if (!isHidden)
        {
            animator.SetTrigger("Hide");
            isHidden = true;
        }
    }

    public void Show()
    {
        ClearName();
        animator.SetTrigger("Show");
        isHidden = false;
    }
}