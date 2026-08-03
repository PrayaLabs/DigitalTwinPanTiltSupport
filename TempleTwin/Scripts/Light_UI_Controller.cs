using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class Light_UI_Controller : MonoBehaviour
{
    [Serializable]
    public class ButtonLightMapping
    {
        public string buttonId;       // Example: e_light
        public string apiFieldName;   // Same field name from API_Reader.cs
        public GameObject lightObject; // Point Light object
    }

    [Header("UI Toolkit")]
    public UIDocument uiDocument;

    [Header("API")]
    public string apiKey;
    private string baseUrl = "https://api.prayalabs.com/api/generate.php?api_key=";

    [Header("Button - Light Mapping")]
    public List<ButtonLightMapping> mappings = new List<ButtonLightMapping>();

    void OnEnable()
    {
        VisualElement root = uiDocument.rootVisualElement;

        foreach (ButtonLightMapping item in mappings)
        {
            Button btn = root.Q<Button>(item.buttonId);

            if (btn == null)
            {
                Debug.LogError("Button not found: " + item.buttonId);
                continue;
            }

            btn.clicked += () =>
            {
                ToggleLight(item);
            };
        }
    }

    void ToggleLight(ButtonLightMapping item)
    {
        if (item.lightObject == null)
        {
            Debug.LogError("Light object missing for: " + item.buttonId);
            return;
        }

        bool currentStatus = item.lightObject.activeSelf;
        bool newStatus = !currentStatus;

        item.lightObject.SetActive(newStatus);

        StartCoroutine(PostLightStatus(item.apiFieldName, newStatus));
    }

    IEnumerator PostLightStatus(string fieldName, bool status)
    {
        string url = baseUrl + apiKey;

        WWWForm form = new WWWForm();
        form.AddField(fieldName, status ? "true" : "false");

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("POST API Error: " + request.error);
        }
        else
        {
            Debug.Log("POST Success: " + fieldName + " = " + status);
            Debug.Log(request.downloadHandler.text);
        }
    }
}