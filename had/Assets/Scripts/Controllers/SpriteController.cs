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
    }

    public void Show(Vector2 position)
    {
        animator.SetTrigger("Show");
        rect.localPosition = position;
    }

    public void Hide()
    {
        animator.SetTrigger("Hide");
    }

    public void Move(Vector2 position, float speed)
    {
        StartCoroutine(MoveCoroutine(position, speed));
    }

    private IEnumerator MoveCoroutine(Vector2 position, float speed)
    {
        while (rect.localPosition.x != position.x || rect.localPosition.y != position.y)
        {
            rect.localPosition = Vector2.MoveTowards(rect.localPosition, position, speed * Time.deltaTime * 1000f);
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
