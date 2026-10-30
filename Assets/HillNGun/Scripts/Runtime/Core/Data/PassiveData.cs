using UnityEngine;
using System.Collections.Generic;
using HillNGun.Core;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "PassiveData", menuName = "HillNGun/Data/Passive")]
    public class PassiveData : ScriptableObject
    {
        [Header("Identification")]
        public string id;
        public string nameKey;
        public string descriptionKey;

        [Header("Type")]
        public PassiveType passiveType;

        [Header("Parameters")]
        public PassiveParameter[] parameters;

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
    public class PassiveParameter
    {
        public string key;
        public float value;
    }
}
