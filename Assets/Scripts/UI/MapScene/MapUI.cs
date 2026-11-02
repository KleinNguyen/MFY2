using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    [SerializeField] private EncounterData[] encounters;
    [SerializeField] private Button[] encounterButtons;

    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text mapMessageText;

    [SerializeField] private ShopUI shopUI;
    [SerializeField] private RestUI restUI;

    private void Start()
    {
        if (RunSystem.Instance == null)
        {
            Debug.LogError("No RunSystem in MapScene");
            return;
        }

        shopUI.gameObject.SetActive(false);
        restUI.gameObject.SetActive(false);

        UpdateMapUI();
    }

    public void UpdateMapUI()
    {
        healthText.text = "HP: " + RunSystem.Instance.CurrentHealth;
        goldText.text = "Gold: " + RunSystem.Instance.Gold;

        int currentStage = RunSystem.Instance.CurrentStage;

        for (int i = 0; i < encounterButtons.Length; i++)
        {
            encounterButtons[i].interactable = i == currentStage;
        }

        if (currentStage >= encounterButtons.Length)
        {
            mapMessageText.text = "Run Complete";
        }
        else
        {
            mapMessageText.text = "Choose your next point";
        }
    }

    public void SelectEncounter(int pointIndex)
    {
        if (RunSystem.Instance == null) return;
        if (pointIndex != RunSystem.Instance.CurrentStage) return;

        if (pointIndex == 1)
        {
            shopUI.gameObject.SetActive(true);
            shopUI.Setup();
            return;
        }

        if (pointIndex == 2)
        {
            restUI.gameObject.SetActive(true);
            restUI.Setup();
            return;
        }

        int encounterIndex = -1;

        if (pointIndex == 0) encounterIndex = 0;
        if (pointIndex == 3) encounterIndex = 1;
        if (pointIndex == 4) encounterIndex = 2;

        if (encounterIndex < 0 || encounterIndex >= encounters.Length) return;

        EncounterData encounter = encounters[encounterIndex];

        if (encounter == null)
        {
            Debug.LogError("Encounter is not assigned");
            return;
        }

        RunSystem.Instance.SetEncounter(encounter);

        SceneManager.LoadScene("InGameScene");
    }
}