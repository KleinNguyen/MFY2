using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class MapUI : MonoBehaviour
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

        if (mapScrollRect == null || mapScrollRect.viewport == null ||
            mapContent == null || mapPointPrefab == null ||
            mapMessageText == null || shopUI == null || restUI == null)
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
            if (pointText != null && pointText.gameObject.activeSelf)
            {
                pointText.color = canSelect
                    ? Color.black
                    : new Color(0f, 0f, 0f, 0.4f);
            }
        }

        if (actNameText != null && run.CurrentAct != null) actNameText.text = run.CurrentAct.ActName;
        if (healthText != null && run.HeroData != null)
            healthText.text = $"HP: {run.CurrentHealth}/{run.HeroData.Health}";
        if (goldText != null) goldText.text = "Gold: " + run.Gold;

        MapPoint completedPoint = run.GetMapPoint(run.CompletedMapPointId);
        if (mapMessageText != null)
        {
            mapMessageText.text = completedPoint != null && completedPoint.Type == MapPointType.Boss
                ? "Run Complete"
                : "Choose your next point";
        }
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

























}