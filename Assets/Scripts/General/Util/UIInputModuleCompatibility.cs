using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public static class UIInputModuleCompatibility
{
    private const float MousePositionTolerance = 2f;
    private const int MismatchedFramesBeforeFallback = 3;

    private static EventSystem monitoredEventSystem;
    private static int consecutiveMismatchedFrames;
    private static bool missingEventSystemLogged;

    public static void MonitorAndConfigure(EventSystem eventSystem, Object context)
    {
        if (eventSystem == null)
        {
            if (!missingEventSystemLogged)
            {
                Debug.LogError("[UIInputDebug] Cannot monitor input: EventSystem is missing.", context);
                missingEventSystemLogged = true;
            }

            return;
        }

        missingEventSystemLogged = false;
        InputSystemUIInputModule inputSystemModule =
            eventSystem.GetComponent<InputSystemUIInputModule>();

        if (inputSystemModule == null)
        {
            EnsureStandaloneModule(eventSystem, context, false);
            return;
        }

        if (Mouse.current == null)
        {
            EnsureStandaloneModule(eventSystem, context, true);
            return;
        }

        if (monitoredEventSystem != eventSystem)
        {
            monitoredEventSystem = eventSystem;
            consecutiveMismatchedFrames = 0;
        }

        Vector2 inputSystemPosition = Mouse.current.position.ReadValue();
        Vector2 legacyInputPosition = Input.mousePosition;
        bool positionsMismatch = legacyInputPosition != Vector2.zero
            && Vector2.Distance(inputSystemPosition, legacyInputPosition) > MousePositionTolerance;

        if (!positionsMismatch)
        {
            consecutiveMismatchedFrames = 0;
            return;
        }

        consecutiveMismatchedFrames++;
        if (consecutiveMismatchedFrames >= MismatchedFramesBeforeFallback)
        {
            EnsureStandaloneModule(eventSystem, context, true);
            monitoredEventSystem = null;
            consecutiveMismatchedFrames = 0;
        }
    }

    private static void EnsureStandaloneModule(
        EventSystem eventSystem,
        Object context,
        bool disableInputSystemModule
    )
    {
        bool changed = false;
        InputSystemUIInputModule inputSystemModule =
            eventSystem.GetComponent<InputSystemUIInputModule>();
        if (disableInputSystemModule && inputSystemModule != null && inputSystemModule.enabled)
        {
            inputSystemModule.enabled = false;
            changed = true;
        }

        StandaloneInputModule standaloneModule =
            eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule == null)
        {
            standaloneModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            changed = true;
        }
        else if (!standaloneModule.enabled)
        {
            standaloneModule.enabled = true;
            changed = true;
        }

        if (changed)
        {
            Debug.Log(
                $"[UIInputDebug] Configured {standaloneModule.GetType().Name}; " +
                $"disabledInputSystemModule={disableInputSystemModule && inputSystemModule != null}",
                context
            );
        }
    }
}