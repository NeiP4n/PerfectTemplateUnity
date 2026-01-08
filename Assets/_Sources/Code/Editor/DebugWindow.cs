using System.Text;
using UnityEditor;
using UnityEngine;
using Sources.Code;
using Sources.Code.Gameplay.GameSaves;
using Sources.Controllers;
using Sources.Managers;
using Sources.Characters;
using Sources.Code.Gameplay.Characters;

public class DebugWindow : EditorWindow
{
    private PlayerProgress _progress;
    private Vector2 _scroll;
    private bool _showPlayer = true;
    private bool _showLevel  = true;
    private bool _showNet    = true;
    private bool _showPerf   = true;

    [MenuItem("Tools/Игра/Debug Window _F1")]
    public static void ShowWindow()
    {
        GetWindow<DebugWindow>("Debug");
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawHeader();
        EditorGUILayout.Space();

        var loader = GameSaverLoader.Instance;
        if (loader == null)
        {
            EditorGUILayout.HelpBox(
                "GameSaverLoader.Instance == null.\n" +
                "Нужен объект с GameSaverLoader в стартовой сцене.",
                MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        _progress = loader.PlayerProgress;

        DrawRuntimeSection();
        EditorGUILayout.Space();

        _showPlayer = EditorGUILayout.Foldout(_showPlayer, "Игрок");
        if (_showPlayer)
            DrawPlayerSection();

        EditorGUILayout.Space();

        _showLevel = EditorGUILayout.Foldout(_showLevel, "Уровень");
        if (_showLevel)
            DrawLevelSection();

        EditorGUILayout.Space();

        _showNet = EditorGUILayout.Foldout(_showNet, "Сеть / мультиплеер (заглушки)");
        if (_showNet)
            DrawNetSection();

        EditorGUILayout.Space();

        _showPerf = EditorGUILayout.Foldout(_showPerf, "Перф / системные");
        if (_showPerf)
            DrawPerfSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Debug Window", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"PlayMode: {Application.isPlaying}");
        EditorGUILayout.LabelField($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Большинство действий работают только в Play Mode.", MessageType.Info);
        }

        if (GUILayout.Button("Открыть SaveTools"))
        {
            SaveToolsWindow.ShowWindow();
        }
    }

    // ---------- RUNTIME / GAME ----------

    private void DrawRuntimeSection()
    {
        if (!Application.isPlaying)
            return;

        var main = FindMainInstance();
        if (main == null)
        {
            EditorGUILayout.HelpBox("Main не найден в сцене.", MessageType.Warning);
            return;
        }

        var game = main.Game;

        EditorGUILayout.LabelField("Игра", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("StartGame()"))
            main.StartGame();

        if (game != null && GUILayout.Button("SaveAll()"))
            game.SaveAll();

        EditorGUILayout.EndHorizontal();

        if (_progress != null)
        {
            EditorGUILayout.LabelField($"Текущий уровень: {_progress.LevelNumber}");
            EditorGUILayout.LabelField(
                $"Pos: ({_progress.PlayerPosX:F2}, {_progress.PlayerPosY:F2}, {_progress.PlayerPosZ:F2})");
            EditorGUILayout.LabelField(
                $"Cam: yaw={_progress.CameraYaw:F1} pitch={_progress.CameraPitch:F1}");
        }
    }

    // ---------- PLAYER ----------

    private void DrawPlayerSection()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Секция игрока доступна только в Play Mode.", MessageType.Info);
            return;
        }

        var main = FindMainInstance();
        var game = main != null ? main.Game : null;
        if (game == null)
        {
            EditorGUILayout.HelpBox("Game ещё не создан.", MessageType.Warning);
            return;
        }

        // пытаемся найти PlayerCharacter и его компоненты
        var levelGO        = GameObject.FindFirstObjectByType<Level>();
        PlayerCharacter pc = levelGO != null ? levelGO.PlayerCharacter : null;

        if (pc == null)
        {
            EditorGUILayout.HelpBox("PlayerCharacter не найден.", MessageType.Warning);
            return;
        }

        var mover   = pc.GetComponentInChildren<GroundMover>();
        var cameraC = pc.GetComponentInChildren<CameraController>();

        EditorGUILayout.LabelField("Игрок", EditorStyles.boldLabel);

        Vector3 pos = pc.transform.position;
        EditorGUILayout.LabelField($"Position: {pos}");

        if (mover != null)
        {
            EditorGUILayout.LabelField($"Speed: {mover.CurrentSpeed:F2}");
            EditorGUILayout.LabelField($"IsGrounded: {mover.IsGrounded}");
        }

        if (cameraC != null)
        {
            EditorGUILayout.LabelField($"Cam yaw: {cameraC.GetYaw():F1}");
            EditorGUILayout.LabelField($"Cam pitch: {cameraC.GetPitch():F1}");
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Телепорт к спавну"))
        {
            if (levelGO != null)
                pc.transform.position = levelGO.CharacterSpawnPosition;
        }

        if (GUILayout.Button("Заблокировать ввод"))
        {
            InputManager.Instance?.Lock();
        }

        if (GUILayout.Button("Разблокировать ввод"))
        {
            InputManager.Instance?.Unlock();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("God mode (HP 999)"))
        {
            pc.TakeDamage(-999);
        }

        if (GUILayout.Button("Убить игрока"))
        {
            pc.TakeDamage(9999);
        }

        EditorGUILayout.EndHorizontal();
    }

    // ---------- LEVEL ----------

    private void DrawLevelSection()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Секция уровня доступна только в Play Mode.", MessageType.Info);
            return;
        }

        var level = GameObject.FindFirstObjectByType<Level>();
        if (level == null)
        {
            EditorGUILayout.HelpBox("Level не найден.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Уровень", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Имя префаба:", level.name);
        EditorGUILayout.LabelField("Spawn point:", level.CharacterSpawnPosition.ToString());

        var levelSave = level.GetComponentInChildren<LevelSaveManager>();

        EditorGUILayout.BeginHorizontal();

        if (levelSave != null && GUILayout.Button("Сохранить состояние уровня"))
            levelSave.SaveLevelState();

        if (levelSave != null && GUILayout.Button("Загрузить состояние уровня"))
            levelSave.LoadLevelState();

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Перезагрузить сцену"))
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
        }
    }

    // ---------- NET (ЗАГЛУШКИ ПОД МУЛЬТИПЛЕЕР) ----------

    private void DrawNetSection()
    {
        EditorGUILayout.LabelField("Сеть / мультиплеер (планы)", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Здесь можно будет показать ping, RTT, список игроков, " +
            "лог RPC, управление lag-симуляцией и т.п.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Симулировать лаг (placeholder)"))
        {
            Debug.Log("[DebugWindow] Lag simulation toggle (TODO: интеграция с сетевым слоем).");
        }

        if (GUILayout.Button("Показать игроков (placeholder)"))
        {
            Debug.Log("[DebugWindow] Net players list (TODO).");
        }

        EditorGUILayout.EndHorizontal();
    }

    // ---------- PERF / SYSTEM ----------

    private void DrawPerfSection()
    {
        EditorGUILayout.LabelField("Performance / System", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("FPS (примерно):", (1f / Time.smoothDeltaTime).ToString("F1"));
        EditorGUILayout.LabelField("VSync:", QualitySettings.vSyncCount.ToString());
        EditorGUILayout.LabelField("Target FPS:", Application.targetFrameRate.ToString());

        EditorGUILayout.Space();

        if (GUILayout.Button("Timescale = 0.5"))
            Time.timeScale = 0.5f;

        if (GUILayout.Button("Timescale = 1"))
            Time.timeScale = 1f;

        if (GUILayout.Button("Timescale = 2"))
            Time.timeScale = 2f;

        EditorGUILayout.Space();

        if (GUILayout.Button("Выгрузить все PlayerPrefs"))
        {
            var sb = new StringBuilder();
            sb.AppendLine("PlayerPrefs dump (не все API дают перечень ключей — это заглушка).");
            Debug.Log(sb.ToString());
        }
    }

    // ---------- UTILS ----------

    private Main FindMainInstance()
    {
        return Object.FindFirstObjectByType<Main>();
    }
}
