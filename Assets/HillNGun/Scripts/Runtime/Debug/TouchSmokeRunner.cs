using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Input;
using HillNGun.PvE;
using HillNGun.Characters;
using HillNGun.Combat;
using HillNGun.Progression;
using HillNGun.Settings;
using HillNGun.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace HillNGun.DebugTools
{
    /// <summary>
    /// -smoketest-touch: drives a real build with VIRTUAL multitouch (an Input System Touchscreen device fed with
    /// TouchState events, i.e. the same EnhancedTouch path a phone uses). Also exercises the gun shop / loadout and
    /// field drops. Logs "[TouchSmoke] PASS|FAIL name" per check and a SUMMARY line, saves screenshots to
    /// persistentDataPath/smoketest_touch/ (suffix = window size), then quits.
    /// Optional: -smoketest-res 2560x1600 (window size request).
    /// </summary>
    public class TouchSmokeRunner : MonoBehaviour
    {
        private static bool _enabled;
        private static Vector2Int _res;
        private Touchscreen _ts;
        private string _dir;
        private int _pass, _fail;
        private int _fired;
        private CharacterStateMachine _pc;
        private MobileTouchControls _mtc;
        private PvEMatchManager _pve;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-smoketest-touch", StringComparison.OrdinalIgnoreCase)) _enabled = true;
                if (args[i].Equals("-smoketest-res", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    var p = args[i + 1].ToLowerInvariant().Split('x');
                    if (p.Length == 2 && int.TryParse(p[0], out int w) && int.TryParse(p[1], out int h)) _res = new Vector2Int(w, h);
                }
            }
            if (!_enabled) return;
            ProgressionService.SaveFileOverride = "smoketest_touch_save.json";
            SettingsService.SaveFileOverride = "smoketest_touch_settings.json";
            try { File.Delete(Path.Combine(Application.persistentDataPath, "smoketest_touch_save.json")); } catch { }
            MobileTouchControls.ForceTouch = true;
            var go = new GameObject("[TouchSmokeRunner]");
            DontDestroyOnLoad(go);
            go.AddComponent<TouchSmokeRunner>();
            Debug.Log("[TouchSmoke] enabled");
        }

        private void Start()
        {
            if (_res.x > 0) Screen.SetResolution(_res.x, _res.y, FullScreenMode.Windowed);
            _dir = Path.Combine(Application.persistentDataPath, "smoketest_touch");
            Directory.CreateDirectory(_dir);
            foreach (var f in Directory.GetFiles(_dir, "*.png")) File.Delete(f);
            SettingsService.Instance.Data.tutorialDone = true;
            Application.logMessageReceived += (c, s, t) => { if (t == LogType.Exception || t == LogType.Error) Debug.Log($"[TouchSmoke] Caught {t}: {c}"); };
            StartCoroutine(Run());
        }

        // ---------------------------------------------------------------- helpers
        private void Check(string name, bool ok, string detail = "")
        {
            if (ok) _pass++; else _fail++;
            Debug.Log($"[TouchSmoke] {(ok ? "PASS" : "FAIL")} {name} {detail}");
        }

        private string Suffix => $"{Screen.width}x{Screen.height}";

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string p = Path.Combine(_dir, $"{name}_{Suffix}.png");
            ScreenCapture.CaptureScreenshot(p);
            Debug.Log($"[TouchSmoke] screenshot {p}");
            yield return null;
        }

        private void Touch(int id, TouchPhase phase, Vector2 pos)
        {
            InputSystem.QueueStateEvent(_ts, new TouchState { touchId = id, phase = phase, position = pos, pressure = 1f });
        }

        private IEnumerator Drag(int id, Vector2 from, Vector2 to, float seconds)
        {
            int steps = Mathf.Max(2, Mathf.RoundToInt(seconds * 30f));
            for (int i = 1; i <= steps; i++)
            {
                Touch(id, TouchPhase.Moved, Vector2.Lerp(from, to, i / (float)steps));
                yield return null;
            }
        }

        private static IEnumerator Wait(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }

        private TouchControlRouter R => _mtc != null ? _mtc.Router : null;

        // ---------------------------------------------------------------- script
        private IEnumerator Run()
        {
            _ts = InputSystem.AddDevice<Touchscreen>("VirtualTouch");
            yield return Wait(1.5f);
            Debug.Log($"[TouchSmoke] window {Suffix} safe={Screen.safeArea}");

            // ---- lobby: gun shop + loadout ----
            float t0 = Time.realtimeSinceStartup;
            while (FindFirstObjectByType<LobbyUI>() == null && Time.realtimeSinceStartup - t0 < 20f)
            {
                if (SceneManager.GetActiveScene().name.Contains("Title")) GameFlowManager.Instance?.GoToModeSelect();
                yield return Wait(0.5f);
            }
            var lobby = FindFirstObjectByType<LobbyUI>();
            Check("lobby_loaded", lobby != null);
            var prog = ProgressionService.Instance;
            prog.Data.bounty = 6000; prog.Save();
            if (lobby != null)
            {
                ShopPanel.ShowGuns = true;
                lobby.SetTab(LobbyTab.Shop);
                yield return Wait(0.8f);
                yield return Shot("shop_guns");
            }
            bool bought = prog.TryPurchaseWeapon("wpn_peacemaker");
            bool bought2 = prog.TryPurchaseWeapon("wpn_gatling");
            bool equipped = prog.EquipWeapon("volt", "wpn_peacemaker");
            bool wrongChar = prog.EquipWeapon("raven", "wpn_peacemaker");
            Check("gun_purchase", bought && bought2 && prog.Data.bounty == 6000 - 1800 - 3500, $"bounty=${prog.Data.bounty}");
            Check("gun_equip_role_lock", equipped && !wrongChar);
            prog.Load(); // re-read the save file
            Check("gun_persisted", prog.OwnsWeapon("wpn_peacemaker") && prog.OwnsWeapon("wpn_gatling") && prog.GetEquippedWeapon("volt")?.id == "wpn_peacemaker"
                  && prog.GetEquippedWeapon("buster") == null, $"volt={prog.GetEquippedWeapon("volt")?.id}");
            if (lobby != null)
            {
                yield return Wait(0.5f);
                yield return Shot("shop_guns_owned");
                lobby.SetTab(LobbyTab.Characters);
                lobby.Characters.Selected = 2; // volt
                yield return Wait(0.8f);
                yield return Shot("loadout_volt");
                lobby.Characters.Selected = 0; // buster
                yield return Wait(0.6f);
                yield return Shot("loadout_buster");
                lobby.StartPvE(0);
                yield return Wait(1.5f);
            }
            var flow = GameFlowManager.Instance;
            flow.SetAllCharacters("volt", "buster", "raven");
            flow.StartMatch();

            // ---- match ----
            t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 20f)
            {
                _pve = FindFirstObjectByType<PvEMatchManager>();
                _mtc = ServiceLocator.Get<MobileTouchControls>();
                if (_pve != null && _mtc != null && R != null && _pve.PlayerTeam.Count > 0) break;
                yield return Wait(0.3f);
            }
            Check("touch_controls_active", _mtc != null && _mtc.IsActive && R != null, $"layout={(R != null ? R.Layout.Valid : false)}");
            if (R == null) { Finish(); yield break; }
            _pc = _pve.PlayerTeam[_pve.PlayerControlledIndex];
            Check("equipped_gun_in_match", _pc.Data.weapon != null && _pc.Data.weapon.id == "wpn_peacemaker", _pc.Data.weapon?.id);
            _pc.OnFired += (c, d, dmg) => _fired++;
            yield return Wait(3.5f); // enemies walk in
            var L = R.Layout;
            Debug.Log($"[TouchSmoke] layout scale={L.Scale:F2} tablet={L.IsTablet} attack={L.Attack} roll={L.Roll} skill={L.Skill} grenade={L.Grenade} moveRest={L.MoveRest}");
            bool overlap = false;
            foreach (var z in HudZones.Collect()) if (z.Overlaps(L.BoundsOfRightCluster())) overlap = true;
            Check("layout_clear_of_hud", !overlap);
            Check("layout_inside_safe_area", Screen.safeArea.Contains(L.Attack) && Screen.safeArea.Contains(L.Grenade) && Screen.safeArea.Contains(L.Roll) && Screen.safeArea.Contains(L.Skill));

            // T1 move (finger 1 holds the stick to the right)
            Vector2 m0 = L.MoveRest;
            Vector2 m1 = m0 + new Vector2(L.MoveRadius * 0.9f, 0f);
            float x0 = _pc.Position.x;
            Touch(1, TouchPhase.Began, m0); yield return null;
            yield return Drag(1, m0, m1, 0.15f);
            yield return Wait(0.5f);
            Check("move_stick", _pc.Position.x > x0 + 0.4f && R.MoveActive, $"dx={_pc.Position.x - x0:F2}");

            // T2 tap = auto-aim one shot, movement continues
            int f0 = _fired, s0 = R.ShotsSent; float x1 = _pc.Position.x;
            Touch(2, TouchPhase.Began, L.Attack); yield return Wait(0.06f);
            Touch(2, TouchPhase.Ended, L.Attack); yield return Wait(0.35f);
            Check("tap_fires_one_autoaim_shot", _fired == f0 + 1 && R.AutoAimShots >= 1, $"fired={_fired - f0} sent={R.ShotsSent - s0}");
            Check("moving_while_attacking", _pc.Position.x > x1 + 0.2f, $"dx={_pc.Position.x - x1:F2}");
            yield return Shot("bullets_in_flight_tap");

            // T3 drag aims (aim line, no shot while dragging), release = exactly one shot
            yield return Wait(0.5f);
            int f1 = _fired;
            Vector2 a1 = L.Attack + new Vector2(-L.AttackRadius * 0.5f, L.AttackRadius * 0.9f);
            Touch(2, TouchPhase.Began, L.Attack); yield return null;
            yield return Drag(2, L.Attack, a1, 0.2f);
            yield return Wait(0.35f);
            bool aimShown = _mtc.Overlay != null && (_mtc.Overlay.LastMask & 1) != 0;
            Check("drag_shows_aim_line_no_fire", aimShown && _fired == f1, $"mask={_mtc.Overlay?.LastMask} firedDuringDrag={_fired - f1}");
            yield return Shot("touch_aim_line");
            Touch(2, TouchPhase.Ended, a1); yield return Wait(0.12f);
            yield return Shot("bullets_in_flight");
            yield return Wait(0.3f);
            Check("release_fires_one_shot", _fired == f1 + 1, $"fired={_fired - f1}");

            // T4 three fingers: move held + attack aim held + grenade drag (radius circle), release throws
            int g0 = _pc.GrenadeSystem != null ? _pc.GrenadeSystem.CurrentGrenades : 0;
            Touch(2, TouchPhase.Began, L.Attack); yield return null;
            yield return Drag(2, L.Attack, a1, 0.1f);
            Vector2 gEnd = L.Grenade + new Vector2(-L.GrenadeDragFullPx * 0.25f, L.GrenadeDragFullPx * 0.7f);
            Touch(3, TouchPhase.Began, L.Grenade); yield return null;
            yield return Drag(3, L.Grenade, gEnd, 0.25f);
            yield return Wait(0.25f);
            bool grenadeShown = _mtc.Overlay != null && (_mtc.Overlay.LastMask & 2) != 0;
            Check("three_pointers_tracked", R.ActivePointers == 3 && R.MoveActive && R.AttackActive && R.GrenadeActive, $"pointers={R.ActivePointers}");
            Check("grenade_radius_shown", grenadeShown && g0 > 0, $"mask={_mtc.Overlay?.LastMask} grenades={g0} radius={_mtc.Overlay?.LastGrenadeRadius}");
            yield return Shot("touch_grenade_radius");
            Touch(3, TouchPhase.Ended, gEnd); yield return Wait(0.2f);
            int g1 = _pc.GrenadeSystem != null ? _pc.GrenadeSystem.CurrentGrenades : 0;
            Check("grenade_thrown_on_release", R.GrenadesThrown >= 1 && g1 == g0 - 1, $"thrown={R.GrenadesThrown} {g0}->{g1}");
            Touch(2, TouchPhase.Ended, a1); yield return Wait(0.3f);

            // T5 roll goes the move direction (right) while finger 1 holds
            yield return Wait(Mathf.Max(0f, _pc.RollCooldownRatio * 5f) + 0.2f);
            float xr = _pc.Position.x;
            bool rolled = false;
            Touch(4, TouchPhase.Began, L.Roll); yield return null;
            for (int i = 0; i < 10; i++) { if (_pc.MovementState == MovementState.Rolling) rolled = true; yield return null; }
            Touch(4, TouchPhase.Ended, L.Roll);
            yield return Wait(0.4f);
            Check("roll_in_move_direction", rolled && _pc.Position.x > xr + 1f && R.Rolls >= 1, $"dx={_pc.Position.x - xr:F2}");

            // T6 skill button
            int sp = R.SkillPresses;
            Touch(5, TouchPhase.Began, L.Skill); yield return Wait(0.08f);
            Touch(5, TouchPhase.Ended, L.Skill); yield return Wait(0.3f);
            Check("skill_button", R.SkillPresses == sp + 1);

            // T7 release everything: stops, nothing stuck
            Touch(1, TouchPhase.Ended, m1);
            yield return Wait(0.4f);
            Check("all_released_stops", R.ActivePointers == 0 && !R.MoveActive && _pc.MovementState != MovementState.Moving, $"state={_pc.MovementState}");
            yield return Shot("touch_idle_controls");

            // T8 field drops: one of each type next to the player, then walk over them
            var dd = DropDirector.Instance;
            Check("drop_director", dd != null && dd.System != null);
            if (dd != null)
            {
                var table = dd.System.Table;
                float px = _pc.Position.x;
                float dir = px < 12f ? 1f : -1f;
                for (int i = 0; i < table.entries.Length; i++)
                    dd.System.Spawn(table.entries[i], _pc.Position + new Vector2(dir * (1.5f + i * 0.9f), 7f), new Vector2(px + dir * (1.5f + i * 0.9f), _pc.Position.y), Team.Player);
                yield return Wait(0.8f);
                yield return Shot("drops_on_ground");
                _pc.TakeDamage(Mathf.Max(1, _pc.MaxHp / 3), Vector2.down); // so the heal is useful
                int before = dd.System.PickedUp;
                Vector2 to = m0 + new Vector2(dir * L.MoveRadius, 0f);
                Touch(1, TouchPhase.Began, m0); yield return null;
                yield return Drag(1, m0, to, 0.1f);
                yield return Wait(2.2f);
                Touch(1, TouchPhase.Ended, to);
                yield return Wait(0.3f);
                Check("walk_over_pickup", dd.System.PickedUp > before, dd.System.Summary);
            }

            // T9 destroy a cover: debris effect, drop roll, no longer blocks bullets
            var combat = ServiceLocator.Get<CombatSystem>();
            HillNGun.Cover.CoverInstance cov = null;
            if (combat != null) foreach (var c in combat.Covers) if (c != null && !c.IsDestroyed) { cov = c; break; }
            if (cov != null)
            {
                cov.TakeDamage(999999);
                yield return Wait(0.12f);
                yield return Shot("cover_destroyed_debris");
                Check("cover_destroyed_no_block", cov.IsDestroyed && !combat.CoverBlocksSegment(cov.Position + Vector2.down * 1.5f, cov.Position + Vector2.up * 1.5f, Team.Enemy), "");
            }

            // enemy dodge activity (informational)
            int dodges = 0;
            foreach (var e in _pve.ActiveEnemies) if (e != null) dodges += e.DodgeCount;
            Debug.Log($"[TouchSmoke] enemy dodges so far={dodges} router: requested={R.ShotsRequested} sent={R.ShotsSent} queued={R.ShotsQueued} dropped={R.ShotsDropped} auto={R.AutoAimShots}");
            Finish();
        }

        private void Finish()
        {
            Debug.Log($"[TouchSmoke] SUMMARY pass={_pass} fail={_fail} window={Suffix}");
            StartCoroutine(QuitSoon());
        }

        private IEnumerator QuitSoon()
        {
            yield return Wait(1f);
            Application.Quit(_fail == 0 ? 0 : 1);
        }
    }
}
