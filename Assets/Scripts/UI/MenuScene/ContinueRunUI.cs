using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ContinueRunUI : MonoBehaviour
{
    [SerializeField] private RunSystem runPrefab;

    private Button button;
    private bool loading;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ContinueRun);
    }

    private void OnEnable()
    {
        loading = false;
        RefreshAvailability();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ContinueRun);
        }
    }

    // Update availability after save data changes.
    public void RefreshAvailability()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        button.interactable = !loading && RunSaveSystem.HasSave;
    }

    private void ContinueRun()
    {
        if (loading || !RunSaveSystem.HasSave)
        {
            return;
        }

        if (RunSystem.Instance == null)
        {
            if (runPrefab == null)
            {
                Debug.LogError(
                    "Assign the configured RunSystem prefab in ContinueRunUI.",
                    this
                );

                return;
            }

            Instantiate(runPrefab);
        }

        RunSystem run = RunSystem.Instance;

        if (run == null)
        {
            return;
        }

        loading = true;
        button.interactable = false;

        if (!run.ContinueFromSave())
        {
            loading = false;
            RefreshAvailability();

            Debug.LogError(
                "Could not continue the saved run.",
                this
            );

            return;
        }

        SceneManager.LoadScene("MapScene");
    }
}