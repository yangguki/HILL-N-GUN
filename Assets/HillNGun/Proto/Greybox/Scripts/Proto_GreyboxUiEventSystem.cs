using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace HillNGun.Proto.Greybox
{
    /// <summary>
    /// Runtime-built Greybox HUD has uGUI sliders/buttons but no scene EventSystem. With
    /// activeInputHandler = Input System only, touch drags need InputSystemUIInputModule.
    /// </summary>
    public static class Proto_GreyboxUiEventSystem
    {
        public const string EventSystemName = "Greybox_EventSystem";

        public static void Ensure()
        {
            if (EventSystem.current != null)
            {
                EnsureInputModule(EventSystem.current.gameObject);
                return;
            }

            var existing = GameObject.Find(EventSystemName);
            if (existing != null)
            {
                if (existing.GetComponent<EventSystem>() == null)
                    existing.AddComponent<EventSystem>();
                EnsureInputModule(existing);
                return;
            }

            var go = new GameObject(EventSystemName);
            go.AddComponent<EventSystem>();
            EnsureInputModule(go);
        }

        static void EnsureInputModule(GameObject go)
        {
#if ENABLE_INPUT_SYSTEM
            var legacy = go.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(legacy);
                else
                    Object.DestroyImmediate(legacy);
            }

            var module = go.GetComponent<InputSystemUIInputModule>();
            if (module == null)
                module = go.AddComponent<InputSystemUIInputModule>();
            if (module != null && module.actionsAsset == null)
                module.AssignDefaultActions();
#else
            if (go.GetComponent<StandaloneInputModule>() == null)
                go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}