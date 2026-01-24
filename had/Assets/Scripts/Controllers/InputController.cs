using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InputController : MonoBehaviour
{
    private TMP_InputField inputField;

#if !UNITY_WEBGL || UNITY_EDITOR
    private TouchScreenKeyboard keyboard;
#endif

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();

#if UNITY_WEBGL && !UNITY_EDITOR
        // On WebGL, the TMP_InputField handles input through the browser's native input
        // No need to add event trigger for keyboard - browser handles it automatically
        inputField.richText = false;
        inputField.contentType = TMP_InputField.ContentType.Standard;
#else
        // On mobile platforms, use TouchScreenKeyboard
        EventTrigger trigger = inputField.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Select;
        entry.callback.AddListener((eventData) => { OpenKeyboard(); });
        trigger.triggers.Add(entry);
#endif
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (keyboard != null && keyboard.active)
        {
            inputField.text = keyboard.text;
        }
#endif
    }

    void OpenKeyboard()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (TouchScreenKeyboard.isSupported)
        {
            keyboard = TouchScreenKeyboard.Open(inputField.text, TouchScreenKeyboardType.Default);
        }
#endif
    }
}
