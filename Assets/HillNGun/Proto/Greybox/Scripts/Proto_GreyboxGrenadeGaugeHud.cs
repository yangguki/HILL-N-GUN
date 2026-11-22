using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
  /// <summary>Radial grenade recharge ring on Greybox_Hud_Grenade (fx_v2_1 art).</summary>
  public class Proto_GreyboxGrenadeGaugeHud
  {
    const string RootName = "Greybox_Hud_GrenadeGauge";
    const float RingScaleMul = 132f / 120f;
    const float ShakeDuration = 0.22f;
    const float ShakeAmpPx = 3.5f;

    Transform _root;
    Image _bg;
    Image _fill;
    Image _flash;
    RectTransform _buttonRt;
    RectTransform _shakeTarget;
    Vector2 _buttonRestPos;
    bool _bound;
    Proto_GreyboxFxV2Catalog _fx;
    float _shakeT;

    public void EnsureBound()
    {
      if (_bound && _root != null) return;
      var btn = GameObject.Find(Proto_GreyboxHudBuilder.GrenadeButtonName);
      if (btn == null) return;
      _buttonRt = btn.GetComponent<RectTransform>();
      _shakeTarget = _buttonRt;
      if (_buttonRt != null)
        _buttonRestPos = _buttonRt.anchoredPosition;
      _fx = Proto_GreyboxFxV2Catalog.LoadRuntime();
      var existing = btn.transform.Find(RootName);
      if (existing != null)
      {
        _root = existing;
        _bg = _root.Find("GaugeBg")?.GetComponent<Image>();
        _fill = _root.Find("GaugeFill")?.GetComponent<Image>();
        _flash = _root.Find("GaugeFlash")?.GetComponent<Image>();
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
      rootRt.localScale = Vector3.one * RingScaleMul;
      _root = rootGo.transform;

      _bg = CreateRingChild("GaugeBg", 0);
      _fill = CreateRingChild("GaugeFill", 1);
      _flash = CreateRingChild("GaugeFlash", 2);
      if (_fx != null && _fx.btnGrenadeGaugeBg != null)
        _bg.sprite = _fx.btnGrenadeGaugeBg;
      if (_fx != null && _fx.btnGrenadeGaugeFill != null)
      {
        _fill.sprite = _fx.btnGrenadeGaugeFill;
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Radial360;
        _fill.fillOrigin = (int)Image.Origin360.Top;
        _fill.fillClockwise = true;
        _fill.fillAmount = 0f;
      }
      else if (_fx != null && _fx.btnGrenadeGaugeBaked != null && _fx.btnGrenadeGaugeBaked.Length > 0)
      {
        _bg.sprite = _fx.btnGrenadeGaugeBaked[0];
        _fill.type = Image.Type.Simple;
        _fill.fillAmount = 1f;
      }

      _flash.raycastTarget = false;
      _flash.gameObject.SetActive(false);
      _bound = true;
    }

    Image CreateRingChild(string name, int sibling)
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
      img.raycastTarget = false;
      img.preserveAspect = true;
      img.color = Color.white;
      return img;
    }

    public void Update(Proto_GreyboxGrenade grenade, float simTime, float dt)
    {
      if (!_bound) EnsureBound();
      if (_root == null || grenade == null) return;

      bool charging = !grenade.CanThrow(simTime);
      bool flash = grenade.IsInReadyFlash(simTime);
      float progress = grenade.RechargeProgress01(simTime);

      if (_bg != null) _bg.gameObject.SetActive(charging || flash);
      if (_fill != null)
      {
        _fill.gameObject.SetActive(charging && !flash);
        if (_fill.type == Image.Type.Filled)
          _fill.fillAmount = progress;
        else
          BakeFillStep(progress);
      }

      if (_flash != null)
      {
        bool showFlash = flash && _fx?.btnGrenadeGaugeFull != null && _fx.btnGrenadeGaugeFull.Length >= 2;
        _flash.gameObject.SetActive(showFlash);
        if (showFlash)
        {
          int idx = Mathf.FloorToInt(grenade.ReadyFlashFrame01(simTime)) % 2;
          _flash.sprite = _fx.btnGrenadeGaugeFull[idx];
          _flash.type = Image.Type.Simple;
          _flash.fillAmount = 1f;
        }
      }

      if (_shakeT > 0f)
      {
        _shakeT -= dt;
        if (_shakeTarget != null)
        {
          float wobble = Mathf.Sin(simTime * 48f) * ShakeAmpPx;
          _shakeTarget.anchoredPosition = _buttonRestPos + new Vector2(wobble, wobble * 0.35f);
        }
      }
      else if (_shakeTarget != null)
        _shakeTarget.anchoredPosition = _buttonRestPos;
    }

    void BakeFillStep(float progress01)
    {
      if (_fx?.btnGrenadeGaugeBaked == null || _fx.btnGrenadeGaugeBaked.Length < 5) return;
      int idx = Mathf.Clamp(Mathf.RoundToInt(progress01 * 4f), 0, 4);
      _fill.sprite = _fx.btnGrenadeGaugeBaked[idx];
    }

    public void TriggerChargeShake()
    {
      EnsureBound();
      _shakeT = ShakeDuration;
    }
  }
}