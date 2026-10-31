using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.PvE;
using HillNGun.Settings;
using HillNGun.External;

namespace HillNGun.UI
{
    /// <summary>
    /// First-PvE onboarding (FP 202 "PvE가 튜토리얼을 겸함", D13). Steps come from TutorialData; each waits for the
    /// action (move / row change / shoot / roll / grenade / skill) or times out. Shown once, replay from Settings.
    /// View + small observer only: it never drives the character.
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        public static bool ForceShow; // smoke test hook

        private TutorialData _data;
        private PvEMatchManager _pve;
        private CharacterStateMachine _player;
        private int _step = -1;
        private float _stepTime;
        private float _delay;
        private bool _active;
        private bool _actionDone;
        private Vector2 _startPos;
        private int _startRow;
        private int _startGrenades;

        public bool IsActive => _active;
        public int StepIndex => _step;
        public int StepCount => _data?.steps?.Length ?? 0;

        private void Start()
        {
            _data = ServiceLocator.Get<TutorialData>() ?? RuntimeAssetManager.Instance?.Tutorial;
            _pve = FindFirstObjectByType<PvEMatchManager>();
            var settings = SettingsService.Instance.Data;
            bool want = _data != null && _data.enabled && _data.steps != null && _data.steps.Length > 0 && (!settings.tutorialDone || ForceShow);
            if (!want || _pve == null) { enabled = false; return; }
            _delay = _data.startDelay;
        }

        private void Update()
        {
            if (_pve == null || _data == null) return;
            if (!_active)
            {
                if (_pve.CurrentStageIndex != _data.stageIndex) { enabled = false; return; }
                if (_pve.PlayerTeam == null || _pve.PlayerTeam.Count == 0) return;
                _delay -= Time.deltaTime;
                if (_delay > 0f) return;
                _player = _pve.PlayerTeam[Mathf.Clamp(_pve.PlayerControlledIndex, 0, _pve.PlayerTeam.Count - 1)];
                _player.OnFired += (c, d, dmg) => Mark(TutorialTrigger.Shoot);
                _player.OnRollStarted += c => Mark(TutorialTrigger.Roll);
                _player.OnSkillUsed += c => Mark(TutorialTrigger.Skill);
                _active = true;
                ExternalServices.Log("tutorial_start");
                Next();
                return;
            }

            _stepTime += Time.unscaledDeltaTime;
            var step = _data.steps[_step];
            if (_player != null)
            {
                if (step.trigger == TutorialTrigger.Move && Mathf.Abs(_player.Position.x - _startPos.x) > 0.8f) _actionDone = true;
                if (step.trigger == TutorialTrigger.RowChange && _player.CurrentRowIndex != _startRow) _actionDone = true;
                if (step.trigger == TutorialTrigger.Grenade && _player.GrenadeSystem != null && _player.GrenadeSystem.CurrentGrenades < _startGrenades) _actionDone = true;
            }
            bool timeUp = step.maxSeconds > 0f && _stepTime >= step.maxSeconds;
            bool timerStep = step.trigger == TutorialTrigger.Timer && _stepTime >= step.minSeconds;
            if (_stepTime >= step.minSeconds && (_actionDone || timeUp || timerStep)) Next();
        }

        private void Mark(TutorialTrigger t)
        {
            if (_active && _step >= 0 && _step < StepCount && _data.steps[_step].trigger == t) _actionDone = true;
        }

        public void Next()
        {
            _step++;
            if (_step >= StepCount) { Finish(false); return; }
            _stepTime = 0f;
            _actionDone = false;
            if (_player != null)
            {
                _startPos = _player.Position;
                _startRow = _player.CurrentRowIndex;
                _startGrenades = _player.GrenadeSystem != null ? _player.GrenadeSystem.CurrentGrenades : 0;
            }
            ExternalServices.Log("tutorial_step", new Dictionary<string, object> { { "step", _step }, { "trigger", _data.steps[_step].trigger } });
        }

        public void Finish(bool skipped)
        {
            if (!_active) return;
            _active = false;
            var s = SettingsService.Instance;
            s.Data.tutorialDone = true;
            s.Commit();
            ExternalServices.Log("tutorial_end", new Dictionary<string, object> { { "skipped", skipped }, { "step", _step } });
            Debug.Log($"[Tutorial] {(skipped ? "skipped" : "finished")} at step {_step}/{StepCount}");
            enabled = false;
        }

        private static bool IsTouch()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return UnityEngine.InputSystem.Touchscreen.current != null;
#endif
        }

        private void OnGUI()
        {
            if (!_active || _step < 0 || _step >= StepCount) return;
            GUI.depth = -20;
            Vector2 size = UIKit.Begin();
            var t = UIKit.Theme;
            var step = _data.steps[_step];
            string key = !IsTouch() && !string.IsNullOrEmpty(step.textKeyDesktop) ? step.textKeyDesktop : step.textKey;
            Rect box = new Rect(size.x / 2f - 380f, 56f, 760f, 112f);
            UIKit.Fill(box, new Color(0.08f, 0.06f, 0.04f, 0.88f));
            UIKit.Border(box, t.accent, 3f);
            UIKit.Label(new Rect(box.x + 16f, box.y + 6f, 300f, 26f), UIKit.TF("tutorial.header", "조작 익히기 {0}/{1}", _step + 1, StepCount), t.fontSmall, t.accent, TextAnchor.MiddleLeft, true);
            UIKit.Label(new Rect(box.x + 16f, box.y + 32f, box.width - 32f, 70f), UIKit.T(key, key), t.fontBody + 2, t.text, TextAnchor.MiddleCenter, true, true);
            if (_actionDone)
                UIKit.Label(new Rect(box.xMax - 120f, box.y + 6f, 104f, 26f), UIKit.T("tutorial.good", "좋아요!"), t.fontSmall, t.good, TextAnchor.MiddleRight, true);
            if (UIKit.Button(new Rect(box.xMax + 10f, box.y, 130f, 38f), UIKit.T("tutorial.skip", "건너뛰기"), true, UIKit.ButtonKind.Normal, t.fontSmall))
                Finish(true);
            UIKit.End();
        }
    }
}
