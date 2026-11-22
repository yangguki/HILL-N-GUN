using HillNGun.Proto.Greybox;
using UnityEngine;
using UnityEngine.Scripting;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>
    /// Registers the 3D presenter factory. Must be a linker root: a type used only via
    /// RuntimeInitializeOnLoadMethod can be stripped from IL2CPP player builds, leaving
    /// Registry.Ensure() null so the View: 2D/3D toggle appears to do nothing.
    /// </summary>
    [Preserve]
    public static class Proto_Greybox3DViewBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterOnSubsystem() => Register();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterBeforeScene() => Register();

        public static void Register()
        {
            Proto_GreyboxViewPresenterRegistry.RegisterFactory(() => new Proto_Greybox3DViewHost());
        }
    }
}
