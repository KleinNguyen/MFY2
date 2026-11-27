using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class MapUI
{
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
            GameObject dashObject = new(
                "MapConnectionDash",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
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
}
