using UnityEngine;
using HillNGun.Settings;
using HillNGun.Progression;

namespace HillNGun.UI
{
    /// <summary>
    /// Settings panel content (IMGUI, theme tokens), shared by the lobby tab and the pause menu.
    /// Audio (on/off, master, music, effects), vibration, screen shake, button size (3 steps), language, tutorial replay.
    /// View only: all values live in SettingsService (saved locally).
    /// </summary>
    public class SettingsView
    {
        public bool AllowProgressReset;
        private bool _dirty;
        private float _resetArmedUntil = -1f;
        public string Toast { get; private set; }
        public float ToastUntil { get; private set; }

        public void Draw(Rect area)
        {
            var svc = SettingsService.Instance;
            var d = svc.Data;
            var t = UIKit.Theme;
            float row = 46f, gap = 8f;
            float colW = (area.width - 30f) / 2f;
            Rect L = new Rect(area.x + 10f, area.y + 10f, colW, area.height - 20f);
            Rect R = new Rect(L.xMax + 10f, L.y, colW, L.height);

            // ---- left column: sound ----
            float y = L.y;
            UIKit.Header(new Rect(L.x, y, L.width, 34f), UIKit.T("settings.sound", "소리")); y += 40f;
            bool soundOn = UIKit.Toggle(new Rect(L.x, y, L.width, row), UIKit.T("settings.sound_on", "소리 켜기"), d.soundOn); y += row + gap;
            if (soundOn != d.soundOn) { d.soundOn = soundOn; svc.Commit(); }
            Slide(ref d.masterVolume, new Rect(L.x, y, L.width, row), UIKit.T("settings.master", "전체 볼륨")); y += row + gap;
            Slide(ref d.musicVolume, new Rect(L.x, y, L.width, row), UIKit.T("settings.music", "배경음")); y += row + gap;
            Slide(ref d.sfxVolume, new Rect(L.x, y, L.width, row), UIKit.T("settings.sfx", "효과음")); y += row + gap + 6f;

            UIKit.Header(new Rect(L.x, y, L.width, 34f), UIKit.T("settings.feel", "화면·진동 (접근성)")); y += 40f;
            bool vib = UIKit.Toggle(new Rect(L.x, y, L.width, row), UIKit.T("settings.vibration", "진동"), d.vibrationOn); y += row + gap;
            if (vib != d.vibrationOn) { d.vibrationOn = vib; svc.Commit(); }
            bool shake = UIKit.Toggle(new Rect(L.x, y, L.width, row), UIKit.T("settings.shake", "화면 흔들림"), d.screenShakeOn); y += row + gap;
            if (shake != d.screenShakeOn) { d.screenShakeOn = shake; svc.Commit(); }

            // ---- right column: controls / language / misc ----
            y = R.y;
            UIKit.Header(new Rect(R.x, y, R.width, 34f), UIKit.T("settings.controls", "조작")); y += 40f;
            UIKit.Body(new Rect(R.x, y, R.width, 30f), UIKit.T("settings.button_size", "버튼 크기")); y += 34f;
            string[] sizes = { UIKit.T("settings.size_s", "작게"), UIKit.T("settings.size_m", "보통"), UIKit.T("settings.size_l", "크게") };
            float bw = (R.width - 20f) / 3f;
            for (int i = 0; i < 3; i++)
                if (UIKit.Button(new Rect(R.x + i * (bw + 10f), y, bw, 42f), sizes[i], true, d.buttonSizeIndex == i ? UIKit.ButtonKind.TabActive : UIKit.ButtonKind.Tab))
                { d.buttonSizeIndex = i; svc.Commit(); }
            y += 52f;

            UIKit.Header(new Rect(R.x, y, R.width, 34f), UIKit.T("settings.language", "언어")); y += 40f;
            string[] langNames = { "한국어", "English" };
            for (int i = 0; i < SettingsService.Languages.Length; i++)
            {
                string code = SettingsService.Languages[i];
                if (UIKit.Button(new Rect(R.x + i * (bw + 10f), y, bw, 42f), langNames[i], true, d.language == code ? UIKit.ButtonKind.TabActive : UIKit.ButtonKind.Tab))
                    svc.SetLanguage(code);
            }
            y += 46f;
            UIKit.Small(new Rect(R.x, y, R.width, 22f), UIKit.T("settings.language_note", "번역은 출시 전 (영어는 일부 화면만)")); y += 32f;

            UIKit.Header(new Rect(R.x, y, R.width, 34f), UIKit.T("settings.misc", "기타")); y += 40f;
            if (UIKit.Button(new Rect(R.x, y, R.width, 42f), d.tutorialDone ? UIKit.T("settings.tutorial_replay", "튜토리얼 다시 보기") : UIKit.T("settings.tutorial_pending", "튜토리얼: 다음 PvE 1-1에서 표시"), d.tutorialDone))
            {
                svc.ResetTutorial();
                ShowToast(UIKit.T("settings.tutorial_reset_done", "다음 PvE 1-1에서 튜토리얼을 다시 보여줍니다"));
            }
            y += 50f;
            if (AllowProgressReset)
            {
                bool armed = Time.unscaledTime < _resetArmedUntil;
                if (UIKit.Button(new Rect(R.x, y, R.width, 42f), armed ? UIKit.T("settings.reset_confirm", "한 번 더 누르면 진행이 모두 지워집니다") : UIKit.T("settings.reset_progress", "진행 초기화"), true))
                {
                    if (armed)
                    {
                        ProgressionService.Instance.ResetAll();
                        _resetArmedUntil = -1f;
                        ShowToast(UIKit.T("settings.reset_done", "진행을 초기화했습니다"));
                    }
                    else _resetArmedUntil = Time.unscaledTime + 3f;
                }
            }

            if (_dirty && Event.current.type == EventType.MouseUp) { _dirty = false; svc.Commit(); }
        }

        private void Slide(ref float field, Rect r, string label)
        {
            float v = UIKit.Slider(r, label, field);
            if (Mathf.Approximately(v, field)) return;
            field = v;
            _dirty = true;
            SettingsService.Instance.Apply(); // live preview; saved on release
        }

        public void Flush()
        {
            if (_dirty) { _dirty = false; SettingsService.Instance.Commit(); }
        }

        private void ShowToast(string s) { Toast = s; ToastUntil = Time.unscaledTime + 2.5f; }
    }
}
