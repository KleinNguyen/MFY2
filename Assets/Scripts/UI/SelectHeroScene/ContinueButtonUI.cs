using UnityEngine;
using UnityEngine.SceneManagement;

public class ContinueButtonUI : MonoBehaviour
{
    [SerializeField] private HeroSelectionUI heroSelectionUI;

    public void ContinueClick()
    {
        if (heroSelectionUI == null)
        {
            Debug.LogError("HeroSelectionUI is not assigned.");
            return;
        }

        if (!heroSelectionUI.StartSelectedRun())
        {
            return;
        }

        SceneManager.LoadScene("StoryScene");
    }
}