using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BottomBarController : MonoBehaviour
{
    public TextMeshProUGUI barText;
    public Sprite defaultSprite;
    public Sprite specialSprite;
    private Image bottomBarImage;

    private int sentenceIndex = -1;
    private StoryScene currentScene;
    private State state = State.COMPLETED;
    private Animator animator;
    private bool isHidden = true;
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
        bottomBarImage = GetComponent<Image>();
    }

    public int GetSentenceIndex()
    {
        return sentenceIndex;
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
        if (isHidden)
        {
            animator.SetTrigger("Show");
            isHidden = false;
        }
    }

    public void ClearText()
    {
        barText.text = "";
    }

    public void PlayScene(StoryScene scene, string playerName)
    {
        currentScene = scene;
        sentenceIndex = -1;
        PlayNextSentence(playerName);
    }

    public void PlayNextSentence(string playerName)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        string sentenceText = currentScene.sentences[++sentenceIndex].text;
        if (playerName == "Me" && !string.IsNullOrEmpty(currentScene.sentences[sentenceIndex].alternativeText))
        {
            sentenceText = currentScene.sentences[sentenceIndex].alternativeText;
        }
        sentenceText = sentenceText.Replace("{playerName}", playerName);

        typingCoroutine = StartCoroutine(TypeText(sentenceText));

        var characterName = currentScene.sentences[sentenceIndex].character.characterName;
        if (characterName == "" || characterName == "...")
        {
            nameBar.Hide();
        }
        else
        {
            nameBar.Show();
        }
        nameBar.SetName(characterName);
        ActCharacter();
        if (characterName == "...")
        {
            bottomBarImage.sprite = specialSprite;
        }
        else
        {
            bottomBarImage.sprite = defaultSprite;
        }
    }

    public void SkipToFullSentence(string playerName)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        string sentenceText = currentScene.sentences[sentenceIndex].text;
        if (playerName == "Me" && !string.IsNullOrEmpty(currentScene.sentences[sentenceIndex].alternativeText))
        {
            sentenceText = currentScene.sentences[sentenceIndex].alternativeText;
        }
        sentenceText = sentenceText.Replace("{playerName}", playerName);

        barText.text = sentenceText;
        state = State.COMPLETED;
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
                    controller.Show(action.position, action.targetScale);
                    break;
                case StoryScene.Sentence.Action.Type.MOVE:
                    if (sprites.ContainsKey(action.character))
                    {
                        controller = sprites[action.character];
                        controller.Move(action.position, action.speed, action.targetScale);
                    }
                    break;
                case StoryScene.Sentence.Action.Type.HIDE:
                    if (sprites.ContainsKey(action.character))
                    {
                        controller = sprites[action.character];
                        controller.Hide(action.character.sprites[action.spriteIndex]);
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
