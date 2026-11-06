using UnityEngine;
using UnityEngine.SceneManagement;

public class LibraryNavigation : MonoBehaviour
{
    public void OpenLibrary()
    {
        SceneManager.LoadScene("LibraryScene");
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }
}