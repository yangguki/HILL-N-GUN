using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Progression;
using HillNGun.External;
using HillNGun.Characters;

namespace HillNGun.UI
{
    public enum LobbyTab { Home, Stages, Characters, Shop, Settings }

    /// <summary>
    /// Lobby / home hub (ModeSelect scene). Top bar = sheriff badge + bounty, tabs = 홈 / 스테이지 / 캐릭터 / 상점 / 설정.
    /// View only: reads ProgressionService / SettingsService / ExternalServices and calls GameFlowManager to move on.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        /// <summary>Tab to open next time the lobby loads (e.g. result screen → stages).</summary>
        public static LobbyTab RequestedTab = LobbyTab.Home;

        public LobbyTab CurrentTab { get; private set; }

        private LobbyHomePanel _home;
        private StageSelectPanel _stages;
        private CharacterPanel _characters;
        private ShopPanel _shop;
        private SettingsView _settings;

        private string _toast;
        private float _toastUntil;
        private bool _busy;

        private void Awake()
        {
            _home = new LobbyHomePanel(this);
            _stages = new StageSelectPanel(this);
            _characters = new CharacterPanel(this);
            _shop = new ShopPanel(this);
            _settings = new SettingsView { AllowProgressReset = true };
            CurrentTab = RequestedTab;
            RequestedTab = LobbyTab.Home;
        }

        private void Start()
        {
            HillNGun.Feel.PlaceholderAudio.Instance?.PlayMenuBGM();
            ExternalServices.Log("lobby_open", new System.Collections.Generic.Dictionary<string, object> { { "tab", CurrentTab } });
        }

        public void SetTab(LobbyTab tab)
        {
            if (CurrentTab == LobbyTab.Settings && tab != LobbyTab.Settings) _settings.Flush();
            CurrentTab = tab;
        }

        public void ShowToast(string text, float seconds = 2.5f)
        {
            _toast = text;
            _toastUntil = Time.unscaledTime + seconds;
        }

        public bool Busy { get => _busy; set => _busy = value; }

        // ---------------- shared helpers for panels ----------------
        public static ProgressionService Prog => ProgressionService.Instance;
        public static RuntimeAssetManager Assets => RuntimeAssetManager.Instance;

        public static Sprite IdleSprite(string characterId)
        {
            var lib = ServiceLocator.Get<SpriteLibrary>() ?? Assets?.SpriteLibrary;
            if (lib?.characters == null) return null;
            foreach (var c in lib.characters) if (c != null && c.characterId == characterId) return c.idle;
            return null;
        }

        /// <summary>Menu art: deck portrait when present, else the idle field sprite.</summary>
        public static Sprite PortraitSprite(string characterId)
        {
            var lib = ServiceLocator.Get<SpriteLibrary>() ?? Assets?.SpriteLibrary;
            if (lib?.characters == null) return null;
            foreach (var c in lib.characters) if (c != null && c.characterId == characterId) return c.portrait != null ? c.portrait : c.idle;
            return null;
        }

        public static Sprite BossPortrait()
        {
            var lib = ServiceLocator.Get<SpriteLibrary>() ?? Assets?.SpriteLibrary;
            return lib?.boss == null ? null : (lib.boss.portrait != null ? lib.boss.portrait : lib.boss.idle);
        }

        public static Sprite RegionPortrait(string regionId)
        {
            var lib = ServiceLocator.Get<SpriteLibrary>() ?? Assets?.SpriteLibrary;
            return lib != null ? lib.GetRegionPortrait(regionId) : null;
        }

        public static Sprite SkinPreview(string characterId, SkinDefinition skin)
        {
            var s = PortraitSprite(characterId);
            if (skin == null || skin.unlock == SkinUnlockType.Default) return s;
            return SkinRecolor.Get(s, skin.tint, skin.recolorStrength);
        }

        public static string CharName(string id) => UIKit.T("char." + id + ".name", id);

        public static string SkinName(SkinDefinition s)
        {
            if (s == null) return "";
            if (s.unlock == SkinUnlockType.Default) return UIKit.T("skin.default", "기본");
            return UIKit.T(s.nameKey, s.id);
        }

        public static string SkinRequirement(SkinDefinition s)
        {
            switch (s.unlock)
            {
                case SkinUnlockType.Purchase: return UIKit.TF("skin.req.purchase", "현상금 ${0:N0}로 구매", s.cost);
                case SkinUnlockType.BossReward: return UIKit.TF("skin.req.boss", "{0} 보스 처치 보상", UIKit.T("region." + s.requirementId, s.requirementId));
                case SkinUnlockType.Mastery: return UIKit.TF("skin.req.mastery", "숙련도 Lv.{0} 달성", s.masteryLevel);
                default: return UIKit.T("skin.req.default", "기본 지급");
            }
        }

        // ---------------- drawing ----------------
        private void OnGUI()
        {
            GUI.depth = 10;
            Vector2 size = UIKit.Begin();
            UIKit.Background();
            DrawTopBar(size);
            DrawTabs(size);

            Rect content = new Rect(20f, 132f, size.x - 40f, size.y - 150f);
            switch (CurrentTab)
            {
                case LobbyTab.Home: _home.Draw(content); break;
                case LobbyTab.Stages: _stages.Draw(content); break;
                case LobbyTab.Characters: _characters.Draw(content); break;
                case LobbyTab.Shop: _shop.Draw(content); break;
                case LobbyTab.Settings:
                    UIKit.Panel(content);
                    _settings.Draw(new Rect(content.x + 10f, content.y + 6f, content.width - 20f, content.height - 12f));
                    if (Time.unscaledTime < _settings.ToastUntil) ShowToast(_settings.Toast, _settings.ToastUntil - Time.unscaledTime);
                    break;
            }

            if (Time.unscaledTime < _toastUntil && !string.IsNullOrEmpty(_toast))
            {
                Rect tr = new Rect(size.x / 2f - 330f, size.y - 70f, 660f, 46f);
                UIKit.Fill(tr, new Color(0f, 0f, 0f, 0.8f));
                UIKit.Border(tr, UIKit.Theme.accent, 2f);
                UIKit.Label(tr, _toast, UIKit.Theme.fontBody, UIKit.Theme.accent, TextAnchor.MiddleCenter, true);
            }
            UIKit.End();
        }

        private void DrawTopBar(Vector2 size)
        {
            var t = UIKit.Theme;
            Rect bar = new Rect(0f, 0f, size.x, 70f);
            UIKit.Fill(bar, t.panelDark);
            UIKit.Fill(new Rect(0f, 68f, size.x, 2f), t.panelBorder);

            UIKit.Label(new Rect(20f, 8f, 220f, 54f), UIKit.T("game.title", "힐앤건"), t.fontHeader + 4, t.accent, TextAnchor.MiddleLeft, true);

            var d = Prog.Data;
            var cfg = Prog.Config;
            int need = cfg != null ? cfg.XpToNextBadge(d.badgeLevel) : 100;
            float bx = size.x / 2f - 200f;
            // badge (star placeholder icon)
            UIKit.Fill(new Rect(bx, 15f, 40f, 40f), t.accent);
            UIKit.Label(new Rect(bx, 15f, 40f, 40f), d.badgeLevel.ToString(), t.fontBody, new Color(0.25f, 0.15f, 0.05f), TextAnchor.MiddleCenter, true);
            UIKit.Label(new Rect(bx + 50f, 8f, 360f, 28f), UIKit.TF("reward.badge", "보안관 배지 Lv.{0}", d.badgeLevel), t.fontBody, t.text, TextAnchor.MiddleLeft, true);
            UIKit.Bar(new Rect(bx + 50f, 40f, 300f, 14f), need > 0 ? d.badgeXp / (float)need : 1f, t.accent);
            UIKit.Label(new Rect(bx + 356f, 34f, 120f, 26f), $"{d.badgeXp}/{need}", t.fontSmall, t.textDim);

            Rect money = new Rect(size.x - 250f, 14f, 160f, 42f);
            UIKit.Fill(money, new Color(0f, 0f, 0f, 0.35f));
            UIKit.Label(money, "$ " + d.bounty.ToString("N0"), t.fontHeader, t.accent, TextAnchor.MiddleCenter, true);
            UIKit.Small(new Rect(size.x - 350f, 14f, 94f, 42f), UIKit.T("reward.bounty_total", "보유 현상금"), TextAnchor.MiddleRight);

            if (UIKit.Button(new Rect(size.x - 82f, 14f, 66f, 42f), UIKit.T("ui.to_title", "타이틀"), true, UIKit.ButtonKind.Normal, t.fontSmall))
                GameFlowManager.Instance?.GoToTitle();
        }

        private void DrawTabs(Vector2 size)
        {
            string[] names =
            {
                UIKit.T("lobby.tab.home", "홈"), UIKit.T("lobby.tab.stages", "스테이지"), UIKit.T("lobby.tab.characters", "캐릭터"),
                UIKit.T("lobby.tab.shop", "상점"), UIKit.T("lobby.tab.settings", "설정")
            };
            float w = Mathf.Min(190f, (size.x - 40f - 4 * 8f) / 5f);
            float x0 = (size.x - (w * 5 + 4 * 8f)) / 2f;
            for (int i = 0; i < 5; i++)
            {
                var tab = (LobbyTab)i;
                if (UIKit.Button(new Rect(x0 + i * (w + 8f), 80f, w, 42f), names[i], true, CurrentTab == tab ? UIKit.ButtonKind.TabActive : UIKit.ButtonKind.Tab))
                    SetTab(tab);
            }
        }

        // ---------------- navigation (used by panels and smoke test) ----------------
        public void StartPvE(int stageIndex)
        {
            var flow = GameFlowManager.Instance;
            if (flow == null) return;
            flow.CurrentSession.startStageIndex = stageIndex;
            flow.CurrentSession.continuesUsed = 0;
            ExternalServices.Log("stage_select", new System.Collections.Generic.Dictionary<string, object> { { "stage_index", stageIndex } });
            flow.GoToCharacterSelect(GameMode.PvE);
        }

        public void StartPvP()
        {
            if (_busy) return;
            _busy = true;
            ExternalServices.Matchmaking.FindMatch(Role.Assault, r =>
            {
                _busy = false;
                if (r.status == MatchmakingStatus.OfflineBots)
                    ShowToast(UIKit.T("lobby.pvp_offline", "오프라인 프로토타입: 봇과 3대3 대전"));
                GameFlowManager.Instance?.GoToCharacterSelect(GameMode.PvP);
            });
        }

        public void StartPractice() => GameFlowManager.Instance?.GoToCharacterSelect(GameMode.Practice);
    }
}
