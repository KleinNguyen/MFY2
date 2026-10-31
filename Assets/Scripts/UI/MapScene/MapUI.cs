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
    private void Start()
    {
        if(RunSystem.Instance == null)
        {
            Debug.LogError("No run system in MapScene");
            return;
        }
        UpdateMapUI();
    }
    public void UpdateMapUI()
    {
        healthText.text = "HP: " + RunSystem.Instance.CurrentHealth;
        goldText.text = "Gold: " + RunSystem.Instance.Gold;
        int currentStage = RunSystem.Instance.CurrentStage;
        for(int i = 0; i < encounterButtons.Length; i++)
        {
            encounterButtons[i].interactable = i == currentStage;
        }
        if(currentStage >= encounters.Length)
        {
            mapMessageText.text = "Run Complete";
        }
        else
        {
            mapMessageText.text = "Choose your next battle";
        }
    }
    public void SelectEncounter(int encounterIndex)
    {
        if (RunSystem.Instance == null) return;
        if (encounterIndex != RunSystem.Instance.CurrentStage) return;
        if (encounterIndex < 0 || encounterIndex >= encounters.Length) return;

        EncounterData encounter = encounters[encounterIndex];
        if(encounter == null)
        {
            return;
        }
        RunSystem.Instance.SetEncounter(encounter);
        SceneManager.LoadScene("InGameScene");
    }
}
