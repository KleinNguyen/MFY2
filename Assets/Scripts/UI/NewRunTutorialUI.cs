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
public partial class NewRunTutorialUI : MonoBehaviour
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

        if (!instance.configured)
        {
            return;
        }

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
























}