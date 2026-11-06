using UnityEngine;
using UnityEngine.SceneManagement;

public class SkipButtonUI : MonoBehaviour
{
    public void SkipStory()
    {
        if (RunSystem.Instance == null)
        {
            Debug.LogError("RunSystem is missing while leaving the story.");
            return;
        }

        RunSystem.Instance.StartRunTimer();
        SceneManager.LoadScene("MapScene");
    }
}
