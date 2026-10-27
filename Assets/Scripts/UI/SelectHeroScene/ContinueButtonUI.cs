using UnityEngine;
using UnityEngine.SceneManagement;


public class ContinueButtonUI : MonoBehaviour
{
    public void ContinueClick()
    {
        SceneManager.LoadScene("StoryScene");
    }
}
