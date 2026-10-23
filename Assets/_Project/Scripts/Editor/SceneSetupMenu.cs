using System.Collections.Generic;
using Flowers.Core;
using Flowers.Levels;
using Flowers.UI;
using Flowers.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Flowers.Editor
{
    /// <summary>
    /// Builds the game scene from scratch and wires every reference. Safe to run again at any time:
    /// whenever a new component or serialized field is added, it gets wired here.
    /// Kept between runs: BoardViewConfig values, the level assets and the LevelList (only created if missing).
    /// </summary>
    public static class SceneSetupMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string ConfigPath = "Assets/_Project/Data/BoardViewConfig.asset";
        private const string FlowerPrefabPath = "Assets/_Project/Prefabs/Flower.prefab";
        private const string LevelsFolder = "Assets/_Project/Data/Levels";
        private const string LevelListPath = "Assets/_Project/Data/LevelList.asset";
        private const string JuiceConfigPath = "Assets/_Project/Data/JuiceConfig.asset";
        private const string ParticleMaterialPath = "Assets/_Project/Art/Materials/Petals.mat";
        private const string UnlitSpriteShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

        // UI palette (reference resolution 1080x1920)
        private static readonly Color Bordo = new Color32(0x6A, 0x14, 0x30, 0xFF);
        private static readonly Color BordoButton = new Color32(0x8E, 0x2C, 0x48, 0xFF);
        private static readonly Color Cream = new Color32(0xFF, 0xF7, 0xEE, 0xFF);
        private static readonly Color HudBackground = new Color32(0xF5, 0xE1, 0xDD, 0xFF);
        private static readonly Color LightPink = new Color32(0xF7, 0xA9, 0xC3, 0xFF);
        private static readonly Color LeafGreen = new Color32(0x5E, 0x8C, 0x4A, 0xFF);
        private static readonly Color Dim = new Color(0.2f, 0.03f, 0.08f, 0.55f);
        private static readonly Color StarRed = new Color32(0xC8, 0x1D, 0x3F, 0xFF);
        private static readonly Color LockGray = new Color32(0xB9, 0xA3, 0xA0, 0xFF);

        private static bool _wiringFailed;

        [MenuItem("Flowers/Setup Scene")]
        public static void SetupScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            _wiringFailed = false;

            var art = PlaceholderArt.GenerateAll();
            CreateOrUpdateConfig(art);
            CreateFlowerPrefab(art.Flowers[0]);
            CreateSampleLevelsIfMissing();
            CreateOrUpdateJuiceConfig(PlaceholderAudio.GenerateAll());
            CreateOrUpdateParticleMaterial(art.Petal);
            AssetDatabase.SaveAssets();

            BuildScene();

            if (_wiringFailed)
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

        private static void CreateOrUpdateParticleMaterial(Sprite petal)
        {
            var shader = Shader.Find(UnlitSpriteShader);
            if (shader == null)
            {
                Debug.LogError($"[Flowers] Setup: shader '{UnlitSpriteShader}' not found.");
                _wiringFailed = true;
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

        /// <summary>
        /// Sample levels, tuned by simulating 300 games each with the real board engine
        /// (win rate random player / goal-focused player):
        /// 1: ~99% / 100%, 2: ~73% / 96%, 3: ~65% / 79%.
        /// Existing level assets are never overwritten, so edits made in the Inspector stay.
        /// </summary>
        private static void CreateSampleLevelsIfMissing()
        {
            EditorFolders.Ensure(LevelsFolder);

            var levels = new[]
            {
                // Tutorial: 4 flower types (lots of matches), plenty of moves, one goal.
                CreateLevelIfMissing("Level01_Tutorial", 7, 8, 4, 15,
                    new[] { LevelGoal.Collect(2, 10) }, twoStars: 6, threeStars: 10),
                // Medium: two goals at once.
                CreateLevelIfMissing("Level02_TwoFlowers", 7, 9, 5, 16,
                    new[] { LevelGoal.Collect(0, 15), LevelGoal.Collect(1, 15) }, twoStars: 4, threeStars: 7),
                // One goal, very few moves: chains matter.
                CreateLevelIfMissing("Level03_FewMoves", 7, 9, 5, 8,
                    new[] { LevelGoal.ReachScore(700) }, twoStars: 2, threeStars: 4),
            };

            var list = AssetDatabase.LoadAssetAtPath<LevelList>(LevelListPath);
            if (list == null)
            {
                list = ScriptableObject.CreateInstance<LevelList>();
                list.EditorSetLevels(levels);
                AssetDatabase.CreateAsset(list, LevelListPath);
            }
        }

        private static LevelData CreateLevelIfMissing(string assetName, int width, int height, int types, int moves,
            LevelGoal[] goals, int twoStars, int threeStars)
        {
            string path = $"{LevelsFolder}/{assetName}.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (level != null) return level;

            level = ScriptableObject.CreateInstance<LevelData>();
            level.EditorSetup(width, height, types, moves, goals, twoStars, threeStars);
            AssetDatabase.CreateAsset(level, path);
            return level;
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
            var levelList = AssetDatabase.LoadAssetAtPath<LevelList>(LevelListPath);
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlaceholderArt.Folder}/Frame.png");
            var starSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlaceholderArt.Folder}/Star.png");
            var checkSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlaceholderArt.Folder}/Check.png");
            var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlaceholderArt.Folder}/Circle.png");
            var lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlaceholderArt.Folder}/Lock.png");
            var juiceConfig = AssetDatabase.LoadAssetAtPath<JuiceConfig>(JuiceConfigPath);
            var particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
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

            var feedback = new GameObject("Feedback").AddComponent<FeedbackPlayer>();
            Wire(feedback, "config", juiceConfig);

            // Petals fly over the flowers (order 10); confetti over everything on the board (order 20).
            var petals = CreateParticles("Petals", board.transform, particleMaterial, gravity: 6f, endSize: 0.4f, sortingOrder: 10);
            var confetti = CreateParticles("Confetti", board.transform, particleMaterial, gravity: 0.6f, endSize: 0.8f, sortingOrder: 20);

            var canvas = CreateCanvas();
            canvas.gameObject.AddComponent<ButtonClickSounds>();
            CreateEventSystem();
            var hud = CreateHud(canvas, panelSprite, checkSprite);
            var praise = CreatePraiseText(canvas);

            var juice = board.AddComponent<BoardJuice>();
            Wire(juice, "boardView", boardView);
            Wire(juice, "config", juiceConfig);
            Wire(juice, "petals", petals);
            Wire(juice, "confetti", confetti);
            Wire(juice, "praiseText", praise);
            Wire(juice, "targetCamera", camera);
            var winScreen = CreateEndScreen(canvas, "Win Screen", panelSprite, starSprite, withStars: true);
            var loseScreen = CreateEndScreen(canvas, "Lose Screen", panelSprite, starSprite, withStars: false);
            // Created last so it draws over everything else on the canvas.
            var mapScreen = CreateMapScreen(canvas, circleSprite, lockSprite, starSprite);

            var game = new GameObject("Game");
            var controller = game.AddComponent<LevelController>();
            Wire(controller, "levelList", levelList);
            Wire(controller, "boardView", boardView);
            Wire(controller, "hud", hud);
            Wire(controller, "winScreen", winScreen);
            Wire(controller, "loseScreen", loseScreen);
            Wire(controller, "mapScreen", mapScreen);
            Wire(controller, "boardJuice", juice);
            Wire(controller, "scoreIcon", starSprite);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = game;
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
            if (material == null)
            {
                Debug.LogError($"[Flowers] Setup: particle material missing for {name}.");
                _wiringFailed = true;
            }
            return ps;
        }

        private static PraiseText CreatePraiseText(Transform canvas)
        {
            var rect = UiBuilder.CreateRect("Praise", canvas);
            UiBuilder.Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1000f, 220f));
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var label = UiBuilder.CreateText("Label", rect, "Harika!", 120, Bordo, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Stretch(label.rectTransform);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Cream;
            outline.effectDistance = new Vector2(5f, -5f);

            var praise = rect.gameObject.AddComponent<PraiseText>();
            Wire(praise, "label", label);
            Wire(praise, "canvasGroup", group);
            return praise;
        }

        private static Transform CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go.transform;
        }

        /// <summary>Buttons need an EventSystem. The Input System UI module is required (the old input API is off).</summary>
        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static LevelHud CreateHud(Transform canvas, Sprite panelSprite, Sprite checkSprite)
        {
            var background = UiBuilder.CreateImage("HUD", canvas, panelSprite, HudBackground, sliced: true);
            var root = background.rectTransform;
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.offsetMin = new Vector2(30f, -270f);
            root.offsetMax = new Vector2(-30f, -30f);

            var mapButton = UiBuilder.CreateButton("Map Button", root, panelSprite, BordoButton, "Harita", Cream, 34);
            UiBuilder.Place((RectTransform)mapButton.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(170f, 62f));

            var levelText = UiBuilder.CreateText("Level", root, "Bölüm 1", 40, Bordo, TextAnchor.MiddleLeft);
            UiBuilder.Place(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(214f, -22f), new Vector2(260f, 50f));

            var movesLabel = UiBuilder.CreateText("Moves Label", root, "Hamle", 36, Bordo);
            UiBuilder.Place(movesLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(130f, 20f), new Vector2(200f, 50f));
            var movesText = UiBuilder.CreateText("Moves", root, "20", 96, Bordo, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Place(movesText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(130f, -45f), new Vector2(200f, 110f));

            var goals = UiBuilder.CreateRect("Goals", root);
            goals.anchorMin = new Vector2(0f, 0f);
            goals.anchorMax = new Vector2(1f, 1f);
            goals.offsetMin = new Vector2(260f, 15f);
            goals.offsetMax = new Vector2(-30f, -15f);
            var layout = goals.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 28f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var template = CreateGoalItem(goals, checkSprite);

            var hud = background.gameObject.AddComponent<LevelHud>();
            Wire(hud, "levelText", levelText);
            Wire(hud, "movesText", movesText);
            Wire(hud, "goalsContainer", goals);
            Wire(hud, "goalTemplate", template);
            Wire(hud, "mapButton", mapButton);
            return hud;
        }

        private static GoalItemView CreateGoalItem(Transform parent, Sprite checkSprite)
        {
            var root = UiBuilder.CreateRect("Goal Template", parent);
            root.sizeDelta = new Vector2(140f, 200f);

            var icon = UiBuilder.CreateImage("Icon", root, null, Color.white);
            UiBuilder.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(120f, 120f));

            var count = UiBuilder.CreateText("Count", root, "10", 56, Bordo, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Place(count.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(140f, 70f));

            var check = UiBuilder.CreateImage("Check", root, checkSprite, LeafGreen);
            UiBuilder.Place(check.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(64f, 64f));

            var item = root.gameObject.AddComponent<GoalItemView>();
            Wire(item, "icon", icon);
            Wire(item, "countText", count);
            Wire(item, "checkMark", check);
            root.gameObject.SetActive(false);
            return item;
        }

        private static LevelEndScreen CreateEndScreen(Transform canvas, string name, Sprite panelSprite, Sprite starSprite, bool withStars)
        {
            // Full-screen dim layer; it also blocks touches from reaching anything behind it.
            var dim = UiBuilder.CreateImage(name, canvas, null, Dim);
            dim.raycastTarget = true;
            UiBuilder.Stretch(dim.rectTransform);

            float cardHeight = withStars ? 900f : 720f;
            var card = UiBuilder.CreateImage("Card", dim.transform, panelSprite, Cream, sliced: true);
            UiBuilder.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, cardHeight));

            var title = UiBuilder.CreateText("Title", card.transform, "Tebrikler!", 84, Bordo, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(780f, 110f));

            var message = UiBuilder.CreateText("Message", card.transform, "", 44, Bordo);
            UiBuilder.Place(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(780f, 130f));

            var stars = new Image[withStars ? 3 : 0];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = UiBuilder.CreateImage($"Star {i + 1}", card.transform, starSprite, Color.white);
                // Middle star a bit bigger and higher, like most match-3 games.
                float size = i == 1 ? 200f : 160f;
                float y = i == 1 ? -420f : -440f;
                UiBuilder.Place(stars[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 210f, y), new Vector2(size, size));
            }

            // Harita | Tekrar | Sonraki. "Sonraki" may be hidden (no unlocked next level).
            var buttonSize = new Vector2(240f, 140f);
            var map = UiBuilder.CreateButton("Map Button", card.transform, panelSprite, HudBackground, "Harita", Bordo, 44);
            UiBuilder.Place((RectTransform)map.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-260f, 60f), buttonSize);
            var retry = UiBuilder.CreateButton("Retry Button", card.transform, panelSprite, LightPink, "Tekrar", Bordo, 44);
            UiBuilder.Place((RectTransform)retry.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), buttonSize);
            var next = UiBuilder.CreateButton("Next Button", card.transform, panelSprite, BordoButton, "Sonraki", Cream, 44);
            UiBuilder.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(260f, 60f), buttonSize);

            var screen = dim.gameObject.AddComponent<LevelEndScreen>();
            Wire(screen, "titleText", title);
            Wire(screen, "messageText", message);
            Wire(screen, "retryButton", retry);
            Wire(screen, "nextButton", next);
            Wire(screen, "mapButton", map);
            WireArray(screen, "stars", stars);
            dim.gameObject.SetActive(false);
            return screen;
        }

        private static LevelMapScreen CreateMapScreen(Transform canvas, Sprite circleSprite, Sprite lockSprite, Sprite starSprite)
        {
            // Opaque background: covers the board and blocks touches while the map is open.
            var background = UiBuilder.CreateImage("Map Screen", canvas, null, Cream);
            background.raycastTarget = true;
            UiBuilder.Stretch(background.rectTransform);
            var root = background.rectTransform;

            var title = UiBuilder.CreateText("Title", root, "Çiçek Bahçesi", 76, Bordo, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 100f));

            var totalStar = UiBuilder.CreateImage("Total Star", root, starSprite, StarRed);
            UiBuilder.Place(totalStar.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 0.5f), new Vector2(-10f, -205f), new Vector2(56f, 56f));
            var starsText = UiBuilder.CreateText("Total Stars", root, "0 / 0", 44, Bordo, TextAnchor.MiddleLeft);
            UiBuilder.Place(starsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(10f, -205f), new Vector2(300f, 60f));

            // Scroll area under the title.
            var scrollRoot = UiBuilder.CreateRect("Scroll", root);
            UiBuilder.Stretch(scrollRoot, top: 260f);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            var viewport = UiBuilder.CreateRect("Viewport", scrollRoot);
            UiBuilder.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            // An invisible graphic so drags anywhere in the viewport scroll the map.
            var hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            var content = UiBuilder.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 2000f);

            scroll.viewport = viewport;
            scroll.content = content;

            var node = CreateLevelNode(content, circleSprite, lockSprite, starSprite);

            var map = background.gameObject.AddComponent<LevelMapScreen>();
            Wire(map, "scrollRect", scroll);
            Wire(map, "content", content);
            Wire(map, "nodeTemplate", node);
            Wire(map, "starsText", starsText);
            Wire(map, "dotSprite", circleSprite);
            background.gameObject.SetActive(false);
            return map;
        }

        private static LevelNodeView CreateLevelNode(Transform content, Sprite circleSprite, Sprite lockSprite, Sprite starSprite)
        {
            var root = UiBuilder.CreateRect("Level Node Template", content);
            UiBuilder.Place(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 180f));

            var circle = UiBuilder.CreateImage("Circle", root, circleSprite, Color.white);
            circle.raycastTarget = true;
            UiBuilder.Stretch(circle.rectTransform);
            var button = circle.gameObject.AddComponent<Button>();
            button.targetGraphic = circle;
            var colors = button.colors;
            colors.disabledColor = Color.white; // locked look comes from the node's own color
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;

            var number = UiBuilder.CreateText("Number", circle.transform, "1", 76, Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Stretch(number.rectTransform);

            var lockIcon = UiBuilder.CreateImage("Lock", circle.transform, lockSprite, LockGray);
            UiBuilder.Place(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));

            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = UiBuilder.CreateImage($"Star {i + 1}", root, starSprite, Color.white);
                float y = i == 1 ? -18f : -6f; // little arc
                UiBuilder.Place(stars[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 56f, y), new Vector2(52f, 52f));
            }

            var view = root.gameObject.AddComponent<LevelNodeView>();
            Wire(view, "button", button);
            Wire(view, "background", circle);
            Wire(view, "numberText", number);
            Wire(view, "lockIcon", lockIcon);
            WireArray(view, "stars", stars);
            root.gameObject.SetActive(false);
            return view;
        }

        // ---------- Wiring ----------

        /// <summary>Sets a [SerializeField] private field and fails loudly if the field name is wrong or the value is missing.</summary>
        private static void Wire(Object target, string fieldName, Object value)
        {
            if (value == null)
            {
                Debug.LogError($"[Flowers] Setup: nothing to assign to {target.GetType().Name}.{fieldName} (the value is null/destroyed).");
                _wiringFailed = true;
                return;
            }

            var property = FindProperty(target, fieldName, out var so);
            if (property == null) return;
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireArray(Object target, string fieldName, Object[] values)
        {
            var property = FindProperty(target, fieldName, out var so);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindProperty(Object target, string fieldName, out SerializedObject so)
        {
            so = new SerializedObject(target);
            var property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[Flowers] Setup: {target.GetType().Name} has no serialized field '{fieldName}'.");
                _wiringFailed = true;
            }
            return property;
        }
    }
}
