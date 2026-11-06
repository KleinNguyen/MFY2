
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    private const float MapLayoutWidth = 1200f;
    private const float MapEdgePadding = 90f;
    private const float MapHorizontalScale = 0.82f;
    private const float ConnectionIconClearance = 48f;
    private const float ConnectionDashLength = 14f;
    private const float ConnectionDashGap = 9f;
    private const float ConnectionThickness = 4f;

    [SerializeField] private TMP_Text actNameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text mapMessageText;
    [SerializeField] private RectTransform mapLegend;
    [SerializeField] private RectTransform runTimerDisplay;

    [SerializeField] private ShopUI shopUI;
    [SerializeField] private RestUI restUI;

    [SerializeField] private ScrollRect mapScrollRect;
    [SerializeField] private RectTransform mapContent;
    [SerializeField] private Button mapPointPrefab;

    [Header("Map Point Icons")]
    [SerializeField] private Sprite normalIcon;
    [SerializeField] private Sprite eliteIcon;
    [SerializeField] private Sprite shopIcon;
    [SerializeField] private Sprite restIcon;
    [SerializeField] private Sprite bossIcon;

    private readonly Dictionary<int, Button> pointButtons = new();
    private float mapMinimumY;
    private float mapVerticalScale;
    private GameObject currentPointGlow;
    private RectTransform currentPointGlowRect;
    private Image currentPointGlowImage;
    private Outline currentPointGlowOutline;
    private int glowingMapPointId = -1;
    private TMP_Text runTimerText;

    private void Start()
    {
        ConfigureInputModule();


        if (RunSystem.Instance == null)
        {
            Debug.LogError("No RunSystem in MapScene");
            return;
        }

        mapScrollRect.horizontal = true;
        mapScrollRect.vertical = true;
        HideScrollbar(mapScrollRect.horizontalScrollbar);
        HideScrollbar(mapScrollRect.verticalScrollbar);

        shopUI.gameObject.SetActive(false);
        restUI.gameObject.SetActive(false);

        RunSystem.Instance.GenerateMap();

        ActData currentAct = RunSystem.Instance.CurrentAct;

        if (currentAct == null)
        {
            Debug.LogError("Current Act is missing.");
            return;
        }

        //if (actNameText != null)
        //{
        //    actNameText.text = currentAct.ActName;
        //}

        Canvas.ForceUpdateCanvases();
        StyleMapLegend();
        SetupRunTimer();
        CreateMapUI();
        UpdateMapUI();

        Canvas.ForceUpdateCanvases();
        mapScrollRect.verticalNormalizedPosition = 0f;
    }

    private static void HideScrollbar(Scrollbar scrollbar)
    {
        if (scrollbar == null) return;

        CanvasGroup canvasGroup = scrollbar.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = scrollbar.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ConfigureInputModule()
    {
        UIInputModuleCompatibility.MonitorAndConfigure(EventSystem.current, this);
    }

    private void StyleMapLegend()
    {
        if (mapLegend == null)
        {
            Debug.LogError(
                "Map Legend is not assigned in MapUI."
            );
            return;
        }

        foreach (Button entry in mapLegend.GetComponentsInChildren<Button>(true))
        {
            entry.enabled = false;

            Image icon = entry.GetComponent<Image>();
            if (icon != null)
            {
                icon.raycastTarget = false;
                icon.preserveAspect = true;

                if (entry.gameObject.name == "MapLegend - Title")
                {
                    icon.color = new Color(1f, 1f, 1f, 0f);
                }
                else
                {
                    icon.color = Color.white;

                    Outline iconOutline = icon.GetComponent<Outline>();
                    if (iconOutline == null)
                    {
                        iconOutline = icon.gameObject.AddComponent<Outline>();
                    }

                    iconOutline.effectColor = new Color(0.96f, 0.72f, 0.3f, 0.8f);
                    iconOutline.effectDistance = new Vector2(1.5f, 1.5f);
                }
            }

            TMP_Text label = entry.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                Debug.LogError("Map legend entry is missing its label: " + entry.name);
                continue;
            }

            label.font = mapMessageText.font;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = entry.gameObject.name == "MapLegend - Title" ? 30f : 24f;
            label.color = new Color(0.96f, 0.93f, 0.85f);
            label.alignment = entry.gameObject.name == "MapLegend - Title"
                ? TextAlignmentOptions.Center
                : TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;

            Shadow labelShadow = label.GetComponent<Shadow>();
            if (labelShadow == null)
            {
                labelShadow = label.gameObject.AddComponent<Shadow>();
            }

            labelShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            labelShadow.effectDistance = new Vector2(1f, -1f);
        }
    }

    private void Update()
    {
        ConfigureInputModule();
        UpdateRunTimer();

        if (currentPointGlowImage == null || currentPointGlowRect == null ||
            currentPointGlowOutline == null)
        {
            return;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
        currentPointGlowRect.localScale = Vector3.one * Mathf.Lerp(1.08f, 1.2f, pulse);
        currentPointGlowImage.color = new Color(1f, 0.78f, 0.12f, Mathf.Lerp(0.16f, 0.34f, pulse));
        currentPointGlowOutline.effectColor = new Color(
            1f,
            0.72f,
            0.12f,
            Mathf.Lerp(0.3f, 0.75f, pulse)
        );
    }

    private void SetupRunTimer()
    {
        if (runTimerDisplay == null)
        {
            Debug.LogError("Run Timer Display is not assigned in MapUI.");
            return;
        }

        Transform textTransform = runTimerDisplay.Find("RunTimerText");
        if (textTransform == null)
        {
            Debug.LogError("Run Timer Display is missing its RunTimerText object.");
            return;
        }

        runTimerText = textTransform.GetComponentInChildren<TMP_Text>(true);
        if (runTimerText == null)
        {
            Debug.LogError("RunTimerText must contain a TextMeshPro text object.");
            return;
        }

        runTimerText.font = mapMessageText.font;
        runTimerText.fontSize = 34f;
        runTimerText.fontStyle = FontStyles.Bold;
        runTimerText.color = Color.white;
        runTimerText.alignment = TextAlignmentOptions.Center;
        runTimerText.raycastTarget = false;

        Shadow textShadow = runTimerText.GetComponent<Shadow>();
        if (textShadow == null)
        {
            textShadow = runTimerText.gameObject.AddComponent<Shadow>();
        }

        textShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        textShadow.effectDistance = new Vector2(1f, -1f);

        Transform iconTransform = runTimerDisplay.Find("RunTimerIcon");
        if (iconTransform == null)
        {
            Debug.LogError("Run Timer Display is missing its RunTimerIcon object.");
            return;
        }

        Image icon = iconTransform.GetComponent<Image>();
        if (icon == null)
        {
            Debug.LogError("RunTimerIcon must have an Image component.");
            return;
        }

        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Outline iconOutline = icon.GetComponent<Outline>();
        if (iconOutline == null)
        {
            iconOutline = icon.gameObject.AddComponent<Outline>();
        }

        iconOutline.effectColor = new Color(0.96f, 0.72f, 0.3f, 0.85f);
        iconOutline.effectDistance = new Vector2(1.5f, 1.5f);
    }

    private void UpdateRunTimer()
    {
        if (runTimerText == null || RunSystem.Instance == null)
        {
            return;
        }

        int elapsedSeconds = Mathf.FloorToInt(RunSystem.Instance.ElapsedRunTime);
        int hours = elapsedSeconds / 3600;
        int minutes = (elapsedSeconds / 60) % 60;
        int seconds = elapsedSeconds % 60;

        runTimerText.text = hours > 0
            ? $"{hours:00}:{minutes:00}:{seconds:00}"
            : $"{elapsedSeconds / 60:00}:{seconds:00}";
    }


    private void CreateMapUI()
    {
        pointButtons.Clear();

        foreach (Transform child in mapContent)
        {
            Destroy(child.gameObject);
        }

        float mapScale = mapScrollRect.viewport.rect.width / MapLayoutWidth;
        mapMinimumY = float.MaxValue;
        float mapMaximumY = float.MinValue;
        foreach (MapPoint point in RunSystem.Instance.MapPoints)
        {
            mapMinimumY = Mathf.Min(mapMinimumY, point.Position.y);
            mapMaximumY = Mathf.Max(mapMaximumY, point.Position.y);
        }

        float mapHeight = mapMaximumY - mapMinimumY + MapEdgePadding * 2f;
        float contentHeight = Mathf.Max(
            mapScrollRect.viewport.rect.height / mapScale,
            mapHeight
        );
        mapContent.sizeDelta = new Vector2(MapLayoutWidth, contentHeight);
        mapContent.localScale = Vector3.one * mapScale;
        mapVerticalScale = 1f;

        foreach (MapPoint point in RunSystem.Instance.MapPoints)
        {
            foreach (int nextPointId in point.NextPoints)
            {
                MapPoint nextPoint = RunSystem.Instance.GetMapPoint(nextPointId);

                if (nextPoint == null) continue;

                CreateConnection(GetMapPosition(point), GetMapPosition(nextPoint));
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
        rect.anchoredPosition = GetMapPosition(point);

        Image pointImage = pointButton.GetComponent<Image>();
        if (pointImage == null)
        {
            Debug.LogError("MapPointButton prefab must have an Image component.");
            Destroy(pointButton.gameObject);
            return;
        }

        Sprite pointIcon = GetPointIcon(point.Type);
        pointImage.sprite = pointIcon;
        pointImage.color = pointIcon == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        pointImage.type = Image.Type.Simple;
        pointImage.preserveAspect = true;
        pointImage.raycastTarget = true;
        pointButton.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = pointButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.4f);
        colors.colorMultiplier = 1f;
        pointButton.colors = colors;

        TMP_Text pointText = pointButton.GetComponentInChildren<TMP_Text>(true);
        if (pointText != null)
        {
            pointText.gameObject.SetActive(false);
        }

        pointButton.onClick.RemoveAllListeners();

        int pointId = point.Id;

        pointButton.onClick.AddListener(() => SelectMapPoint(pointId));

        pointButtons.Add(point.Id, pointButton);
    }

    private void CreateConnection(Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        float drawableLength = distance - ConnectionIconClearance * 2f;
        if (drawableLength <= 0f)
        {
            return;
        }

        Vector2 directionNormalized = direction / distance;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float dashStep = ConnectionDashLength + ConnectionDashGap;

        for (float offset = 0f; offset < drawableLength; offset += dashStep)
        {
            float dashLength = Mathf.Min(ConnectionDashLength, drawableLength - offset);
            Vector2 dashStart = start
                + directionNormalized * (ConnectionIconClearance + offset);

            GameObject dashObject = new(
                "MapConnectionDash",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            RectTransform dashRect = dashObject.GetComponent<RectTransform>();
            dashRect.SetParent(mapContent, false);
            dashRect.anchorMin = Vector2.zero;
            dashRect.anchorMax = Vector2.zero;
            dashRect.pivot = new Vector2(0f, 0.5f);
            dashRect.anchoredPosition = dashStart;
            dashRect.sizeDelta = new Vector2(dashLength, ConnectionThickness);
            dashRect.localRotation = Quaternion.Euler(0, 0, angle);

            Image dashImage = dashObject.GetComponent<Image>();
            dashImage.color = new Color(0.08f, 0.16f, 0.25f, 0.9f);
            dashImage.raycastTarget = false;
        }
    }

    private Vector2 GetMapPosition(MapPoint point)
    {
        float centeredX = (point.Position.x - MapLayoutWidth * 0.5f) * MapHorizontalScale;
        float mappedY = MapEdgePadding + (point.Position.y - mapMinimumY) * mapVerticalScale;
        return new Vector2(MapLayoutWidth * 0.5f + centeredX, mappedY);
    }

    private Sprite GetPointIcon(MapPointType type)
    {
        switch (type)
        {
            case MapPointType.Normal:
                return normalIcon;
            case MapPointType.Elite:
                return eliteIcon;
            case MapPointType.Shop:
                return shopIcon;
            case MapPointType.Rest:
                return restIcon;
            case MapPointType.Boss:
                return bossIcon;
            default:
                return null;
        }
    }

    public void UpdateMapUI()
    {
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

    private void UpdateCurrentPointGlow()
    {
        int currentPointId = RunSystem.Instance.CompletedMapPointId;
        if (currentPointId == glowingMapPointId && currentPointGlow != null)
        {
            return;
        }

        if (currentPointGlow != null)
        {
            Destroy(currentPointGlow);
        }

        currentPointGlow = null;
        currentPointGlowRect = null;
        currentPointGlowImage = null;
        currentPointGlowOutline = null;
        glowingMapPointId = currentPointId;

        if (currentPointId < 0 ||
            !pointButtons.TryGetValue(currentPointId, out Button pointButton))
        {
            return;
        }

        Image pointImage = pointButton.GetComponent<Image>();
        if (pointImage == null || pointImage.sprite == null)
        {
            return;
        }

        GameObject glowObject = new(
            "CurrentMapPointGlow",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        currentPointGlow = glowObject;
        currentPointGlowRect = glowObject.GetComponent<RectTransform>();
        currentPointGlowRect.SetParent(mapContent, false);
        currentPointGlowRect.anchorMin = Vector2.zero;
        currentPointGlowRect.anchorMax = Vector2.zero;
        RectTransform pointRect = pointButton.GetComponent<RectTransform>();
        currentPointGlowRect.pivot = pointRect.pivot;
        currentPointGlowRect.anchoredPosition = pointRect.anchoredPosition;
        currentPointGlowRect.sizeDelta = pointRect.sizeDelta;
        currentPointGlowRect.localScale = Vector3.one * 1.08f;
        currentPointGlowRect.SetSiblingIndex(pointButton.transform.GetSiblingIndex());

        currentPointGlowImage = glowObject.GetComponent<Image>();
        currentPointGlowImage.sprite = pointImage.sprite;
        currentPointGlowImage.type = Image.Type.Simple;
        currentPointGlowImage.preserveAspect = pointImage.preserveAspect;
        currentPointGlowImage.raycastTarget = false;

        currentPointGlowOutline = glowObject.AddComponent<Outline>();
        currentPointGlowOutline.effectColor = new Color(1f, 0.72f, 0.12f, 0.6f);
        currentPointGlowOutline.effectDistance = new Vector2(2f, 2f);
        currentPointGlowOutline.useGraphicAlpha = true;
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