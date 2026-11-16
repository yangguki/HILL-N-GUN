using System.Collections.Generic;
using UnityEngine;
using HillNGun.Characters;
using HillNGun.Combat;
using HillNGun.Core;
using HillNGun.Cover;
using HillNGun.PvE;

namespace HillNGun.Proto.TopDown
{
    /// <summary>
    /// Pixel plate and block diorama. Both are moved from logic every frame; only the active root renders.
    /// Character sprites are scaled so the player is about 13% of the screen. Hit radii stay on the data.
    /// </summary>
    public class ProtoPresentation : MonoBehaviour
    {
        public const float ScreenHeightFraction = 0.13f;
        const float FigureHeight = 1.12f;
        /// <summary>Cover art height as a fraction of the hero on-screen height (view only). TopDown keeps these.</summary>
        const float CoverBarrelFrac = 0.55f;
        const float CoverCrateFrac = 0.42f;
        const float CoverBagFrac = 0.38f;
        const float CoverBrickFrac = 0.40f;
        /// <summary>
        /// Frontline top/bottom capture was ~half the v2_1 preview: brick ~100 vs ~200 px wide,
        /// hay/crate ~40 vs ~90, barrel ~45 vs ~70 tall. Display-only multipliers (collision unchanged).
        /// </summary>
        const float FrontlineCoverBrickMul = 2.00f;
        const float FrontlineCoverHayMul = 2.25f;
        const float FrontlineCoverCrateMul = 2.25f;
        const float FrontlineCoverBarrelMul = 1.55f;

        public bool PixelOn { get; private set; } = true;
        public Camera ActiveCamera => PixelOn ? _pixelCam : _dioCam;

        Camera _pixelCam, _dioCam;
        Transform _pixelRoot, _dioRoot;
        Sprite _down, _up, _side, _white, _softShadow;
        ProtoPixelArt _art;
        Material _cel, _lineMat;
        float _dioScale = 1f;
        bool _fitted;
        bool _holdFit;
        float _aspect = 16f / 9f;

        IProtoBoard _match;
        Vis _player;
        bool _hasFrame;
        float _frameMinX, _frameMaxX, _frameMinY, _frameMaxY;
        Transform _divider;
        bool _dividerUsesFence;
        bool _dividerHorizontal;
        /// <summary>
        /// Horizontal fence scale. Planner asked 1.3×0.10=0.13; full 438px tile at 0.13 ≈ 53 plate-px vs
        /// preview rail ~41 px (~29% tall). Preview-matching scale kept at 0.10 (within ~1% of 41 px).
        /// Length comes from match DividerMinX/MaxX (fence span x 506–1425, not full dirt).
        /// </summary>
        const float FenceScaleH = 0.10f;
        const float FenceScaleV = 0.183f;
        readonly Dictionary<EnemyInstance, Vis> _enemies = new Dictionary<EnemyInstance, Vis>();
        readonly Dictionary<CharacterStateMachine, Vis> _fighters = new Dictionary<CharacterStateMachine, Vis>();
        readonly List<CharacterStateMachine> _fighterDrop = new List<CharacterStateMachine>();
        readonly Dictionary<CoverInstance, CoverVis> _covers = new Dictionary<CoverInstance, CoverVis>();
        readonly List<EnemyInstance> _drop = new List<EnemyInstance>();
        readonly List<SpriteRenderer> _pixelShots = new List<SpriteRenderer>();
        readonly List<Transform> _dioShots = new List<Transform>();
        readonly List<Marker> _markers = new List<Marker>();
        readonly List<Flash> _flashes = new List<Flash>();
        Transform _bangPixel, _bangDio;
        Transform _bgPixel;
        SpriteRenderer _bgSr;
        float _playerRecoil;

        static readonly Color Sand = new Color(0.86f, 0.71f, 0.46f);
        static readonly Color SandDark = new Color(0.74f, 0.58f, 0.36f);
        static readonly Color Brick = new Color(0.74f, 0.36f, 0.28f);
        static readonly Color Bag = new Color(0.73f, 0.64f, 0.46f);
        static readonly Color CrateC = new Color(0.80f, 0.58f, 0.32f);
        static readonly Color BarrelC = new Color(0.52f, 0.34f, 0.22f);
        static readonly Color Wood = new Color(0.62f, 0.43f, 0.26f);
        static readonly Color BlueB = new Color(0.46f, 0.66f, 0.80f);
        static readonly Color PinkB = new Color(0.87f, 0.56f, 0.60f);
        static readonly Color Cactus = new Color(0.42f, 0.64f, 0.38f);
        static readonly Color Sky = new Color(0.56f, 0.78f, 0.92f);
        /// <summary>Enemy grunts share one red-bandana kit. Pixel sheet is tinted; hit radius stays on data.</summary>
        static readonly Color EnemyShirt = new Color(0.78f, 0.52f, 0.38f);
        static readonly Color EnemyBandana = new Color(0.86f, 0.16f, 0.14f);
        static readonly Color AllyShirt = new Color(0.35f, 0.48f, 0.85f);

        class Vis
        {
            public Transform Pixel, PixelSpin, Dio, DioSpin;
            public SpriteRenderer Body, Shadow, Hp;
            public ProtoPixelArt.PoseSet Poses;
            public ProtoFlipbookSet FlipSet;
            public ProtoFlipbookAnimator Flip;
            public float Recoil;
            public bool FootPivot;
        }

        class CoverVis
        {
            public GameObject Pixel, Dio;
            public SpriteRenderer PixelSprite;
            public SpriteRenderer[] StackSprites;
            public string Kind;
            public Rect Bounds;
            public bool VerticalBrickArt;
            public bool VerticalStack;
        }

        class Marker
        {
            public Transform PixelBomb, PixelShadow, DioBomb, DioShadow;
            public LineRenderer PixelRing, DioRing;
        }

        class Flash
        {
            public Vector2 Pos;
            public float Radius;
            public float Age;
            public LineRenderer Pixel, Dio;
        }

        public void Init(IProtoBoard match, Sprite down, Sprite up, Sprite side, ProtoPixelArt art = null)
        {
            _match = match;
            _down = down;
            _up = up;
            _side = side;
            _art = art;
            if (_art != null && _art.buster != null && _art.buster.Ready)
            {
                _down = _art.buster.front != null ? _art.buster.front : _down;
                _up = _art.buster.back != null ? _art.buster.back : _up;
                _side = _art.buster.side != null ? _art.buster.side : _side;
            }
            _white = WhiteSprite();
            _softShadow = SoftEllipseSprite();
            _cel = new Material(Shader.Find("HillNGun/ProtoCel") ?? Shader.Find("Diffuse"));
            _lineMat = new Material(Shader.Find("Sprites/Default"));
            _lineMat.color = new Color(1f, 0.92f, 0.35f, 0.9f);

            _pixelRoot = new GameObject("PixelRoot").transform;
            _dioRoot = new GameObject("DioramaRoot").transform;
            _pixelCam = MakeCam("PixelCamera", _pixelRoot, true);
            _dioCam = MakeCam("DioramaCamera", _dioRoot, false);
            _pixelCam.tag = "MainCamera";
            _dioCam.tag = "Untagged";
            _pixelCam.depth = 0;
            _dioCam.depth = -1;
            _dioCam.enabled = false;

            var lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(_dioRoot, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            light.shadowBias = 0.05f;
            lightGo.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.60f, 0.64f);

            BuildScenery();
            // Frontline uses SyncFighters only — skip the unused TopDown player Vis so it cannot
            // sit unscaled at the origin (that looked like an oversized Buster on the move stick).
            if (match.Fighters == null)
            {
                _player = MakeActor(_pixelRoot, _dioRoot, new Color(0.93f, 0.86f, 0.72f), new Color(0.45f, 0.28f, 0.16f), new Color(0.75f, 0.18f, 0.18f), true);
                if (_art != null)
                {
                    _player.Poses = _art.HeroForSlot(0);
                    BindFlip(_player, _art.FlipForSlot(0), _player.Poses);
                }
                if (match.Player != null)
                    match.Player.OnFired += (_, __, ___) => _playerRecoil = 1f;
            }
            foreach (var piece in match.Covers)
                _covers[piece] = MakeCover(piece);
            _bangPixel = Bang(_pixelRoot, 0.01f);
            _bangDio = Bang(_dioRoot, 0.4f);
            if (match.Combat != null)
                match.Combat.OnGrenadeExploded += (_, pos) => _flashes.Add(MakeFlash(pos, match.Player != null && match.Player.Data.grenade != null ? match.Player.Data.grenade.radius : 1.5f));
            SetPixel(true);
            float aspect = CamAspect();
            if (aspect > 0.2f) Fit(aspect);
        }

        public void SetPixel(bool pixel)
        {
            PixelOn = pixel;
            ProtoSkinSwitch.Apply(_pixelRoot.gameObject, _dioRoot.gameObject, pixel);
            if (_pixelCam != null)
            {
                _pixelCam.enabled = pixel;
                if (pixel) _pixelCam.tag = "MainCamera";
            }
            if (_dioCam != null)
            {
                _dioCam.enabled = !pixel;
                if (!pixel) _dioCam.tag = "MainCamera";
                else _dioCam.tag = "Untagged";
            }
        }

        public void FitTo(RenderTexture rt)
        {
            _holdFit = true;
            var prevPixel = _pixelCam.targetTexture;
            var prevDio = _dioCam.targetTexture;
            _pixelCam.targetTexture = rt;
            _dioCam.targetTexture = rt;
            Fit((float)rt.width / rt.height);
            _pixelCam.targetTexture = prevPixel;
            _dioCam.targetTexture = prevDio;
        }

        public void SetFrame(float minX, float maxX, float minY, float maxY)
        {
            _hasFrame = true;
            _frameMinX = minX;
            _frameMaxX = maxX;
            _frameMinY = minY;
            _frameMaxY = maxY;
            _fitted = false;
            LayoutBackground();
        }

        public void Fit(float aspect)
        {
            if (aspect < 0.2f) aspect = 16f / 9f;
            _aspect = aspect;
            _fitted = true;
            if (_pixelCam != null) _pixelCam.aspect = aspect;
            if (_dioCam != null) _dioCam.aspect = aspect;
            FitPixel(_pixelCam, aspect);
            FitDiorama(_dioCam, aspect);
            LayoutBackground();
            var feet = ProtoArena.ToWorld3D(_match.Player != null ? _match.Player.Position : ProtoArena.PlayerSpawn);
            float dy = _dioCam.WorldToViewportPoint(feet + Vector3.up * FigureHeight).y - _dioCam.WorldToViewportPoint(feet).y;
            _dioScale = dy > 0.002f ? ScreenHeightFraction / dy : 1f;
        }

        void LateUpdate()
        {
            if (_match == null) return;
            if (!_holdFit && _pixelCam != null)
            {
                float aspect = CamAspect();
                if (!_fitted || Mathf.Abs(aspect - _aspect) > 0.01f) Fit(aspect);
            }
            SyncActors();
            SyncDivider();
            SyncCovers();
            SyncShots();
            SyncGrenades();
            SyncFlashes();
            SyncBang();
        }

        float CamAspect()
        {
            if (_pixelCam != null && _pixelCam.pixelWidth > 1 && _pixelCam.pixelHeight > 1)
                return (float)_pixelCam.pixelWidth / _pixelCam.pixelHeight;
            return (float)Screen.width / Mathf.Max(1, Screen.height);
        }

        void SyncActors()
        {
            if (_match.Fighters != null)
            {
                SyncFighters();
                return;
            }
            var player = _match.Player;
            if (player != null && _player != null)
            {
                _playerRecoil = Mathf.MoveTowards(_playerRecoil, 0f, Time.deltaTime * 8f);
                bool shooting = player.ActionState == ActionState.Firing || _playerRecoil > 0.2f ||
                                (_player.Flip != null && _player.Flip.ShootPlaying);
                bool aiming = player.IsAimingWeapon || shooting;
                float maxSpd = player.Data != null ? player.Data.moveSpeed : 5f;
                PlaceFlip(_player, player.Position, player.FreeVelocity, _match.AimDirection,
                    aiming, shooting, player.FreeVelocity.magnitude, maxSpd, _playerRecoil, player.HpRatio);
            }
            _drop.Clear();
            foreach (var kv in _enemies)
                if (!Listed(kv.Key)) _drop.Add(kv.Key);
            for (int i = 0; i < _drop.Count; i++)
            {
                Destroy(_enemies[_drop[i]].Pixel.gameObject);
                Destroy(_enemies[_drop[i]].Dio.gameObject);
                _enemies.Remove(_drop[i]);
            }
            var list = _match.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (!_enemies.TryGetValue(e, out var vis))
                {
                    vis = MakeActor(_pixelRoot, _dioRoot, EnemyShirt, EnemyBandana * 0.75f, EnemyBandana, false);
                    if (_art != null)
                    {
                        vis.Poses = _art.EnemyPoses;
                        // Designer grunt sheet already has its own colors — drop the red tint.
                        if (vis.Poses != null) vis.Body.color = Color.white;
                        BindFlip(vis, _art.EnemyFlip, vis.Poses);
                    }
                    _enemies[e] = vis;
                    e.OnAttack += (_, __, ___) => vis.Recoil = 1f;
                }
                bool show = e.IsBodyVisible && !e.IsGone;
                vis.Pixel.gameObject.SetActive(show);
                vis.Dio.gameObject.SetActive(show);
                if (!show) continue;
                Vector2 face = e.InEntrance ? Vector2.down : (e.IsWindingUp ? e.AttackDirection : player.Position - e.Position);
                if (face.sqrMagnitude < 1e-4f) face = Vector2.down;
                vis.Recoil = Mathf.MoveTowards(vis.Recoil, 0f, Time.deltaTime * 8f);
                bool moving = !e.InEntrance && (e.IsDodging || (e.Post - e.Position).sqrMagnitude > 0.05f);
                Vector2 moveVec = moving ? ((e.Post - e.Position).sqrMagnitude > 1e-4f ? (e.Post - e.Position) : face) : Vector2.zero;
                bool shooting = e.IsWindingUp || vis.Recoil > 0.2f || (vis.Flip != null && vis.Flip.ShootPlaying);
                float speed = moving ? Mathf.Max(ProtoFacingSelector.MoveSpeedEps * 2f, (e.Post - e.Position).magnitude) : 0f;
                PlaceFlip(vis, e.Position, moveVec, face.normalized, shooting, shooting, speed, 3f, vis.Recoil,
                    e.CurrentHp / (float)Mathf.Max(1, e.MaxHp));
            }
        }

        void BindFlip(Vis vis, ProtoFlipbookSet set, ProtoPixelArt.PoseSet poses)
        {
            vis.FlipSet = set;
            vis.Poses = poses;
            if (vis.Flip == null) vis.Flip = new ProtoFlipbookAnimator();
            vis.Flip.Bind(vis.Body, set, poses);
            vis.FootPivot = set != null;
        }

        void PlaceFlip(Vis vis, Vector2 logic, Vector2 moveVec, Vector2 aimVec, bool aiming, bool shooting,
            float actualSpeed, float maxSpeed, float recoil, float hp)
        {
            Vector2 face = aimVec.sqrMagnitude > 1e-4f ? aimVec.normalized :
                (moveVec.sqrMagnitude > 1e-4f ? moveVec.normalized : Vector2.down);
            bool usedFlip = false;
            if (vis.Flip != null)
                usedFlip = vis.Flip.Tick(moveVec, aimVec, aiming, shooting, actualSpeed, maxSpeed, Time.deltaTime);
            if (!usedFlip)
                ApplySprite(vis, face);

            if (vis.Flip != null)
                face = ProtoFacingSelector.FaceDirFromState(vis.Flip.Facing, vis.Flip.FlipX);

            // Flipbooks replace the old temp bounce; keep a tiny bob only for static-pose fallback.
            float bob = (!usedFlip && actualSpeed > ProtoFacingSelector.MoveSpeedEps)
                ? Mathf.Sin(Time.time * 14f) * 0.035f : 0f;
            Vector2 kick = -face * recoil * 0.12f;
            vis.Pixel.position = ProtoArena.ToPixel(logic + kick);
            vis.PixelSpin.localPosition = new Vector3(0f, bob, 0f);
            float targetH = _pixelCam.orthographicSize * 2f * ScreenHeightFraction;
            var sprite = vis.Body.sprite;
            float units = sprite != null ? sprite.rect.height / sprite.pixelsPerUnit : 1f;
            float s = units > 0.01f ? targetH / units : 1f;
            vis.PixelSpin.localScale = new Vector3(s, s, 1f);
            vis.Shadow.transform.localScale = new Vector3(targetH * 0.38f, targetH * 0.11f, 1f);
            // Foot-pivoted frames sit on the transform; center-pivoted poses need the old offset.
            float shadowY = vis.FootPivot && usedFlip ? -targetH * 0.02f : -targetH * 0.36f;
            float hpY = vis.FootPivot && usedFlip ? targetH * 1.05f : targetH * 0.62f;
            vis.Shadow.transform.localPosition = new Vector3(0f, shadowY, 0f);
            vis.Hp.transform.localScale = new Vector3(targetH * 0.7f * Mathf.Clamp01(hp), targetH * 0.06f, 1f);
            vis.Hp.transform.localPosition = new Vector3(0f, hpY, 0f);
            vis.Hp.color = hp > 0.35f ? new Color(0.35f, 0.85f, 0.4f) : new Color(0.9f, 0.3f, 0.25f);
            int order = 1000 - Mathf.RoundToInt(logic.y * 20f);
            vis.Body.sortingOrder = order;
            vis.Shadow.sortingOrder = order - 1;
            vis.Hp.sortingOrder = order + 2;

            vis.Dio.position = ProtoArena.ToWorld3D(logic, 0f);
            float yaw = Mathf.Atan2(face.x, face.y) * Mathf.Rad2Deg;
            vis.Dio.rotation = Quaternion.Euler(0f, yaw, 0f);
            vis.DioSpin.localPosition = new Vector3(-face.x * recoil * 0.08f, bob, -face.y * recoil * 0.08f);
            vis.DioSpin.localScale = Vector3.one * _dioScale;
        }

        void SyncFighters()
        {
            var list = _match.Fighters;
            _fighterDrop.Clear();
            foreach (var kv in _fighters)
            {
                bool keep = false;
                for (int i = 0; i < list.Count; i++) if (list[i] == kv.Key) { keep = true; break; }
                if (!keep) _fighterDrop.Add(kv.Key);
            }
            for (int i = 0; i < _fighterDrop.Count; i++)
            {
                Destroy(_fighters[_fighterDrop[i]].Pixel.gameObject);
                Destroy(_fighters[_fighterDrop[i]].Dio.gameObject);
                _fighters.Remove(_fighterDrop[i]);
            }
            int playerSlot = 0, enemySlot = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var f = list[i];
                if (f == null) continue;
                int slot = f.Team == HillNGun.Core.Team.Player ? playerSlot++ : enemySlot++;
                if (!_fighters.TryGetValue(f, out var vis))
                {
                    bool human = f == _match.Player;
                    bool ally = f.Team == HillNGun.Core.Team.Player;
                    Color shirt = human ? new Color(0.93f, 0.86f, 0.72f) : ally ? AllyShirt : EnemyShirt;
                    Color hat = human ? new Color(0.45f, 0.28f, 0.16f) : ally ? AllyShirt * 0.7f : EnemyBandana * 0.75f;
                    Color band = human ? new Color(0.75f, 0.18f, 0.18f) : ally ? AllyShirt : EnemyBandana;
                    vis = MakeActor(_pixelRoot, _dioRoot, shirt, hat, band, human);
                    if (_art != null)
                    {
                        var poses = ally ? _art.HeroForSlot(slot) : _art.EnemyPoses;
                        var flip = ally ? _art.FlipForSlot(slot) : _art.EnemyFlip;
                        BindFlip(vis, flip, poses);
                        if (vis.Poses != null) vis.Body.color = Color.white;
                    }
                    _fighters[f] = vis;
                    var body = vis;
                    f.OnFired += (_, __, ___) => body.Recoil = 1f;
                }
                bool show = f.OverallState != CharacterOverallState.Eliminated;
                vis.Pixel.gameObject.SetActive(show);
                vis.Dio.gameObject.SetActive(show);
                if (!show) continue;
                Vector2 idleFace = DefaultFace(f.Team);
                Vector2 aim = f == _match.Player
                    ? _match.AimDirection
                    : (f.AimDirection.sqrMagnitude > 0.01f ? f.AimDirection : idleFace);
                if (aim.sqrMagnitude < 1e-4f) aim = idleFace;
                vis.Recoil = Mathf.MoveTowards(vis.Recoil, 0f, Time.deltaTime * 8f);
                bool shooting = f.ActionState == ActionState.Firing || vis.Recoil > 0.2f ||
                                (vis.Flip != null && vis.Flip.ShootPlaying);
                bool aiming = f.IsAimingWeapon || shooting;
                float maxSpd = f.Data != null ? f.Data.moveSpeed : 5f;
                PlaceFlip(vis, f.Position, f.FreeVelocity, aim, aiming, shooting,
                    f.FreeVelocity.magnitude, maxSpd, vis.Recoil, f.HpRatio);
            }
        }

        Vector2 DefaultFace(HillNGun.Core.Team team)
        {
            bool topBottom = _match != null && !float.IsNaN(_match.DividerY);
            if (topBottom) return team == HillNGun.Core.Team.Player ? Vector2.up : Vector2.down;
            return team == HillNGun.Core.Team.Player ? Vector2.right : Vector2.left;
        }

        void SyncDivider()
        {
            bool horizontal = _match != null && !float.IsNaN(_match.DividerY);
            bool vertical = _match != null && !float.IsNaN(_match.DividerX);
            if (!horizontal && !vertical)
            {
                if (_divider != null) _divider.gameObject.SetActive(false);
                return;
            }
            Sprite fence = null;
            Sprite cap = null;
            if (_art != null)
            {
                if (horizontal)
                {
                    fence = _art.fenceDividerH != null ? _art.fenceDividerH : _art.fenceDivider;
                    cap = _art.fenceCapH != null ? _art.fenceCapH : _art.fenceCap;
                }
                else
                {
                    fence = _art.fenceDivider;
                    cap = _art.fenceCap;
                }
            }
            bool useFence = fence != null;
            if (_divider != null && (_dividerUsesFence != useFence || _dividerHorizontal != horizontal))
            {
                Object.Destroy(_divider.gameObject);
                _divider = null;
            }
            if (_divider == null)
            {
                var go = new GameObject("Divider");
                go.transform.SetParent(_pixelRoot, false);
                _divider = go.transform;
                _dividerUsesFence = useFence;
                _dividerHorizontal = horizontal;
                if (!useFence)
                {
                    // Fallback faint dashed midline when fence art is missing.
                    const int dashes = 16;
                    var tint = new Color(0.32f, 0.22f, 0.14f, 0.38f);
                    for (int i = 0; i < dashes; i++)
                    {
                        var dash = new GameObject("dash_" + i);
                        dash.transform.SetParent(_divider, false);
                        var sr = dash.AddComponent<SpriteRenderer>();
                        sr.sprite = _white;
                        sr.color = tint;
                        sr.sortingOrder = 40;
                    }
                }
            }
            _divider.gameObject.SetActive(PixelOn);
            if (horizontal)
            {
                float y = _match.DividerY;
                var a = ProtoArena.ToPixel(new Vector2(_match.DividerMinX, y));
                var b = ProtoArena.ToPixel(new Vector2(_match.DividerMaxX, y));
                float x0 = Mathf.Min(a.x, b.x);
                float x1 = Mathf.Max(a.x, b.x);
                float span = Mathf.Max(0.2f, x1 - x0);
                if (useFence)
                {
                    LayoutFenceDividerH(fence, cap, a.y, x0, span);
                    return;
                }
                LayoutDashDivider(true, a.y, x0, span);
                return;
            }
            {
                float x = _match.DividerX;
                var a = ProtoArena.ToPixel(new Vector2(x, _match.DividerMinY));
                var b = ProtoArena.ToPixel(new Vector2(x, _match.DividerMaxY));
                float y0 = Mathf.Min(a.y, b.y);
                float y1 = Mathf.Max(a.y, b.y);
                float span = Mathf.Max(0.2f, y1 - y0);
                if (useFence)
                {
                    LayoutFenceDividerV(fence, cap, a.x, y0, span);
                    return;
                }
                LayoutDashDivider(false, a.x, y0, span);
            }
        }

        void LayoutDashDivider(bool horizontal, float fixedCoord, float start, float span)
        {
            int n = _divider.childCount;
            float pitch = span / Mathf.Max(1, n);
            float dash = pitch * 0.42f;
            const float thin = 0.055f;
            for (int i = 0; i < n; i++)
            {
                var t = _divider.GetChild(i);
                float along = start + (i + 0.5f) * pitch;
                t.position = horizontal
                    ? new Vector3(along, fixedCoord, 0f)
                    : new Vector3(fixedCoord, along, 0f);
                t.localScale = horizontal
                    ? new Vector3(dash, thin, 1f)
                    : new Vector3(thin, dash, 1f);
            }
        }

        void LayoutFenceDividerV(Sprite fence, Sprite cap, float x, float y0, float span)
        {
            float unitsH = fence.rect.height / fence.pixelsPerUnit;
            if (unitsH < 0.01f) return;
            float scale = FenceScaleV;
            float tileH = unitsH * scale;
            int n = Mathf.Max(1, Mathf.CeilToInt(span / Mathf.Max(0.01f, tileH * 0.92f)));
            for (int i = 0; i < n; i++)
            {
                var t = EnsureDividerChild("fence_" + i, fence, 35);
                t.gameObject.SetActive(true);
                t.position = new Vector3(x, y0 + (i + 0.5f) * tileH * 0.92f, 0f);
                t.localScale = new Vector3(scale, scale, 1f);
            }
            HideExtraFence(n);
            float railBottom = y0;
            float railTop = y0 + n * tileH * 0.92f;
            LayoutFenceCapsV(cap, x, railBottom, railTop, scale);
        }

        void LayoutFenceDividerH(Sprite fence, Sprite cap, float y, float x0, float span)
        {
            float unitsW = fence.rect.width / fence.pixelsPerUnit;
            if (unitsW < 0.01f) return;
            float scale = FenceScaleH;
            float tileW = unitsW * scale;
            // Slight overlap between tiles along the split.
            float step = tileW * 0.92f;
            int n = Mathf.Max(1, Mathf.CeilToInt(span / Mathf.Max(0.01f, step)));
            for (int i = 0; i < n; i++)
            {
                var t = EnsureDividerChild("fence_" + i, fence, 35);
                t.gameObject.SetActive(true);
                t.position = new Vector3(x0 + (i + 0.5f) * step, y, 0f);
                t.localScale = new Vector3(scale, scale, 1f);
            }
            HideExtraFence(n);
            float railLeft = x0;
            float railRight = x0 + n * step;
            LayoutFenceCapsH(cap, y, railLeft, railRight, scale);
        }

        void HideExtraFence(int n)
        {
            for (int i = n; ; i++)
            {
                var extra = _divider.Find("fence_" + i);
                if (extra == null) break;
                extra.gameObject.SetActive(false);
            }
        }

        void LayoutFenceCapsV(Sprite cap, float x, float railBottom, float railTop, float scale)
        {
            var bot = _divider.Find("capBot");
            var top = _divider.Find("capTop");
            if (cap == null)
            {
                if (bot != null) bot.gameObject.SetActive(false);
                if (top != null) top.gameObject.SetActive(false);
                return;
            }
            float capH = (cap.rect.height / cap.pixelsPerUnit) * scale;
            const float overlap = 0.05f;
            bot = EnsureDividerChild("capBot", cap, 36);
            bot.gameObject.SetActive(true);
            bot.position = new Vector3(x, railBottom + capH * 0.5f - overlap, 0f);
            bot.localScale = new Vector3(scale, scale, 1f);
            top = EnsureDividerChild("capTop", cap, 36);
            top.gameObject.SetActive(true);
            top.position = new Vector3(x, railTop - capH * 0.5f + overlap, 0f);
            top.localScale = new Vector3(scale, scale, 1f);
        }

        void LayoutFenceCapsH(Sprite cap, float y, float railLeft, float railRight, float scale)
        {
            var left = _divider.Find("capBot");
            var right = _divider.Find("capTop");
            if (cap == null)
            {
                if (left != null) left.gameObject.SetActive(false);
                if (right != null) right.gameObject.SetActive(false);
                return;
            }
            float capW = (cap.rect.width / cap.pixelsPerUnit) * scale;
            const float overlap = 0.05f;
            left = EnsureDividerChild("capBot", cap, 36);
            left.gameObject.SetActive(true);
            left.position = new Vector3(railLeft + capW * 0.5f - overlap, y, 0f);
            left.localScale = new Vector3(scale, scale, 1f);
            right = EnsureDividerChild("capTop", cap, 36);
            right.gameObject.SetActive(true);
            right.position = new Vector3(railRight - capW * 0.5f + overlap, y, 0f);
            // Mirror the right cap so both ends read as posts.
            right.localScale = new Vector3(-scale, scale, 1f);
        }

        Transform EnsureDividerChild(string name, Sprite sprite, int sortingOrder)
        {
            var t = _divider.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(_divider, false);
                t = go.transform;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = sortingOrder;
            }
            var renderer = t.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = sprite;
                renderer.color = Color.white;
                renderer.sortingOrder = sortingOrder;
            }
            return t;
        }

        bool Listed(EnemyInstance e)
        {
            var list = _match.Enemies;
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++) if (list[i] == e) return true;
            return false;
        }

        void ApplySprite(Vis vis, Vector2 face)
        {
            if (_art != null && vis.Poses != null)
            {
                var spr = _art.Pick(vis.Poses, face, out bool flip);
                if (spr != null)
                {
                    vis.Body.sprite = spr;
                    vis.Body.flipX = flip;
                    return;
                }
            }
            if (_side == null || _down == null || _up == null)
            {
                vis.Body.sprite = _white;
                return;
            }
            if (Mathf.Abs(face.x) > Mathf.Abs(face.y) * 0.85f)
            {
                vis.Body.sprite = _side;
                vis.Body.flipX = face.x < 0f;
            }
            else
            {
                vis.Body.flipX = false;
                vis.Body.sprite = face.y >= 0f ? _up : _down;
            }
        }

        void SyncCovers()
        {
            float heroH = _pixelCam != null ? _pixelCam.orthographicSize * 2f * ScreenHeightFraction : 1f;
            foreach (var kv in _covers)
            {
                bool up = kv.Key != null && !kv.Key.IsDestroyed;
                if (kv.Value.Pixel != null) kv.Value.Pixel.SetActive(up);
                if (kv.Value.Dio != null) kv.Value.Dio.SetActive(up);
                if (!up) continue;
                if (!kv.Value.VerticalBrickArt && !kv.Value.VerticalStack && kv.Value.PixelSprite != null && _art != null)
                {
                    // Hay/crate: swap s0/s1/s2 by durability (same canvas / foot-aligned).
                    float ratio = kv.Key.HpRatio;
                    var next = _art.CoverSprite(kv.Value.Kind, ratio);
                    if (next != null && kv.Value.PixelSprite.sprite != next)
                        kv.Value.PixelSprite.sprite = next;
                }
                if (kv.Value.VerticalBrickArt) ScaleVerticalBrick(kv.Value, heroH);
                else if (kv.Value.VerticalStack) ScaleCoverStack(kv.Value, heroH);
                else if (kv.Value.PixelSprite != null && kv.Value.PixelSprite.sprite != null)
                    ScaleCoverSprite(kv.Value, heroH);
            }
        }

        bool IsFrontlineBoard => _match != null && _match.Fighters != null;

        float CoverFracFor(string kind)
        {
            float frac = kind == "barrel" ? CoverBarrelFrac
                : kind == "crate" ? CoverCrateFrac
                : kind == "brick" ? CoverBrickFrac
                : CoverBagFrac;
            if (!IsFrontlineBoard) return frac;
            float mul = kind == "barrel" ? FrontlineCoverBarrelMul
                : kind == "crate" ? FrontlineCoverCrateMul
                : kind == "brick" ? FrontlineCoverBrickMul
                : FrontlineCoverHayMul; // hay + sandbag
            return frac * mul;
        }

        void ScaleCoverSprite(CoverVis vis, float heroH)
        {
            var spr = vis.PixelSprite.sprite;
            float unitsH = spr.rect.height / spr.pixelsPerUnit;
            float unitsW = spr.rect.width / spr.pixelsPerUnit;
            if (unitsH < 0.01f) return;
            float frac = CoverFracFor(vis.Kind);
            float targetH = heroH * frac;
            float s = targetH / unitsH;
            // TopDown only: keep wide brick footprint near the collision box. Frontline skips so
            // preview-matching display scale is not capped by the smaller collider.
            if (!IsFrontlineBoard && vis.Kind == "brick" && vis.Bounds.width > vis.Bounds.height * 1.2f && unitsW > 0.01f)
            {
                float widthS = Mathf.Max(0.2f, vis.Bounds.width / unitsW);
                s = Mathf.Min(s, widthS);
            }
            vis.Pixel.transform.localScale = new Vector3(s, s, 1f);
            int order = 40 + Mathf.RoundToInt((20f - vis.Bounds.center.y) * 2f);
            vis.PixelSprite.sortingOrder = order;
        }

        void ScaleVerticalBrick(CoverVis vis, float heroH)
        {
            if (vis.Pixel == null || vis.PixelSprite == null) return;
            float s = HorizontalBrickWorldScale(heroH) * 1.35f;
            vis.Pixel.transform.localScale = new Vector3(s, s, 1f);
            int order = 40 + Mathf.RoundToInt((20f - vis.Bounds.center.y) * 2f);
            vis.PixelSprite.sortingOrder = order;
        }

        float HorizontalBrickWorldScale(float heroH)
        {
            Sprite brick = _art != null ? _art.brick : null;
            if (brick == null) return 1f;
            float unitsH = brick.rect.height / brick.pixelsPerUnit;
            if (unitsH < 0.01f) return 1f;
            return (heroH * CoverFracFor("brick")) / unitsH;
        }

        void ScaleCoverStack(CoverVis vis, float heroH)
        {
            if (vis.StackSprites == null) return;
            int order = 40 + Mathf.RoundToInt((20f - vis.Bounds.center.y) * 2f);
            for (int i = 0; i < vis.StackSprites.Length; i++)
            {
                var sr = vis.StackSprites[i];
                if (sr == null || sr.sprite == null) continue;
                float unitsH = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                if (unitsH < 0.01f) continue;
                bool barrel = (i % 2) == 1;
                float frac = barrel ? CoverFracFor("barrel") * 0.92f : CoverFracFor("crate");
                float s = (heroH * frac) / unitsH;
                sr.transform.localScale = new Vector3(s, s, 1f);
                sr.sortingOrder = order + i;
            }
        }

        void SyncShots()
        {
            var pool = _match.Combat != null ? _match.Combat.ProjectilePool.ActiveProjectiles : null;
            int n = 0;
            if (pool != null)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    var p = pool[i];
                    if (p == null || !p.IsActive) continue;
                    var sr = ShotSprite(n);
                    var dio = ShotDio(n);
                    sr.enabled = true;
                    dio.gameObject.SetActive(true);
                    sr.transform.position = ProtoArena.ToPixel(p.Position);
                    sr.color = ProtoShotLook.ColorFor(p.OwnerTeam);
                    float scale = ProtoShotLook.ScaleFor(p.OwnerTeam);
                    sr.transform.localScale = Vector3.one * scale;
                    dio.position = ProtoArena.ToWorld3D(p.Position, 0.35f);
                    dio.localScale = Vector3.one * scale;
                    n++;
                }
            }
            for (int i = n; i < _pixelShots.Count; i++) { _pixelShots[i].enabled = false; _dioShots[i].gameObject.SetActive(false); }
        }

        void SyncGrenades()
        {
            int n = 0;
            var combat = _match.Combat;
            if (combat != null)
            {
                var list = combat.ActiveGrenades;
                for (int i = 0; i < list.Count; i++)
                {
                    var g = list[i];
                    if (g == null || !g.IsActive) continue;
                    var m = MarkerAt(n++);
                    float r = g.Data != null ? g.Data.radius : 1.5f;
                    PlaceMarker(m, g.CurrentPosition, g.HeightOffset, r, true);
                }
            }
            if (_match.ShowGrenadeAim)
            {
                var m = MarkerAt(n++);
                float r = _match.Player != null && _match.Player.Data != null && _match.Player.Data.grenade != null ? _match.Player.Data.grenade.radius : 1.5f;
                PlaceMarker(m, _match.GrenadeAimPoint, 0f, r, false);
            }
            for (int i = n; i < _markers.Count; i++) SetMarker(false, _markers[i]);
        }

        void PlaceMarker(Marker m, Vector2 ground, float height, float radius, bool bomb)
        {
            SetMarker(true, m);
            m.PixelBomb.gameObject.SetActive(bomb);
            m.DioBomb.gameObject.SetActive(bomb);
            m.PixelShadow.gameObject.SetActive(bomb);
            m.DioShadow.gameObject.SetActive(bomb);
            m.PixelBomb.position = ProtoArena.ToPixel(ground + new Vector2(0f, height));
            m.PixelShadow.position = ProtoArena.ToPixel(ground);
            m.DioBomb.position = ProtoArena.ToWorld3D(ground, height);
            m.DioShadow.position = ProtoArena.ToWorld3D(ground, 0.05f);
            Ring(m.PixelRing, ProtoArena.ToPixel(ground), radius, radius * ProtoArena.PixelYCompress, true);
            Ring(m.DioRing, ProtoArena.ToWorld3D(ground, 0.06f), radius, radius, false);
        }

        void SyncFlashes()
        {
            for (int i = _flashes.Count - 1; i >= 0; i--)
            {
                var f = _flashes[i];
                f.Age += Time.deltaTime;
                float u = Mathf.Clamp01(f.Age / 0.28f);
                var c = Color.Lerp(new Color(1f, 0.85f, 0.4f, 0.9f), new Color(1f, 0.5f, 0.2f, 0f), u);
                f.Pixel.startColor = f.Pixel.endColor = c;
                f.Dio.startColor = f.Dio.endColor = c;
                Ring(f.Pixel, ProtoArena.ToPixel(f.Pos), f.Radius * (0.4f + u), f.Radius * ProtoArena.PixelYCompress * (0.4f + u), true);
                Ring(f.Dio, ProtoArena.ToWorld3D(f.Pos, 0.08f), f.Radius * (0.4f + u), f.Radius * (0.4f + u), false);
                if (f.Age >= 0.28f)
                {
                    Destroy(f.Pixel.gameObject);
                    Destroy(f.Dio.gameObject);
                    _flashes.RemoveAt(i);
                }
            }
        }

        void SyncBang()
        {
            bool show = false;
            var list = _match.Enemies;
            if (list != null)
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Entrance == EntrancePhase.Telegraph) show = true;
            _bangPixel.gameObject.SetActive(show);
            _bangDio.gameObject.SetActive(show);
            if (!show) return;
            float bob = 0.15f + Mathf.Sin(Time.time * 10f) * 0.08f;
            var door = ProtoArena.Door().TelegraphPos;
            _bangPixel.position = ProtoArena.ToPixel(door) + new Vector3(0f, bob, 0f);
            _bangDio.position = ProtoArena.ToWorld3D(door, 1.3f + bob);
        }

        Vis MakeActor(Transform pixelParent, Transform dioParent, Color shirt, Color hat, Color band, bool playerTint)
        {
            var vis = new Vis();
            var pixel = new GameObject(playerTint ? "BusterPixel" : "EnemyPixel");
            pixel.transform.SetParent(pixelParent, false);
            vis.Pixel = pixel.transform;
            var spin = new GameObject("Spin");
            spin.transform.SetParent(pixel.transform, false);
            vis.PixelSpin = spin.transform;
            vis.Body = spin.AddComponent<SpriteRenderer>();
            vis.Body.sprite = _down != null ? _down : _white;
            if (!playerTint) vis.Body.color = Color.Lerp(Color.white, shirt, 0.72f);
            vis.Shadow = MakeSprite(pixel.transform, "Shadow", _softShadow != null ? _softShadow : _white, Color.white, -1);
            vis.Hp = MakeSprite(pixel.transform, "Hp", _white, Color.green, 2);

            var dio = new GameObject(playerTint ? "Buster3D" : "Enemy3D");
            dio.transform.SetParent(dioParent, false);
            vis.Dio = dio.transform;
            var dspin = new GameObject("Spin");
            dspin.transform.SetParent(dio.transform, false);
            vis.DioSpin = dspin.transform;
            var shirtC = playerTint ? new Color(0.93f, 0.88f, 0.78f) : shirt;
            var pants = playerTint ? new Color(0.28f, 0.40f, 0.72f) : shirt * 0.55f;
            Part(dspin.transform, "legs", new Vector3(0f, 0.18f, 0f), new Vector3(0.36f, 0.36f, 0.22f), pants);
            Part(dspin.transform, "body", new Vector3(0f, 0.52f, 0f), new Vector3(0.46f, 0.40f, 0.28f), shirtC);
            Part(dspin.transform, "head", new Vector3(0f, 0.86f, 0f), new Vector3(0.32f, 0.30f, 0.30f), new Color(0.93f, 0.76f, 0.58f));
            Part(dspin.transform, "brim", new Vector3(0f, 1.00f, 0f), new Vector3(0.56f, 0.05f, 0.56f), hat);
            Part(dspin.transform, "hat", new Vector3(0f, 1.10f, 0f), new Vector3(0.34f, 0.14f, 0.34f), hat);
            Part(dspin.transform, "band", new Vector3(0f, 0.70f, 0.12f), new Vector3(0.30f, 0.08f, 0.06f), band);
            Part(dspin.transform, "gun", new Vector3(0.22f, 0.58f, 0.22f), new Vector3(0.12f, 0.08f, 0.42f), new Color(0.25f, 0.25f, 0.28f));
            var blob = Part(dio.transform, "contact", new Vector3(0f, 0.02f, 0f), new Vector3(0.5f, 0.03f, 0.32f), new Color(0.35f, 0.26f, 0.16f));
            blob.transform.localScale = new Vector3(0.55f, 0.02f, 0.32f);
            return vis;
        }

        CoverVis MakeCover(CoverInstance cover)
        {
            var b = cover.GetBounds();
            var kind = CoverKind(cover);
            var color = kind == "brick" ? Brick : kind == "sandbag" || kind == "hay" ? Bag : kind == "barrel" ? BarrelC : CrateC;
            float h = kind == "brick" ? 1.15f : kind == "sandbag" || kind == "hay" ? 0.72f : kind == "barrel" ? 1.05f : 0.9f;
            // Vertical brick segments (height > width): dedicated vertical wall art; collision unchanged.
            bool verticalBrick = kind == "brick" && b.height > b.width;
            Sprite vertArt = verticalBrick && _art != null ? _art.brickVertical : null;
            var vis = new CoverVis { Kind = kind, Bounds = b };
            if (verticalBrick && vertArt != null)
            {
                vis.VerticalBrickArt = true;
                var go = new GameObject("brick_v");
                go.transform.SetParent(_pixelRoot, false);
                go.transform.position = ProtoArena.ToPixel(b.center);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = vertArt;
                sr.color = Color.white;
                sr.sortingOrder = 40;
                vis.Pixel = go;
                vis.PixelSprite = sr;
                float heroH = _pixelCam != null ? _pixelCam.orthographicSize * 2f * ScreenHeightFraction : 1.5f;
                ScaleVerticalBrick(vis, heroH);
            }
            else if (verticalBrick)
            {
                // Fallback if brickVertical is missing.
                vis.VerticalStack = true;
                vis.Pixel = MakeVerticalStack(b, vis);
            }
            else
            {
                Sprite art = _art != null ? _art.CoverSprite(kind, cover.HpRatio) : null;
                if (art != null)
                {
                    var go = new GameObject(kind);
                    go.transform.SetParent(_pixelRoot, false);
                    go.transform.position = ProtoArena.ToPixel(b.center);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = art;
                    sr.color = Color.white;
                    sr.sortingOrder = 40;
                    vis.Pixel = go;
                    vis.PixelSprite = sr;
                    float heroH = _pixelCam != null ? _pixelCam.orthographicSize * 2f * ScreenHeightFraction : 1.5f;
                    ScaleCoverSprite(vis, heroH);
                }
                else
                {
                    vis.Pixel = Quad(_pixelRoot, kind, b.center, new Vector2(b.width, b.height), color, 40);
                }
            }
            if (kind == "barrel")
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "barrel";
                go.transform.SetParent(_dioRoot, false);
                Strip(go);
                go.transform.position = ProtoArena.ToWorld3D(b.center, h * 0.5f);
                go.transform.localScale = new Vector3(b.width, h * 0.5f, b.height);
                Paint(go.GetComponent<Renderer>(), color);
                vis.Dio = go;
            }
            else
            {
                var go = Block(_dioRoot, kind, ProtoArena.ToWorld3D(b.center, h * 0.5f), new Vector3(b.width, h, b.height), color);
                vis.Dio = go;
            }
            return vis;
        }

        void BuildScenery()
        {
            if (_art != null && _art.background != null)
            {
                // One plate fitted to the active pixel frame (no tiling). Soft upscaled BG uses Bilinear on import.
                var go = new GameObject("Background");
                go.transform.SetParent(_pixelRoot, false);
                _bgSr = go.AddComponent<SpriteRenderer>();
                _bgSr.sprite = _art.background;
                _bgSr.color = Color.white;
                _bgSr.sortingOrder = -300;
                _bgPixel = go.transform;
                LayoutBackground();
            }
            else
            {
                Quad(_pixelRoot, "sand", new Vector2(9f, 7.2f), new Vector2(22f, 16f), Sand, -200);
                Quad(_pixelRoot, "sky", new Vector2(9f, 15.2f), new Vector2(22f, 4f), Sky, -220);
                Quad(_pixelRoot, "left", new Vector2(-0.3f, 7f), new Vector2(2.4f, 12f), BlueB, -100);
                Quad(_pixelRoot, "right", new Vector2(18.3f, 7f), new Vector2(2.4f, 12f), PinkB, -100);
                Quad(_pixelRoot, "fenceL", new Vector2(4.6f, 13.3f), new Vector2(7.2f, 0.7f), Wood, -80);
                Quad(_pixelRoot, "fenceR", new Vector2(13.4f, 13.3f), new Vector2(7.2f, 0.7f), Wood, -80);
                Quad(_pixelRoot, "doorL", new Vector2(8.15f, 13.15f), new Vector2(0.28f, 1.3f), Wood, -70);
                Quad(_pixelRoot, "doorR", new Vector2(9.85f, 13.15f), new Vector2(0.28f, 1.3f), Wood, -70);
                Quad(_pixelRoot, "front", new Vector2(9f, 0.45f), new Vector2(18f, 0.7f), Wood, -80);
                Quad(_pixelRoot, "patch", new Vector2(9f, 6.5f), new Vector2(3.2f, 1.4f), SandDark, -190);
                Quad(_pixelRoot, "cactusL", new Vector2(0.55f, 2.6f), new Vector2(0.35f, 0.9f), Cactus, -60);
                Quad(_pixelRoot, "cactusR", new Vector2(17.45f, 2.6f), new Vector2(0.35f, 0.9f), Cactus, -60);
            }

            Block(_dioRoot, "ground", new Vector3(9f, -0.15f, 7.2f), new Vector3(22f, 0.3f, 16f), Sand);
            Block(_dioRoot, "leftHouse", new Vector3(-0.2f, 1.15f, 7f), new Vector3(2.2f, 2.3f, 11f), BlueB);
            Block(_dioRoot, "leftRoof", new Vector3(-0.2f, 2.45f, 7f), new Vector3(2.5f, 0.28f, 11.4f), BlueB * 0.85f);
            Block(_dioRoot, "rightHouse", new Vector3(18.2f, 1.15f, 7f), new Vector3(2.2f, 2.3f, 11f), PinkB);
            Block(_dioRoot, "rightRoof", new Vector3(18.2f, 2.45f, 7f), new Vector3(2.5f, 0.28f, 11.4f), PinkB * 0.85f);
            Block(_dioRoot, "fenceL", new Vector3(4.5f, 0.55f, 13.35f), new Vector3(7f, 1.1f, 0.35f), Wood);
            Block(_dioRoot, "fenceR", new Vector3(13.5f, 0.55f, 13.35f), new Vector3(7f, 1.1f, 0.35f), Wood);
            Block(_dioRoot, "postL", new Vector3(8.15f, 0.7f, 13.2f), new Vector3(0.28f, 1.5f, 0.28f), Wood);
            Block(_dioRoot, "postR", new Vector3(9.85f, 0.7f, 13.2f), new Vector3(0.28f, 1.5f, 0.28f), Wood);
            Block(_dioRoot, "lintel", new Vector3(9f, 1.4f, 13.2f), new Vector3(2f, 0.22f, 0.32f), Wood);
            Block(_dioRoot, "frontFence", new Vector3(9f, 0.35f, 0.35f), new Vector3(16f, 0.7f, 0.35f), Wood);
            Block(_dioRoot, "cactusL", new Vector3(0.6f, 0.45f, 3.2f), new Vector3(0.28f, 0.9f, 0.28f), Cactus);
            Block(_dioRoot, "cactusR", new Vector3(17.4f, 0.45f, 3.2f), new Vector3(0.28f, 0.9f, 0.28f), Cactus);
        }

        void LayoutBackground()
        {
            if (_bgPixel == null || _bgSr == null || _bgSr.sprite == null) return;
            const float pad = 0.35f;
            float aspect = _aspect > 0.2f ? _aspect : 16f / 9f;
            var frame = _hasFrame
                ? ProtoSafeLayout.PixelFit(aspect, _frameMinX - pad, _frameMaxX + pad, _frameMinY - pad, _frameMaxY + pad)
                : ProtoSafeLayout.PixelFit(aspect);
            // Cover the camera view (no blue letterbox). Edge crop of the plate is OK.
            float camW = frame.OrthoSize * 2f * aspect;
            float camH = frame.OrthoSize * 2f;
            _bgPixel.position = new Vector3(frame.Center.x, frame.Center.y, 0f);
            float unitsW = _bgSr.sprite.rect.width / _bgSr.sprite.pixelsPerUnit;
            float unitsH = _bgSr.sprite.rect.height / _bgSr.sprite.pixelsPerUnit;
            if (unitsW > 0.01f && unitsH > 0.01f)
            {
                float s = ProtoSafeLayout.BackgroundCoverScale(camW, camH, unitsW, unitsH);
                _bgPixel.localScale = new Vector3(s, s, 1f);
            }
        }

        GameObject MakeVerticalStack(Rect b, CoverVis vis)
        {
            var go = new GameObject("stack");
            go.transform.SetParent(_pixelRoot, false);
            go.transform.position = ProtoArena.ToPixel(b.center);
            int n = Mathf.Clamp(Mathf.RoundToInt(b.height / 0.85f), 2, 4);
            var sprites = new SpriteRenderer[n];
            Sprite crate = _art != null ? _art.crate : null;
            Sprite barrel = _art != null ? _art.barrel : null;
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) / n;
                float ly = b.yMin + t * b.height;
                var piece = new GameObject((i % 2 == 0) ? "crate" : "barrel");
                piece.transform.SetParent(go.transform, false);
                piece.transform.position = ProtoArena.ToPixel(new Vector2(b.center.x, ly));
                var sr = piece.AddComponent<SpriteRenderer>();
                sr.sprite = (i % 2 == 0 ? crate : barrel) ?? crate ?? barrel ?? _white;
                sr.color = Color.white;
                sr.sortingOrder = 40 + i;
                sprites[i] = sr;
            }
            vis.StackSprites = sprites;
            float heroH = _pixelCam != null ? _pixelCam.orthographicSize * 2f * ScreenHeightFraction : 1.5f;
            ScaleCoverStack(vis, heroH);
            return go;
        }

        static string CoverKind(CoverInstance c)
        {
            // Frontline stamps "front:<kind>:<i>" so sandbag props can reuse hay CoverData.
            string inst = c.InstanceId;
            if (!string.IsNullOrEmpty(inst) && inst.StartsWith("front:"))
            {
                int a = inst.IndexOf(':');
                int b = inst.IndexOf(':', a + 1);
                if (a >= 0 && b > a)
                {
                    string kind = inst.Substring(a + 1, b - a - 1);
                    if (kind == "brick" || kind == "hay" || kind == "sandbag" || kind == "crate" || kind == "barrel")
                        return kind;
                }
            }
            var id = c.Data != null ? c.Data.id : "";
            if (id == "cover_rock") return "brick";
            if (id == "cover_hay") return "hay";
            if (id == "cover_barrel") return "barrel";
            return "crate";
        }

        GameObject Quad(Transform parent, string name, Vector2 center, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _white;
            sr.color = color;
            sr.sortingOrder = order;
            go.transform.position = ProtoArena.ToPixel(center);
            go.transform.localScale = new Vector3(size.x, size.y * ProtoArena.PixelYCompress, 1f);
            return go;
        }

        GameObject Block(Transform parent, string name, Vector3 center, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            Strip(go);
            go.transform.position = center;
            go.transform.localScale = size;
            Paint(go.GetComponent<Renderer>(), color);
            return go;
        }

        Transform Part(Transform parent, string name, Vector3 localPos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            Strip(go);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            Paint(go.GetComponent<Renderer>(), color);
            return go.transform;
        }

        void Paint(Renderer r, Color color)
        {
            r.sharedMaterial = _cel;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            r.SetPropertyBlock(block);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }

        static void Strip(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        SpriteRenderer MakeSprite(Transform parent, string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        Transform Bang(Transform parent, float z)
        {
            var go = new GameObject("Bang");
            go.transform.SetParent(parent, false);
            if (parent == _pixelRoot)
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _white;
                sr.color = new Color(1f, 0.85f, 0.2f);
                sr.sortingOrder = 4000;
                go.transform.localScale = new Vector3(0.18f, 0.45f, 1f);
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(go.transform, false);
                Strip(cube);
                cube.transform.localScale = new Vector3(0.16f, 0.45f, 0.16f);
                Paint(cube.GetComponent<Renderer>(), new Color(1f, 0.85f, 0.2f));
            }
            go.SetActive(false);
            return go.transform;
        }

        SpriteRenderer ShotSprite(int i)
        {
            while (_pixelShots.Count <= i)
            {
                var sr = MakeSprite(_pixelRoot, "shot", _white, Color.white, 3000);
                sr.enabled = false;
                _pixelShots.Add(sr);
            }
            return _pixelShots[i];
        }

        Transform ShotDio(int i)
        {
            while (_dioShots.Count <= i)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.SetParent(_dioRoot, false);
                Strip(go);
                Paint(go.GetComponent<Renderer>(), new Color(1f, 0.9f, 0.4f));
                go.SetActive(false);
                _dioShots.Add(go.transform);
            }
            return _dioShots[i];
        }

        Marker MarkerAt(int i)
        {
            while (_markers.Count <= i) _markers.Add(MakeMarker());
            return _markers[i];
        }

        Marker MakeMarker()
        {
            var m = new Marker();
            m.PixelBomb = MakeSprite(_pixelRoot, "bomb", _white, new Color(0.2f, 0.2f, 0.22f), 3500).transform;
            m.PixelBomb.localScale = Vector3.one * 0.28f;
            m.PixelShadow = MakeSprite(_pixelRoot, "bombShadow", _white, new Color(0f, 0f, 0f, 0.45f), 3400).transform;
            m.PixelShadow.localScale = new Vector3(0.34f, 0.12f, 1f);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(_dioRoot, false);
            Strip(sphere);
            sphere.transform.localScale = Vector3.one * 0.28f;
            Paint(sphere.GetComponent<Renderer>(), new Color(0.22f, 0.22f, 0.24f));
            m.DioBomb = sphere.transform;
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.transform.SetParent(_dioRoot, false);
            Strip(shadow);
            shadow.transform.localScale = new Vector3(0.34f, 0.02f, 0.34f);
            Paint(shadow.GetComponent<Renderer>(), new Color(0.25f, 0.2f, 0.14f));
            m.DioShadow = shadow.transform;
            m.PixelRing = RingObj(_pixelRoot);
            m.DioRing = RingObj(_dioRoot);
            SetMarker(false, m);
            return m;
        }

        LineRenderer RingObj(Transform parent)
        {
            var go = new GameObject("ring");
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = _lineMat;
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.widthMultiplier = 0.06f;
            lr.positionCount = 0;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        static void Ring(LineRenderer lr, Vector3 center, float rx, float ry, bool flatXY)
        {
            const int seg = 28;
            lr.positionCount = seg;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var o = flatXY
                    ? new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0f)
                    : new Vector3(Mathf.Cos(a) * rx, 0f, Mathf.Sin(a) * ry);
                lr.SetPosition(i, center + o);
            }
        }

        static void SetMarker(bool on, Marker m)
        {
            m.PixelBomb.gameObject.SetActive(on);
            m.PixelShadow.gameObject.SetActive(on);
            m.DioBomb.gameObject.SetActive(on);
            m.DioShadow.gameObject.SetActive(on);
            m.PixelRing.gameObject.SetActive(on);
            m.DioRing.gameObject.SetActive(on);
        }

        Flash MakeFlash(Vector2 pos, float radius)
        {
            return new Flash
            {
                Pos = pos,
                Radius = radius,
                Pixel = RingObj(_pixelRoot),
                Dio = RingObj(_dioRoot)
            };
        }

        Camera MakeCam(string name, Transform parent, bool pixel)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Dirt fallback if the plate ever fails to cover; avoid blue letterbox bars.
            cam.backgroundColor = pixel ? new Color(0.72f, 0.58f, 0.38f) : Sky;
            cam.nearClipPlane = pixel ? 0.1f : 0.2f;
            cam.farClipPlane = pixel ? 40f : 80f;
            cam.orthographic = pixel;
            if (!pixel) cam.fieldOfView = 34f;
            return cam;
        }

        void FitPixel(Camera cam, float aspect)
        {
            cam.orthographic = true;
            // Slight pad so left/right walk edges and both banks stay inside the Game view.
            const float pad = 0.35f;
            var frame = _hasFrame
                ? ProtoSafeLayout.PixelFit(aspect, _frameMinX - pad, _frameMaxX + pad, _frameMinY - pad, _frameMaxY + pad)
                : ProtoSafeLayout.PixelFit(aspect);
            cam.orthographicSize = frame.OrthoSize;
            cam.aspect = aspect;
            cam.transform.position = new Vector3(frame.Center.x, frame.Center.y, -10f);
            cam.transform.rotation = Quaternion.identity;
        }

        void FitDiorama(Camera cam, float aspect)
        {
            cam.orthographic = false;
            cam.fieldOfView = 34f;
            cam.aspect = aspect;
            cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            var look = new Vector3(ProtoArena.MidX, 0.4f, (ProtoArena.MinY + ProtoArena.MaxY) * 0.5f);
            var corners = new[]
            {
                new Vector3(-1.4f, 0f, 0.2f), new Vector3(19.4f, 0f, 0.2f),
                new Vector3(-1.4f, 0f, 14.3f), new Vector3(19.4f, 0f, 14.3f),
                new Vector3(-0.2f, 2.6f, 7f), new Vector3(18.2f, 2.6f, 7f)
            };
            float dist = 16f;
            for (int step = 0; step < 80; step++)
            {
                cam.transform.position = look - cam.transform.forward * dist;
                if (Sees(cam, corners)) break;
                dist += 0.55f;
            }
            dist += 0.7f;
            cam.transform.position = look - cam.transform.forward * dist;
        }

        static bool Sees(Camera cam, Vector3[] corners)
        {
            const float pad = 0.04f;
            for (int i = 0; i < corners.Length; i++)
            {
                var vp = cam.WorldToViewportPoint(corners[i]);
                if (vp.z <= 0.1f || vp.x < pad || vp.x > 1f - pad || vp.y < pad || vp.y > 1f - pad) return false;
            }
            return true;
        }

        static Sprite WhiteSprite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pix = new Color32[16];
            for (int i = 0; i < pix.Length; i++) pix[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pix);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        static Sprite SoftEllipseSprite()
        {
            const int w = 32;
            const int h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float cx = (w - 1) * 0.5f;
            float cy = (h - 1) * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - cx) / (w * 0.5f);
                    float ny = (y - cy) / (h * 0.5f);
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = d >= 1f ? 0f : Mathf.SmoothStep(1f, 0f, d) * 0.42f;
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                }
            }
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
