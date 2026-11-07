using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.PvE;
using HillNGun.PvP;
using HillNGun.Combat;
using HillNGun.Cover;
using HillNGun.Characters;

namespace HillNGun.DebugTools
{
    /// <summary>
    /// -smoketest-terrain (with -smoketest-fullpve / -smoketest-fullpvp): 12 §8 P0 terrain smoke.
    /// For every PvE stage (and PvP round 1) at -terrain-shot-at seconds: layout / coverage / safe-width / actor cover
    /// status / barrel log + screenshot "terrain_{stage}_{W}x{H}.png" (persistentDataPath/scale_smoke). Stage summaries
    /// log unique barrel explosions and cover destroys. -terrain-first-only quits after the first shot.
    /// Screen size here is a desktop window size; it simulates an aspect ratio only, never a real device.
    /// </summary>
    public class TerrainSmokeRunner : MonoBehaviour
    {
        private static bool _wanted, _firstOnly;
        private static float _shotAt = 6f;
        private string _dir;
        private string _lastShotKey;
        private int _destroys, _barrelEvents;
        private CombatSystem _hooked;
        private float _sceneT;
        private string _scene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-smoketest-terrain", StringComparison.OrdinalIgnoreCase)) _wanted = true;
                if (args[i].Equals("-terrain-first-only", StringComparison.OrdinalIgnoreCase)) _firstOnly = true;
                if (args[i].Equals("-terrain-shot-at", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length &&
                    float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float t)) _shotAt = t;
            }
            if (!_wanted) return;
            var go = new GameObject("[TerrainSmokeRunner]");
            DontDestroyOnLoad(go);
            go.AddComponent<TerrainSmokeRunner>();
        }

        private void Awake()
        {
            _dir = Path.Combine(Application.persistentDataPath, "scale_smoke");
            Directory.CreateDirectory(_dir);
            HillNGun.Settings.SettingsService.Instance.Data.tutorialDone = true; // in-memory only
            Debug.Log($"[TerrainSmoke] start screen={Screen.width}x{Screen.height} aspect={(float)Screen.width / Screen.height:F3} (desktop window = simulated aspect, not a device) coverDebug={TerrainRules.CoverDebug} layout={TerrainRules.Data.layoutId} v{TerrainRules.Data.layoutVersion}");
        }

        private void Update()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene != _scene) { _scene = scene; _sceneT = 0f; }
            _sceneT += Time.deltaTime;
            if (!scene.EndsWith("_Match")) return;
            var combat = ServiceLocator.Get<CombatSystem>();
            if (combat == null) return;
            if (_hooked != combat)
            {
                _hooked = combat;
                combat.OnCoverDestroyed += c => _destroys++;
            }
            var pve = FindFirstObjectByType<PvEMatchManager>();
            var pvp = pve == null ? FindFirstObjectByType<PvPMatchManager>() : null;
            string key; float t;
            if (pve != null) { if (!pve.IsMatchActive) return; key = pve.CurrentStageId ?? "pve"; t = pve.StageTimer; }
            else if (pvp != null) { if (!pvp.IsMatchActive || pvp.CurrentRound != 1) return; key = "pvp_r1"; t = _sceneT; }
            else return;
            if (key == _lastShotKey || t < _shotAt) return;
            _lastShotKey = key;
            LogLayout(key, combat, pve);
            string path = Path.Combine(_dir, $"terrain_{key}_{Screen.width}x{Screen.height}{(TerrainRules.CoverDebug ? "_debug" : "")}.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[TerrainSmoke] SHOT {path} t={t:F1}s");
            if (_firstOnly) StartCoroutine(QuitSoon());
        }

        private void LogLayout(string key, CombatSystem combat, PvEMatchManager pve)
        {
            float actorR = 0.5f * ScalePresets.CharacterMul;
            float sumP = 0f, sumE = 0f; int nP = 0, nE = 0;
            foreach (var c in combat.Covers)
            {
                if (c == null) continue;
                float inc = CoverSafeDebugView.IncomingRadiusFor(c.Team, combat);
                float h = TerrainLayout.SafeHalfWidth(c.Width, actorR, inc);
                if (c.Team == Team.Player) { sumP += c.Width; nP++; } else { sumE += c.Width; nE++; }
                Debug.Log($"[TerrainSmoke] COVER {key} {c.InstanceId} {c.Data.id} class={c.HeightClass} team={c.Team} row={c.RowIndex} x={c.Position.x:F2} y={c.Position.y:F2} w={c.Width:F2} hp={c.CurrentHp}/{c.Data.maxHp} " +
                          $"safeHalf={(h >= 0f ? h.ToString("F3") : "none")} actorR={actorR:F3} incomingR={inc:F2}");
            }
            // perf evidence: which texture each cover draws from (atlas page vs individual PNG)
            var texSeen = new System.Collections.Generic.HashSet<int>();
            foreach (var v in FindObjectsByType<CoverView>(FindObjectsSortMode.None))
            {
                var sr = v.Renderer; if (sr == null || sr.sprite == null) continue;
                var tex = sr.sprite.texture; if (tex == null || !texSeen.Add(tex.GetInstanceID())) continue;
                Debug.Log($"[TerrainSmoke] COVERTEX {key} sprite={sr.sprite.name} packed={sr.sprite.packed} tex={tex.name} {tex.width}x{tex.height} fmt={tex.format} order={sr.sortingOrder} mat={sr.sharedMaterial?.name}");
            }
            int rows = 3;
            Debug.Log($"[TerrainSmoke] COVERAGE {key} player={nP} covers sum={sumP:F2} -> {sumP / (rows * 24f):P1} of {rows}x24 (runtime widths) enemy={nE} sum={sumE:F2} spawnerKey={CoverFieldSpawner.LastLayoutKey} spawned={CoverFieldSpawner.LastSpawnCount}");
            foreach (var ch in combat.Characters)
            {
                if (ch == null) continue;
                string st = CoverSafeDebugView.ActorCoverStatus(ch, combat, out var cv);
                Debug.Log($"[TerrainSmoke] ACTOR {key} {ch.Data?.id} team={ch.Team} pos=({ch.Position.x:F2},{ch.Position.y:F2}) hitR={ch.HitRadius:F3} cover={(cv != null ? cv.InstanceId : "-")} status={(st.Length > 0 ? st : "open")} behindFull={PlaceholderCharacterView.IsBehindFullCover(ch, combat.Covers)}");
            }
            if (pve != null && pve.Hazards != null)
                foreach (var b in pve.Hazards)
                    { var hb = b; Debug.Log($"[TerrainSmoke] BARREL {key} {hb.InstanceId} pos=({hb.Position.x:F1},{hb.Position.y:F1}) state={hb.State} acc={hb.Accumulated}"); }
            var rules = ServiceLocator.Get<MatchRulesData>() ?? RuntimeAssetManager.Instance?.MatchRules;
            Debug.Log($"[TerrainSmoke] RULES supply={rules?.supplyCrateEnabled} decay={rules?.lateCoverDecayEnabled} pvpCoverDrops={TerrainRules.Data.pvpCoverDropsEnabled} bulletMul={WeaponData.GlobalProjectileSpeedMul:0.00} destroysSoFar={_destroys}" +
                      (pve != null ? $" barrelsExplodedThisStage={pve.BarrelExplosions}" : ""));
        }

        private System.Collections.IEnumerator QuitSoon()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            Debug.Log("[TerrainSmoke] DONE first-only");
            Application.Quit();
        }
    }
}
