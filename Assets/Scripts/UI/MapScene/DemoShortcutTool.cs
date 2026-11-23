using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DemoShortcutTool : MonoBehaviour
{
    private enum BattleOutcome
    {
        None,
        Victory,
        Defeat
    }

    private static DemoShortcutTool instance;

    private bool isPanelOpen;
    private Vector2 panelScrollPosition;
    private string statusMessage = "F1: show/hide demo controls";
    private BattleOutcome pendingOutcome;
    private bool pendingFullGameVictory;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreatePersistentTool()
    {
        if (instance != null) return;

        GameObject toolObject = new("Demo Shortcut Tool");
        instance = toolObject.AddComponent<DemoShortcutTool>();
        DontDestroyOnLoad(toolObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f1Key.wasPressedThisFrame) isPanelOpen = !isPanelOpen;
        if (keyboard.f2Key.wasPressedThisFrame) OpenRest();
        if (keyboard.f3Key.wasPressedThisFrame) StartMysteryEncounter();
        if (keyboard.f4Key.wasPressedThisFrame) OpenAct(0);
        if (keyboard.f5Key.wasPressedThisFrame) OpenAct(1);
        if (keyboard.f6Key.wasPressedThisFrame) OpenAct(2);
        bool controlPressed = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        if (keyboard.f7Key.wasPressedThisFrame)
        {
            if (controlPressed) FinishWholeGame();
            else RequestBattleOutcome(BattleOutcome.Victory);
        }
        if (keyboard.f8Key.wasPressedThisFrame) OpenShop();
        if (keyboard.f9Key.wasPressedThisFrame) RequestBattleOutcome(BattleOutcome.Defeat);
        if (keyboard.f10Key.wasPressedThisFrame) StartBossEncounter(0);
        if (keyboard.f11Key.wasPressedThisFrame) StartBossEncounter(1);
        if (keyboard.f12Key.wasPressedThisFrame) StartBossEncounter(2);
        if (keyboard.eKey.wasPressedThisFrame) StartEliteEncounter();
    }

    private void OnGUI()
    {
        if (!isPanelOpen) return;

        float panelHeight = Mathf.Max(160f, Screen.height - 24f);
        GUILayout.BeginArea(new Rect(12f, 12f, 320f, panelHeight), GUI.skin.window);
        GUILayout.Label("DEMO SHORTCUTS");
        GUILayout.Label(statusMessage);
        panelScrollPosition = GUILayout.BeginScrollView(panelScrollPosition);

        if (GUILayout.Button("Open Shop (F8)")) OpenShop();
        if (GUILayout.Button("Open Rest (F2)")) OpenRest();
        if (GUILayout.Button("Start Mystery (F3)")) StartMysteryEncounter();
        if (GUILayout.Button("Go to Act 1 - fresh run (F4)")) OpenAct(0);
        if (GUILayout.Button("Go to Act 2 - fresh run (F5)")) OpenAct(1);
        if (GUILayout.Button("Go to Act 3 - fresh run (F6)")) OpenAct(2);
        if (GUILayout.Button("Show Victory (F7)")) RequestBattleOutcome(BattleOutcome.Victory);
        if (GUILayout.Button("Win Entire Game (Ctrl+F7)")) FinishWholeGame();
        if (GUILayout.Button("Show Defeat (F9)")) RequestBattleOutcome(BattleOutcome.Defeat);
        if (GUILayout.Button("Start Act 1 Boss (F10)")) StartBossEncounter(0);
        if (GUILayout.Button("Start Act 2 Boss (F11)")) StartBossEncounter(1);
        if (GUILayout.Button("Start Act 3 Boss (F12)")) StartBossEncounter(2);
        if (GUILayout.Button("Start Elite (E)")) StartEliteEncounter();

        GUILayout.EndScrollView();
        if (GUILayout.Button("Hide (F1)")) isPanelOpen = false;
        GUILayout.EndArea();
    }

    private void OpenShop() => OpenMapPanel(true);
    private void OpenRest() => OpenMapPanel(false);

    private void OpenMapPanel(bool shop)
    {
        if (!TryGetRun(out _)) return;
        if (SceneManager.GetActiveScene().name != "MapScene")
        {
            statusMessage = "Shop and Rest can only be opened from the map.";
            return;
        }

        MapUI mapUI = FindFirstObjectByType<MapUI>();
        if (mapUI == null)
        {
            Debug.LogError("Could not find MapUI in MapScene.", this);
            statusMessage = "Map UI was not found.";
            return;
        }

        bool opened = shop ? mapUI.OpenShopForDemo() : mapUI.OpenRestForDemo();
        if (!opened)
        {
            statusMessage = "Close the other map panel and try again.";
            return;
        }

        statusMessage = shop ? "Shop opened." : "Rest opened.";
    }

    private void OpenAct(int actIndex)
    {
        if (!TryGetRun(out RunSystem run)) return;
        if (!ResetRunToAct(run, actIndex)) return;

        SceneManager.LoadScene("MapScene");
        statusMessage = $"Opened Act {actIndex + 1} with a fresh demo run.";
    }

    private bool ResetRunToAct(RunSystem run, int actIndex)
    {
        if (actIndex < 0 || actIndex > 2)
        {
            Debug.LogError($"Invalid demo Act index: {actIndex}.", this);
            return false;
        }

        HeroData hero = run.HeroData;
        run.StartNewRun(hero);
        for (int index = 0; index < actIndex; index++)
        {
            if (run.MoveToNextAct()) continue;

            Debug.LogError($"Could not start demo Act {actIndex + 1}. Check the Acts assigned to RunSystem.", this);
            statusMessage = $"Could not open Act {actIndex + 1}.";
            return false;
        }

        run.GenerateMap();
        if (run.CurrentAct == null || run.MapPoints == null || run.MapPoints.Count == 0)
        {
            Debug.LogError($"Could not generate the map for demo Act {actIndex + 1}.", this);
            statusMessage = $"Could not generate Act {actIndex + 1} map.";
            return false;
        }

        return true;
    }

    private void StartMysteryEncounter()
    {
        if (!TryGetRun(out RunSystem run)) return;
        if (!EnsureMapExists(run)) return;

        MapPoint point = FindPoint(run, MapPointType.Mystery);
        if (point == null)
        {
            point = FindPoint(run, MapPointType.Normal);
            if (point != null) point.SetType(MapPointType.Mystery);
        }

        if (!StartEncounter(run, point, EncounterType.Normal, "Mystery"))
            statusMessage = "Could not start a Mystery. Check this Act's normal encounters.";
    }

    private void StartEliteEncounter()
    {
        if (!TryGetRun(out RunSystem run)) return;
        if (!EnsureMapExists(run)) return;

        MapPoint point = FindPoint(run, MapPointType.Elite);
        if (point == null || FindEncounter(run.CurrentAct, EncounterType.Elite, point.Floor + 1) == null)
        {
            point = FindPointForEncounter(run, EncounterType.Elite);
        }

        if (!StartEncounter(run, point, EncounterType.Elite, "Elite"))
            statusMessage = "Could not start an Elite. Check this Act's elite encounters.";
    }

    private void StartBossEncounter(int actIndex)
    {
        if (!TryGetRun(out RunSystem run)) return;
        if (!ResetRunToAct(run, actIndex)) return;

        MapPoint bossPoint = FindPoint(run, MapPointType.Boss);
        EncounterData boss = run.CurrentAct.BossEncounter;
        if (boss == null || boss.Type != EncounterType.Boss || bossPoint == null)
        {
            Debug.LogError($"Act {actIndex + 1} is missing a valid Boss encounter or map point.", this);
            statusMessage = $"Act {actIndex + 1} Boss is not configured.";
            return;
        }

        LoadEncounter(run, bossPoint, boss);
        statusMessage = $"Started Act {actIndex + 1} Boss.";
    }

    private bool StartEncounter(RunSystem run, MapPoint point, EncounterType type, string label)
    {
        if (point == null)
        {
            Debug.LogError($"Could not find a map point for the {label} encounter.", this);
            return false;
        }

        EncounterData encounter = FindEncounter(run.CurrentAct, type, point.Floor + 1);
        if (encounter == null)
        {
            Debug.LogError($"Act {run.CurrentActIndex + 1} has no suitable {label} encounter for floor {point.Floor + 1}.", this);
            return false;
        }

        LoadEncounter(run, point, encounter);
        statusMessage = $"Started {label} encounter.";
        return true;
    }

    private void LoadEncounter(RunSystem run, MapPoint point, EncounterData encounter)
    {
        run.SelectMapPoint(point.Id);
        run.SetEncounter(encounter);
        SceneManager.LoadScene("InGameScene");
    }

    private void RequestBattleOutcome(BattleOutcome outcome)
    {
        if (!TryGetRun(out RunSystem run)) return;

        BattleSystem battle = BattleSystem.Instance;
        if (SceneManager.GetActiveScene().name == "InGameScene" && battle != null && !battle.IsBattleOver)
        {
            StartCoroutine(ApplyBattleOutcomeWhenReady(outcome));
            return;
        }

        if (!EnsureMapExists(run)) return;
        MapPoint point = FindPoint(run, MapPointType.Normal);
        if (point == null)
        {
            Debug.LogError("Could not find a map point for the demo battle.", this);
            statusMessage = "Could not start a demo battle.";
            return;
        }

        EncounterData encounter = FindEncounter(run.CurrentAct, EncounterType.Normal, point.Floor + 1);
        if (encounter == null)
        {
            Debug.LogError("The current Act has no suitable normal encounter for the demo battle.", this);
            statusMessage = "Could not start a demo battle.";
            return;
        }

        pendingOutcome = outcome;
        LoadEncounter(run, point, encounter);
        statusMessage = outcome == BattleOutcome.Victory ? "Victory will appear after the battle loads." : "Defeat will appear after the battle loads.";
    }

    private void FinishWholeGame()
    {
        if (!TryGetRun(out RunSystem run)) return;

        run.FinishRun(RunResult.Won);
        pendingFullGameVictory = true;
        statusMessage = "Full-game victory. Opening the victory panel.";
        SceneManager.LoadScene("InGameScene");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "InGameScene") return;

        if (pendingFullGameVictory)
        {
            pendingFullGameVictory = false;
            StartCoroutine(ShowFullGameVictory());
            return;
        }

        if (pendingOutcome == BattleOutcome.None) return;

        BattleOutcome outcome = pendingOutcome;
        pendingOutcome = BattleOutcome.None;
        StartCoroutine(ApplyBattleOutcomeWhenReady(outcome));
    }

    private IEnumerator ShowFullGameVictory()
    {
        yield return null;
        yield return null;

        RunResultUI resultUI = FindFirstObjectByType<RunResultUI>(FindObjectsInactive.Include);
        if (resultUI == null)
        {
            Debug.LogError("Could not find RunResultUI in InGameScene.", this);
            statusMessage = "Victory panel was not found.";
            yield break;
        }

        resultUI.Show();
        statusMessage = "Full-game victory panel is open.";
    }

    private IEnumerator ApplyBattleOutcomeWhenReady(BattleOutcome outcome)
    {
        float timeout = 20f;
        while (timeout > 0f)
        {
            BattleSystem battle = BattleSystem.Instance;
            if (battle != null && !battle.IsPreparingBattle)
            {
                if (outcome == BattleOutcome.Victory) battle.Victory();
                else if (outcome == BattleOutcome.Defeat) battle.Defeat();
                yield break;
            }

            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogError("The demo battle did not finish setup in time.", this);
        statusMessage = "Battle setup timed out.";
    }

    private bool EnsureMapExists(RunSystem run)
    {
        run.GenerateMap();
        if (run.CurrentAct != null && run.MapPoints != null && run.MapPoints.Count > 0) return true;

        Debug.LogError("The current Act map is not configured.", this);
        statusMessage = "The current Act map is not configured.";
        return false;
    }

    private static MapPoint FindPoint(RunSystem run, MapPointType type)
    {
        if (run.MapPoints == null) return null;
        foreach (MapPoint point in run.MapPoints)
        {
            if (point != null && point.Type == type) return point;
        }

        return null;
    }

    private static MapPoint FindPointForEncounter(RunSystem run, EncounterType type)
    {
        if (run.MapPoints == null) return null;

        foreach (MapPoint point in run.MapPoints)
        {
            if (point != null && point.Type != MapPointType.Boss &&
                FindEncounter(run.CurrentAct, type, point.Floor + 1) != null)
                return point;
        }

        return null;
    }

    private static EncounterData FindEncounter(ActData act, EncounterType type, int floor)
    {
        if (act == null) return null;

        if (type == EncounterType.Boss)
        {
            EncounterData boss = act.BossEncounter;
            return boss != null && boss.Type == type && floor >= boss.MinFloor && floor <= boss.MaxFloor ? boss : null;
        }

        List<EncounterData> encounters = type == EncounterType.Elite ? act.EliteEncounters : act.NormalEncounters;
        if (encounters == null) return null;

        List<EncounterData> available = new();
        foreach (EncounterData encounter in encounters)
        {
            if (encounter != null && encounter.Type == type && floor >= encounter.MinFloor && floor <= encounter.MaxFloor)
                available.Add(encounter);
        }

        return available.Count > 0 ? available[Random.Range(0, available.Count)] : null;
    }

    private bool TryGetRun(out RunSystem run)
    {
        run = RunSystem.Instance;
        if (run != null && run.HeroData != null)
        {
            if (run.IsRunFinished) run.StartNewRun(run.HeroData);
            return true;
        }

        Debug.LogError("An active run and hero are required for demo shortcuts.", this);
        statusMessage = "Start a run before using demo shortcuts.";
        return false;
    }
}
