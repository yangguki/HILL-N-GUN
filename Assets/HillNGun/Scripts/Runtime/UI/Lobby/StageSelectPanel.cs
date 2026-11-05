using System;
using UnityEngine;
using HillNGun.Data;

namespace HillNGun.UI
{
    /// <summary>Stage tab: regions (left) and the selected region's stages (right) with clear / unlocked / locked state.</summary>
    public class StageSelectPanel
    {
        private readonly LobbyUI _lobby;
        private string _regionId;
        public StageSelectPanel(LobbyUI lobby) { _lobby = lobby; }

        public void Draw(Rect area)
        {
            var t = UIKit.Theme;
            var p = LobbyUI.Prog;
            var cfg = p.Config;
            var regions = cfg?.regions ?? Array.Empty<RegionDefinition>();
            if (_regionId == null && regions.Length > 0) _regionId = regions[0].id;

            Rect left = new Rect(area.x, area.y, 300f, area.height);
            Rect right = new Rect(left.xMax + 16f, area.y, area.width - 316f, area.height);
            UIKit.Panel(left, true);
            UIKit.Header(new Rect(left.x + 14f, left.y + 8f, left.width - 28f, 34f), UIKit.T("stage.regions", "지역"));
            float y = left.y + 50f;
            RegionDefinition selected = null;
            foreach (var reg in regions)
            {
                if (reg == null) continue;
                bool unlocked = p.Data.unlockedRegions.Contains(reg.id);
                string label = UIKit.T(reg.nameKey, reg.id) + (unlocked ? "" : " " + UIKit.T("lobby.locked_paren", "(잠김)"));
                bool active = reg.id == _regionId;
                if (UIKit.Button(new Rect(left.x + 12f, y, left.width - 24f, 52f), label, true, active ? UIKit.ButtonKind.TabActive : UIKit.ButtonKind.Tab))
                    _regionId = reg.id;
                if (active) selected = reg;
                y += 60f;
            }

            UIKit.Panel(right);
            if (selected == null) return;
            bool regionUnlocked = p.Data.unlockedRegions.Contains(selected.id);
            UIKit.Header(new Rect(right.x + 16f, right.y + 10f, right.width - 32f, 36f), UIKit.T(selected.nameKey, selected.id));
            if (!string.IsNullOrEmpty(selected.bossNameKey))
                UIKit.Label(new Rect(right.x + 16f, right.y + 46f, right.width - 32f, 28f),
                    UIKit.TF("stage.wanted", "현상수배: {0}  ${1:N0}", UIKit.T(selected.bossNameKey, selected.bossNameKey), selected.bossBounty), t.fontBody, t.textDim);

            Rect body = new Rect(right.x + 16f, right.y + 84f, right.width - 32f, right.height - 100f);
            var portrait = LobbyUI.RegionPortrait(selected.id);
            if ((!regionUnlocked || selected.stageIds == null || selected.stageIds.Length == 0) && portrait != null)
            {
                // wanted poster from the design deck on the left, message on the right
                float pw = Mathf.Min(body.height * 0.75f, body.width * 0.4f);
                Rect pr = new Rect(body.x, body.y, pw, pw / 0.75f);
                UIKit.Sprite(pr, portrait, regionUnlocked ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f));
                UIKit.Border(pr, t.panelBorder, 2f);
                body = new Rect(pr.xMax + 16f, body.y, body.width - pw - 16f, body.height);
            }
            if (!regionUnlocked)
            {
                UIKit.Label(body, UIKit.T("stage.region_locked", "이전 지역 보스를 잡으면 열립니다 (FP 528)"), t.fontHeader, t.textDim, TextAnchor.MiddleCenter, true, true);
                return;
            }
            if (selected.stageIds == null || selected.stageIds.Length == 0)
            {
                UIKit.Label(body, UIKit.T("stage.region_soon", "해금 완료! 스테이지는 다음 업데이트에서 (프로토타입 범위 밖)"), t.fontHeader, t.accent, TextAnchor.MiddleCenter, true, true);
                return;
            }

            var stages = LobbyUI.Assets != null ? LobbyUI.Assets.StageDatas : null;
            int n = selected.stageIds.Length;
            float cw = (body.width - (n - 1) * 12f) / n;
            for (int i = 0; i < n; i++)
            {
                string id = selected.stageIds[i];
                int globalIndex = stages != null ? Array.FindIndex(stages, s => s != null && s.id == id) : -1;
                StageData sd = globalIndex >= 0 ? stages[globalIndex] : null;
                DrawStageCard(new Rect(body.x + i * (cw + 12f), body.y, cw, body.height - 40f), selected, sd, globalIndex, stages);
            }
            UIKit.Small(new Rect(body.x, body.yMax - 30f, body.width, 26f), UIKit.T("stage.chain_note", "출격하면 고른 스테이지부터 보스까지 이어서 진행합니다"));
        }

        private void DrawStageCard(Rect r, RegionDefinition reg, StageData sd, int globalIndex, StageData[] stages)
        {
            var t = UIKit.Theme;
            var p = LobbyUI.Prog;
            bool isBoss = sd != null && sd.id == reg.bossStageId;
            bool cleared = sd != null && p.IsStageCleared(sd.id);
            bool unlocked = sd != null && p.IsStageUnlocked(stages, globalIndex);
            UIKit.Fill(r, unlocked ? (isBoss ? new Color(0.42f, 0.2f, 0.16f) : t.panelDark) : t.locked);
            UIKit.Border(r, cleared ? t.good : unlocked ? t.accent : new Color(0, 0, 0, 0.4f), 2f);
            var theme = HillNGun.Scenery.MenuBackdrop.ThemeFor(sd);
            var thumb = HillNGun.Scenery.MenuBackdrop.Thumb(theme);
            if (isBoss && thumb != null && Event.current.type == EventType.Repaint)
            {
                // boss card: the sunset showdown backdrop behind the card content
                var inner = new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f);
                GUI.color = new Color(1f, 1f, 1f, unlocked ? 0.45f : 0.2f);
                GUI.DrawTexture(inner, thumb, ScaleMode.ScaleAndCrop, false);
                GUI.color = Color.white;
            }

            string num = sd == null ? "?" : isBoss ? "BOSS" : $"{sd.stageNumber}-{sd.subStageNumber}";
            UIKit.Label(new Rect(r.x, r.y + 12f, r.width, 44f), num, t.fontTitle - 4, unlocked ? t.accent : t.textDim, TextAnchor.MiddleCenter, true);
            string name = sd == null ? "-" : UIKit.T(sd.nameKey, sd.id);
            UIKit.Label(new Rect(r.x + 8f, r.y + 60f, r.width - 16f, 56f), name, t.fontBody, unlocked ? t.text : t.textDim, TextAnchor.MiddleCenter, true, true);
            if (sd != null && !isBoss)
                UIKit.Label(new Rect(r.x + 8f, r.y + 116f, r.width - 16f, 26f), UIKit.TF("stage.target_time", "목표 {0:0}~{1:0}초", sd.stageTimeMin, sd.stageTimeMax), t.fontSmall, t.textDim, TextAnchor.MiddleCenter);
            int best = sd != null ? p.GetStageStars(sd.id) : 0;
            string starText = best > 0 ? new string('★', best) + new string('☆', 3 - best) : "☆☆☆";
            string status = cleared ? starText + "  " + UIKit.T("stage.cleared", "클리어") : unlocked ? UIKit.T("stage.open", "도전 가능") : UIKit.T("stage.locked", "잠김: 이전 스테이지 클리어 필요");
            var bossArt = isBoss ? LobbyUI.RegionPortrait(reg.id) : null;
            if (bossArt != null)
            {
                // boss card: deck portrait between the name and the bounty
                Rect art = new Rect(r.x + 10f, r.y + 114f, r.width - 20f, r.height - 114f - 150f);
                UIKit.Sprite(art, bossArt, unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f));
                UIKit.Label(new Rect(r.x + 8f, r.yMax - 144f, r.width - 16f, 30f), "$" + reg.bossBounty.ToString("N0"), t.fontHeader, t.accent, TextAnchor.MiddleCenter, true);
                UIKit.Label(new Rect(r.x + 8f, r.yMax - 112f, r.width - 16f, 44f), status, t.fontBody, cleared ? t.good : unlocked ? t.text : t.textDim, TextAnchor.MiddleCenter, true, true);
            }
            else
            {
                if (isBoss)
                    UIKit.Label(new Rect(r.x + 8f, r.y + 116f, r.width - 16f, 26f), "$" + reg.bossBounty.ToString("N0"), t.fontHeader, t.accent, TextAnchor.MiddleCenter, true);
                UIKit.Label(new Rect(r.x + 8f, r.y + 150f, r.width - 16f, 48f), status, t.fontBody, cleared ? t.good : unlocked ? t.text : t.textDim, TextAnchor.MiddleCenter, true, true);
                // stage theme preview (the real baked field backdrop) + theme name
                Rect tr = new Rect(r.x + 10f, r.y + 202f, r.width - 20f, r.yMax - 72f - (r.y + 202f));
                if (!isBoss && theme != null && tr.height > 40f)
                {
                    if (thumb != null && Event.current.type == EventType.Repaint)
                    {
                        GUI.color = unlocked ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
                        GUI.DrawTexture(tr, thumb, ScaleMode.ScaleAndCrop, false);
                        GUI.color = Color.white;
                    }
                    UIKit.Border(tr, new Color(0f, 0f, 0f, 0.5f), 2f);
                    Rect cap = new Rect(tr.x, tr.yMax - 30f, tr.width, 30f);
                    UIKit.Fill(cap, new Color(0.08f, 0.05f, 0.03f, 0.62f));
                    UIKit.Label(cap, UIKit.T(theme.nameKey, theme.nameFallback), t.fontSmall, unlocked ? t.text : t.textDim, TextAnchor.MiddleCenter, true);
                }
            }

            if (UIKit.Button(new Rect(r.x + 12f, r.yMax - 62f, r.width - 24f, 50f), UIKit.T("stage.go", "출격"), unlocked, UIKit.ButtonKind.Primary))
                _lobby.StartPvE(globalIndex);
        }
    }
}
