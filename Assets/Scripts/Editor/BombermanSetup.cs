using System.Collections.Generic;
using System.Linq;
using ThanhHoang.Bomberman.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman.EditorTools
{
    /// <summary>
    /// One-click migration to the UI Toolkit + multiplayer setup. Safe to run more than once.
    /// - creates the PanelSettings asset used by every UIDocument
    /// - moves the Player prefab into Resources (needed by PhotonNetwork.Instantiate)
    /// - MainMenu: removes the old uGUI canvas, adds the UI Toolkit menu
    /// - Level1: removes the old uGUI HUD and the player placed in the scene (players are spawned
    ///   at runtime), adds the BombManager and the UI Toolkit HUD
    /// </summary>
    public static class BombermanSetup
    {
        const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        const string LevelScenePath = "Assets/Scenes/Level1.unity";
        const string PanelSettingsPath = "Assets/UI/PanelSettings.asset";
        const string ThemePath = "Assets/UI/Theme/BombermanTheme.tss";
        const string MainMenuUxmlPath = "Assets/UI/MainMenu/MainMenu.uxml";
        const string HudUxmlPath = "Assets/UI/Hud/Hud.uxml";
        const string PlayerPrefabOldPath = "Assets/Prefabs/Player.prefab";
        const string PlayerPrefabPath = "Assets/Resources/Player.prefab";
        const string BombPrefabPath = "Assets/Prefabs/Bomb.prefab";
        const string ExplosionPrefabPath = "Assets/Prefabs/Explosion.prefab";

        [MenuItem("Tools/Bomberman/Setup UI Toolkit + Multiplayer")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Bomberman Setup] Exit Play Mode first.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var log = new List<string>();
            PanelSettings panelSettings = EnsurePanelSettings(log);
            EnsurePlayerPrefabInResources(log);
            SetupMainMenu(panelSettings, log);
            SetupLevel(panelSettings, log);
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            Debug.Log("[Bomberman Setup] Done:\n- " + string.Join("\n- ", log));
        }

        static PanelSettings EnsurePanelSettings(List<string> log)
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
                log.Add("Created " + PanelSettingsPath);
            }

            panelSettings.themeStyleSheet = Require<ThemeStyleSheet>(ThemePath);
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            EditorUtility.SetDirty(panelSettings);
            return panelSettings;
        }

        static void EnsurePlayerPrefabInResources(List<string> log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) != null) return;

            string error = AssetDatabase.MoveAsset(PlayerPrefabOldPath, PlayerPrefabPath);
            if (!string.IsNullOrEmpty(error))
                throw new System.InvalidOperationException("[Bomberman Setup] Could not move the Player prefab: " + error);
            log.Add($"Moved {PlayerPrefabOldPath} -> {PlayerPrefabPath}");
        }

        static void SetupMainMenu(PanelSettings panelSettings, List<string> log)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            RemoveCanvasRoots(scene, log);

            GameObject ui = FindOrCreateRoot(scene, "MainMenuUI");
            SetupDocument(ui, panelSettings, Require<VisualTreeAsset>(MainMenuUxmlPath));
            GetOrAdd<MainMenuUI>(ui);

            if (Object.FindFirstObjectByType<NetworkController>() == null)
                FindOrCreateRoot(scene, "Network").AddComponent<NetworkController>();

            Save(scene, log);
        }

        static void SetupLevel(PanelSettings panelSettings, List<string> log)
        {
            Scene scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
            RemoveCanvasRoots(scene, log);

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
                throw new System.InvalidOperationException("[Bomberman Setup] No GameManager found in " + LevelScenePath);

            // The player in the scene becomes the Story spawn point.
            foreach (Player player in Object.FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(player.gameObject) ?? player.gameObject;
                Transform spawn = FindOrCreateRoot(scene, "StorySpawn").transform;
                spawn.position = root.transform.position;
                SetReference(gameManager, "_storySpawn", spawn);
                Object.DestroyImmediate(root);
                log.Add("Level1: replaced the scene Player with 'StorySpawn'");
            }

            BombManager bombManager = GetOrAdd<BombManager>(gameManager.gameObject);
            SetReference(bombManager, "_bombPrefab", Require<GameObject>(BombPrefabPath).GetComponent<Bomb>());
            SetReference(bombManager, "_explosionPrefab", Require<GameObject>(ExplosionPrefabPath));

            GameObject hud = FindOrCreateRoot(scene, "HUD");
            SetupDocument(hud, panelSettings, Require<VisualTreeAsset>(HudUxmlPath));
            GetOrAdd<HudUI>(hud);

            Save(scene, log);
        }

        static void RemoveCanvasRoots(Scene scene, List<string> log)
        {
            foreach (GameObject root in scene.GetRootGameObjects().Where(r => r.GetComponent<Canvas>() != null))
            {
                log.Add($"{scene.name}: removed old uGUI canvas '{root.name}'");
                Object.DestroyImmediate(root);
            }
        }

        static void SetupDocument(GameObject go, PanelSettings panelSettings, VisualTreeAsset uxml)
        {
            UIDocument document = GetOrAdd<UIDocument>(go);
            document.panelSettings = panelSettings;
            document.visualTreeAsset = uxml;
            EditorUtility.SetDirty(document);
        }

        static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new System.InvalidOperationException($"[Bomberman Setup] Field '{field}' not found on {target.GetType().Name}");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject FindOrCreateRoot(Scene scene, string name)
        {
            GameObject existing = scene.GetRootGameObjects().FirstOrDefault(r => r.name == name);
            if (existing != null) return existing;

            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            return go.TryGetComponent(out T component) ? component : go.AddComponent<T>();
        }

        static T Require<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new System.InvalidOperationException($"[Bomberman Setup] Missing asset: {path} ({typeof(T).Name})");
            return asset;
        }

        static void Save(Scene scene, List<string> log)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log.Add($"Saved {scene.path}");
        }
    }
}
