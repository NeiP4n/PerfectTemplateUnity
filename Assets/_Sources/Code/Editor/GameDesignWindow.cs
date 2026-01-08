using UnityEditor;
using UnityEngine;
using Sources.Code;
using Sources.Code.Configs;
using Sources.Code.Interfaces;

public class GameDesignWindow : EditorWindow
{
    private Vector2 _scroll;
    private bool   _showFlow   = true;
    private bool   _showLevels = true;
    private bool   _showZones  = true;

    [MenuItem("Tools/Игра/Game Design Window")]
    public static void ShowWindow()
    {
        GetWindow<GameDesignWindow>("Game Design");
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawHeader();
        EditorGUILayout.Space();

        _showFlow = EditorGUILayout.Foldout(_showFlow, "Игровой флоу / сложность");
        if (_showFlow)
            DrawFlowSection();

        EditorGUILayout.Space();

        _showLevels = EditorGUILayout.Foldout(_showLevels, "Уровни и спавн");
        if (_showLevels)
            DrawLevelsSection();

        EditorGUILayout.Space();

        _showZones = EditorGUILayout.Foldout(_showZones, "Триггер‑зоны / интеракции");
        if (_showZones)
            DrawZonesSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Панель геймдизайнера", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Текущая сцена:",
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Некоторые настройки применяются только в Play Mode.", MessageType.Info);
    }

    // ---------- FLOW / DIFFICULTY ----------

    private void DrawFlowSection()
    {
        var gameFlow = GameFlowConfig.Instance;
        if (gameFlow == null)
        {
            EditorGUILayout.HelpBox("GameFlowConfig.Instance == null", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Глобальные настройки флоу", EditorStyles.boldLabel);

        gameFlow.EnableLoadingScreen = EditorGUILayout.Toggle("Loading Screen", gameFlow.EnableLoadingScreen);
        gameFlow.EnableCutscene      = EditorGUILayout.Toggle("Cutscene перед игрой", gameFlow.EnableCutscene);

        if (GUI.changed)
            EditorUtility.SetDirty(gameFlow);

        EditorGUILayout.Space();

        var levels = LevelsConfig.Instance;
        if (levels != null)
            EditorGUILayout.LabelField("Количество уровней: " + levels.LevelCount);

        if (Application.isPlaying)
        {
            var main = FindMainInstance();
            var game = main != null ? main.Game : null;

            if (game != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Текущий уровень во время игры", EditorStyles.boldLabel);

                int cur   = game.CurrentLevelNumber;
                int newVal = EditorGUILayout.IntSlider("Level Number", cur, 1, game.MaxLevels);
                if (newVal != cur)
                    game.CurrentLevelNumber = newVal;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Перезапустить с этим уровнем"))
                    main.StartGame();
                if (GUILayout.Button("Следующий уровень"))
                {
                    game.CurrentLevelNumber = Mathf.Clamp(cur + 1, 1, game.MaxLevels);
                    main.StartGame();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    // ---------- LEVELS / SPAWN ----------

    private void DrawLevelsSection()
    {
        var levels = LevelsConfig.Instance;
        if (levels == null)
        {
            EditorGUILayout.HelpBox("LevelsConfig.Instance == null", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Уровни (LevelsConfig)", EditorStyles.boldLabel);

        for (int i = 0; i < levels.LevelCount; i++)
        {
            var lvl = levels.GetLevelPrefabByIndex(i);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField($"Level {i + 1}", lvl, typeof(Level), false);
            if (lvl != null && GUILayout.Button("Открыть префаб", GUILayout.Width(120)))
                Selection.activeObject = lvl.gameObject;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        if (Application.isPlaying)
        {
            var level = Object.FindFirstObjectByType<Level>();
            if (level != null)
            {
                EditorGUILayout.LabelField("Текущий инстанс Level", EditorStyles.boldLabel);
                EditorGUILayout.ObjectField("Level", level, typeof(Level), true);
                EditorGUILayout.Vector3Field("SpawnPoint", level.CharacterSpawnPosition);
            }
        }
    }

    // ---------- TRIGGER ZONES / INTERACTION ----------

    private void DrawZonesSection()
    {
        EditorGUILayout.LabelField("Триггер‑зоны / интеракция", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            var interactables = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            int countInteract = 0;
            var list = new System.Collections.Generic.List<Object>();

            foreach (var m in interactables)
            {
                if (m is IInteractable)
                {
                    countInteract++;
                    list.Add(m.gameObject);
                }
            }

            EditorGUILayout.LabelField("IInteractable в сцене:", countInteract.ToString());

            if (countInteract > 0 && GUILayout.Button("Выделить все IInteractable"))
                Selection.objects = list.ToArray();
        }
        else
        {
            EditorGUILayout.HelpBox("Поиск IInteractable работает только в Play Mode.", MessageType.Info);
        }
    }

    // ---------- UTILS ----------

    private Main FindMainInstance()
    {
        return Object.FindFirstObjectByType<Main>();
    }
}
