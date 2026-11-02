using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlayButtonUI: MonoBehaviour
{
    private void Start()
    {
        UIInputModuleCompatibility.MonitorAndConfigure(EventSystem.current, this);
    }

    private void Update()
    {
        UIInputModuleCompatibility.MonitorAndConfigure(EventSystem.current, this);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("SelectHeroScene");
    }
}
