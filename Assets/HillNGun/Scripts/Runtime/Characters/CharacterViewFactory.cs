using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Characters
{
    /// <summary>
    /// Creates the view for a character. Logic never knows which view it gets (FP 1089: "로직은 Spine을 모르고 CharacterView만 Spine을 앎").
    /// STUB-EXTERNAL: a Spine view (spine-unity runtime, front/back rigs, skin names skin_(id)_(type) per FP 1156)
    /// needs a Spine license that is not purchased (05 §5.4), so only the placeholder sprite view exists.
    /// </summary>
    public static class CharacterViewFactory
    {
        private static bool _warned;

        public static ICharacterView Create(GameObject viewGo)
        {
            var cfg = ServiceLocator.Get<ExternalServicesData>() ?? RuntimeAssetManager.Instance?.ExternalServices;
            if (cfg != null && cfg.useSpineCharacterViews && !_warned)
            {
                _warned = true;
                Debug.LogWarning("[View][STUB] useSpineCharacterViews is on but the Spine runtime is not installed -> placeholder sprite view");
            }
            // Real art: return viewGo.AddComponent<SpineCharacterView>();  (implements ICharacterView)
            return viewGo.AddComponent<PlaceholderCharacterView>();
        }
    }
}
