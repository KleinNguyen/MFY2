using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButtonUI: MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("SelectHeroScene");
    }
}
