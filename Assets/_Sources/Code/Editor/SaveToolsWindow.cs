using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Sources.Code;
using Sources.Code.Gameplay.GameSaves;

public class SaveToolsWindow : EditorWindow
{
    private PlayerProgress _progress;

    [MenuItem("Tools/Игра/Панель сохранений")]
    public static void ShowWindow()
    {
        GetWindow<SaveToolsWindow>("Сохранения");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Панель сохранений", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        var loader = GameSaverLoader.Instance;
        if (loader == null)
        {
            EditorGUILayout.HelpBox(
                "GameSaverLoader.Instance == null.\n" +
                "Нужен один объект с GameSaverLoader в стартовой сцене.",
                MessageType.Warning);
            return;
        }

        _progress = loader.PlayerProgress;
        if (_progress == null)
        {
            EditorGUILayout.HelpBox("PlayerProgress == null.", MessageType.Warning);
            return;
        }

        DrawProgressSection(loader);
        EditorGUILayout.Space();
        DrawRuntimeSection(loader);
    }

    private void DrawProgressSection(GameSaverLoader loader)
    {
        EditorGUILayout.LabelField("Прогресс игрока", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int level = EditorGUILayout.IntField("Номер уровня", _progress.LevelNumber);
        if (EditorGUI.EndChangeCheck())
        {
            if (level < 1) level = 1;
            _progress.LevelNumber = level;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Сохранить прогресс (PlayerPrefs)"))
        {
            ForceSave(loader);
            Debug.Log("[SaveTools] Прогресс сохранён в PlayerPrefs.");
        }

        if (GUILayout.Button("Сбросить прогресс"))
        {
            if (EditorUtility.DisplayDialog(
                    "Сброс прогресса",
                    "Сбросить прогресс игрока к уровню 1?",
                    "Да", "Нет"))
            {
                ResetProgress();
                ForceSave(loader);
                Debug.Log("[SaveTools] Прогресс сброшен и сохранён.");
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Уровень - 1"))
        {
            if (_progress.LevelNumber > 1)
                _progress.LevelNumber--;
        }

        if (GUILayout.Button("Уровень + 1"))
        {
            _progress.LevelNumber++;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Кнопки выше меняют только PlayerProgress.\n" +
            "Чтобы игра увидела изменения, нажми \"Сохранить прогресс\".",
            MessageType.Info);
    }

    private void DrawRuntimeSection(GameSaverLoader loader)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Во время игры (Play Mode)", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Кнопки ниже работают только в Play Mode.",
                MessageType.Info);
            return;
        }

        Main main = FindMainInstance();
        if (main == null)
        {
            EditorGUILayout.HelpBox(
                "Объект Sources.Code.Main не найден в сцене.",
                MessageType.Warning);
            return;
        }

        Sources.Code.Gameplay.Game game = main.Game;

        if (game != null && GUILayout.Button("Сохранить ВСЁ (уровень + игрок + камера)"))
        {
            game.SaveAll();     // внутри должен звать LevelSaveManager.SaveLevelState и сохранить PlayerProgress
            ForceSave(loader);  // записать PlayerProgress в PlayerPrefs
            Debug.Log("[SaveTools] SaveAll: всё сохранено.");
        }

        if (GUILayout.Button("Перезапустить игру с текущим прогрессом"))
        {
            main.StartGame();
            Debug.Log("[SaveTools] Игра перезапущена через Main.StartGame().");
        }

        if (GUILayout.Button("Новая игра (сброс прогресса)"))
        {
            if (EditorUtility.DisplayDialog(
                    "Новая игра",
                    "Сбросить прогресс и начать с уровня 1?",
                    "Да", "Нет"))
            {
                ResetProgress();
                ForceSave(loader);
                main.StartGame();
                Debug.Log("[SaveTools] Прогресс сброшен. Новая игра запущена.");
            }
        }
    }

    private Main FindMainInstance()
    {
        return Object.FindFirstObjectByType<Main>();
    }

    private void ResetProgress()
    {
        _progress.LevelNumber = 1;
        _progress.PlayerPosX  = 0;
        _progress.PlayerPosY  = 0;
        _progress.PlayerPosZ  = 0;
        _progress.CameraYaw   = 0;
        _progress.CameraPitch = 0;
        _progress.ObjectsState = new Dictionary<string, string>();
    }

    private void ForceSave(GameSaverLoader loader)
    {
        string json      = JsonConvert.SerializeObject(loader.PlayerProgress);
        string encrypted = Encrypt(json);
        PlayerPrefs.SetString("SettingsProgress", encrypted);
        PlayerPrefs.Save();
    }

    private string Encrypt(string plain)
    {
        const string key = "VerySimpleKey123";

        if (string.IsNullOrEmpty(plain))
            return plain;

        char[] buffer = new char[plain.Length];
        for (int i = 0; i < plain.Length; i++)
        {
            char keyChar = key[i % key.Length];
            buffer[i] = (char)(plain[i] ^ keyChar);
        }

        return System.Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(buffer));
    }
}
