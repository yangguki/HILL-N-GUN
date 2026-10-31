using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Core
{
    public class RuntimeAssetManager : MonoBehaviour
    {
        private static RuntimeAssetManager _instance;
        public static RuntimeAssetManager Instance => _instance;

        [Header("Direct References (for builds)")]
        [SerializeField] private SpriteLibrary _spriteLibrary;
        [SerializeField] private AudioLibrary _audioLibrary;
        [SerializeField] private CharacterData[] _characterDatas;
        [SerializeField] private EnemyData[] _enemyDatas;
        [SerializeField] private BossData[] _bossDatas;
        [SerializeField] private StageData[] _stageDatas;
        [SerializeField] private CoverData[] _coverDatas;
        [SerializeField] private BotBehaviorData _botBehavior;
        [SerializeField] private BattlefieldData _battlefieldData;
        [SerializeField] private DownedData _downedData;
        [SerializeField] private FeelData _feelData;
        [SerializeField] private MatchRulesData _matchRules;
        [SerializeField] private InputData _inputData;
        [SerializeField] private TouchControlsData _touchControlsData;

        public SpriteLibrary SpriteLibrary => _spriteLibrary;
        public AudioLibrary AudioLibrary => _audioLibrary;
        public CharacterData[] CharacterDatas => _characterDatas;
        public EnemyData[] EnemyDatas => _enemyDatas;
        public BossData[] BossDatas => _bossDatas;
        public StageData[] StageDatas => _stageDatas;
        public CoverData[] CoverDatas => _coverDatas;
        public BotBehaviorData BotBehavior => _botBehavior;
        public BattlefieldData BattlefieldData => _battlefieldData;
        public DownedData DownedData => _downedData;
        public FeelData FeelData => _feelData;
        public MatchRulesData MatchRules => _matchRules;
        public InputData InputData => _inputData;
        public TouchControlsData TouchControlsData => _touchControlsData;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            RegisterServices();
        }

        private void RegisterServices()
        {
            if (_spriteLibrary != null)
                ServiceLocator.Register(_spriteLibrary);
            if (_audioLibrary != null)
                ServiceLocator.Register(_audioLibrary);
            if (_downedData != null)
                ServiceLocator.Register(_downedData);
            if (_feelData != null)
                ServiceLocator.Register(_feelData);
            if (_matchRules != null)
                ServiceLocator.Register(_matchRules);
            if (_inputData != null)
                ServiceLocator.Register(_inputData);
            if (_touchControlsData != null)
                ServiceLocator.Register(_touchControlsData);
            if (_battlefieldData != null)
                ServiceLocator.Register(_battlefieldData);
            if (_botBehavior != null)
                ServiceLocator.Register(_botBehavior);

            ServiceLocator.Register(this);
            Debug.Log("[RuntimeAssetManager] Services registered");
        }

        public CharacterData GetCharacterById(string id)
        {
            if (_characterDatas == null) return null;
            foreach (var data in _characterDatas)
            {
                if (data != null && data.id == id)
                    return data;
            }
            return _characterDatas.Length > 0 ? _characterDatas[0] : null;
        }

        public EnemyData GetEnemyById(string id)
        {
            if (_enemyDatas == null) return null;
            foreach (var data in _enemyDatas)
            {
                if (data != null && data.id == id)
                    return data;
            }
            return null;
        }

        public StageData GetStageByIndex(int index)
        {
            if (_stageDatas == null || index < 0 || index >= _stageDatas.Length)
                return null;
            return _stageDatas[index];
        }
    }
}
