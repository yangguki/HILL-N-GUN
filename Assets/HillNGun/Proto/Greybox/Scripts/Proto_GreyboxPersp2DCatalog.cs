using System;
using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    [CreateAssetMenu(fileName = "Proto_GreyboxPersp2DCatalog", menuName = "HillNGun/Greybox Persp2D Catalog")]
    public class Proto_GreyboxPersp2DCatalog : ScriptableObject
    {
        public const string ArtFolder = "Assets/HillNGun/Proto/Greybox/Art/Persp2D";
        public const string ResourcesName = "Proto_GreyboxPersp2DCatalog";
        public const string EditorCatalogAssetPath = "Assets/HillNGun/Proto/Greybox/Resources/Proto_GreyboxPersp2DCatalog.asset";
        public const float ImportPpu = 235f;
        public const float CharacterCellPx = 512f;

        public TextAsset heroAnimJson;
        public TextAsset banditAnimJson;
        public Sprite charShadow;
        public Sprite sky;
        public Sprite road;

        [Serializable]
        public class BuildingSet
        {
            public string id;
            public Sprite back;
            public Sprite front;
            public Sprite sign;
            public float porchEdgeX;
            public float nearPorchZ;
        }

        public BuildingSet[] sceneBuildings;
        public Sprite[] props;
        public Sprite propShadow;

        [Header("FX (Art/Persp2D/FX)")]
        public Sprite fxTracerEnemyHead;
        public Sprite fxTracerEnemyTrail;
        public Sprite fxTracerPlayerHead;
        public Sprite fxTracerPlayerTrail;
        public Sprite[] fxMuzzleFlash;
        public Sprite[] fxGroundImpact;

        public TextAsset envLayoutJson;

        [Serializable]
        public class BakedPlane
        {
            public float nativeW;
            public float nativeH;
            public float topLeftX;
            public float topLeftY;
        }

        [Serializable]
        public class BakedBuilding
        {
            public string id;
            public float nativeW;
            public float nativeH;
            public float pivotX;
            public float pivotY;
            public float signOffsetX;
            public float signOffsetY;
            public float signScale;
            public float signNativeW;
            public float porchEdgeX;
            public float nearPorchZ;
        }

        [Serializable]
        public class BakedPropDef
        {
            public string name;
            public float nativeW;
            public float nativeH;
            public Sprite sprite;
        }

        [Serializable]
        public class BakedPropPlacement
        {
            public string name;
            public float pivotX;
            public float pivotY;
            public float jsonScale;
            public bool flipX;
        }

        [Serializable]
        public class BakedSpritePivot
        {
            public string fileContains;
            public Vector2 pivot;
        }

        public BakedPlane roadPlane;
        public BakedPlane skyPlane;
        public BakedBuilding[] bakedBuildings;
        public BakedPropDef[] propDefs;
        public BakedPropPlacement[] scenePropPlacements;
        public BakedSpritePivot[] spritePivots;

        [Serializable]
        public class DirFrames
        {
            public string dir;
            public Sprite[] frames;
        }

        [Serializable]
        public class CharacterAnims
        {
            public DirFrames[] idle = Array.Empty<DirFrames>();
            public DirFrames[] run = Array.Empty<DirFrames>();
            public DirFrames[] shoot = Array.Empty<DirFrames>();
            public DirFrames[] roll = Array.Empty<DirFrames>();
            public DirFrames[] hit = Array.Empty<DirFrames>();
            public DirFrames[] down = Array.Empty<DirFrames>();
        }

        public CharacterAnims hero;
        public CharacterAnims bandit;

        [Header("HUD (Art/Persp2D/UI, fallback Greybox3D for score/stage)")]
        public Sprite uiJoystickBase;
        public Sprite uiJoystickKnob;
        public Sprite uiJoystickBasePressed;
        public Sprite uiJoystickKnobPressed;
        public Sprite uiBtnAttack;
        public Sprite uiBtnReload;
        public Sprite uiBtnSkill;
        public Sprite uiBtnPause;
        public Sprite uiBtnRoll;
        public Sprite uiTextScore;
        public Sprite uiTextStage;
        public Sprite uiHeartFull;
        public Sprite uiHeartEmpty;
        public Sprite uiCrosshair;
        public Sprite uiBtnCooldownOverlay;
        public Sprite uiBtnAttackPressed;
        public Sprite uiBtnRollPressed;
        public Sprite uiBtnReloadPressed;
        public Sprite uiBtnSkillPressed;
        public Sprite uiBtnPausePressed;
        public Sprite uiIconAttack;
        public Sprite uiIconRoll;
        public Sprite uiIconReload;
        public Sprite uiIconSkill;
        public Sprite uiIconPause;
        public Sprite uiLabelAttack;
        public Sprite uiLabelRoll;
        public Sprite uiLabelReload;
        public Sprite uiLabelSkill;

        public bool IsGameplayReady =>
            road != null && sky != null && hero.idle != null && hero.idle.Length > 0
            && bandit.idle != null && bandit.idle.Length > 0;

        public void EnsureAnimJsonWarmed() => Proto_GreyboxPersp2DAnimJson.Warm(this);

        /// <summary>Required HUD sprites for Persp2D overlay (score/stage text sprites optional).</summary>
        public bool HudGameplayReady(out string missingFields)
        {
            var miss = new System.Collections.Generic.List<string>();
            if (uiJoystickBase == null) miss.Add(nameof(uiJoystickBase));
            if (uiJoystickKnob == null) miss.Add(nameof(uiJoystickKnob));
            if (uiBtnAttack == null) miss.Add(nameof(uiBtnAttack));
            if (uiBtnRoll == null) miss.Add(nameof(uiBtnRoll));
            if (uiBtnSkill == null) miss.Add(nameof(uiBtnSkill));
            if (uiBtnReload == null) miss.Add(nameof(uiBtnReload));
            if (uiHeartFull == null) miss.Add(nameof(uiHeartFull));
            if (uiHeartEmpty == null) miss.Add(nameof(uiHeartEmpty));
            if (uiBtnCooldownOverlay == null) miss.Add(nameof(uiBtnCooldownOverlay));
            if (uiIconAttack == null) miss.Add(nameof(uiIconAttack));
            if (uiBtnPause == null) miss.Add(nameof(uiBtnPause));
            if (uiIconPause == null) miss.Add(nameof(uiIconPause));
            if (uiIconRoll == null) miss.Add(nameof(uiIconRoll));
            if (uiIconReload == null) miss.Add(nameof(uiIconReload));
            if (uiIconSkill == null) miss.Add(nameof(uiIconSkill));
            if (uiLabelAttack == null) miss.Add(nameof(uiLabelAttack));
            if (uiLabelRoll == null) miss.Add(nameof(uiLabelRoll));
            if (uiLabelReload == null) miss.Add(nameof(uiLabelReload));
            if (uiLabelSkill == null) miss.Add(nameof(uiLabelSkill));
            missingFields = miss.Count > 0 ? string.Join(", ", miss) : null;
            return miss.Count == 0;
        }

        public Sprite[] FramesFor(string who, string anim, string dir)
        {
            var set = who == "bandit" ? bandit : hero;
            DirFrames[] list = anim switch
            {
                "run" => set.run,
                "shoot" => set.shoot,
                "roll" => set.roll,
                "hit" => set.hit,
                "down" => set.down,
                _ => set.idle
            };
            if (list == null) return null;
            foreach (var d in list)
                if (d != null && d.dir == dir)
                    return d.frames;
            return null;
        }

        public static Proto_GreyboxPersp2DCatalog LoadRuntime()
        {
#if UNITY_EDITOR
            var editorCat = UnityEditor.AssetDatabase.LoadAssetAtPath<Proto_GreyboxPersp2DCatalog>(EditorCatalogAssetPath);
            if (editorCat != null && editorCat.IsGameplayReady)
            {
                editorCat.EnsureEditorUiResolved();
                return editorCat;
            }
#endif
            var cat = Resources.Load<Proto_GreyboxPersp2DCatalog>(ResourcesName);
            if (cat != null && cat.IsGameplayReady)
            {
                cat.EnsureAnimJsonWarmed();
                return cat;
            }
            return null;
        }

#if UNITY_EDITOR
        static Sprite EditorUi(string file)
        {
            var path = $"{ArtFolder}/UI/{file}";
            var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;
            foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s) return s;
            return null;
        }

        public void EnsureEditorUiResolved()
        {
            uiBtnPause ??= EditorUi("hng_ui_btn_pause_normal.png");
            uiBtnPausePressed ??= EditorUi("hng_ui_btn_pause_pressed.png");
            uiIconPause ??= EditorUi("hng_ui_icon_pause_normal.png");
        }

        public Sprite UiBtnPauseOrEditor => uiBtnPause ?? EditorUi("hng_ui_btn_pause_normal.png");
        public Sprite UiBtnPausePressedOrEditor => uiBtnPausePressed ?? EditorUi("hng_ui_btn_pause_pressed.png");
        public Sprite UiIconPauseOrEditor => uiIconPause ?? EditorUi("hng_ui_icon_pause_normal.png");
#else
        public Sprite UiBtnPauseOrEditor => uiBtnPause;
        public Sprite UiBtnPausePressedOrEditor => uiBtnPausePressed;
        public Sprite UiIconPauseOrEditor => uiIconPause;
#endif

        public int FrameCount(string who, string anim)
        {
            var set = who == "bandit" ? bandit : hero;
            DirFrames[] list = anim switch
            {
                "run" => set.run,
                "shoot" => set.shoot,
                "roll" => set.roll,
                "hit" => set.hit,
                "down" => set.down,
                _ => set.idle
            };
            if (list == null || list.Length == 0) return 0;
            return list[0].frames?.Length ?? 0;
        }

        public bool ReferencesKenney()
        {
            return AssetPathContainsKenney(hero) || AssetPathContainsKenney(bandit);
        }

        static bool AssetPathContainsKenney(CharacterAnims a)
        {
            return ContainsKenney(a.idle) || ContainsKenney(a.run) || ContainsKenney(a.shoot)
                   || ContainsKenney(a.roll) || ContainsKenney(a.hit) || ContainsKenney(a.down);
        }

        static bool ContainsKenney(DirFrames[] dirs)
        {
            if (dirs == null) return false;
            foreach (var d in dirs)
            {
                if (d?.frames == null) continue;
                foreach (var s in d.frames)
                {
                    if (s == null) continue;
#if UNITY_EDITOR
                    var p = UnityEditor.AssetDatabase.GetAssetPath(s);
                    if (!string.IsNullOrEmpty(p) && p.IndexOf("Kenney", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
#endif
                }
            }
            return false;
        }
    }
}