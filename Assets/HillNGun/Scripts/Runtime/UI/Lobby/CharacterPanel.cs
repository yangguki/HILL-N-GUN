using UnityEngine;
using HillNGun.Data;
using HillNGun.External;

namespace HillNGun.UI
{
    /// <summary>Character tab: mastery level / title / XP (no stat growth, D3) and the skin list with equip (FP 526-528).</summary>
    public class CharacterPanel
    {
        private readonly LobbyUI _lobby;
        private int _selected;
        private string _previewSkinId;
        public CharacterPanel(LobbyUI lobby) { _lobby = lobby; }

        public int Selected { get => _selected; set { _selected = value; _previewSkinId = null; } }

        public void Draw(Rect area)
        {
            var t = UIKit.Theme;
            var chars = LobbyUI.Assets?.CharacterDatas;
            if (chars == null || chars.Length == 0) { UIKit.Body(area, "no characters"); return; }
            _selected = Mathf.Clamp(_selected, 0, chars.Length - 1);

            // character tabs
            float tw = 200f;
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == null) continue;
                if (UIKit.Button(new Rect(area.x + i * (tw + 10f), area.y, tw, 44f), LobbyUI.CharName(chars[i].id), true, i == _selected ? UIKit.ButtonKind.TabActive : UIKit.ButtonKind.Tab))
                    Selected = i;
            }

            var cd = chars[_selected];
            var p = LobbyUI.Prog;
            var save = p.Data.GetCharacter(cd.id, false);
            int lvl = save != null ? save.masteryLevel : 1;
            int xp = save != null ? save.masteryXp : 0;
            var cfg = p.Config;

            Rect left = new Rect(area.x, area.y + 56f, 380f, area.height - 56f);
            Rect right = new Rect(left.xMax + 16f, left.y, area.width - left.width - 16f, left.height);
            UIKit.Panel(left, true);

            // preview (equipped or previewed skin)
            string equippedId = p.GetEquippedSkinId(cd.id);
            var previewSkin = p.GetSkin(_previewSkinId ?? equippedId);
            Rect stage = new Rect(left.x + 20f, left.y + 16f, left.width - 40f, 230f);
            UIKit.Fill(stage, new Color(0.55f, 0.42f, 0.28f, 0.5f));
            var tex3D = HillNGun.View3D.Actor3DPreview.Get(cd.id, previewSkin);
            if (tex3D != null)
            {
                // turning 3D model (code-built, same rig as in matches)
                float side = stage.height - 8f;
                GUI.DrawTexture(new Rect(stage.center.x - side * 0.5f, stage.y + 2f, side, side), tex3D, ScaleMode.ScaleToFit, true);
            }
            else
                UIKit.Sprite(new Rect(stage.x + 10f, stage.y + 6f, stage.width - 20f, stage.height - 12f), LobbyUI.SkinPreview(cd.id, previewSkin));
            UIKit.Label(new Rect(stage.x, stage.yMax - 28f, stage.width, 26f), LobbyUI.SkinName(previewSkin) + (_previewSkinId != null && _previewSkinId != equippedId ? "  " + UIKit.T("char.preview", "(미리보기)") : ""), t.fontSmall, t.text, TextAnchor.MiddleCenter, true);

            float y = stage.yMax + 12f, x = left.x + 20f, w = left.width - 40f;
            UIKit.Label(new Rect(x, y, w, 34f), LobbyUI.CharName(cd.id), t.fontHeader, t.accent, TextAnchor.MiddleLeft, true);
            UIKit.Label(new Rect(x, y, w, 34f), UIKit.T("role." + cd.role.ToString().ToLowerInvariant(), cd.role.ToString()), t.fontBody, t.textDim, TextAnchor.MiddleRight);
            y += 38f;
            string title = UIKit.T(p.GetMasteryTitleKey(cd.id) ?? "", "");
            UIKit.Label(new Rect(x, y, w, 28f), UIKit.TF("char.mastery", "숙련도 Lv.{0}  「{1}」", lvl, title), t.fontBody, t.text, TextAnchor.MiddleLeft, true);
            y += 30f;
            int cur = cfg != null && cfg.masteryLevelXp != null && lvl - 1 < cfg.masteryLevelXp.Length ? cfg.masteryLevelXp[lvl - 1] : 0;
            bool max = cfg == null || cfg.masteryLevelXp == null || lvl >= cfg.masteryLevelXp.Length;
            int next = max ? cur : cfg.masteryLevelXp[lvl];
            UIKit.Bar(new Rect(x, y, w, 14f), max ? 1f : (xp - cur) / (float)Mathf.Max(1, next - cur), t.accent);
            y += 18f;
            UIKit.Small(new Rect(x, y, w, 22f), max ? UIKit.T("char.mastery_max", "최고 숙련도") : UIKit.TF("char.mastery_xp", "{0} / {1} XP", xp, next), TextAnchor.MiddleRight);
            y += 26f;
            UIKit.Small(new Rect(x, y, w, 22f), UIKit.TF("char.record", "{0}판 · {1}승", save?.matches ?? 0, save?.wins ?? 0));
            y += 26f;
            string skill = cd.skill != null ? UIKit.T(cd.skill.nameKey, cd.skill.id) : "-";
            string passive = cd.passive != null ? UIKit.T(cd.passive.nameKey, cd.passive.id) : "-";
            UIKit.Small(new Rect(x, y, w, 22f), UIKit.TF("char.kit", "스킬 {0} · 패시브 {1}", skill, passive));

            // guns (t48u loadout) on top, skins below
            UIKit.Panel(right);
            var guns = new System.Collections.Generic.List<WeaponOffer>();
            var cat = p.WeaponCatalog;
            if (cat?.offers != null) foreach (var o in cat.offers) if (o != null && o.weapon != null && o.AllowedFor(cd.id)) guns.Add(o);
            float gy = right.y + 10f;
            if (guns.Count > 0)
            {
                UIKit.Header(new Rect(right.x + 16f, gy, right.width - 32f, 34f), UIKit.T("char.guns", "총"));
                gy += 40f;
                string eq = p.GetEquippedWeaponId(cd.id, cd.weapon);
                foreach (var o in guns)
                {
                    DrawGunRow(new Rect(right.x + 14f, gy, right.width - 28f, 66f), cd.id, o, eq);
                    gy += 72f;
                }
                gy += 6f;
            }
            Rect skinArea = new Rect(right.x, gy, right.width, right.yMax - gy);
            UIKit.Header(new Rect(skinArea.x + 16f, skinArea.y, skinArea.width - 32f, 34f), UIKit.T("char.skins", "스킨"));
            UIKit.Small(new Rect(skinArea.x + 160f, skinArea.y + 6f, skinArea.width - 176f, 22f), UIKit.T("char.skins_note", "능력치 변화 없음 · 임시 색 변경 (실제 아트는 외부 작업)"), TextAnchor.MiddleRight);
            var skins = p.GetSkinsFor(cd.id);
            float ry = skinArea.y + 40f, rowH = Mathf.Min(96f, (skinArea.height - 44f) / Mathf.Max(1, skins.Count));
            foreach (var s in skins)
            {
                DrawSkinRow(new Rect(right.x + 14f, ry, right.width - 28f, rowH - 8f), cd.id, s, equippedId);
                ry += rowH;
            }
        }

        private void DrawGunRow(Rect r, string charId, WeaponOffer o, string equippedId)
        {
            var t = UIKit.Theme;
            var p = LobbyUI.Prog;
            bool owned = o.ownedByDefault || p.OwnsWeapon(o.Id);
            bool equipped = o.Id == equippedId;
            UIKit.Fill(r, equipped ? new Color(0.45f, 0.33f, 0.18f) : t.panelDark);
            UIKit.Border(r, equipped ? t.accent : new Color(0, 0, 0, 0.35f), equipped ? 2f : 1f);
            float x = r.x + 12f, w = r.width - 24f - 170f;
            UIKit.Label(new Rect(x, r.y + 2f, w, 30f), ShopPanel.GunName(o), t.fontBody, owned ? t.text : t.textDim, TextAnchor.MiddleLeft, true);
            UIKit.Label(new Rect(x, r.y + 32f, w, 32f), ShopPanel.GunStats(o.weapon), t.fontSmall, t.textDim, TextAnchor.MiddleLeft, false, true);
            Rect btn = new Rect(r.xMax - 160f, r.y + (r.height - 44f) / 2f, 150f, 44f);
            if (equipped) UIKit.Button(btn, UIKit.T("skin.equipped", "장착 중"), false);
            else if (owned)
            {
                if (UIKit.Button(btn, UIKit.T("skin.equip", "장착"), true, UIKit.ButtonKind.Primary) && p.EquipWeapon(charId, o.Id))
                    _lobby.ShowToast(UIKit.TF("gun.equipped_toast", "{0}: {1} 장착", LobbyUI.CharName(charId), ShopPanel.GunName(o)));
            }
            else if (UIKit.Button(btn, "$" + o.cost.ToString("N0"), true)) { ShopPanel.ShowGuns = true; _lobby.SetTab(LobbyTab.Shop); }
        }

        private void DrawSkinRow(Rect r, string charId, SkinDefinition s, string equippedId)
        {
            var t = UIKit.Theme;
            var p = LobbyUI.Prog;
            bool owned = s.unlock == SkinUnlockType.Default || p.OwnsSkin(s.id);
            bool equipped = s.id == equippedId;
            UIKit.Fill(r, equipped ? new Color(0.45f, 0.33f, 0.18f) : t.panelDark);
            UIKit.Border(r, equipped ? t.accent : new Color(0, 0, 0, 0.35f), equipped ? 2f : 1f);
            Rect icon = new Rect(r.x + 6f, r.y + 4f, r.height - 8f, r.height - 8f);
            UIKit.Fill(icon, new Color(0f, 0f, 0f, 0.25f));
            UIKit.Sprite(icon, LobbyUI.SkinPreview(charId, s), owned ? Color.white : new Color(1f, 1f, 1f, 0.45f));
            if (GUI.Button(icon, GUIContent.none, GUIStyle.none)) _previewSkinId = s.id;

            float x = icon.xMax + 12f;
            UIKit.Label(new Rect(x, r.y + 4f, r.width - (x - r.x) - 170f, r.height / 2f), LobbyUI.SkinName(s), t.fontBody, owned ? t.text : t.textDim, TextAnchor.MiddleLeft, true);
            UIKit.Small(new Rect(x, r.y + r.height / 2f, r.width - (x - r.x) - 170f, r.height / 2f - 4f), owned ? UIKit.T("skin.owned", "보유 중") : LobbyUI.SkinRequirement(s));

            Rect btn = new Rect(r.xMax - 160f, r.y + (r.height - 44f) / 2f, 150f, 44f);
            if (equipped)
                UIKit.Button(btn, UIKit.T("skin.equipped", "장착 중"), false);
            else if (owned)
            {
                if (UIKit.Button(btn, UIKit.T("skin.equip", "장착"), true, UIKit.ButtonKind.Primary))
                {
                    if (p.EquipSkin(charId, s.id))
                    {
                        _previewSkinId = null;
                        _lobby.ShowToast(UIKit.TF("skin.equipped_toast", "{0} 장착", LobbyUI.SkinName(s)));
                        ExternalServices.Log("skin_equip", new System.Collections.Generic.Dictionary<string, object> { { "skin", s.id } });
                    }
                }
            }
            else if (s.unlock == SkinUnlockType.Purchase)
            {
                if (UIKit.Button(btn, UIKit.T("skin.to_shop", "상점에서 구매"), true)) _lobby.SetTab(LobbyTab.Shop);
            }
            else UIKit.Button(btn, UIKit.T("skin.locked", "잠김"), false);
        }
    }
}
