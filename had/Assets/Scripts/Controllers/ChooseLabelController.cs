using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ChooseLabelController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Color defaultColor;
    public Color hoverColor;
    private StoryScene scene;
    private TextMeshProUGUI textMesh;
    private ChooseController controller;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        textMesh.color = defaultColor;
    }

    public void Setup(ChooseScene.ChooseLabel label, ChooseController controller)
    {
        scene = label.nextScene;
        textMesh.text = label.text;
        this.controller = controller;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller.gameController.currentScene is ChooseScene)
        {
            controller.PerformChoose(scene);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        textMesh.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        textMesh.color = defaultColor;
    }
}
