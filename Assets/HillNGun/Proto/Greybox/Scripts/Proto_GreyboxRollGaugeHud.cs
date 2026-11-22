using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxRollGaugeHud
    {
        const string RootName = "Greybox_Hud_RollGauge";
        const float ShakeDuration = 0.22f;
        const float ShakeAmpPx = 3.5f;

        Transform _root;
        Image _bg;
        Image _fill;
        Text _seconds;
        RectTransform _buttonRt;
        Vector2 _buttonRestPos;
        bool _bound;
        float _shakeT;

        public void EnsureBound()
        {
            if (_bound && _root != null) return;
            var btn = GameObject.Find(Proto_GreyboxHudBuilder.RollButtonName);
            if (btn == null) return;
            _buttonRt = btn.GetComponent<RectTransform>();
            if (_buttonRt != null)
                _buttonRestPos = _buttonRt.anchoredPosition;

            var existing = btn.transform.Find(RootName);
            if (existing != null)
            {
                _root = existing;
                _bg = _root.Find("GaugeBg")?.GetComponent<Image>();
                _fill = _root.Find("GaugeFill")?.GetComponent<Image>();
                _seconds = _root.Find("GaugeSeconds")?.GetComponent<Text>();
                _bound = true;
                return;
            }

            var rootGo = new GameObject(RootName, typeof(RectTransform));
            rootGo.transform.SetParent(btn.transform, false);
            var rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            _root = rootGo.transform;

            _bg = CreateRingChild("GaugeBg", 0, new Color(0.05f, 0.06f, 0.08f, 0.55f));
            _fill = CreateRingChild("GaugeFill", 1, new Color(0.75f, 0.35f, 0.95f, 0.65f));
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Radial360;
            _fill.fillOrigin = (int)Image.Origin360.Top;
            _fill.fillClockwise = true;
            _fill.fillAmount = 0f;

            var secGo = new GameObject("GaugeSeconds", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            secGo.transform.SetParent(_root, false);
            var secRt = secGo.GetComponent<RectTransform>();
            secRt.anchorMin = Vector2.zero;
            secRt.anchorMax = Vector2.one;
            secRt.offsetMin = Vector2.zero;
            secRt.offsetMax = Vector2.zero;
            _seconds = secGo.GetComponent<Text>();
            _seconds.font = Proto_GreyboxHudBuilder.GetUiFont(out _);
            _seconds.fontSize = 11;
            _seconds.alignment = TextAnchor.MiddleCenter;
            _seconds.color = new Color(1f, 0.96f, 0.88f, 0.95f);
            _seconds.raycastTarget = false;
            _bound = true;
        }

        Image CreateRingChild(string name, int sibling, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.transform.SetSiblingIndex(sibling);
            var img = go.GetComponent<Image>();
            img.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(true);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public void Update(Proto_GreyboxRollDizzy dizzy, float simTime, float dt)
        {
            if (!_bound) EnsureBound();
            if (_root == null || dizzy == null) return;

            float remain = dizzy.ChainLockRemaining(simTime);
            float dur = dizzy.ChainLockDuration;
            bool locked = remain > 0.01f;
            if (_bg != null) _bg.gameObject.SetActive(locked);
            if (_fill != null)
            {
                _fill.gameObject.SetActive(locked);
                _fill.fillAmount = dur > 0.001f ? Mathf.Clamp01(remain / dur) : 0f;
            }

            if (_seconds != null)
            {
                _seconds.gameObject.SetActive(locked);
                _seconds.text = locked ? $"{remain:0.0}" : "";
            }

            if (_shakeT > 0f)
            {
                _shakeT -= dt;
                if (_buttonRt != null)
                {
                    float wobble = Mathf.Sin(simTime * 48f) * ShakeAmpPx;
                    _buttonRt.anchoredPosition = _buttonRestPos + new Vector2(wobble, wobble * 0.35f);
                }
            }
            else if (_buttonRt != null)
                _buttonRt.anchoredPosition = _buttonRestPos;
        }

        public void TriggerLockedNudge() => _shakeT = ShakeDuration;
    }
}