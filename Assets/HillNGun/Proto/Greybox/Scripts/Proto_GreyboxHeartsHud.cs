using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxHeartsHud
    {
        const string RootName = "Greybox_Hud_Hearts";
        const int HeartCount = 3;

        Transform _root;
        readonly Image[] _fills = new Image[HeartCount];
        readonly Image[] _backs = new Image[HeartCount];
        readonly RectTransform[] _rts = new RectTransform[HeartCount];
        bool _bound;
        float _shakeT;
        int _shakeIndex = -1;
        float[] _flashT = new float[HeartCount];

        public void EnsureBound(Transform hudRoot)
        {
            if (hudRoot == null) return;
            HideLegacyHp(hudRoot);

            var existing = hudRoot.Find(RootName);
            if (existing != null)
            {
                _root = existing;
                var rootRt = existing.GetComponent<RectTransform>();
                if (rootRt != null)
                    Proto_GreyboxHudBuilder.ApplyHeartsRootRect(rootRt);
                BindChildren();
                _bound = true;
                return;
            }

            if (_bound && _root != null)
            {
                var rootRt = _root.GetComponent<RectTransform>();
                if (rootRt != null)
                    Proto_GreyboxHudBuilder.ApplyHeartsRootRect(rootRt);
                return;
            }

            var rootGo = new GameObject(RootName, typeof(RectTransform));
            rootGo.transform.SetParent(hudRoot, false);
            var rootRtNew = rootGo.GetComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyHeartsRootRect(rootRtNew);
            _root = rootGo.transform;

            float[] xs = { 0f, 34f, 68f };
            for (int i = 0; i < HeartCount; i++)
            {
                float x0 = xs[i];
                float x1 = x0 + 28f;
                _backs[i] = CreateHeartChild(_root, $"HeartBack_{i}", x0, 5f, x1, 95f,
                    new Color(0.22f, 0.08f, 0.1f, 0.75f));
                _fills[i] = CreateHeartChild(_root, $"HeartFill_{i}", x0, 5f, x1, 95f,
                    new Color(0.92f, 0.18f, 0.28f, 0.95f));
                _fills[i].type = Image.Type.Filled;
                _fills[i].fillMethod = Image.FillMethod.Vertical;
                _fills[i].fillOrigin = (int)Image.OriginVertical.Bottom;
                _fills[i].fillAmount = 1f;
                _rts[i] = _fills[i].rectTransform;
            }

            _bound = true;
        }

        static void HideLegacyHp(Transform hudRoot)
        {
            var track = hudRoot.Find("Greybox_Hud_HpBarTrack");
            if (track != null) track.gameObject.SetActive(false);
            var hpFill = GameObject.Find(Proto_GreyboxHudBuilder.HpBarFillName);
            if (hpFill != null) hpFill.SetActive(false);
            var hpText = GameObject.Find(Proto_GreyboxHudBuilder.HpTextName);
            if (hpText != null) hpText.SetActive(false);
        }

        void BindChildren()
        {
            for (int i = 0; i < HeartCount; i++)
            {
                _backs[i] = _root.Find($"HeartBack_{i}")?.GetComponent<Image>();
                _fills[i] = _root.Find($"HeartFill_{i}")?.GetComponent<Image>();
                if (_fills[i] != null)
                    _rts[i] = _fills[i].rectTransform;
            }
        }

        static Image CreateHeartChild(Transform parent, string name, float x0, float y0, float x1, float y1, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);
            var img = go.GetComponent<Image>();
            img.sprite = Proto_GreyboxSpriteUtil.GetHeartSprite();
            img.color = color;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        public void Update(Proto_GreyboxPlayerHearts hearts, float simTime, float dt)
        {
            if (!_bound) return;
            if (hearts == null) return;

            for (int i = 0; i < HeartCount; i++)
            {
                if (_fills[i] == null) continue;
                float fill = hearts.FillForHeart(i);
                _fills[i].fillAmount = fill;
                bool empty = fill <= 0.01f;
                _fills[i].color = empty
                    ? new Color(0.35f, 0.12f, 0.16f, 0.55f)
                    : new Color(0.92f, 0.18f, 0.28f, 0.95f);
                if (hearts.RestoreFlash01 > 0f && i == hearts.Hearts - 1 && fill >= 0.99f)
                {
                    float flash = 1f - hearts.RestoreFlash01;
                    _fills[i].color = Color.Lerp(Color.white, _fills[i].color, flash);
                }
            }

            if (hearts.PopShake01 > 0f && hearts.Hearts >= 0 && hearts.Hearts < HeartCount && _rts[hearts.Hearts] != null)
            {
                int idx = hearts.Hearts;
                float amp = 4f * hearts.PopShake01;
                float wob = Mathf.Sin(simTime * 42f) * amp;
                _rts[idx].anchoredPosition = new Vector2(_rts[idx].anchoredPosition.x, wob);
            }

            if (_shakeT > 0f)
            {
                _shakeT -= dt;
                if (_shakeIndex >= 0 && _shakeIndex < HeartCount && _rts[_shakeIndex] != null)
                {
                    float wob = Mathf.Sin(simTime * 48f) * 5f;
                    _rts[_shakeIndex].anchoredPosition = new Vector2(_rts[_shakeIndex].anchoredPosition.x, wob);
                }
            }
        }

        public void TriggerPop(int lostHeartIndex)
        {
            _shakeT = 0.28f;
            _shakeIndex = Mathf.Clamp(lostHeartIndex, 0, HeartCount - 1);
        }
    }
}