using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    private int lastFocusedPointId = int.MinValue;
    private const float MapLayoutWidth = 1200f;
    private const float MapEdgePadding = 90f;
    private const float MapHorizontalScale = 0.82f;
    private const float ConnectionIconClearance = 48f;
    private const float ConnectionDashLength = 14f;
    private const float ConnectionDashGap = 9f;
    private const float ConnectionThickness = 4f;

    [Header("Run interface")]
    [SerializeField] private TMP_Text actNameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text mapMessageText;
    [SerializeField] private RectTransform mapLegend;
    [SerializeField] private RectTransform runTimerDisplay;

    [Header("Panels")]
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private RestUI restUI;

    [Header("Map layout")]
    [SerializeField] private ScrollRect mapScrollRect;
    [SerializeField] private RectTransform mapContent;
    [SerializeField] private Button mapPointPrefab;

    [Header("Map point icons")]
    [SerializeField] private Sprite normalIcon;
    [SerializeField] private Sprite eliteIcon;
    [SerializeField] private Sprite shopIcon;
    [SerializeField] private Sprite restIcon;
    [SerializeField] private Sprite bossIcon;
    [SerializeField] private Sprite mysteryIcon;

    private readonly Dictionary<int, Button> pointButtons = new();
    private float mapMinimumY;
    private TMP_Text runTimerText;
    private int glowingMapPointId = -1;
    private GameObject currentPointGlow;
    private RectTransform currentPointGlowRect;
    private Image currentPointGlowImage;
    private Outline currentPointGlowOutline;
    private TMP_Text currentPointGlowText;

    // Unity
    private void Start()
    {
        ConfigureInputModule();

        RunSystem run = RunSystem.Instance;
        if (!ValidateSetup(run)) return;

        shopUI.gameObject.SetActive(false);
        restUI.gameObject.SetActive(false);

        mapScrollRect.horizontal = false;
        mapScrollRect.vertical = true;
        HideScrollbar(mapScrollRect.horizontalScrollbar);
        HideScrollbar(mapScrollRect.verticalScrollbar);

        run.GenerateMap();

        if (run.MapPoints == null || run.MapPoints.Count == 0)
        {
            Debug.LogError("The map is empty. Check the current Act settings.", this);
            return;
        }

        run.StartRunTimer();
        run.SaveCheckpoint();

        Canvas.ForceUpdateCanvases();
        StyleMapLegend();
        SetupRunTimer();

        if (!CreateMapUI(run)) return;

        // UpdateMapUI also scrolls to the current map point.
        UpdateMapUI();
    }

    private void Update()
    {
        ConfigureInputModule();
        UpdateRunTimer();
        AnimateCurrentPointGlow();
    }

    private void ConfigureInputModule() => UIInputModuleCompatibility.MonitorAndConfigure(EventSystem.current, this);

    public bool OpenShopForDemo()
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || run.IsRunFinished || shopUI == null || shopUI.gameObject.activeSelf ||
            (restUI != null && restUI.gameObject.activeSelf))
            return false;

        shopUI.gameObject.SetActive(true);
        shopUI.Setup();
        return true;
    }

    public bool OpenRestForDemo()
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || run.IsRunFinished || restUI == null || restUI.gameObject.activeSelf ||
            (shopUI != null && shopUI.gameObject.activeSelf))
            return false;

        restUI.gameObject.SetActive(true);
        restUI.Setup();
        return true;
    }

    private bool ValidateSetup(RunSystem run)
    {
        if (run == null || run.HeroData == null || run.CurrentAct == null)
        {
            Debug.LogError("An active run, hero and Act are required for MapUI.", this);
            return false;
        }

        if (mapScrollRect == null || mapScrollRect.viewport == null || mapContent == null || mapPointPrefab == null || mapMessageText == null || shopUI == null || restUI == null)
        {
            Debug.LogError("MapUI is missing references. Check the Inspector.", this);
            return false;
        }

        if (mapPointPrefab.GetComponent<Image>() == null)
        {
            Debug.LogError("MapPointButton prefab must have an Image component.", this);
            return false;
        }

        return true;
    }

    // Map creation
    private bool CreateMapUI(RunSystem run)
    {
        float viewportWidth = mapScrollRect.viewport.rect.width;
        float viewportHeight = mapScrollRect.viewport.rect.height;
        if (viewportWidth <= 0f || viewportHeight <= 0f)
        {
            Debug.LogError("Map viewport must have a positive width and height.", this);
            return false;
        }

        ClearCurrentPointGlow();
        pointButtons.Clear();
        foreach (Transform child in mapContent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        float mapScale = viewportWidth / MapLayoutWidth;
        mapMinimumY = float.MaxValue;
        float mapMaximumY = float.MinValue;
        foreach (MapPoint point in run.MapPoints)
        {
            mapMinimumY = Mathf.Min(mapMinimumY, point.Position.y);
            mapMaximumY = Mathf.Max(mapMaximumY, point.Position.y);
        }

        float mapHeight = mapMaximumY - mapMinimumY + MapEdgePadding * 2f;
        mapContent.sizeDelta = new Vector2(MapLayoutWidth, Mathf.Max(viewportHeight / mapScale, mapHeight));
        mapContent.localScale = Vector3.one * mapScale;

        foreach (MapPoint point in run.MapPoints)
        {
            foreach (int nextPointId in point.NextPoints)
            {
                MapPoint nextPoint = run.GetMapPoint(nextPointId);
                if (nextPoint != null) CreateConnection(GetMapPosition(point), GetMapPosition(nextPoint));
            }
        }

        foreach (MapPoint point in run.MapPoints) CreateMapPoint(point);
        return true;
    }

    private void CreateMapPoint(MapPoint point)
    {
        Button pointButton = Instantiate(mapPointPrefab, mapContent);
        pointButton.gameObject.SetActive(true);
        pointButton.name = "MapPoint " + point.Id;

        RectTransform rect = pointButton.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = GetMapPosition(point);

        Image pointImage = pointButton.GetComponent<Image>();
        Sprite pointIcon = GetPointIcon(point.Type);
        if (point.Type == MapPointType.Mystery && pointIcon == null) pointIcon = pointImage.sprite;
        pointImage.sprite = pointIcon;
        pointImage.color = pointIcon == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        pointImage.type = Image.Type.Simple;
        pointImage.preserveAspect = true;
        pointImage.raycastTarget = true;
        pointButton.targetGraphic = pointImage;
        pointButton.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = pointButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.4f);
        colors.colorMultiplier = 1f;
        pointButton.colors = colors;

        ConfigurePointText(pointButton);
        pointButton.onClick.RemoveAllListeners();
        int pointId = point.Id;
        pointButton.onClick.AddListener(() => SelectMapPoint(pointId));
        pointButtons.Add(point.Id, pointButton);
    }

    private void ConfigurePointText(Button pointButton)
    {
        TMP_Text pointText = pointButton.GetComponentInChildren<TMP_Text>(true);
        if (pointText == null) return;
        pointText.gameObject.SetActive(false);
    }

    private void CreateConnection(Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        float drawableLength = distance - ConnectionIconClearance * 2f;
        if (drawableLength <= 0f) return;

        Vector2 directionNormalized = direction / distance;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float dashStep = ConnectionDashLength + ConnectionDashGap;

        for (float offset = 0f; offset < drawableLength; offset += dashStep)
        {
            float dashLength = Mathf.Min(ConnectionDashLength, drawableLength - offset);
            Vector2 dashStart = start + directionNormalized * (ConnectionIconClearance + offset);
            GameObject dashObject = new("MapConnectionDash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dashObject.layer = mapContent.gameObject.layer;

            RectTransform dashRect = dashObject.GetComponent<RectTransform>();
            dashRect.SetParent(mapContent, false);
            dashRect.anchorMin = Vector2.zero;
            dashRect.anchorMax = Vector2.zero;
            dashRect.pivot = new Vector2(0f, 0.5f);
            dashRect.anchoredPosition = dashStart;
            dashRect.sizeDelta = new Vector2(dashLength, ConnectionThickness);
            dashRect.localRotation = Quaternion.Euler(0f, 0f, angle);

            Image dashImage = dashObject.GetComponent<Image>();
            dashImage.color = new Color(0.08f, 0.16f, 0.25f, 0.9f);
            dashImage.raycastTarget = false;
        }
    }

    private Vector2 GetMapPosition(MapPoint point)
    {
        float centeredX = (point.Position.x - MapLayoutWidth * 0.5f) * MapHorizontalScale;
        float mappedY = MapEdgePadding + point.Position.y - mapMinimumY;
        return new Vector2(MapLayoutWidth * 0.5f + centeredX, mappedY);
    }

    private Sprite GetPointIcon(MapPointType type)
    {
        return type switch
        {
            MapPointType.Normal => normalIcon,
            MapPointType.Elite => eliteIcon,
            MapPointType.Shop => shopIcon,
            MapPointType.Rest => restIcon,
            MapPointType.Boss => bossIcon,
            MapPointType.Mystery => mysteryIcon,
            _ => null
        };
    }

    // Map state and selection
    public void UpdateMapUI()
    {
        RunSystem run = RunSystem.Instance;
        if (run == null) return;

        foreach (KeyValuePair<int, Button> pair in pointButtons)
        {
            if (pair.Value == null) continue;
            bool canSelect = run.CanSelectMapPoint(pair.Key);
            pair.Value.interactable = canSelect;

            TMP_Text pointText = pair.Value.GetComponentInChildren<TMP_Text>(true);
            if (pointText != null && pointText.gameObject.activeSelf) pointText.color = canSelect ? Color.black : new Color(0f, 0f, 0f, 0.4f);
        }

        if (actNameText != null && run.CurrentAct != null) actNameText.text = run.CurrentAct.ActName;
        if (healthText != null && run.HeroData != null) healthText.text = $"HP: {run.CurrentHealth}/{run.HeroData.Health}";
        if (goldText != null) goldText.text = "Gold: " + run.Gold;

        MapPoint completedPoint = run.GetMapPoint(run.CompletedMapPointId);
        if (mapMessageText != null) mapMessageText.text = completedPoint != null && completedPoint.Type == MapPointType.Boss ? "Run Complete" : "Choose your next point";
        UpdateCurrentPointGlow(run);
        FocusCurrentMapPoint();
    }

    public void SelectMapPoint(int pointId)
    {
        if (NewRunTutorialUI.IsOpen) return;

        RunSystem run = RunSystem.Instance;
        if (run == null || !run.CanSelectMapPoint(pointId)) return;

        MapPoint point = run.GetMapPoint(pointId);
        if (point == null) return;

        if (point.Type == MapPointType.Shop)
        {
            if (shopUI == null) return;

            run.SelectMapPoint(pointId);
            shopUI.gameObject.SetActive(true);
            shopUI.Setup();
            UpdateMapUI();
            return;
        }

        if (point.Type == MapPointType.Rest)
        {
            if (restUI == null) return;

            run.SelectMapPoint(pointId);
            restUI.gameObject.SetActive(true);
            restUI.Setup();
            UpdateMapUI();
            return;
        }

        EncounterData encounter = GetEncounter(point);

        if (encounter == null)
        {
            Debug.LogError("No suitable encounter is assigned for this map point.", this);
            return;
        }

        run.SelectMapPoint(pointId);
        run.SetEncounter(encounter);
        SceneManager.LoadScene("InGameScene");
    }

    // Encounter selection
    private EncounterData GetEncounter(MapPoint point)
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || run.CurrentAct == null || point == null) return null;

        ActData act = run.CurrentAct;
        int floor = point.Floor + 1;
        if (point.Type == MapPointType.Boss)
        {
            if (IsEncounterAvailable(act.BossEncounter, EncounterType.Boss, floor)) return act.BossEncounter;
            Debug.LogError($"Invalid Boss encounter at floor {floor} in {act.name}.", this);
            return null;
        }

        bool normal = point.Type == MapPointType.Normal || point.Type == MapPointType.Mystery;
        if (!normal && point.Type != MapPointType.Elite) return null;

        EncounterType type = normal ? EncounterType.Normal : EncounterType.Elite;
        List<EncounterData> pool = normal ? act.NormalEncounters : act.EliteEncounters;
        List<EncounterData> available = new();
        if (pool != null)
        {
            foreach (EncounterData encounter in pool)
            {
                if (IsEncounterAvailable(encounter, type, floor) && !available.Contains(encounter)) available.Add(encounter);
            }
        }

        if (available.Count == 0)
        {
            Debug.LogError($"No suitable {type} encounter at floor {floor} in {act.name}.", this);
            return null;
        }

        EncounterData previous = normal ? run.LastNormalEncounter : run.LastEliteEncounter;
        if (available.Count > 1) available.Remove(previous);

        int totalWeight = 0;
        foreach (EncounterData encounter in available) totalWeight += encounter.Weight;
        int roll = Random.Range(0, totalWeight);
        foreach (EncounterData encounter in available)
        {
            roll -= encounter.Weight;
            if (roll >= 0) continue;
            Debug.Log($"{act.name}, floor {floor}: {available.Count} candidates -> {encounter.name}", encounter);
            return encounter;
        }

        return null;
    }

    private bool IsEncounterAvailable(EncounterData encounter, EncounterType type, int floor)
    {
        if (encounter == null || encounter.Type != type || encounter.Weight <= 0) return false;
        if (floor < encounter.MinFloor || floor > encounter.MaxFloor) return false;
        if (encounter.Enemies == null || encounter.Enemies.Count == 0 || encounter.Enemies.Count > 3) return false;
        if (type != EncounterType.Normal && encounter.Enemies.Count != 1) return false;

        EnemyRank rank = type switch
        {
            EncounterType.Elite => EnemyRank.Elite,
            EncounterType.Boss => EnemyRank.Boss,
            _ => EnemyRank.Normal
        };

        foreach (EnemyData enemy in encounter.Enemies)
        {
            if (enemy == null || enemy.Rank != rank || enemy.Health <= 0 || enemy.Moves == null || enemy.Moves.Count == 0) return false;
        }

        return true;
    }

    // Current point glow
    private void UpdateCurrentPointGlow(RunSystem run)
    {
        int pointId = run.CompletedMapPointId;
        if (pointId == glowingMapPointId && currentPointGlow != null) return;
        ClearCurrentPointGlow();
        glowingMapPointId = pointId;
        if (pointId < 0 || !pointButtons.TryGetValue(pointId, out Button pointButton) || pointButton == null) return;

        Image pointImage = pointButton.GetComponent<Image>();
        TMP_Text pointText = pointButton.GetComponentInChildren<TMP_Text>(true);
        bool useImage = pointImage != null && pointImage.sprite != null;
        if (!useImage && (pointText == null || !pointText.gameObject.activeSelf)) return;

        currentPointGlow = new GameObject("CurrentMapPointGlow", typeof(RectTransform), typeof(CanvasRenderer));
        currentPointGlow.layer = mapContent.gameObject.layer;
        currentPointGlowRect = currentPointGlow.GetComponent<RectTransform>();
        currentPointGlowRect.SetParent(mapContent, false);
        currentPointGlowRect.anchorMin = Vector2.zero;
        currentPointGlowRect.anchorMax = Vector2.zero;

        RectTransform pointRect = pointButton.GetComponent<RectTransform>();
        currentPointGlowRect.pivot = pointRect.pivot;
        currentPointGlowRect.anchoredPosition = pointRect.anchoredPosition;
        currentPointGlowRect.sizeDelta = pointRect.sizeDelta;
        currentPointGlowRect.localScale = Vector3.one * 1.08f;
        currentPointGlowRect.SetSiblingIndex(pointButton.transform.GetSiblingIndex());

        if (useImage)
        {
            currentPointGlowImage = currentPointGlow.AddComponent<Image>();
            currentPointGlowImage.sprite = pointImage.sprite;
            currentPointGlowImage.type = Image.Type.Simple;
            currentPointGlowImage.preserveAspect = pointImage.preserveAspect;
            currentPointGlowImage.raycastTarget = false;
            currentPointGlowOutline = currentPointGlow.AddComponent<Outline>();
            currentPointGlowOutline.effectDistance = new Vector2(2f, 2f);
            currentPointGlowOutline.useGraphicAlpha = true;
        }
        else
        {
            currentPointGlowText = currentPointGlow.AddComponent<TextMeshProUGUI>();
            currentPointGlowText.font = pointText.font;
            currentPointGlowText.text = pointText.text;
            currentPointGlowText.fontSize = pointText.fontSize;
            currentPointGlowText.fontStyle = pointText.fontStyle;
            currentPointGlowText.enableAutoSizing = pointText.enableAutoSizing;
            currentPointGlowText.fontSizeMin = pointText.fontSizeMin;
            currentPointGlowText.fontSizeMax = pointText.fontSizeMax;
            currentPointGlowText.alignment = TextAlignmentOptions.Center;
            currentPointGlowText.raycastTarget = false;
        }

        AnimateCurrentPointGlow();
    }

    private void AnimateCurrentPointGlow()
    {
        if (currentPointGlowRect == null) return;

        float pulse = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
        currentPointGlowRect.localScale = Vector3.one * Mathf.Lerp(1.08f, 1.2f, pulse);
        if (currentPointGlowImage != null) currentPointGlowImage.color = new Color(1f, 0.78f, 0.12f, Mathf.Lerp(0.16f, 0.34f, pulse));
        if (currentPointGlowOutline != null) currentPointGlowOutline.effectColor = new Color(1f, 0.72f, 0.12f, Mathf.Lerp(0.3f, 0.75f, pulse));
        if (currentPointGlowText != null) currentPointGlowText.color = new Color(1f, 0.78f, 0.12f, Mathf.Lerp(0.3f, 0.7f, pulse));
    }

    private void ClearCurrentPointGlow()
    {
        if (currentPointGlow != null)
        {
            currentPointGlow.SetActive(false);
            Destroy(currentPointGlow);
        }

        currentPointGlow = null;
        currentPointGlowRect = null;
        currentPointGlowImage = null;
        currentPointGlowOutline = null;
        currentPointGlowText = null;
        glowingMapPointId = -1;
    }

    // Legend and timer
    private void StyleMapLegend()
    {
        if (mapLegend == null) return;

        foreach (Button entry in mapLegend.GetComponentsInChildren<Button>(true))
        {
            entry.enabled = false;
            bool isTitle = entry.name == "MapLegend - Title";
            Image icon = entry.GetComponent<Image>();
            if (icon != null)
            {
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.color = isTitle ? new Color(1f, 1f, 1f, 0f) : Color.white;
                if (!isTitle) ConfigureIconOutline(icon, new Color(0.96f, 0.72f, 0.3f, 0.8f));
            }

            TMP_Text label = entry.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                Debug.LogError("Map legend entry is missing its label: " + entry.name, this);
                continue;
            }

            StyleText(label, isTitle ? 30f : 24f, new Color(0.96f, 0.93f, 0.85f), isTitle ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft);
            ConfigureTextShadow(label);
        }
    }

    private void SetupRunTimer()
    {
        if (runTimerDisplay == null) return;

        Transform textTransform = runTimerDisplay.Find("RunTimerText");
        runTimerText = textTransform != null ? textTransform.GetComponentInChildren<TMP_Text>(true) : null;
        if (runTimerText == null)
        {
            Debug.LogError("Run Timer Display needs a RunTimerText object with a TMP text component.", this);
            return;
        }

        StyleText(runTimerText, 34f, Color.white, TextAlignmentOptions.Center);
        ConfigureTextShadow(runTimerText);

        Transform iconTransform = runTimerDisplay.Find("RunTimerIcon");
        Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        if (icon == null)
        {
            Debug.LogError("Run Timer Display needs a RunTimerIcon object with an Image component.", this);
            return;
        }

        icon.preserveAspect = true;
        icon.raycastTarget = false;
        ConfigureIconOutline(icon, new Color(0.96f, 0.72f, 0.3f, 0.85f));
        UpdateRunTimer();
    }

    private void UpdateRunTimer()
    {
        RunSystem run = RunSystem.Instance;
        if (runTimerText == null || run == null) return;

        int elapsedSeconds = Mathf.FloorToInt(run.ElapsedRunTime);
        int hours = elapsedSeconds / 3600;
        int minutes = (elapsedSeconds / 60) % 60;
        int seconds = elapsedSeconds % 60;
        runTimerText.text = hours > 0 ? $"{hours:00}:{minutes:00}:{seconds:00}" : $"{minutes:00}:{seconds:00}";
    }

    // Styling helpers
    private void StyleText(TMP_Text text, float size, Color color, TextAlignmentOptions alignment)
    {
        text.font = mapMessageText.font;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
    }

    private static void ConfigureIconOutline(Image image, Color color)
    {
        Outline outline = image.GetComponent<Outline>();
        if (outline == null) outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1.5f, 1.5f);
    }

    private static void ConfigureTextShadow(TMP_Text text)
    {
        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    private static void HideScrollbar(Scrollbar scrollbar)
    {
        if (scrollbar == null) return;
        CanvasGroup canvasGroup = scrollbar.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = scrollbar.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
    private void FocusCurrentMapPoint()
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || mapScrollRect == null || mapContent == null || mapScrollRect.viewport == null) return;

        int pointId = run.CompletedMapPointId;
        if (pointId == lastFocusedPointId) return;

        lastFocusedPointId = pointId;
        Canvas.ForceUpdateCanvases();
        mapScrollRect.StopMovement();

        if (pointId < 0)
        {
            mapScrollRect.verticalNormalizedPosition = 0f;
            return;
        }

        if (!pointButtons.TryGetValue(pointId, out Button pointButton) || pointButton == null) return;

        RectTransform viewport = mapScrollRect.viewport;
        Bounds contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, mapContent);
        float scrollableHeight = contentBounds.size.y - viewport.rect.height;
        if (scrollableHeight <= 0f) return;

        float pointY = viewport.InverseTransformPoint(pointButton.transform.position).y;
        float desiredY = viewport.rect.yMin + viewport.rect.height * 0.3f;
        float offset = (pointY - desiredY) / scrollableHeight;

        mapScrollRect.verticalNormalizedPosition = Mathf.Clamp01(mapScrollRect.verticalNormalizedPosition + offset);
    }
}