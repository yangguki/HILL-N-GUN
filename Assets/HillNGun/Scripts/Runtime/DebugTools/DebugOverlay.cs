using UnityEngine;
using HillNGun.Core;
using HillNGun.Combat;
using HillNGun.Characters;
using HillNGun.Cover;
using HillNGun.Battlefield;
using System.Collections.Generic;

namespace HillNGun.DebugTools
{
    public class DebugOverlay : MonoBehaviour
    {
        [Header("Display Options")]
        [SerializeField] private bool _showHitCircles = false;
        [SerializeField] private bool _showCoverColliders = false;
        [SerializeField] private bool _showRanges = false;
        [SerializeField] private bool _showProjectiles = false;
        [SerializeField] private bool _showStats = false;
        [SerializeField] private bool _showGrid = false;

        [Header("Debug Controls")]
        [SerializeField] private bool _invincible = false;
        [SerializeField] private float _timeScale = 1f;

        private CombatSystem _combatSystem;
        private BattlefieldSystem _battlefield;
        private List<CharacterStateMachine> _characters = new List<CharacterStateMachine>();

        public bool Invincible
        {
            get => _invincible;
            set => _invincible = value;
        }

        public float TimeScaleOverride
        {
            get => _timeScale;
            set
            {
                _timeScale = Mathf.Clamp(value, 0.1f, 2f);
                GameTime.SetSlowMotion(_timeScale);
            }
        }

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<DebugOverlay>();
        }

        private void Start()
        {
            _combatSystem = ServiceLocator.Get<CombatSystem>();
            _battlefield = ServiceLocator.Get<BattlefieldSystem>();
        }

        private void Update()
        {
            HandleDebugInput();

            if (_timeScale != 1f)
            {
                GameTime.SetSlowMotion(_timeScale);
            }
        }

        private void HandleDebugInput()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null) return;

            if (UnityEngine.InputSystem.Keyboard.current.f1Key.wasPressedThisFrame)
                _showHitCircles = !_showHitCircles;
            if (UnityEngine.InputSystem.Keyboard.current.f2Key.wasPressedThisFrame)
                _showCoverColliders = !_showCoverColliders;
            if (UnityEngine.InputSystem.Keyboard.current.f3Key.wasPressedThisFrame)
                _showRanges = !_showRanges;
            if (UnityEngine.InputSystem.Keyboard.current.f4Key.wasPressedThisFrame)
                _showProjectiles = !_showProjectiles;
            if (UnityEngine.InputSystem.Keyboard.current.f5Key.wasPressedThisFrame)
                _showStats = !_showStats;
            if (UnityEngine.InputSystem.Keyboard.current.f6Key.wasPressedThisFrame)
                _showGrid = !_showGrid;
            if (UnityEngine.InputSystem.Keyboard.current.f7Key.wasPressedThisFrame)
                _invincible = !_invincible;
            if (UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame)
            {
                _timeScale = _timeScale < 1f ? 1f : 0.25f;
                GameTime.SetSlowMotion(_timeScale);
            }
        }

        public void RegisterCharacter(CharacterStateMachine character)
        {
            if (!_characters.Contains(character))
            {
                _characters.Add(character);
            }
        }

        public void UnregisterCharacter(CharacterStateMachine character)
        {
            _characters.Remove(character);
        }

        private void OnGUI()
        {
            if (!_showStats) return;

            float panelWidth = 180;
            float panelX = Screen.width - panelWidth - 10;

            GUILayout.BeginArea(new Rect(panelX, 120, panelWidth, 400));
            GUILayout.BeginVertical("box");

            var smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.fontSize = 10;
            smallStyle.richText = true;

            GUILayout.Label("<b>Debug (F5 off)</b>", smallStyle);
            GUILayout.Label($"FPS: {1f / Time.unscaledDeltaTime:F0} | Proj: {_combatSystem?.ProjectilePool?.ActiveCount ?? 0}", smallStyle);
            GUILayout.Label($"F1:Hit F2:Cover F3:Range F4:Proj", smallStyle);
            GUILayout.Label($"F6:Grid F7:God F8:Slow({_timeScale:F2}x)", smallStyle);

            foreach (var character in _characters)
            {
                if (character == null) continue;
                GUILayout.Label($"<b>{character.Data?.id}</b> HP:{character.CurrentHp}/{character.MaxHp}", smallStyle);
                GUILayout.Label($"  Pos:({character.Position.x:F0},{character.Position.y:F0}) R{character.CurrentRowIndex}", smallStyle);
                GUILayout.Label($"  Ammo:{character.AmmoGauge?.CurrentCells ?? 0}/{character.AmmoGauge?.MaxCells ?? 0}", smallStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            if (_battlefield == null)
                _battlefield = ServiceLocator.Get<BattlefieldSystem>();
            if (_combatSystem == null)
                _combatSystem = ServiceLocator.Get<CombatSystem>();

            if (_showGrid)
                DrawGrid();
            if (_showHitCircles)
                DrawHitCircles();
            if (_showCoverColliders)
                DrawCoverColliders();
            if (_showRanges)
                DrawRanges();
            if (_showProjectiles)
                DrawProjectiles();
        }

        private void DrawGrid()
        {
            if (_battlefield?.Data == null) return;

            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            var data = _battlefield.Data;

            for (float x = 0; x <= data.size.x; x++)
            {
                Vector3 start = _battlefield.LogicToWorldPosition(new Vector2(x, 0));
                Vector3 end = _battlefield.LogicToWorldPosition(new Vector2(x, data.size.y));
                Gizmos.DrawLine(start, end);
            }

            for (float y = 0; y <= data.size.y; y++)
            {
                Vector3 start = _battlefield.LogicToWorldPosition(new Vector2(0, y));
                Vector3 end = _battlefield.LogicToWorldPosition(new Vector2(data.size.x, y));
                Gizmos.DrawLine(start, end);
            }

            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            foreach (float rowY in data.ourRowsY)
            {
                Vector3 start = _battlefield.LogicToWorldPosition(new Vector2(0, rowY));
                Vector3 end = _battlefield.LogicToWorldPosition(new Vector2(data.size.x, rowY));
                Gizmos.DrawLine(start, end);
            }

            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            foreach (float rowY in data.enemyRowsY)
            {
                Vector3 start = _battlefield.LogicToWorldPosition(new Vector2(0, rowY));
                Vector3 end = _battlefield.LogicToWorldPosition(new Vector2(data.size.x, rowY));
                Gizmos.DrawLine(start, end);
            }
        }

        private void DrawHitCircles()
        {
            foreach (var character in _characters)
            {
                if (character == null || character.OverallState == CharacterOverallState.Eliminated) continue;

                Vector3 worldPos = _battlefield?.LogicToWorldPosition(character.Position) ?? 
                                   (Vector3)character.Position;

                Gizmos.color = character.IsInvulnerable ? Color.cyan : 
                              (character.Team == Team.Player ? Color.green : Color.red);
                DrawWireCircle(worldPos, character.HitRadius);

                if (character.Data?.weakpointRadius > 0)
                {
                    Gizmos.color = Color.yellow;
                    DrawWireCircle(worldPos, character.Data.weakpointRadius);
                }
            }
        }

        private void DrawCoverColliders()
        {
            if (_combatSystem == null) return;

            foreach (var cover in _combatSystem.Covers)
            {
                if (cover.IsDestroyed) continue;

                Vector3 worldPos = _battlefield?.LogicToWorldPosition(cover.Position) ?? 
                                   (Vector3)cover.Position;

                Gizmos.color = GetCoverStageColor(cover.Stage);
                Gizmos.DrawWireCube(worldPos, new Vector3(cover.Data.width, cover.Data.depth, 0.1f));
            }
        }

        private Color GetCoverStageColor(CoverStage stage)
        {
            switch (stage)
            {
                case CoverStage.Intact: return Color.white;
                case CoverStage.Cracked: return Color.yellow;
                case CoverStage.Damaged: return new Color(1f, 0.5f, 0f);
                default: return Color.gray;
            }
        }

        private void DrawRanges()
        {
            foreach (var character in _characters)
            {
                if (character?.Data?.weapon == null) continue;
                if (character.OverallState != CharacterOverallState.Active) continue;

                Vector3 worldPos = _battlefield?.LogicToWorldPosition(character.Position) ?? 
                                   (Vector3)character.Position;

                var w = character.Data.weapon;
                Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
                DrawWireCircle(worldPos, w.range); // v2 radii: Buster 8 / Raven 16 / Volt 10
                if (w.fullPelletRange > 0f)
                {
                    Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
                    DrawWireCircle(worldPos, w.fullPelletRange);
                }
                if (w.damageFalloffStart > 0f)
                {
                    Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.3f);
                    DrawWireCircle(worldPos, w.damageFalloffStart);
                }
            }
        }

        private void DrawProjectiles()
        {
            if (_combatSystem?.ProjectilePool == null) return;

            foreach (var projectile in _combatSystem.ProjectilePool.ActiveProjectiles)
            {
                if (!projectile.IsActive) continue;

                Vector3 worldPos = _battlefield?.LogicToWorldPosition(projectile.Position) ?? 
                                   (Vector3)projectile.Position;

                Gizmos.color = projectile.OwnerTeam == Team.Player ? Color.blue : Color.magenta;
                Gizmos.DrawSphere(worldPos, projectile.Radius);
            }
        }

        private void DrawWireCircle(Vector3 center, float radius, int segments = 32)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}
