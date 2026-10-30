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

            foreach (var downedChar in _characters)
            {
                if (downedChar == null) continue;
                if (downedChar.OverallState != CharacterOverallState.Downed) continue;

                foreach (var reviver in _characters)
                {
                    if (reviver == null || reviver == downedChar) continue;
                    if (reviver.OverallState != CharacterOverallState.Active) continue;
                    if (reviver.Team != downedChar.Team) continue;

                    float distance = CoordinateHelper.Distance(reviver.Position, downedChar.Position);
                    if (distance <= reviveRange)
                    {
                        break;
                    }
                }
            }
        }

        public IReadOnlyList<DummyAlly> DummyAllies => _dummyAllies;
    }
}
