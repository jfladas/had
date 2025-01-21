using System.Collections;
using UnityEngine;

public class SpriteController : MonoBehaviour
{
    private SpriteSwitcher switcher;
    private Animator animator;
    private RectTransform rect;

    private void Awake()
    {
        switcher = GetComponent<SpriteSwitcher>();
        animator = GetComponent<Animator>();
        rect = GetComponent<RectTransform>();
    }

    public void Setup(Sprite sprite)
    {
        switcher.SetImage(sprite);
        switcher.SetOtherImage(sprite);
    }

    public void Show(Vector2 position, float targetScale)
    {
        rect.localPosition = position;
        if (targetScale != 0)
        {
            rect.localScale = new Vector3(targetScale, targetScale, targetScale);
        }
        animator.SetTrigger("Show");
    }

    public void Hide(Sprite sprite)
    {
        switcher.SetOtherImage(sprite);
        animator.SetTrigger("Hide");
    }

    public void Move(Vector2 position, float speed, float targetScale)
    {
        Vector3 targetScaleVector3 = new Vector3(targetScale, targetScale, targetScale);
        StartCoroutine(MoveCoroutine(position, speed, targetScaleVector3));
    }

    private IEnumerator MoveCoroutine(Vector2 position, float speed, Vector3 targetScale)
    {
        if (speed == -1)
        {
            rect.localPosition = position;
            rect.localScale = targetScale;
            yield break;
        }
        while (rect.localPosition.x != position.x || rect.localPosition.y != position.y || rect.localScale != targetScale)
        {
            rect.localPosition = Vector2.MoveTowards(rect.localPosition, position, speed * Time.deltaTime * 1000f);
            rect.localScale = Vector3.MoveTowards(rect.localScale, targetScale, speed * Time.deltaTime);
            yield return new WaitForSeconds(0.01f);
        }
    }

    public void SwitchSprite(Sprite sprite)
    {
        if (switcher.GetImage() != sprite)
        {
            switcher.SwitchImage(sprite);
        }
    }
}
