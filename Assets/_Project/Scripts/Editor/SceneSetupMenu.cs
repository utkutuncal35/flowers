using Flowers.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Flowers.Editor
{
    /// <summary>
    /// Builds the game scene from scratch and wires every reference. Safe to run again at any time:
    /// whenever a new component or serialized field is added, it gets wired here.
    /// Tuning values in BoardViewConfig.asset are kept between runs.
    /// </summary>
    public static class SceneSetupMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string ConfigPath = "Assets/_Project/Data/BoardViewConfig.asset";
        private const string FlowerPrefabPath = "Assets/_Project/Prefabs/Flower.prefab";

        [MenuItem("Flowers/Setup Scene")]
        public static void SetupScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            _wiringFailed = false;

            var art = PlaceholderArt.GenerateAll();
            CreateOrUpdateConfig(art);
            CreateFlowerPrefab(art.Flowers[0]);
            AssetDatabase.SaveAssets();

            BuildScene();

            if (_wiringFailed)
                Debug.LogError("[Flowers] Setup Scene finished with wiring errors (see above). The scene will not work until they are fixed.");
            else
                Debug.Log($"[Flowers] Setup Scene done: {ScenePath} is open and set as the first build scene. Press Play.");
        }

        private static bool _wiringFailed;

        private static void CreateOrUpdateConfig(PlaceholderArt.Result art)
        {
            EditorFolders.Ensure("Assets/_Project/Data");
            var config = AssetDatabase.LoadAssetAtPath<BoardViewConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BoardViewConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.EditorAssignSprites(art.Flowers, art.Tile, art.Frame, art.Mask);
            EditorUtility.SetDirty(config);
        }

        private static void CreateFlowerPrefab(Sprite previewSprite)
        {
            EditorFolders.Ensure("Assets/_Project/Prefabs");

            var go = new GameObject("Flower");
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = previewSprite;
            spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            var flower = go.AddComponent<FlowerView>();
            Wire(flower, "spriteRenderer", spriteRenderer);

            PrefabUtility.SaveAsPrefabAsset(go, FlowerPrefabPath);
            Object.DestroyImmediate(go);
        }

        private static void BuildScene()
        {
            EditorFolders.Ensure("Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load assets only AFTER NewScene: opening a scene unloads assets nothing is using, which turns
            // any asset reference held from before into a dead (null) object and it gets wired as "None".
            var config = AssetDatabase.LoadAssetAtPath<BoardViewConfig>(ConfigPath);
            var prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(FlowerPrefabPath);
            var flowerPrefab = prefabObject != null ? prefabObject.GetComponent<FlowerView>() : null;
            if (config == null)
            {
                Debug.LogError($"[Flowers] Setup: could not load {ConfigPath}.");
                _wiringFailed = true;
                return;
            }

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = config.BackgroundColor;
            cameraObject.AddComponent<AudioListener>();

            // Sprites use the 2D Lit material; one global light keeps them at their true colors.
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;

            var board = new GameObject("Board");
            var boardView = board.AddComponent<BoardView>();
            Wire(boardView, "config", config);
            Wire(boardView, "flowerPrefab", flowerPrefab);
            Wire(boardView, "targetCamera", camera);

            var swipe = board.AddComponent<SwipeInput>();
            Wire(swipe, "boardView", boardView);
            Wire(swipe, "targetCamera", camera);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = board;
        }

        /// <summary>Sets a [SerializeField] private field and fails loudly if the field name is wrong or the value is missing.</summary>
        private static void Wire(Object target, string fieldName, Object value)
        {
            if (value == null)
            {
                Debug.LogError($"[Flowers] Setup: nothing to assign to {target.GetType().Name}.{fieldName} (the value is null/destroyed).");
                _wiringFailed = true;
                return;
            }

            var so = new SerializedObject(target);
            var property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[Flowers] Setup: {target.GetType().Name} has no serialized field '{fieldName}'.");
                _wiringFailed = true;
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
