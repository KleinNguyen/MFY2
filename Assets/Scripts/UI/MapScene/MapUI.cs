using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    [SerializeField] private bool debugInput = true;

    [SerializeField] private EncounterData[] encounters;

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

        if (debugInput)
        {
            StartCoroutine(LogInputSetupNextFrame());
        }

        if (RunSystem.Instance == null)
        {
            Debug.LogError("No RunSystem in MapScene");
            return;
        }

        shopUI.gameObject.SetActive(false);
        restUI.gameObject.SetActive(false);

        RunSystem.Instance.GenerateMap();

        CreateMapUI();
        UpdateMapUI();

        Canvas.ForceUpdateCanvases();
        mapScrollRect.verticalNormalizedPosition = 0f;
    }

    private void ConfigureInputModule()
    {
        UIInputModuleCompatibility.UseStandaloneInputModule(EventSystem.current, this);
    }

    private IEnumerator LogInputSetupNextFrame()
    {
        yield return null;
        LogInputSetup();
    }

    private void Update()
    {
        bool inputSystemClick = Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame;
        bool legacyInputClick = Input.GetMouseButtonDown(0);

        if (debugInput && (inputSystemClick || legacyInputClick))
        {
            LogClick(inputSystemClick, legacyInputClick);
        }
    }

    private void LogInputSetup()
    {
        EventSystem eventSystem = EventSystem.current;
        string eventSystemStatus = eventSystem == null
            ? "missing"
            : $"active; currentModule={eventSystem.currentInputModule?.GetType().Name ?? "not selected yet"}; " +
              $"attachedModules={DescribeInputModules(eventSystem)}";
        string mouseStatus = Mouse.current == null
            ? "missing"
            : $"available; position={Mouse.current.position.ReadValue()}";

        Debug.Log(
            $"[MapInputDebug] MapScene input setup: EventSystem={eventSystemStatus}; " +
            $"Mouse={mouseStatus}; applicationFocused={Application.isFocused}",
            this
        );
    }

    private static string DescribeInputModules(EventSystem eventSystem)
    {
        BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
        if (modules.Length == 0) return "none";

        List<string> descriptions = new(modules.Length);
        foreach (BaseInputModule module in modules)
        {
            descriptions.Add(
                $"{module.GetType().Name}(enabled={module.enabled}, active={module.isActiveAndEnabled})"
            );
        }

        return string.Join(", ", descriptions);
    }

    private void LogClick(bool inputSystemClick, bool legacyInputClick)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogWarning(
                $"[MapInputDebug] Mouse click detected (Input System={inputSystemClick}, " +
                $"{nameof(Input)}={legacyInputClick}), but EventSystem.current is missing.",
                this
            );
            return;
        }

        PointerEventData pointerData = new(eventSystem)
        {
            position = inputSystemClick
                ? Mouse.current.position.ReadValue()
                : Input.mousePosition,
        };
        List<RaycastResult> raycastResults = new();
        eventSystem.RaycastAll(pointerData, raycastResults);

        List<string> hitDescriptions = new();
        int resultCount = Mathf.Min(raycastResults.Count, 8);
        for (int i = 0; i < resultCount; i++)
        {
            GameObject hitObject = raycastResults[i].gameObject;
            Button button = hitObject.GetComponentInParent<Button>();
            string buttonStatus = button == null
                ? "no Button"
                : $"Button={button.name}, interactable={button.interactable}, active={button.isActiveAndEnabled}";
            hitDescriptions.Add($"{hitObject.name} ({buttonStatus})");
        }

        string hitSummary = hitDescriptions.Count == 0
            ? "no UI raycast hits"
            : string.Join(" -> ", hitDescriptions);

        Debug.Log(
            $"[MapInputDebug] Mouse click detected (Input System={inputSystemClick}, " +
            $"{nameof(Input)}={legacyInputClick}); position={pointerData.position}; " +
            $"inputSystemPosition={(Mouse.current != null ? Mouse.current.position.ReadValue().ToString() : "unavailable")}; " +
            $"legacyInputPosition={Input.mousePosition}; " +
            $"applicationFocused={Application.isFocused}; " +
            $"pointerOverUI={eventSystem.IsPointerOverGameObject()}; " +
            $"currentModule={eventSystem.currentInputModule?.GetType().Name ?? "missing"}; " +
            $"attachedModules={DescribeInputModules(eventSystem)}; " +
            $"raycastHits={raycastResults.Count}; {hitSummary}",
            this
        );
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

        pointButton.onClick.AddListener(() =>
        {
            if (debugInput)
            {
                Debug.Log(
                    $"[MapInputDebug] Map point button callback received: {pointButton.name}; " +
                    $"interactable={pointButton.interactable}",
                    pointButton
                );
            }

            SelectMapPoint(pointId);
        });

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
        if (type == MapPointType.Battle) return "Battle";
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
        if (encounters == null || encounters.Length == 0) return null;

        if (type == MapPointType.Boss)
        {
            return encounters[encounters.Length - 1];
        }

        if (encounters.Length < 2) return null;

        int randomIndex = Random.Range(0, encounters.Length - 1);

        return encounters[randomIndex];
    }
}