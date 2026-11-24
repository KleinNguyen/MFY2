using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(
    typeof(Canvas),
    typeof(CanvasGroup),
    typeof(GraphicRaycaster)
)]
public class NewRunTutorialUI : MonoBehaviour
{
    [System.Serializable]
    public class TutorialPage
    {
        public Sprite image;
        public string title;

        [TextArea(3, 8)]
        public string description;
    }

    private enum TutorialPart
    {
        None,
        Map,
        Battle
    }

    [Header("Page Display")]
    [SerializeField] private Image tutorialImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text pageCounterText;
    [SerializeField] private TMP_Text nextButtonText;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;

    [Header("Optional Map Pages")]
    [SerializeField]
    private TutorialPage[] mapPages = new TutorialPage[0];

    [Header("Battle Pages")]
    [SerializeField]
    private TutorialPage[] battlePages =
    {
        new TutorialPage
        {
            title = "Play Cards",
            description =
                "Drag a card onto an enemy or into the play area.\n\n" +
                "Cards cost Mana, which refills each turn."
        },

        new TutorialPage
        {
            title = "Gain Block",
            description =
                "Block absorbs damage before you lose HP.\n\n" +
                "It normally clears at the start of your next turn."
        },

        new TutorialPage
        {
            title = "Read Your Cards",
            description =
                "The top-left number is the Mana cost.\n\n" +
                "The description explains what the card does."
        },

        new TutorialPage
        {
            title = "Check Enemy Intent",
            description =
                "Check the number above each enemy before pressing End Turn.\n\n" +
                "It shows incoming damage and hit count."
        }
    };

    private static NewRunTutorialUI instance;

    private CanvasGroup rootGroup;
    private List<TutorialPage> currentPages;
    private TutorialPart currentPart;

    private GameObject previousSelection;

    private bool configured;
    private bool mapPending;
    private bool battlePending;
    private bool isOpen;

    private int pageIndex;

    public static bool IsOpen
    {
        get
        {
            if (instance == null)
            {
                return false;
            }

            if (instance.isOpen)
            {
                return true;
            }

            // Block input between battle setup ending and the panel opening.
            return instance.battlePending &&
                   SceneManager.GetActiveScene().name == "InGameScene" &&
                   BattleSystem.Instance != null &&
                   !BattleSystem.Instance.IsPreparingBattle &&
                   !BattleSystem.Instance.IsBattleOver;
        }
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void BeginNewGame(NewRunTutorialUI prefab)
    {
        if (instance == null)
        {
            if (prefab == null || !prefab.gameObject.activeSelf)
            {
                Debug.LogError("Assign an active tutorial prefab in PlayButtonUI.");
                return;
            }

            instance = Instantiate(prefab);
        }

        if (!instance.configured) return;

        instance.StopAllCoroutines();
        instance.HideWindow();

        instance.mapPending = HasPages(instance.mapPages);
        instance.battlePending = HasPages(instance.battlePages);

        if (!instance.mapPending && !instance.battlePending)
        {
            Debug.LogWarning("Assign tutorial images in the tutorial prefab.", instance);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        rootGroup = GetComponent<CanvasGroup>();

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32766;

        configured =
            tutorialImage != null &&
            pageCounterText != null &&
            nextButtonText != null &&
            backButton != null &&
            nextButton != null &&
            skipButton != null;

        HideWindow();

        if (!configured)
        {
            Debug.LogError(
                "Assign the image, page counter, next label " +
                "and all three tutorial buttons.",
                this
            );

            return;
        }

        tutorialImage.raycastTarget = false;
        tutorialImage.preserveAspect = true;

        SetNavigation(backButton);
        SetNavigation(nextButton);
        SetNavigation(skipButton);

        backButton.onClick.AddListener(PreviousPage);
        nextButton.onClick.AddListener(NextPage);
        skipButton.onClick.AddListener(SkipTutorial);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (instance != this)
        {
            return;
        }

        StopAllCoroutines();

        mapPending = false;
        battlePending = false;

        HideWindow();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(PreviousPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextPage);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipTutorial);
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
        {
            return;
        }

        StopAllCoroutines();
        HideWindow();

        if (
            scene.name == "MenuScene" ||
            scene.name == "EndingScene"
        )
        {
            mapPending = false;
            battlePending = false;

            return;
        }

        if (scene.name == "MapScene" && mapPending)
        {
            ShowPages(
                mapPages,
                TutorialPart.Map
            );
        }
        else if (scene.name == "InGameScene" && battlePending)
        {
            StartCoroutine(
                WaitForBattle(scene.handle)
            );
        }
    }

    private IEnumerator WaitForBattle(int sceneHandle)
    {
        // Allow Mystery setup and the opening card draw to finish first.
        while (SceneManager.GetActiveScene().handle == sceneHandle)
        {
            BattleSystem battle = BattleSystem.Instance;

            if (battle != null && battle.IsBattleOver)
            {
                battlePending = false;
                yield break;
            }

            if (
                battle != null &&
                !battle.IsPreparingBattle &&
                ActionSystem.Instance != null &&
                !ActionSystem.Instance.IsPerforming
            )
            {
                ShowPages(
                    battlePages,
                    TutorialPart.Battle
                );

                yield break;
            }

            yield return null;
        }
    }

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

    private void CompletePart(TutorialPart part)
    {
        if (part == TutorialPart.Map)
        {
            mapPending = false;
        }
        else if (part == TutorialPart.Battle)
        {
            battlePending = false;
        }
    }

    private void SkipTutorial()
    {
        StopAllCoroutines();

        mapPending = false;
        battlePending = false;

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