using System.Collections.Generic;
using Flowers.Core;
using Flowers.Levels;
using Flowers.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Flowers.Editor.Wiring;

namespace Flowers.Editor
{
    /// <summary>
    /// Builds the game scene from scratch and wires every reference. Safe to run again at any time:
    /// whenever a new component or serialized field is added, it gets wired here.
    /// Kept between runs: BoardViewConfig / JuiceConfig tuning and the level assets (Flowers → Rebuild Levels rewrites those).
    /// The UI screens themselves are built by <see cref="UiScreensBuilder"/>.
    /// </summary>
    public static class SceneSetupMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string ConfigPath = "Assets/_Project/Data/BoardViewConfig.asset";
        private const string FlowerPrefabPath = "Assets/_Project/Prefabs/Flower.prefab";
        private const string JuiceConfigPath = "Assets/_Project/Data/JuiceConfig.asset";
        private const string MessagesPath = "Assets/_Project/Data/PersonalMessages.asset";
        private const string ParticleMaterialPath = "Assets/_Project/Art/Materials/Petals.mat";
        private const string UnlitSpriteShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

        [MenuItem("Flowers/Setup Scene")]
        public static void SetupScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Wiring.Reset();

            var art = GameArt.GenerateAll();
            CreateOrUpdateConfig(art);
            CreateFlowerPrefab(art.Flowers[0]);
            LevelBuilder.Build(overwriteExisting: false); // creates missing catalog levels only
            CreateOrUpdateJuiceConfig(PlaceholderAudio.GenerateAll());
            CreateOrUpdateParticleMaterial(art.Petal);
            CreateMessagesIfMissing();
            AssetDatabase.SaveAssets();

            BuildScene();

            if (Wiring.Failed)
                Debug.LogError("[Flowers] Setup Scene finished with wiring errors (see above). The scene will not work until they are fixed.");
            else
                Debug.Log($"[Flowers] Setup Scene done: {ScenePath} is open and set as the first build scene. Press Play.");
        }

        // ---------- Assets ----------

        /// <summary>Creates JuiceConfig once (tuning is kept), and always plugs in the freshly generated clips.</summary>
        private static void CreateOrUpdateJuiceConfig(Dictionary<SoundId, AudioClip> clips)
        {
            var juice = AssetDatabase.LoadAssetAtPath<JuiceConfig>(JuiceConfigPath);
            if (juice == null)
            {
                juice = ScriptableObject.CreateInstance<JuiceConfig>();
                AssetDatabase.CreateAsset(juice, JuiceConfigPath);
            }

            foreach (var pair in clips)
                juice.EditorAssignClip(pair.Key, pair.Value, PlaceholderAudio.DefaultVolume(pair.Key));
            EditorUtility.SetDirty(juice);
        }

        /// <summary>Creates the personal texts once with the default wording. Never overwrites: Utku edits them.</summary>
        private static void CreateMessagesIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<PersonalMessages>(MessagesPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PersonalMessages>(), MessagesPath);
        }

        private static void CreateOrUpdateParticleMaterial(Sprite petal)
        {
            var shader = Shader.Find(UnlitSpriteShader);
            if (shader == null)
            {
                Fail($"shader '{UnlitSpriteShader}' not found.");
                return;
            }

            EditorFolders.Ensure("Assets/_Project/Art/Materials");
            var material = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ParticleMaterialPath);
            }
            material.shader = shader;
            material.mainTexture = petal != null ? petal.texture : null;
            EditorUtility.SetDirty(material);
        }

        private static void CreateOrUpdateConfig(GameArt.Result art)
        {
            EditorFolders.Ensure("Assets/_Project/Data");
            var config = AssetDatabase.LoadAssetAtPath<BoardViewConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BoardViewConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            if (config.EditorUpgradeLook())
                Debug.Log("[Flowers] BoardViewConfig colors updated to the new look.");
            config.EditorAssignSprites(art.Flowers, art.Tile, art.Frame, art.Mask);
            config.EditorAssignObstacleSprites(art.Pot, art.PotCracked, art.Vine, art.Soil);
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

        // ---------- Scene ----------

        private static void BuildScene()
        {
            EditorFolders.Ensure("Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load assets only AFTER NewScene: opening a scene unloads assets nothing is using, which turns
            // any asset reference held from before into a dead (null) object and it gets wired as "None".
            var config = AssetDatabase.LoadAssetAtPath<BoardViewConfig>(ConfigPath);
            var prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(FlowerPrefabPath);
            var flowerPrefab = prefabObject != null ? prefabObject.GetComponent<FlowerView>() : null;
            var levelList = AssetDatabase.LoadAssetAtPath<LevelList>(LevelBuilder.LevelListPath);
            var juiceConfig = AssetDatabase.LoadAssetAtPath<JuiceConfig>(JuiceConfigPath);
            var messages = AssetDatabase.LoadAssetAtPath<PersonalMessages>(MessagesPath);
            var particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            var sprites = LoadUiSprites(config);
            if (config == null)
            {
                Fail($"could not load {ConfigPath}.");
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

            // Garden picture behind the board, always covering the camera view.
            var background = new GameObject("Background");
            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = sprites.Garden;
            backgroundRenderer.sortingOrder = -100;
            var cover = background.AddComponent<CameraBackground>();
            Wire(cover, "targetCamera", camera);
            Wire(cover, "spriteRenderer", backgroundRenderer);

            var board = new GameObject("Board");
            var boardView = board.AddComponent<BoardView>();
            Wire(boardView, "config", config);
            Wire(boardView, "flowerPrefab", flowerPrefab);
            Wire(boardView, "targetCamera", camera);

            var swipe = board.AddComponent<SwipeInput>();
            Wire(swipe, "boardView", boardView);
            Wire(swipe, "targetCamera", camera);

            var feedback = new GameObject("Feedback").AddComponent<FeedbackPlayer>();
            Wire(feedback, "config", juiceConfig);

            // Petals fly over the flowers (order 10); confetti over everything on the board (order 20).
            var petals = CreateParticles("Petals", board.transform, particleMaterial, gravity: 6f, endSize: 0.4f, sortingOrder: 10);
            var confetti = CreateParticles("Confetti", board.transform, particleMaterial, gravity: 0.6f, endSize: 0.8f, sortingOrder: 20);

            var ui = UiScreensBuilder.Build(sprites);

            var juice = board.AddComponent<BoardJuice>();
            Wire(juice, "boardView", boardView);
            Wire(juice, "config", juiceConfig);
            Wire(juice, "petals", petals);
            Wire(juice, "confetti", confetti);
            Wire(juice, "praiseText", ui.Praise);
            Wire(juice, "targetCamera", camera);

            var game = new GameObject("Game");
            var controller = game.AddComponent<LevelController>();
            Wire(controller, "levelList", levelList);
            Wire(controller, "boardView", boardView);
            Wire(controller, "hud", ui.Hud);
            Wire(controller, "winScreen", ui.Win);
            Wire(controller, "loseScreen", ui.Lose);
            Wire(controller, "mapScreen", ui.Map);
            Wire(controller, "titleScreen", ui.Title);
            Wire(controller, "levelStartPopup", ui.LevelStart);
            Wire(controller, "boardJuice", juice);
            Wire(controller, "scoreIcon", sprites.Star);
            Wire(controller, "finalScreen", ui.Final);
            Wire(controller, "personalMessages", messages);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = game;
        }

        private static UiScreensBuilder.Sprites LoadUiSprites(BoardViewConfig config)
        {
            Sprite Load(string path)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) Fail($"sprite missing: {path}");
                return sprite;
            }

            return new UiScreensBuilder.Sprites
            {
                Panel = Load(GameArt.PanelPath),
                Button = Load(GameArt.ButtonPath),
                Round = Load(GameArt.RoundButtonPath),
                Star = Load(GameArt.StarPath),
                Check = Load(GameArt.CheckPath),
                Circle = Load(GameArt.CirclePath),
                Lock = Load(GameArt.LockPath),
                Ribbon = Load(GameArt.RibbonPath),
                Rays = Load(GameArt.RaysPath),
                Marker = Load(GameArt.MarkerPath),
                Garden = Load(GameArt.GardenPath),
                Meadow = Load(GameArt.MapMeadowPath),
                SkyCap = Load(GameArt.MapSkyCapPath),
                Heart = Load(GameArt.HeartPath),
                Flowers = config != null ? config.FlowerSprites : new Sprite[0],
            };
        }

        /// <summary>
        /// A particle system that only emits from code (BoardJuice calls Emit). It runs all the time with
        /// emission off, so emitted particles always simulate. Particles fade out and shrink over their life.
        /// </summary>
        private static ParticleSystem CreateParticles(string name, Transform parent, Material material,
            float gravity, float endSize, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.startSpeed = 0f;
            main.startLifetime = 1f;
            main.maxParticles = 1500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;

            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endSize));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            if (material == null) Fail($"particle material missing for {name}.");
            return ps;
        }
    }
}
