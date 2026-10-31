using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.Combat;
using HillNGun.Cover;

namespace HillNGun.Feel
{
    /// <summary>
    /// View-layer listener: turns combat logic events (skill used, cover repaired, weak point, revive)
    /// into floating labels. No game logic here.
    /// </summary>
    public class CombatFeedback : MonoBehaviour
    {
        private CombatSystem _combat;
        private ReviveSystem _revive;

        public static CombatFeedback Ensure()
        {
            var existing = FindFirstObjectByType<CombatFeedback>();
            if (existing != null) return existing;
            return new GameObject("CombatFeedback").AddComponent<CombatFeedback>();
        }

        private void Start()
        {
            _combat = ServiceLocator.Get<CombatSystem>();
            _revive = ServiceLocator.Get<ReviveSystem>();
            if (_combat != null)
            {
                _combat.OnSkillEffect += HandleSkill;
                _combat.OnCoverRepaired += HandleRepair;
                _combat.OnWeakpointHit += HandleWeakpoint;
            }
            if (_revive != null)
            {
                _revive.OnReviveStarted += HandleReviveStarted;
                _revive.OnRevived += HandleRevived;
            }
        }

        private void OnDestroy()
        {
            if (_combat != null)
            {
                _combat.OnSkillEffect -= HandleSkill;
                _combat.OnCoverRepaired -= HandleRepair;
                _combat.OnWeakpointHit -= HandleWeakpoint;
            }
            if (_revive != null)
            {
                _revive.OnReviveStarted -= HandleReviveStarted;
                _revive.OnRevived -= HandleRevived;
            }
        }

        private static string L(string key, string fallback)
        {
            var loc = ServiceLocator.Get<HillNGun.UI.LocalizationManager>();
            return loc != null && loc.HasString(key) ? loc.GetString(key) : fallback;
        }

        private void HandleSkill(CharacterStateMachine c, SkillEffect effect)
        {
            if (effect == SkillEffect.RepairCover) return; // shown on the cover instead
            string text = effect == SkillEffect.PierceNextShot && !c.IsPierceArmed
                ? L("fx.pierce_shot", "관통!")
                : L(c.Data?.skill?.nameKey ?? "", c.Data?.skill?.id ?? "SKILL");
            EffectsManager.Instance?.SpawnFloatingText(c.Position + Vector2.up * 0.6f, text, new Color(1f, 0.9f, 0.3f));
        }

        private void HandleRepair(CharacterStateMachine c, CoverInstance cover, int amount)
        {
            string text = L("fx.repair", "보수") + $" +{amount}";
            EffectsManager.Instance?.SpawnFloatingText(cover.Position + Vector2.up * 0.6f, text, new Color(0.45f, 1f, 0.6f));
        }

        private float _lastWeakpointText;
        private void HandleWeakpoint(Projectile p, Vector2 at, int damage)
        {
            if (Time.time - _lastWeakpointText < 0.3f) return;
            _lastWeakpointText = Time.time;
            EffectsManager.Instance?.SpawnFloatingText(at + Vector2.up * 0.9f, L("fx.weakpoint", "약점!"), new Color(1f, 0.5f, 0.3f));
        }

        private void HandleReviveStarted(CharacterStateMachine reviver, CharacterStateMachine downed)
        {
            EffectsManager.Instance?.SpawnFloatingText(downed.Position + Vector2.up * 1.1f, L("fx.reviving", "소생 중..."), new Color(0.6f, 1f, 0.7f));
        }

        private void HandleRevived(CharacterStateMachine reviver, CharacterStateMachine revived, float time)
        {
            EffectsManager.Instance?.SpawnFloatingText(revived.Position + Vector2.up * 0.8f, L("fx.revived", "소생!"), new Color(0.35f, 1f, 0.5f));
        }
    }
}
