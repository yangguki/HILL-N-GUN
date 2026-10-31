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
        [SerializeField] private ProgressionData _progression;
        [SerializeField] private UIThemeData _uiTheme;
        [SerializeField] private ExternalServicesData _externalServices;
        [SerializeField] private TutorialData _tutorial;
        [SerializeField] private MovementFeelData _movementFeel;
        [SerializeField] private MotionViewData _motionView;
        [SerializeField] private Actor3DData _actor3D;
        [SerializeField] private Actor3DAnimData _actor3DAnim;

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
        public ProgressionData Progression => _progression;
        public UIThemeData UITheme => _uiTheme;
        public ExternalServicesData ExternalServices => _externalServices;
        public TutorialData Tutorial => _tutorial;
        public MovementFeelData MovementFeel => _movementFeel;
        public MotionViewData MotionView => _motionView;
        public Actor3DData Actor3D => _actor3D;
        public Actor3DAnimData Actor3DAnim => _actor3DAnim;

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
            if (_progression != null)
                ServiceLocator.Register(_progression);
            if (_uiTheme != null)
                ServiceLocator.Register(_uiTheme);
            if (_externalServices != null)
                ServiceLocator.Register(_externalServices);
            if (_tutorial != null)
                ServiceLocator.Register(_tutorial);
            if (_movementFeel != null)
                ServiceLocator.Register(_movementFeel);
            if (_motionView != null)
                ServiceLocator.Register(_motionView);
            if (_actor3D != null)
                ServiceLocator.Register(_actor3D);
            if (_actor3DAnim != null)
                ServiceLocator.Register(_actor3DAnim);

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
