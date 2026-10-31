using UnityEngine;
using HillNGun.Data;
using HillNGun.External;

namespace HillNGun.UI
{
    /// <summary>Home tab: mode cards (PvE → stage select, PvP → matchmaking stub → team select, practice) + profile summary.</summary>
    public class LobbyHomePanel
    {
        private readonly LobbyUI _lobby;
        public LobbyHomePanel(LobbyUI lobby) { _lobby = lobby; }

        public void Draw(Rect area)
        {
            var t = UIKit.Theme;
            float profileW = 340f;
            Rect cards = new Rect(area.x, area.y, area.width - profileW - 16f, area.height);
            Rect profile = new Rect(cards.xMax + 16f, area.y, profileW, area.height);

            float cw = (cards.width - 2 * 14f) / 3f;
            DrawModeCard(new Rect(cards.x, cards.y, cw, cards.height), "mode.pve.title", "PvE 스토리", "mode.pve.desc", "먼지 마을을 지켜라!\n3개 스테이지 + 보스전",
                new Color(0.32f, 0.55f, 0.36f), UIKit.T("lobby.pve_go", "스테이지 선택"), () => _lobby.SetTab(LobbyTab.Stages), null,
                new[] { LobbyUI.BossPortrait() });
            DrawModeCard(new Rect(cards.x + cw + 14f, cards.y, cw, cards.height), "mode.pvp.title", "PvP 대전", "mode.pvp.desc", "봇과 3v3 탈락전\n3판 2선승",
                new Color(0.62f, 0.33f, 0.28f), UIKit.T("lobby.pvp_go", "대전 시작"), _lobby.StartPvP,
                ExternalServices.Matchmaking.IsOnline ? null : UIKit.T("stub.online", "온라인 매칭·랭크: 서버 필요"),
                new[] { LobbyUI.PortraitSprite("buster"), LobbyUI.PortraitSprite("raven"), LobbyUI.PortraitSprite("volt") });
            DrawModeCard(new Rect(cards.x + (cw + 14f) * 2, cards.y, cw, cards.height), "mode.practice.title", "연습장", "mode.practice.desc", "조작 연습 및 테스트\n과녁과 엄폐물",
                new Color(0.36f, 0.42f, 0.62f), UIKit.T("lobby.practice_go", "연습하기"), _lobby.StartPractice, null,
                new[] { LobbyUI.PortraitSprite("volt") });

            DrawProfile(profile);
        }

        private void DrawModeCard(Rect r, string titleKey, string titleFb, string descKey, string descFb, Color color, string action, System.Action onClick, string stub, Sprite[] arts = null)
        {
            var t = UIKit.Theme;
            UIKit.Panel(r);
            Rect art = new Rect(r.x + 14f, r.y + 14f, r.width - 28f, r.height * 0.46f);
            UIKit.Fill(art, color);
            // simple self-made emblem: wanted-poster frame
            UIKit.Border(new Rect(art.x + 10f, art.y + 10f, art.width - 20f, art.height - 20f), new Color(1f, 1f, 1f, 0.35f), 3f);
            int n = 0;
            if (arts != null) foreach (var a in arts) if (a != null) n++;
            if (n > 0)
            {
                // deck character art (HILLNGUN_기획서) over the card color, title on a dark strip below
                Rect pic = new Rect(art.x + 6f, art.y + 6f, art.width - 12f, art.height - 50f);
                float each = pic.width / n; int i = 0;
                foreach (var a in arts)
                {
                    if (a == null) continue;
                    float overlap = n > 1 ? each * 0.18f : 0f;
                    UIKit.Sprite(new Rect(pic.x + i * each - overlap, pic.y, each + overlap * 2f, pic.height), a);
                    i++;
                }
                Rect strip = new Rect(art.x, art.yMax - 44f, art.width, 44f);
                UIKit.Fill(strip, new Color(0f, 0f, 0f, 0.55f));
                UIKit.Label(strip, UIKit.T(titleKey, titleFb), t.fontHeader + 2, Color.white, TextAnchor.MiddleCenter, true);
            }
            else
                UIKit.Label(art, UIKit.T(titleKey, titleFb), t.fontTitle - 6, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Label(new Rect(r.x + 16f, art.yMax + 12f, r.width - 32f, 90f), UIKit.T(descKey, descFb), t.fontBody, t.text, TextAnchor.UpperCenter, false, true);
            if (!string.IsNullOrEmpty(stub))
                UIKit.StubTag(new Rect(r.x + 16f, r.yMax - 118f, r.width - 32f, 28f), stub);
            if (UIKit.Button(new Rect(r.x + 16f, r.yMax - 76f, r.width - 32f, 58f), action, !_lobby.Busy, UIKit.ButtonKind.Primary, t.fontHeader - 2))
                onClick?.Invoke();
        }

        private void DrawProfile(Rect r)
        {
            var t = UIKit.Theme;
            var p = LobbyUI.Prog;
            var d = p.Data;
            UIKit.Panel(r, true);
            float y = r.y + 12f, x = r.x + 16f, w = r.width - 32f;
            UIKit.Header(new Rect(x, y, w, 32f), UIKit.T("lobby.profile", "내 기록")); y += 40f;
            Line(ref y, x, w, UIKit.T("lobby.stat.pve_runs", "PvE 출격"), d.pveRuns.ToString());
            Line(ref y, x, w, UIKit.T("lobby.stat.boss_kills", "보스 처치"), d.bossKills.ToString());
            Line(ref y, x, w, UIKit.T("lobby.stat.pvp", "PvP 승/패/무"), $"{d.pvpWins} / {d.pvpLosses} / {d.pvpDraws}");
            y += 8f;
            UIKit.Header(new Rect(x, y, w, 32f), UIKit.T("lobby.region_progress", "지역 진행")); y += 38f;
            var cfg = p.Config;
            if (cfg?.regions != null)
            {
                foreach (var reg in cfg.regions)
                {
                    if (reg == null) continue;
                    bool unlocked = d.unlockedRegions.Contains(reg.id);
                    int total = reg.stageIds != null ? reg.stageIds.Length : 0;
                    int cleared = 0;
                    if (reg.stageIds != null) foreach (var s in reg.stageIds) if (p.IsStageCleared(s)) cleared++;
                    string status = !unlocked ? UIKit.T("lobby.locked", "잠김") : total == 0 ? UIKit.T("lobby.coming_soon", "준비 중") : $"{cleared}/{total}";
                    UIKit.Label(new Rect(x, y, w - 90f, 26f), UIKit.T(reg.nameKey, reg.id), t.fontBody, unlocked ? t.text : t.textDim);
                    UIKit.Label(new Rect(x + w - 90f, y, 90f, 26f), status, t.fontBody, unlocked ? (total > 0 && cleared == total ? t.good : t.accent) : t.textDim, TextAnchor.MiddleRight);
                    y += 28f;
                    if (y > r.yMax - 40f) break;
                }
            }
            UIKit.Small(new Rect(x, r.yMax - 34f, w, 26f), UIKit.T("lobby.no_stats_note", "능력치 성장 없음: 보상은 스킨·칭호"));
        }

        private static void Line(ref float y, float x, float w, string k, string v)
        {
            var t = UIKit.Theme;
            UIKit.Label(new Rect(x, y, w - 120f, 26f), k, t.fontBody, t.textDim);
            UIKit.Label(new Rect(x + w - 120f, y, 120f, 26f), v, t.fontBody, t.text, TextAnchor.MiddleRight, true);
            y += 30f;
        }
    }
}
