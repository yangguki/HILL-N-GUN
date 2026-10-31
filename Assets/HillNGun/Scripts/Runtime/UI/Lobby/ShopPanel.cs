using System.Collections.Generic;
using UnityEngine;
using HillNGun.Data;
using HillNGun.External;

namespace HillNGun.UI
{
    /// <summary>
    /// Shop tab. Left: bounty shop (skins bought with in-game currency, works offline).
    /// Right: premium products that need real payment — listed from ExternalServicesData, purchase goes to the IAP stub.
    /// </summary>
    public class ShopPanel
    {
        private readonly LobbyUI _lobby;
        public ShopPanel(LobbyUI lobby) { _lobby = lobby; }

        public void Draw(Rect area)
        {
            var t = UIKit.Theme;
            var p = LobbyUI.Prog;
            Rect left = new Rect(area.x, area.y, area.width * 0.58f, area.height);
            Rect right = new Rect(left.xMax + 16f, area.y, area.width - left.width - 16f, area.height);

            // ---- bounty shop ----
            UIKit.Panel(left);
            UIKit.Header(new Rect(left.x + 16f, left.y + 10f, left.width - 32f, 34f), UIKit.T("shop.bounty_title", "현상금 상점"));
            UIKit.Small(new Rect(left.x + 16f, left.y + 44f, left.width - 32f, 22f), UIKit.T("shop.bounty_note", "꾸미기 전용 · 능력치 변화 없음 (FP 514)"));
            var items = new List<SkinDefinition>();
            if (p.Config?.skins != null)
                foreach (var s in p.Config.skins) if (s != null && s.unlock == SkinUnlockType.Purchase) items.Add(s);
            float cw = (left.width - 32f - (items.Count - 1) * 12f) / Mathf.Max(1, items.Count);
            for (int i = 0; i < items.Count; i++)
                DrawSkinItem(new Rect(left.x + 16f + i * (cw + 12f), left.y + 78f, cw, left.height - 94f), items[i]);

            // ---- premium (external) ----
            UIKit.Panel(right, true);
            UIKit.Header(new Rect(right.x + 16f, right.y + 10f, right.width - 32f, 34f), UIKit.T("shop.premium_title", "프리미엄"));
            UIKit.StubTag(new Rect(right.x + 16f, right.y + 46f, right.width - 32f, 26f),
                ExternalServices.Iap.IsRealSdk ? "IAP" : UIKit.T("stub.iap", "외부 결제(IAP) 필요 · 오프라인 스텁"));
            float y = right.y + 84f;
            var products = ExternalServices.Iap.Products;
            float rowH = Mathf.Min(120f, (right.height - 96f) / Mathf.Max(1, products.Count));
            foreach (var prod in products)
            {
                if (prod == null) continue;
                Rect r = new Rect(right.x + 14f, y, right.width - 28f, rowH - 10f);
                UIKit.Fill(r, t.panel);
                UIKit.Border(r, new Color(0, 0, 0, 0.35f), 1f);
                UIKit.Label(new Rect(r.x + 12f, r.y + 6f, r.width - 150f, 28f), UIKit.T(prod.nameKey, prod.id), t.fontBody, t.text, TextAnchor.MiddleLeft, true);
                UIKit.Label(new Rect(r.x + 12f, r.y + 34f, r.width - 150f, r.height - 40f), UIKit.T(prod.descKey, ""), t.fontSmall, t.textDim, TextAnchor.UpperLeft, false, true);
                Rect btn = new Rect(r.xMax - 130f, r.y + (r.height - 44f) / 2f, 120f, 44f);
                if (UIKit.Button(btn, UIKit.T(prod.priceLabelKey, "가격 미정"), !_lobby.Busy))
                {
                    _lobby.Busy = true;
                    ExternalServices.Iap.Purchase(prod.id, res =>
                    {
                        _lobby.Busy = false;
                        _lobby.ShowToast(res == IapResult.Success ? UIKit.T("shop.iap_ok", "구매 완료")
                            : UIKit.T("shop.iap_offline", "오프라인 프로토타입: 실제 결제는 지원하지 않습니다 (스텁)"));
                    });
                }
                y += rowH;
            }
        }

        private void DrawSkinItem(Rect r, SkinDefinition s)
        {
            var t = UIKit.Theme;
            var p = LobbyUI.Prog;
            bool owned = p.OwnsSkin(s.id);
            bool affordable = p.Data.bounty >= s.cost;
            UIKit.Fill(r, t.panelDark);
            UIKit.Border(r, owned ? t.good : t.panelBorder, 2f);
            Rect art = new Rect(r.x + 8f, r.y + 8f, r.width - 16f, r.height * 0.5f);
            UIKit.Fill(art, new Color(0.55f, 0.42f, 0.28f, 0.45f));
            UIKit.Sprite(new Rect(art.x + 6f, art.y + 6f, art.width - 12f, art.height - 12f), LobbyUI.SkinPreview(s.characterId, s));
            float y = art.yMax + 8f;
            UIKit.Label(new Rect(r.x + 8f, y, r.width - 16f, 52f), LobbyUI.SkinName(s), t.fontBody, t.text, TextAnchor.MiddleCenter, true, true);
            y += 54f;
            UIKit.Label(new Rect(r.x + 8f, y, r.width - 16f, 30f), "$" + s.cost.ToString("N0"), t.fontHeader, owned ? t.textDim : affordable ? t.accent : t.bad, TextAnchor.MiddleCenter, true);
            Rect btn = new Rect(r.x + 10f, r.yMax - 56f, r.width - 20f, 46f);
            if (owned)
            {
                bool equipped = p.GetEquippedSkinId(s.characterId) == s.id;
                if (UIKit.Button(btn, equipped ? UIKit.T("skin.equipped", "장착 중") : UIKit.T("skin.equip", "장착"), !equipped, UIKit.ButtonKind.Normal))
                {
                    p.EquipSkin(s.characterId, s.id);
                    _lobby.ShowToast(UIKit.TF("skin.equipped_toast", "{0} 장착", LobbyUI.SkinName(s)));
                }
            }
            else if (UIKit.Button(btn, affordable ? UIKit.T("shop.buy", "구매") : UIKit.T("shop.not_enough", "현상금 부족"), affordable, UIKit.ButtonKind.Primary))
            {
                if (p.TryPurchaseSkin(s.id))
                {
                    _lobby.ShowToast(UIKit.TF("shop.bought", "{0} 구매 완료!", LobbyUI.SkinName(s)));
                    ExternalServices.Log("bounty_purchase", new Dictionary<string, object> { { "skin", s.id }, { "cost", s.cost } });
                }
            }
        }
    }
}
