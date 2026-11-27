using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class NewRunTutorialUI
{
    private void ShowPages(
        TutorialPage[] pages,
        TutorialPart part
    )
    {
        currentPages = new List<TutorialPage>();

        if (pages != null)
        {
            foreach (TutorialPage page in pages)
            {
                if (page != null && page.image != null)
                {
                    currentPages.Add(page);
                }
            }
        }

        if (currentPages.Count == 0)
        {
            CompletePart(part);
            HideWindow();

            return;
        }

        currentPart = part;
        pageIndex = 0;
        isOpen = true;

        rootGroup.alpha = 1f;
        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = true;

        if (EventSystem.current != null)
        {
            previousSelection =
                EventSystem.current.currentSelectedGameObject;
        }

        RenderPage();
        SelectNextButton();
    }

    private void RenderPage()
    {
        TutorialPage page = currentPages[pageIndex];

        tutorialImage.sprite = page.image;

        if (titleText != null)
        {
            titleText.text = page.title;

            titleText.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(page.title)
            );
        }

        if (descriptionText != null)
        {
            descriptionText.text = page.description;

            descriptionText.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(page.description)
            );
        }

        pageCounterText.text =
            (pageIndex + 1) + " / " + currentPages.Count;

        backButton.gameObject.SetActive(pageIndex > 0);

        if (pageIndex == currentPages.Count - 1)
        {
            nextButtonText.text = "GOT IT";
        }
        else
        {
            nextButtonText.text = "NEXT";
        }
    }

    private void PreviousPage()
    {
        if (!isOpen || pageIndex <= 0)
        {
            return;
        }

        pageIndex--;

        RenderPage();
        SelectNextButton();
    }

    private void NextPage()
    {
        if (!isOpen)
        {
            return;
        }

        if (pageIndex < currentPages.Count - 1)
        {
            pageIndex++;
            RenderPage();

            return;
        }

        CompletePart(currentPart);
        HideWindow();
    }

    private void HideWindow()
    {
        isOpen = false;
        currentPart = TutorialPart.None;
        currentPages = null;

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }

        if (tutorialImage != null)
        {
            tutorialImage.sprite = null;
        }

        if (
            EventSystem.current != null &&
            previousSelection != null &&
            previousSelection.activeInHierarchy &&
            previousSelection.scene.handle ==
            SceneManager.GetActiveScene().handle
        )
        {
            EventSystem.current.SetSelectedGameObject(
                previousSelection
            );
        }

        previousSelection = null;
    }

    private void SelectNextButton()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(
                nextButton.gameObject
            );
        }
    }

    private void SetNavigation(Button button)
    {
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;

        button.navigation = navigation;
    }

    private static bool HasPages(TutorialPage[] pages)
    {
        if (pages == null)
        {
            return false;
        }

        foreach (TutorialPage page in pages)
        {
            if (page != null && page.image != null)
            {
                return true;
            }
        }

        return false;
    }
}
