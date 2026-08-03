using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class TempleEnergyController : MonoBehaviour
{
    [Serializable]
    public class EnergyItem
    {
        [Header("Display")]
        public string name;

        [Header("API Field Name")]
        public string apiFieldName;

        [Header("Scene Model")]
        public GameObject sceneObject;

        [Header("Point Light")]
        public GameObject lightObject;

        [Header("LED Power")]
        public float voltage = 5f;
        public float currentAmp = 0.02f;

        [HideInInspector]
        public bool apiStatus;
    }

    [Header("UI Toolkit")]
    public UIDocument uiDocument;

    [Header("API")]
    public string apiKey;
    private string baseUrl = "https://api.prayalabs.com/api/generate.php?api_key=";

    [Header("API Refresh Time")]
    public float refreshTime = 2f;

    [Header("Button IDs")]
    public string templeButtonId = "temple";
    public string shivalingaButtonId = "shivalinga";
    public string mountainButtonId = "mountain";

    [Header("Label ID")]
    public string energyLabelId = "light_energy";

    [Header("Temple - 4 Gopurams")]
    public List<EnergyItem> templeItems = new List<EnergyItem>();

    [Header("Shivalinga")]
    public EnergyItem shivalingaItem;

    [Header("Mountain")]
    public EnergyItem mountainItem;

    private Label energyLabel;

    void OnEnable()
    {
        VisualElement root = uiDocument.rootVisualElement;

        Button templeBtn = root.Q<Button>(templeButtonId);
        Button shivalingaBtn = root.Q<Button>(shivalingaButtonId);
        Button mountainBtn = root.Q<Button>(mountainButtonId);

        energyLabel = root.Q<Label>(energyLabelId);

        if (templeBtn != null)
            templeBtn.clicked += ToggleTempleModelsOnly;

        if (shivalingaBtn != null)
            shivalingaBtn.clicked += () => ToggleModelOnly(shivalingaItem);

        if (mountainBtn != null)
            mountainBtn.clicked += () => ToggleModelOnly(mountainItem);

        StartCoroutine(APIStatusReader());
    }

    void ToggleTempleModelsOnly()
    {
        if (templeItems.Count == 0) return;

        bool newStatus = true;

        for (int i = 0; i < templeItems.Count; i++)
        {
            if (templeItems[i].sceneObject != null)
            {
                newStatus = !templeItems[i].sceneObject.activeSelf;
                break;
            }
        }

        foreach (EnergyItem item in templeItems)
        {
            if (item.sceneObject != null)
                item.sceneObject.SetActive(newStatus);
        }
    }

    void ToggleModelOnly(EnergyItem item)
    {
        if (item == null || item.sceneObject == null) return;

        item.sceneObject.SetActive(!item.sceneObject.activeSelf);
    }

    IEnumerator APIStatusReader()
    {
        while (true)
        {
            yield return StartCoroutine(ReadAPIStatus());
            UpdateEnergyLabel();

            yield return new WaitForSecondsRealtime(refreshTime);
        }
    }

    IEnumerator ReadAPIStatus()
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError("API Key is empty.");
            yield break;
        }

        string url = baseUrl + apiKey;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API Read Error: " + request.error);
                yield break;
            }

            string json = request.downloadHandler.text;

            foreach (EnergyItem item in templeItems)
            {
                UpdateLightStatusFromJson(json, item);
            }

            UpdateLightStatusFromJson(json, shivalingaItem);
            UpdateLightStatusFromJson(json, mountainItem);
        }
    }

    void UpdateLightStatusFromJson(string json, EnergyItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.apiFieldName))
            return;

        string truePattern1 = "\"" + item.apiFieldName + "\":true";
        string truePattern2 = "\"" + item.apiFieldName + "\":\"true\"";
        string truePattern3 = "\"" + item.apiFieldName + "\":1";
        string truePattern4 = "\"" + item.apiFieldName + "\":\"1\"";

        string falsePattern1 = "\"" + item.apiFieldName + "\":false";
        string falsePattern2 = "\"" + item.apiFieldName + "\":\"false\"";
        string falsePattern3 = "\"" + item.apiFieldName + "\":0";
        string falsePattern4 = "\"" + item.apiFieldName + "\":\"0\"";

        if (json.Contains(truePattern1) || json.Contains(truePattern2) ||
            json.Contains(truePattern3) || json.Contains(truePattern4))
        {
            item.apiStatus = true;

            if (item.lightObject != null)
                item.lightObject.SetActive(true);
        }
        else if (json.Contains(falsePattern1) || json.Contains(falsePattern2) ||
                 json.Contains(falsePattern3) || json.Contains(falsePattern4))
        {
            item.apiStatus = false;

            if (item.lightObject != null)
                item.lightObject.SetActive(false);
        }
    }

    void UpdateEnergyLabel()
    {
        if (energyLabel == null) return;

        float totalPower = 0f;
        int activeLedCount = 0;

        string text = "Energy Consumption\n\n";

        foreach (EnergyItem item in templeItems)
        {
            text += GetEnergyLine(item);

            if (item.apiStatus)
            {
                totalPower += GetPowerWatt(item);
                activeLedCount++;
            }
        }

        text += GetEnergyLine(shivalingaItem);

        if (shivalingaItem != null && shivalingaItem.apiStatus)
        {
            totalPower += GetPowerWatt(shivalingaItem);
            activeLedCount++;
        }

        text += GetEnergyLine(mountainItem);

        if (mountainItem != null && mountainItem.apiStatus)
        {
            totalPower += GetPowerWatt(mountainItem);
            activeLedCount++;
        }

        text += "\nActive LEDs: " + activeLedCount + " / 6";
        text += "\nTotal Power: " + totalPower.ToString("0.00") + " W";

        energyLabel.text = text;
    }

    string GetEnergyLine(EnergyItem item)
    {
        if (item == null) return "";

        float watt = item.apiStatus ? GetPowerWatt(item) : 0f;

        return item.name + " : " +
               (item.apiStatus ? "ON" : "OFF") +
               " | " + watt.ToString("0.00") + " W\n";
    }

    float GetPowerWatt(EnergyItem item)
    {
        return item.voltage * item.currentAmp;
    }
}