using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ContinueRunUI : MonoBehaviour
{
    [SerializeField] private RunSystem runPrefab;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ContinueRun);
    }

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        button.interactable = RunSaveSystem.HasSave;
    }

    private void OnDestroy() => button.onClick.RemoveListener(ContinueRun);

    private void ContinueRun()
    {
        if (!RunSaveSystem.HasSave) return;

        if (RunSystem.Instance == null)
        {
            if (runPrefab == null)
            {
                Debug.LogError("Assign the configured RunSystem prefab in ContinueRunUI.", this);
                return;
            }

            Instantiate(runPrefab);
        }

        if (RunSystem.Instance != null && RunSystem.Instance.TryLoadCheckpoint())
            SceneManager.LoadScene("MapScene");
    }
}