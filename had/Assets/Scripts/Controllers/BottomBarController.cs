using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BottomBarController : MonoBehaviour
{
    public TextMeshProUGUI barText;

    private int sentenceIndex = -1;
    private StoryScene currentScene;
    private State state = State.COMPLETED;
    private Animator animator;
    private bool isHidden = false;
    private Coroutine typingCoroutine;

    public Dictionary<Character, SpriteController> sprites;
    public GameObject spritesPrefab;
    public NameBarController nameBar;

    private enum State
    {
        PLAYING, COMPLETED
    }

    private void Start()
    {
        sprites = new Dictionary<Character, SpriteController>();
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
        animator.SetTrigger("Show");
        isHidden = false;
    }

    public void ClearText()
    {
        barText.text = "";
    }

    public void PlayScene(StoryScene scene)
    {
        currentScene = scene;
        sentenceIndex = -1;
        PlayNextSentence();
    }

    public void PlayNextSentence()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(currentScene.sentences[++sentenceIndex].text));
        nameBar.SetName(currentScene.sentences[sentenceIndex].character.characterName);
        ActCharacter();
    }

    public void SkipToFullSentence()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            barText.text = currentScene.sentences[sentenceIndex].text;
            state = State.COMPLETED;
        }
    }

    public bool IsCompleted()
    {
        return state == State.COMPLETED;
    }

    public bool IsLastSentence()
    {
        return sentenceIndex + 1 == currentScene.sentences.Count;
    }

    private IEnumerator TypeText(string text)
    {
        barText.text = "";
        state = State.PLAYING;
        int wordIndex = 0;

        while (state != State.COMPLETED)
        {
            barText.text += text[wordIndex];
            yield return new WaitForSeconds(0.03f);
            if (++wordIndex == text.Length)
            {
                state = State.COMPLETED;
                break;
            }
        }
    }

    private void ActCharacter()
    {
        List<StoryScene.Sentence.Action> actions = currentScene.sentences[sentenceIndex].actions;
        foreach (StoryScene.Sentence.Action action in actions)
        {
            SpriteController controller = null;
            switch (action.type)
            {
                case StoryScene.Sentence.Action.Type.SHOW:
                    if (!sprites.ContainsKey(action.character))
                    {
                        controller = Instantiate(action.character.prefab.gameObject, spritesPrefab.transform).GetComponent<SpriteController>();
                        sprites.Add(action.character, controller);
                    }
                    else
                    {
                        controller = sprites[action.character];
                    }
                    controller.Setup(action.character.sprites[action.spriteIndex]);
                    controller.Show(action.position);
                    break;
                case StoryScene.Sentence.Action.Type.MOVE:
                    if (sprites.ContainsKey(action.character))
                    {
                        controller = sprites[action.character];
                        controller.Move(action.position, action.speed);
                    }
                    break;
                case StoryScene.Sentence.Action.Type.HIDE:
                    if (sprites.ContainsKey(action.character))
                    {
                        controller = sprites[action.character];
                        controller.Hide();
                    }
                    break;
                case StoryScene.Sentence.Action.Type.NONE:
                    if (sprites.ContainsKey(action.character))
                    {
                        controller = sprites[action.character];
                    }
                    break;
            }
            if (controller != null)
            {
                controller.SwitchSprite(action.character.sprites[action.spriteIndex]);
            }
        }
    }
}
