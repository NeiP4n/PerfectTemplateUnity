using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sources.Code.Configs;
using Sources.Code.Gameplay.Characters;
using Sources.Code.Gameplay.GameSaves;
using Sources.Code.Gameplay.Inventory;
using Sources.Code.Interfaces;
using Sources.Code.UI;
using Sources.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sources.Code.Gameplay
{
    public class Game
    {
        // ---------- FIELDS ----------

        private readonly PlayerProgress _playerProgress;
        private readonly ScreenSwitcher _screenSwitcher;
        private readonly PopupSwitcher _popupSwitcher;
        private readonly IMain _main;
        private readonly List<IMonoBehaviour> _monoBehaviours = new();
        private readonly GameTokenProvider _tokenProvider;
        private readonly LevelsConfig _levelsConfig;
        private readonly IInputManager _inputManager;

        private CancellationTokenSource _gameTokenSource;
        private CancellationToken _gameToken;

        private GameEventPopup _gameEventScreen;
        private Level _levelInstance;
        private PlayerCharacter _playerCharacter;
        private PlayerSaveManager _playerSave;
        private ScreenInventory _screenInventory;
        private bool _isWin;

        private GameFlowConfig _gameFlowConfig => GameFlowConfig.Instance;

        // ---------- PROPERTIES ----------

        public int CurrentLevelNumber
        {
            get => _playerProgress.LevelNumber;
            set => _playerProgress.LevelNumber = value;
        }

        public int MaxLevels => _levelsConfig.LevelCount;

        // ---------- CONSTRUCTOR ----------

        public Game(IMain main)
        {
            _main           = main;
            _tokenProvider  = GameTokenProvider.Instance;
            _playerProgress = GameSaverLoader.Instance.PlayerProgress;
            _screenSwitcher = ScreenSwitcher.Instance;
            _popupSwitcher  = PopupSwitcher.Instance;
            _levelsConfig   = LevelsConfig.Instance;
            _inputManager   = InputManager.Instance;
        }

        // ---------- UPDATE LOOP ----------

        public void ThisUpdate()
        {
            for (int i = 0; i < _monoBehaviours.Count; i++)
                _monoBehaviours[i].Tick();
        }

        // ---------- GAME START ----------

        public void StartGame()
        {
            ClearLevel();

            InitPopups();
            InitGameToken();

            SpawnLevelInstance();

            var levelSave = _levelInstance.GetComponentInChildren<LevelSaveManager>();
            levelSave?.LoadLevelState();

            SetupPlayer();
            SetupGameScreen();

            TryAddPlayerToMonoList();
            ApplyGameplayCursor();

            SetupInventoryUI();
        }

        private void InitPopups()
        {
            _popupSwitcher.Init();
            _popupSwitcher.PopupClosed += OnPopupClosed;
        }

        private void InitGameToken()
        {
            _gameTokenSource = new CancellationTokenSource();
            _gameToken       = _gameTokenSource.Token;
            _tokenProvider.Init(_gameToken);
        }

        private void SpawnLevelInstance()
        {
            int index = CurrentLevelNumber - 1;

            if (index < 0 || index >= _levelsConfig.LevelCount)
            {
                Debug.LogError($"[Game] Некорректный номер уровня: {CurrentLevelNumber}. " +
                               $"Доступно уровней: {_levelsConfig.LevelCount}. " +
                               "Ставлю 1.");
                CurrentLevelNumber = 1;
                index = 0;
            }

            _levelInstance = Object.Instantiate(_levelsConfig.GetLevelPrefabByIndex(index));

            // ищем сейвер игрока на уровне
            _playerSave = _levelInstance.GetComponentInChildren<PlayerSaveManager>();
        }

        private void SetupPlayer()
        {
            _playerCharacter = _levelInstance.PlayerCharacter;
            _playerCharacter.Construct(_inputManager);

            var progress = GameSaverLoader.Instance.PlayerProgress;

            bool hasSavedPos =
                progress.LevelNumber == CurrentLevelNumber &&
                !(progress.PlayerPosX == 0f &&
                  progress.PlayerPosY == 0f &&
                  progress.PlayerPosZ == 0f);

            if (_playerSave != null && hasSavedPos)
            {
                _playerSave.LoadPlayer();
            }
            else
            {
                _playerCharacter.transform.position = _levelInstance.CharacterSpawnPosition;
            }
        }

        private void SetupGameScreen()
        {
            var gameScreen = _screenSwitcher.ShowScreen<GameScreen>();
            gameScreen.Init(_playerCharacter);

            var uiInteract = gameScreen.GetUIInteract();
            uiInteract.Init(_playerCharacter.Interact);
            
            var inventoryUI = gameScreen.GetComponentInChildren<ScreenInventory>();
            inventoryUI.Init(_playerCharacter.Inventory);
        }


        private void TryAddPlayerToMonoList()
        {
            if (_playerCharacter is IMonoBehaviour mono)
                _monoBehaviours.Add(mono);
        }

        // ---------- LOADING FLOW ----------

        public async void LoadingGame()
        {
            if (!_gameFlowConfig.EnableLoadingScreen)
            {
                RunNextStepAfterLoading();
            }
            else
            {
                var loadingScreen = _screenSwitcher.ShowScreen<LoadingScreen>();
                loadingScreen.Show();

                await UniTask.Yield();

                RunNextStepAfterLoading();

                loadingScreen.Hide();
            }
        }

        private void RunNextStepAfterLoading()
        {
            if (_gameFlowConfig.EnableCutscene)
            {
                GameFlow.StartGameplayAfterSceneLoad = true;
                SceneManager.LoadScene("Cutscene");
            }
            else
            {
                StartGame();
            }
        }

        // ---------- DISPOSE / CLEAR ----------

        public void Dispose()
        {
            _popupSwitcher.PopupClosed -= OnPopupClosed;
            ClearLevel();
            CancelGameToken();
        }

        private void CancelGameToken()
        {
            if (_gameTokenSource == null)
                return;

            _gameTokenSource.Cancel();
            _gameTokenSource.Dispose();
            _gameTokenSource = null;
        }

        private void ClearLevel()
        {
            for (int i = 0; i < _monoBehaviours.Count; i++)
                _monoBehaviours[i].Dispose();

            _monoBehaviours.Clear();

            if (_gameEventScreen != null)
            {
                _gameEventScreen.Close();
                _gameEventScreen = null;
            }

            if (_levelInstance != null)
            {
                Object.Destroy(_levelInstance.gameObject);
                _levelInstance = null;
            }

            _playerCharacter = null;
            _playerSave      = null;
        }

        // ---------- LEVEL RESULT ----------

        private void RestartLevel()
        {
            ClearLevel();
            _isWin = false;
            StartGame();
        }

        private async UniTaskVoid DefeatLevel()
        {
            if (_isWin)
                return;

            _gameEventScreen = _popupSwitcher.Show<GameEventPopup>();
            _gameEventScreen.ShowDefeat();

            await UniTask.Delay(1500, cancellationToken: _gameToken);

            _gameEventScreen.Close();
            RestartLevel();
        }

        // ---------- CURSOR / POPUPS ----------

        private void ApplyGameplayCursor()
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void OnPopupClosed(BasePopup popup)
        {
            popup.Closed -= OnPopupClosed;
            ApplyGameplayCursor();
        }

        // ---------- SAVE SYSTEM ----------

        public void SaveAll()
        {
            // динамика уровня
            var levelSave = _levelInstance.GetComponentInChildren<LevelSaveManager>();
            levelSave?.SaveLevelState();

            _playerSave?.SavePlayer();
        }

        // ---------- INVENTORY UI ----------
        private void SetupInventoryUI()
        {
            if (_playerCharacter == null)
                return;

            if (_screenInventory == null)
                _screenInventory = Object.FindFirstObjectByType<ScreenInventory>();

            if (_screenInventory == null)
                return;

            _screenInventory.Init(_playerCharacter.Inventory);
        }
    }
}
