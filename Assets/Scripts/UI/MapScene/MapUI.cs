
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    [SerializeField] private TMP_Text actNameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text mapMessageText;

    [SerializeField] private ShopUI shopUI;
    [SerializeField] private RestUI restUI;

    [SerializeField] private ScrollRect mapScrollRect;
    [SerializeField] private RectTransform mapContent;
    [SerializeField] private Button mapPointPrefab;

    private readonly Dictionary<int, Button> pointButtons = new();

    private void Start()
    {
        ConfigureInputModule();


        if (RunSystem.Instance == null)
        {
            Debug.LogError("No RunSystem in MapScene");
            return;
        }

        shopUI.gameObject.SetActive(false);
        restUI.gameObject.SetActive(false);

        RunSystem.Instance.GenerateMap();

        ActData currentAct = RunSystem.Instance.CurrentAct;

        if (currentAct == null)
        {
            Debug.LogError("Current Act is missing.");
            return;
        }

        if (actNameText != null)
        {
            actNameText.text = currentAct.ActName;
        }

        CreateMapUI();
        UpdateMapUI();

        Canvas.ForceUpdateCanvases();
        mapScrollRect.verticalNormalizedPosition = 0f;
    }

    private void ConfigureInputModule()
    {
        UIInputModuleCompatibility.MonitorAndConfigure(EventSystem.current, this);
    }

    private void Update()
    {
        ConfigureInputModule();
    }


    private void CreateMapUI()
    {
        pointButtons.Clear();

        foreach (Transform child in mapContent)
        {
            Destroy(child.gameObject);
        }

        mapContent.sizeDelta = new Vector2(1200f, 1900f);

        foreach (MapPoint point in RunSystem.Instance.MapPoints)
        {
            foreach (int nextPointId in point.NextPoints)
            {
                MapPoint nextPoint = RunSystem.Instance.GetMapPoint(nextPointId);

                if (nextPoint == null) continue;

                CreateConnection(point.Position, nextPoint.Position);
            }
        }

        foreach (MapPoint point in RunSystem.Instance.MapPoints)
        {
            CreateMapPoint(point);
        }
    }

    private void CreateMapPoint(MapPoint point)
    {
        Button pointButton = Instantiate(mapPointPrefab, mapContent);

        pointButton.gameObject.SetActive(true);
        pointButton.gameObject.name = "MapPoint " + point.Id;

        RectTransform rect = pointButton.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = point.Position;

        TMP_Text pointText = pointButton.GetComponentInChildren<TMP_Text>(true);

        if (pointText != null)
        {
            pointText.text = GetPointText(point.Type);
        }

        pointButton.onClick.RemoveAllListeners();

        int pointId = point.Id;

        pointButton.onClick.AddListener(() => SelectMapPoint(pointId));

        pointButtons.Add(point.Id, pointButton);
    }

    private void CreateConnection(Vector2 start, Vector2 end)
    {
        GameObject lineObject = new(
            "MapConnection",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();

        lineRect.SetParent(mapContent, false);

        lineRect.anchorMin = Vector2.zero;
        lineRect.anchorMax = Vector2.zero;
        lineRect.pivot = new Vector2(0f, 0.5f);

        Vector2 direction = end - start;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRect.anchoredPosition = start;
        lineRect.sizeDelta = new Vector2(direction.magnitude, 4f);
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);

        Image lineImage = lineObject.GetComponent<Image>();

        lineImage.color = Color.white;
        lineImage.raycastTarget = false;
    }

    private string GetPointText(MapPointType type)
    {
        if (type == MapPointType.Normal) return "Normal";
        if (type == MapPointType.Elite) return "Elite";
        if (type == MapPointType.Shop) return "Shop";
        if (type == MapPointType.Rest) return "Rest";
        if (type == MapPointType.Boss) return "Boss";

        return "";
    }

    public void UpdateMapUI()
    {
        healthText.text = "HP: " + RunSystem.Instance.CurrentHealth;
        goldText.text = "Gold: " + RunSystem.Instance.Gold;

        foreach (KeyValuePair<int, Button> pair in pointButtons)
        {
            pair.Value.interactable = RunSystem.Instance.CanSelectMapPoint(pair.Key);
        }

        MapPoint completedPoint = RunSystem.Instance.GetMapPoint(
            RunSystem.Instance.CompletedMapPointId
        );

        if (completedPoint != null && completedPoint.Type == MapPointType.Boss)
        {
            mapMessageText.text = "Run Complete";
        }
        else
        {
            mapMessageText.text = "Choose your next point";
        }
    }

    public void SelectMapPoint(int pointId)
    {
        if (!RunSystem.Instance.CanSelectMapPoint(pointId)) return;

        MapPoint point = RunSystem.Instance.GetMapPoint(pointId);

        if (point == null) return;

        if (point.Type == MapPointType.Shop)
        {
            RunSystem.Instance.SelectMapPoint(pointId);

            shopUI.gameObject.SetActive(true);
            shopUI.Setup();

            return;
        }

        if (point.Type == MapPointType.Rest)
        {
            RunSystem.Instance.SelectMapPoint(pointId);

            restUI.gameObject.SetActive(true);
            restUI.Setup();

            return;
        }

        EncounterData encounter = GetEncounter(point.Type);

        if (encounter == null)
        {
            Debug.LogError("No encounter is assigned for this map point");
            return;
        }

        RunSystem.Instance.SelectMapPoint(pointId);
        RunSystem.Instance.SetEncounter(encounter);

        SceneManager.LoadScene("InGameScene");
    }

    private EncounterData GetEncounter(MapPointType type)
    {
        ActData currentAct = RunSystem.Instance.CurrentAct;

        if (currentAct == null)
        {
            Debug.LogError("Current Act is missing.");
            return null;
        }

        if (type == MapPointType.Boss)
        {
            return currentAct.BossEncounter;
        }

        List<EncounterData> encounterList = null;

        if (type == MapPointType.Normal)
        {
            encounterList = currentAct.NormalEncounters;
        }
        else if (type == MapPointType.Elite)
        {
            encounterList = currentAct.EliteEncounters;
        }

        if (encounterList == null || encounterList.Count == 0)
        {
            Debug.LogError(
                "No encounter is assigned for " +
                type +
                " in " +
                currentAct.ActName
            );

            return null;
        }

        int randomIndex = Random.Range(0, encounterList.Count);

        return encounterList[randomIndex];
    }
}