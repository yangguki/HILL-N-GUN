using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;
using System.Collections.Generic;

namespace HillNGun.Characters
{
    public class ReviveSystem : MonoBehaviour
    {
        [SerializeField] private DownedData _downedData;

        private List<DummyAlly> _dummyAllies = new List<DummyAlly>();
        private List<CharacterStateMachine> _characters = new List<CharacterStateMachine>();

        private void Awake()
        {
            ServiceLocator.Register(this);
            if (_downedData == null) _downedData = ServiceLocator.Get<DownedData>();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ReviveSystem>();
        }

        public void RegisterDummyAlly(DummyAlly ally)
        {
            if (!_dummyAllies.Contains(ally))
            {
                _dummyAllies.Add(ally);
            }
        }

        public void UnregisterDummyAlly(DummyAlly ally)
        {
            _dummyAllies.Remove(ally);
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

        private void Update()
        {
            if (_downedData == null) _downedData = ServiceLocator.Get<DownedData>();
            float reviveRange = _downedData?.reviveRange ?? 1.0f;

            foreach (var ally in _dummyAllies)
            {
                if (ally == null || ally.State != CharacterOverallState.Downed) continue;

                bool hasNearbyReviver = false;

                foreach (var character in _characters)
                {
                    if (character == null) continue;
                    if (character.OverallState != CharacterOverallState.Active) continue;
                    if (character.Team != Team.Player) continue;

                    float distance = CoordinateHelper.Distance(character.Position, ally.Position);
                    if (distance <= reviveRange)
                    {
                        hasNearbyReviver = true;
                        break;
                    }
                }

                if (hasNearbyReviver)
                {
                    ally.StartRevive();
                }
                else
                {
                    ally.StopRevive();
                }
            }

            // Character revive: an Active same-team ally inside reviveRange fills progress.
            // Time = baseReviveTime, or the reviver's FastRevive passive reviveTime (볼트 2s, decision D7).
            float dt = GameTime.DeltaTime;
            if (dt <= 0f) return;
            for (int i = 0; i < _characters.Count; i++)
            {
                var downedChar = _characters[i];
                if (downedChar == null) continue;
                if (downedChar.OverallState != CharacterOverallState.Downed) continue;

                CharacterStateMachine best = null;
                float bestTime = float.MaxValue;
                for (int j = 0; j < _characters.Count; j++)
                {
                    var reviver = _characters[j];
                    if (reviver == null || reviver == downedChar) continue;
                    if (reviver.OverallState != CharacterOverallState.Active) continue;
                    if (reviver.Team != downedChar.Team) continue;

                    float distance = CoordinateHelper.Distance(reviver.Position, downedChar.Position);
                    if (distance > reviveRange) continue;
                    float t = GetReviveTime(reviver);
                    if (t < bestTime) { bestTime = t; best = reviver; }
                }

                if (best != null)
                {
                    bool wasZero = downedChar.ReviveProgress <= 0f;
                    if (wasZero) OnReviveStarted?.Invoke(best, downedChar);
                    downedChar.AddReviveProgress(dt / Mathf.Max(0.1f, bestTime));
                    if (downedChar.OverallState == CharacterOverallState.Active)
                        OnRevived?.Invoke(best, downedChar, bestTime);
                }
                else if (downedChar.IsBeingRevived || downedChar.ReviveProgress > 0f)
                {
                    if (_downedData == null || _downedData.resetProgressWhenInterrupted)
                        downedChar.ResetReviveProgress();
                    else
                        downedChar.AddReviveProgress(0f);
                }
            }
        }

        public event System.Action<CharacterStateMachine, CharacterStateMachine> OnReviveStarted;
        /// <summary>(reviver, revived, reviveTime)</summary>
        public event System.Action<CharacterStateMachine, CharacterStateMachine, float> OnRevived;

        public float GetReviveTime(CharacterStateMachine reviver)
        {
            float baseTime = _downedData?.baseReviveTime ?? 3f;
            var passive = reviver?.Data?.passive;
            if (passive != null && passive.passiveType == PassiveType.FastRevive)
                return passive.GetParam("reviveTime", baseTime);
            return baseTime;
        }

        public void ClearCharacters()
        {
            _characters.Clear();
        }

        public IReadOnlyList<DummyAlly> DummyAllies => _dummyAllies;
    }
}
