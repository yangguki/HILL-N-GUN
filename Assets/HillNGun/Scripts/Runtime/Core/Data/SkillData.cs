using UnityEngine;
using HillNGun.Core;
using System.Collections.Generic;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "SkillData", menuName = "HillNGun/Data/Skill")]
    public class SkillData : ScriptableObject
    {
        [Header("Identification")]
        public string id;
        public string nameKey;

        [Header("Use Type")]
        [Tooltip("사용 방식: Tap=즉발, Hold=누르고 있기, DragRelease=끌어 조준 후 발동")]
        public SkillUseType useType = SkillUseType.Tap;

        [Tooltip("스킬 효과 종류 (수치는 parameters)")]
        public SkillEffect effect = SkillEffect.None;

        [Header("Timing")]
        [Tooltip("재사용 대기 시간 (초)")]
        public float cooldown = 12f;

        [Tooltip("누르고 있기 시간 (Hold 타입)")]
        public float holdTime = 1.0f;

        [Tooltip("지속 시간 (초)")]
        public float duration = 3f;

        [Header("Targeting")]
        [Tooltip("대상 지정 사거리 (타일)")]
        public float targetRange = 6f;

        [Header("Parameters")]
        public SkillParameter[] parameters;

        private Dictionary<string, float> _paramCache;

        public float GetParam(string key, float defaultValue = 0f)
        {
            if (_paramCache == null)
            {
                _paramCache = new Dictionary<string, float>();
                if (parameters != null)
                {
                    foreach (var p in parameters)
                    {
                        _paramCache[p.key] = p.value;
                    }
                }
            }

            return _paramCache.TryGetValue(key, out float value) ? value : defaultValue;
        }

        public void ClearCache()
        {
            _paramCache = null;
        }

        private void OnValidate()
        {
            ClearCache();
        }
    }

    [System.Serializable]
    public class SkillParameter
    {
        public string key;
        public float value;
    }
}
