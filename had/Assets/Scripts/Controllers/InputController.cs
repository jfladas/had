using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InputController : MonoBehaviour
{
    private TMP_InputField inputField;
    private TouchScreenKeyboard keyboard;

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();

        EventTrigger trigger = inputField.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Select;
        entry.callback.AddListener((eventData) => { OpenKeyboard(); });
        trigger.triggers.Add(entry);
    }

    void Update()
    {
        if (keyboard != null && keyboard.active)
        {
            inputField.text = keyboard.text;
        }
    }

    void OpenKeyboard()
    {
        if (TouchScreenKeyboard.isSupported)
        {
            keyboard = TouchScreenKeyboard.Open(inputField.text, TouchScreenKeyboardType.Default);
        }
    }
}
