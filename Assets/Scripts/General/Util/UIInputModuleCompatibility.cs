using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public static class UIInputModuleCompatibility
{
    public static void UseStandaloneInputModule(EventSystem eventSystem, Object context)
    {
        if (eventSystem == null)
        {
            return;
        }

        InputSystemUIInputModule inputSystemModule =
            eventSystem.GetComponent<InputSystemUIInputModule>();

        if (inputSystemModule != null && Mouse.current != null)
        {
            Vector2 inputSystemPosition = Mouse.current.position.ReadValue();
            Vector2 legacyInputPosition = Input.mousePosition;

            if (inputSystemPosition != Vector2.zero || legacyInputPosition == Vector2.zero)
            {
                return;
            }
        }

        if (inputSystemModule != null)
        {
            inputSystemModule.enabled = false;
        }

        StandaloneInputModule standaloneModule =
            eventSystem.GetComponent<StandaloneInputModule>();

        if (standaloneModule == null)
        {
            standaloneModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
        else
        {
            standaloneModule.enabled = true;
        }
    }
}