using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Designer cylinder on FIRE; procedural chamber dots when fx_v2 HUD art is missing.</summary>
    public class Proto_GreyboxCylinderHud
    {
        const string RootName = "Greybox_Hud_Cylinder";
        const float RefillFps = 20f;
        const float EmptyBlinkFps = 4f;

        Transform _root;
        Image _cylFace;
        readonly Image[] _chambers = new Image[5];
        Image _blinkRing;
        bool _bound;
        Proto_GreyboxFxV2Catalog _fx;
        int _lastAmmo = -1;
        int _lastRot = -1;
        bool _lastReloading;
        float _refillAnimT = -1f;
        Sprite[] _refillClip;
        int _refillFrame;
        float _emptyBlinkT;

        public void EnsureBound()
        {
            if (_bound && _root != null) return;
            _fx = Proto_GreyboxFxV2Catalog.LoadRuntime();
            var fire = GameObject.Find(Proto_GreyboxHudBuilder.FireButtonName);
            if (fire == null) return;

            var fireImg = fire.GetComponent<Image>();
            if (fireImg != null && _fx != null && _fx.btnFireCyl != null && _fx.btnFireCyl.Length > 0)
                fireImg.color = new Color(1f, 1f, 1f, 0.02f);

            var existing = fire.transform.Find(RootName);
            if (existing != null)
            {
                _root = existing;
                var face = _root.Find("CylFace");
                if (face != null)
                    _cylFace = face.GetComponent<Image>();
                for (int i = 0; i < 5; i++)
                {
                    var ch = _root.Find("Ch_" + i);
                    if (ch != null)
                        _chambers[i] = ch.GetComponent<Image>();
                }

                var blink = _root.Find("Blink");
                if (blink != null)
                    _blinkRing = blink.GetComponent<Image>();
                _bound = true;
                return;
            }

            var go = new GameObject(RootName);
            go.transform.SetParent(fire.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var faceGo = new GameObject("CylFace");
            faceGo.transform.SetParent(go.transform, false);
            var faceRt = faceGo.AddComponent<RectTransform>();
            faceRt.anchorMin = Vector2.zero;
            faceRt.anchorMax = Vector2.one;
            faceRt.offsetMin = Vector2.zero;
            faceRt.offsetMax = Vector2.zero;
            _cylFace = faceGo.AddComponent<Image>();
            _cylFace.raycastTarget = false;
            _cylFace.preserveAspect = true;

            float r = Proto_GreyboxHudBuilder.CircleVisualRadiusPx(Proto_GreyboxHudBuilder.FirePlaceRadiusPercent) * 0.55f;
            for (int i = 0; i < 5; i++)
            {
                var chGo = new GameObject("Ch_" + i);
                chGo.transform.SetParent(go.transform, false);
                var chRt = chGo.AddComponent<RectTransform>();
                chRt.sizeDelta = new Vector2(r * 0.38f, r * 0.38f);
                float ang = (90f + i * 72f) * Mathf.Deg2Rad;
                chRt.anchoredPosition = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r * 0.62f;
                var img = chGo.AddComponent<Image>();
                img.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                img.raycastTarget = false;
                _chambers[i] = img;
            }

            var blinkGo = new GameObject("Blink");
            blinkGo.transform.SetParent(go.transform, false);
            var blinkRt = blinkGo.AddComponent<RectTransform>();
            blinkRt.anchorMin = Vector2.zero;
            blinkRt.anchorMax = Vector2.one;
            blinkRt.offsetMin = Vector2.zero;
            blinkRt.offsetMax = Vector2.zero;
            _blinkRing = blinkGo.AddComponent<Image>();
            _blinkRing.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
            _blinkRing.color = new Color(1f, 0.15f, 0.1f, 0f);
            _blinkRing.raycastTarget = false;

            _root = go.transform;
            _bound = true;
        }

        public void Update(Proto_GreyboxGun gun, float simTime, float dt)
        {
            EnsureBound();
            if (_root == null) return;
            _fx ??= Proto_GreyboxFxV2Catalog.LoadRuntime();
            bool useDesigner = _fx != null && _fx.btnFireCyl != null && _fx.btnFireCyl.Length >= 6 && _cylFace != null;

            if (useDesigner)
            {
                foreach (var ch in _chambers)
                {
                    if (ch != null)
                        ch.gameObject.SetActive(false);
                }

                if (_blinkRing != null)
                    _blinkRing.gameObject.SetActive(false);

                if (_refillAnimT >= 0f)
                {
                    _refillAnimT += dt;
                    int maxF = _refillClip != null ? _refillClip.Length : 0;
                    _refillFrame = Mathf.Min(Mathf.FloorToInt(_refillAnimT * RefillFps), Mathf.Max(0, maxF - 1));
                    if (_refillClip != null && _refillFrame < _refillClip.Length)
                        _cylFace.sprite = _refillClip[_refillFrame];
                    if (maxF > 0 && _refillAnimT >= maxF / RefillFps)
                    {
                        _refillAnimT = -1f;
                        _cylFace.sprite = _fx.btnFireCyl[Mathf.Clamp(gun.Ammo, 0, 5)];
                    }
                }
                else if (gun.Ammo <= 0 && !gun.IsReloading && _fx.btnFireEmptyBlink != null && _fx.btnFireEmptyBlink.Length > 0)
                {
                    _emptyBlinkT += dt;
                    int f = Mathf.FloorToInt(_emptyBlinkT * EmptyBlinkFps) % _fx.btnFireEmptyBlink.Length;
                    _cylFace.sprite = _fx.btnFireEmptyBlink[f];
                }
                else
                {
                    _cylFace.sprite = _fx.btnFireCyl[Mathf.Clamp(gun.Ammo, 0, 5)];
                }

                _cylFace.gameObject.SetActive(true);
                _cylFace.color = Color.white;

                if (gun.Ammo != _lastAmmo && gun.IsReloading && gun.Ammo > _lastAmmo && _lastAmmo >= 0)
                {
                    _refillClip = _fx.RefillClip(_lastAmmo);
                    if (_refillClip != null && _refillClip.Length > 0)
                    {
                        _refillAnimT = 0f;
                        _refillFrame = 0;
                    }
                }
            }
            else
                UpdateProceduralChambers(gun, simTime);

            _lastAmmo = gun.Ammo;
            _lastRot = gun.CylinderRotationIndex;
            _lastReloading = gun.IsReloading;
        }

        void UpdateProceduralChambers(Proto_GreyboxGun gun, float simTime)
        {
            if (_cylFace != null)
                _cylFace.gameObject.SetActive(false);
            int mag = gun.MagazineSize;
            int rot = gun.CylinderRotationIndex % Mathf.Max(1, mag);
            for (int i = 0; i < _chambers.Length; i++)
            {
                var img = _chambers[i];
                if (img == null) continue;
                img.gameObject.SetActive(true);
                if (i >= mag)
                {
                    img.gameObject.SetActive(false);
                    continue;
                }

                int chamberIdx = (i + rot) % mag;
                bool loaded = chamberIdx < gun.Ammo;
                img.color = loaded
                    ? new Color(0.92f, 0.82f, 0.35f, 0.95f)
                    : new Color(0.22f, 0.2f, 0.18f, 0.85f);
            }

            bool emptyBlink = gun.Ammo <= 0 && !gun.IsReloading;
            if (_blinkRing != null)
            {
                _blinkRing.gameObject.SetActive(true);
                float a = emptyBlink ? 0.35f + 0.35f * Mathf.Abs(Mathf.Sin(simTime * 9f)) : 0f;
                _blinkRing.color = new Color(1f, 0.12f, 0.08f, a);
            }
        }
    }
}