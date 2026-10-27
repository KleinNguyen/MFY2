using UnityEngine;
using UnityEngine.SceneManagement;
public class SkipButtonUI : MonoBehaviour
{
    public void SkipStory()
    {
        SceneManager.LoadScene("InGameScene");
    }
}
