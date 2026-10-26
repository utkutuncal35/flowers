using Flowers.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static Flowers.Editor.Wiring;

namespace Flowers.Editor
{
    /// <summary>
    /// Builds every UI screen for Setup Scene (reference resolution 1080x1920, portrait).
    /// Look: garden backgrounds, cream Kenney panels with bordo accents, ribbon titles,
    /// chunky rounded font with a bordo outline on colorful backgrounds.
    /// </summary>
    public static class UiScreensBuilder
    {
        // ---------- Palette ----------
        public static readonly Color Bordo = new Color32(0x6A, 0x14, 0x30, 0xFF);
        public static readonly Color BordoDark = new Color32(0x4A, 0x0D, 0x22, 0xFF);
        public static readonly Color BordoButton = new Color32(0x8E, 0x2C, 0x48, 0xFF);
        public static readonly Color Cream = new Color32(0xFF, 0xF7, 0xEE, 0xFF);
        public static readonly Color SoftPink = new Color32(0xFB, 0xE3, 0xE8, 0xFF);
        public static readonly Color Pink = new Color32(0xF7, 0xA9, 0xC3, 0xFF);
        public static readonly Color Leaf = new Color32(0x5E, 0x8C, 0x4A, 0xFF);
        public static readonly Color Meadow = new Color32(0xA8, 0xD0, 0x8C, 0xFF);
        public static readonly Color Dim = new Color(0.2f, 0.03f, 0.08f, 0.62f);
        public static readonly Color StarRed = new Color32(0xFF, 0x5A, 0x75, 0xFF);
        public static readonly Color LockGray = new Color32(0xB9, 0xA3, 0xA0, 0xFF);
        public static readonly Color RaysColor = new Color(1f, 0.93f, 0.86f, 0.55f);

        /// <summary>All sprites the UI needs, loaded by Setup Scene after the scene is created.</summary>
        public struct Sprites
        {
            public Sprite Panel, Button, Round, Star, Check, Circle, Lock, Ribbon, Rays, Marker, Garden, Meadow, SkyCap, Heart;
            public Sprite[] Flowers;
        }

        public struct Result
        {
            public LevelHud Hud;
            public PraiseText Praise;
            public LevelEndScreen Win;
            public LevelEndScreen Lose;
            public LevelMapScreen Map;
            public LevelStartPopup LevelStart;
            public TitleScreen Title;
            public FinalSurpriseScreen Final;
        }

        public static Result Build(Sprites s)
        {
            var canvas = CreateCanvas();
            canvas.gameObject.AddComponent<ButtonClickSounds>();
            CreateEventSystem();

            // Order matters: later children draw on top.
            var result = new Result
            {
                Hud = CreateHud(canvas, s),
                Praise = CreatePraiseText(canvas),
                Win = CreateEndScreen(canvas, "Win Screen", s, won: true),
                Lose = CreateEndScreen(canvas, "Lose Screen", s, won: false),
                Map = CreateMapScreen(canvas, s),
            };
            result.LevelStart = CreateLevelStartPopup(canvas, s);
            result.Title = CreateTitleScreen(canvas, s);
            result.Final = CreateFinalSurprise(canvas, s);
            return result;
        }

        // ---------- Canvas ----------

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

        // ---------- HUD ----------

        private static LevelHud CreateHud(Transform canvas, Sprites s)
        {
            var root = UiBuilder.CreateRect("HUD", canvas);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.offsetMin = new Vector2(0f, -290f);
            root.offsetMax = Vector2.zero;

            // Round map button with the heart marker as its icon.
            var mapButton = RoundButton("Map Button", root, s.Round, Cream, s.Marker, 120f);
            UiBuilder.Place((RectTransform)mapButton.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -26f), new Vector2(120f, 120f));

            // Moves: big bordo badge.
            var badge = UiBuilder.CreateImage("Moves Badge", root, s.Round, BordoButton);
            UiBuilder.Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(285f, -150f), new Vector2(230f, 230f));
            var movesLabel = UiBuilder.CreateText("Label", badge.transform, "Hamle", 34, Cream);
            UiBuilder.Place(movesLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 58f), new Vector2(200f, 50f));
            var movesText = UiBuilder.Stroke(UiBuilder.CreateText("Moves", badge.transform, "20", 100, Cream), BordoDark, 4f);
            UiBuilder.Place(movesText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(220f, 120f));

            // Goals: cream panel to the right.
            var panel = UiBuilder.CreateImage("Goals Panel", root, s.Panel, Cream, sliced: true);
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.offsetMin = new Vector2(420f, -270f);
            panelRect.offsetMax = new Vector2(-26f, -34f);

            var levelText = UiBuilder.CreateText("Level", panelRect, "Bölüm 1", 34, BordoButton, TextAnchor.MiddleCenter);
            levelText.rectTransform.anchorMin = new Vector2(0f, 1f);
            levelText.rectTransform.anchorMax = new Vector2(1f, 1f);
            levelText.rectTransform.pivot = new Vector2(0.5f, 1f);
            levelText.rectTransform.offsetMin = new Vector2(0f, -58f);
            levelText.rectTransform.offsetMax = new Vector2(0f, -10f);

            var goals = HorizontalRow("Goals", panelRect, 30f);
            goals.anchorMin = new Vector2(0f, 0f);
            goals.anchorMax = new Vector2(1f, 1f);
            goals.offsetMin = new Vector2(16f, 26f);
            goals.offsetMax = new Vector2(-16f, -58f);
            var template = CreateGoalItem(goals, s.Check, 120f, 96f, 42);

            var hud = root.gameObject.AddComponent<LevelHud>();
            Wire(hud, "levelText", levelText);
            Wire(hud, "movesText", movesText);
            Wire(hud, "goalsContainer", goals);
            Wire(hud, "goalTemplate", template);
            Wire(hud, "mapButton", mapButton);
            return hud;
        }

        /// <summary>Goal icon with the number under it; a green check replaces the number when the goal is done.</summary>
        private static GoalItemView CreateGoalItem(Transform parent, Sprite checkSprite, float width, float iconSize, int fontSize)
        {
            var root = UiBuilder.CreateRect("Goal Template", parent);
            root.sizeDelta = new Vector2(width, iconSize + fontSize + 4f);

            var icon = UiBuilder.CreateImage("Icon", root, null, Color.white);
            UiBuilder.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(iconSize, iconSize));

            var count = UiBuilder.CreateText("Count", root, "10", fontSize, Bordo);
            UiBuilder.Place(count.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -4f), new Vector2(width + 40f, fontSize + 10f));

            var check = UiBuilder.CreateImage("Check", root, checkSprite, Leaf);
            UiBuilder.Place(check.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(fontSize, fontSize));

            var item = root.gameObject.AddComponent<GoalItemView>();
            Wire(item, "icon", icon);
            Wire(item, "countText", count);
            Wire(item, "checkMark", check);
            root.gameObject.SetActive(false);
            return item;
        }

        private static PraiseText CreatePraiseText(Transform canvas)
        {
            var rect = UiBuilder.CreateRect("Praise", canvas);
            UiBuilder.Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1000f, 220f));
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var label = UiBuilder.Stroke(UiBuilder.CreateText("Label", rect, "Harika!", 130, Cream), BordoButton, 7f);
            UiBuilder.Stretch(label.rectTransform);

            var praise = rect.gameObject.AddComponent<PraiseText>();
            Wire(praise, "label", label);
            Wire(praise, "canvasGroup", group);
            return praise;
        }

        // ---------- Popups ----------

        /// <summary>Dim layer + a "Window" that springs in. Returns the window; the dim object is the screen root.</summary>
        private static RectTransform PopupRoot(Transform canvas, string name, out Image dim)
        {
            dim = UiBuilder.CreateImage(name, canvas, null, Dim);
            dim.raycastTarget = true; // blocks touches from reaching anything behind
            UiBuilder.Stretch(dim.rectTransform);

            var window = UiBuilder.CreateRect("Window", dim.transform);
            UiBuilder.Place(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 900f));
            window.gameObject.AddComponent<PopIn>();
            return window;
        }

        private static Text Ribbon(Transform parent, Sprite ribbon, string title, float y, float width)
        {
            var image = UiBuilder.CreateImage("Ribbon", parent, ribbon, Color.white, sliced: true);
            UiBuilder.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(width, 210f));
            var text = UiBuilder.Stroke(UiBuilder.CreateText("Title", image.transform, title, 74, Cream), BordoDark, 4f);
            UiBuilder.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(width - 200f, 110f));
            return text;
        }

        private static LevelEndScreen CreateEndScreen(Transform canvas, string name, Sprites s, bool won)
        {
            var window = PopupRoot(canvas, name, out var dim);

            if (won)
            {
                // Slowly turning light rays behind the stars.
                var rays = UiBuilder.CreateImage("Rays", dim.transform, s.Rays, RaysColor);
                UiBuilder.Place(rays.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1400f, 1400f));
                rays.transform.SetSiblingIndex(0);
                rays.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Rotate, 12f, 1f);
            }

            float cardHeight = won ? 740f : 660f;
            var card = UiBuilder.CreateImage("Card", window, s.Panel, Cream, sliced: true);
            UiBuilder.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(880f, cardHeight));
            float top = -60f + cardHeight * 0.5f;

            var title = Ribbon(window, s.Ribbon, won ? "Tebrikler!" : "Hamleler bitti", top, 800f);

            var stars = new Image[won ? 3 : 0];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = UiBuilder.CreateImage($"Star {i + 1}", window, s.Star, Color.white);
                bool middle = i == 1;
                float size = middle ? 230f : 180f;
                float y = top - (middle ? 230f : 255f);
                UiBuilder.Place(stars[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 245f, y), new Vector2(size, size));
                stars[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1 - i) * 12f);
            }

            if (!won)
            {
                var heart = UiBuilder.CreateImage("Heart", window, s.Marker, Color.white);
                UiBuilder.Place(heart.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, top - 195f), new Vector2(150f, 150f));
                heart.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Pulse, 0.06f, 3f);
            }

            float messageY = won ? top - 440f : top - 335f;
            var message = UiBuilder.CreateText("Message", window, "", 50, Bordo);
            UiBuilder.Place(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, messageY), new Vector2(800f, 140f));

            // Harita | Tekrar | Sonraki. "Sonraki" may be hidden (no unlocked next level).
            float buttonsY = -60f - cardHeight * 0.5f + 110f;
            var size3 = new Vector2(250f, 150f);
            var map = UiBuilder.CreateButton("Map Button", window, s.Button, SoftPink, "Harita", Bordo, 44);
            UiBuilder.Place((RectTransform)map.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-275f, buttonsY), size3);
            var retry = UiBuilder.CreateButton("Retry Button", window, s.Button, Pink, "Tekrar", Bordo, 44);
            UiBuilder.Place((RectTransform)retry.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, buttonsY), size3);
            var next = UiBuilder.CreateButton("Next Button", window, s.Button, BordoButton, "Sonraki", Cream, 44);
            UiBuilder.Place((RectTransform)next.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(275f, buttonsY), size3);

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

        private static LevelStartPopup CreateLevelStartPopup(Transform canvas, Sprites s)
        {
            var window = PopupRoot(canvas, "Level Start Popup", out var dim);

            const float cardHeight = 780f;
            var card = UiBuilder.CreateImage("Card", window, s.Panel, Cream, sliced: true);
            UiBuilder.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(860f, cardHeight));
            float top = -40f + cardHeight * 0.5f;

            var title = Ribbon(window, s.Ribbon, "Bölüm 1", top, 760f);

            var goalsLabel = UiBuilder.CreateText("Goals Label", window, "Hedefler", 46, BordoButton);
            UiBuilder.Place(goalsLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, top - 170f), new Vector2(600f, 60f));

            var goalsBack = UiBuilder.CreateImage("Goals Back", window, s.Panel, SoftPink, sliced: true);
            UiBuilder.Place(goalsBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, top - 330f), new Vector2(700f, 230f));
            var goals = HorizontalRow("Goals", goalsBack.transform, 24f);
            UiBuilder.Stretch(goals, 20f, 20f, 20f, 30f);
            var template = CreateGoalItem(goals, s.Check, 140f, 120f, 50);

            var moves = UiBuilder.CreateText("Moves", window, "15 hamle", 46, Bordo);
            UiBuilder.Place(moves.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, top - 500f), new Vector2(600f, 60f));

            var play = UiBuilder.CreateButton("Play Button", window, s.Button, BordoButton, "Başla", Cream, 76);
            UiBuilder.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f - cardHeight * 0.5f + 130f), new Vector2(520f, 170f));
            play.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Pulse, 0.035f, 3f);

            var close = UiBuilder.CreateButton("Close Button", window, s.Round, Pink, "X", Bordo, 56);
            UiBuilder.Place((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(390f, top - 40f), new Vector2(110f, 110f));
            close.GetComponent<Image>().type = Image.Type.Simple;

            var popup = dim.gameObject.AddComponent<LevelStartPopup>();
            Wire(popup, "titleText", title);
            Wire(popup, "movesText", moves);
            Wire(popup, "goalsContainer", goals);
            Wire(popup, "goalTemplate", template);
            Wire(popup, "playButton", play);
            Wire(popup, "closeButton", close);
            dim.gameObject.SetActive(false);
            return popup;
        }

        // ---------- Map ----------

        private static LevelMapScreen CreateMapScreen(Transform canvas, Sprites s)
        {
            // Meadow-colored base, in case the map content is shorter than the screen.
            var background = UiBuilder.CreateImage("Map Screen", canvas, null, Meadow);
            background.raycastTarget = true;
            UiBuilder.Stretch(background.rectTransform);
            var root = background.rectTransform;

            var scrollRoot = UiBuilder.CreateRect("Scroll", root);
            UiBuilder.Stretch(scrollRoot);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            var viewport = UiBuilder.CreateRect("Viewport", scrollRoot);
            UiBuilder.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            // An invisible graphic so drags anywhere in the viewport scroll the map.
            viewport.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

            var content = UiBuilder.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 2000f);
            scroll.viewport = viewport;
            scroll.content = content;

            var node = CreateLevelNode(content, s);

            var marker = UiBuilder.CreateImage("Current Level Marker", content, s.Marker, Color.white);
            UiBuilder.Place(marker.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(120f, 120f));
            marker.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Bob, 26f, 4f);

            // Top bar: title on the left, total stars on the right.
            var bar = UiBuilder.CreateImage("Top Bar", root, s.Panel, Cream, sliced: true);
            var barRect = bar.rectTransform;
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.offsetMin = new Vector2(24f, -190f);
            barRect.offsetMax = new Vector2(-24f, -24f);
            var title = UiBuilder.CreateText("Title", barRect, "Çiçek Bahçesi", 62, BordoButton, TextAnchor.MiddleLeft);
            UiBuilder.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 8f), new Vector2(620f, 90f));

            var pill = UiBuilder.CreateImage("Stars Pill", barRect, s.Button, SoftPink, sliced: true);
            UiBuilder.Place(pill.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 4f), new Vector2(330f, 110f));
            var pillStar = UiBuilder.CreateImage("Star", pill.transform, s.Star, StarRed);
            UiBuilder.Place(pillStar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 6f), new Vector2(80f, 80f));
            pillStar.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Twinkle, 0.15f, 2f);
            var starsText = UiBuilder.CreateText("Total Stars", pill.transform, "0 / 0", 46, Bordo, TextAnchor.MiddleCenter);
            UiBuilder.Place(starsText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 6f), new Vector2(210f, 70f));

            // Big Play button at the bottom.
            var play = UiBuilder.CreateButton("Play Button", root, s.Button, BordoButton, "Oyna", Cream, 84);
            UiBuilder.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(560f, 180f));
            play.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Pulse, 0.035f, 3f);

            var map = background.gameObject.AddComponent<LevelMapScreen>();
            Wire(map, "scrollRect", scroll);
            Wire(map, "content", content);
            Wire(map, "nodeTemplate", node);
            Wire(map, "starsText", starsText);
            Wire(map, "marker", marker.rectTransform);
            Wire(map, "playButton", play);
            Wire(map, "meadowSprite", s.Meadow);
            Wire(map, "skyCapSprite", s.SkyCap);
            Wire(map, "pathSprite", s.Circle);
            background.gameObject.SetActive(false);
            return map;
        }

        private static LevelNodeView CreateLevelNode(Transform content, Sprites s)
        {
            var root = UiBuilder.CreateRect("Level Node Template", content);
            UiBuilder.Place(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f));

            var circle = UiBuilder.CreateImage("Circle", root, s.Round, Color.white);
            circle.raycastTarget = true;
            UiBuilder.Stretch(circle.rectTransform);
            var button = circle.gameObject.AddComponent<Button>();
            button.targetGraphic = circle;
            var colors = button.colors;
            colors.disabledColor = Color.white; // the locked look comes from the node's own color
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;

            var number = UiBuilder.Stroke(UiBuilder.CreateText("Number", circle.transform, "1", 84, Cream), BordoDark, 4f);
            UiBuilder.Stretch(number.rectTransform, bottom: 16f);

            var lockIcon = UiBuilder.CreateImage("Lock", circle.transform, s.Lock, LockGray);
            UiBuilder.Place(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(92f, 92f));

            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = UiBuilder.CreateImage($"Star {i + 1}", root, s.Star, Color.white);
                float y = i == 1 ? -16f : -2f; // small smile-shaped arc under the node
                UiBuilder.Place(stars[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 64f, y), new Vector2(70f, 70f));
                stars[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1 - i) * 14f);
                // Gentle sparkle, each star a beat after the previous one.
                stars[i].gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Twinkle, 0.2f, 2.4f, i * 1.3f);
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

        // ---------- Title ----------

        private static TitleScreen CreateTitleScreen(Transform canvas, Sprites s)
        {
            var root = UiBuilder.CreateImage("Title Screen", canvas, null, Cream);
            root.raycastTarget = true;
            UiBuilder.Stretch(root.rectTransform);

            // Garden picture covering the whole screen, whatever its shape.
            var garden = UiBuilder.CreateImage("Garden", root.transform, s.Garden, Color.white);
            garden.preserveAspect = false;
            UiBuilder.Stretch(garden.rectTransform);
            var fitter = garden.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = s.Garden.rect.width / s.Garden.rect.height;

            // Logo: two big words with a thick bordo outline, flowers around it.
            var logo = UiBuilder.CreateRect("Logo", root.transform);
            UiBuilder.Place(logo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(900f, 520f));
            var line1 = UiBuilder.Stroke(UiBuilder.CreateText("Line 1", logo, "Çiçek", 190, Cream), BordoButton, 9f);
            UiBuilder.Place(line1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-30f, 110f), new Vector2(900f, 230f));
            line1.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            var line2 = UiBuilder.Stroke(UiBuilder.CreateText("Line 2", logo, "Bahçesi", 160, Pink), BordoButton, 9f);
            UiBuilder.Place(line2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, -90f), new Vector2(900f, 210f));
            line2.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -3f);
            logo.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Pulse, 0.015f, 1.6f);

            var flowerSpots = new[]
            {
                (type: 0, pos: new Vector2(-370f, 250f), size: 170f, rot: 15f),
                (type: 2, pos: new Vector2(390f, 260f), size: 150f, rot: -20f),
                (type: 1, pos: new Vector2(-400f, -150f), size: 150f, rot: 10f),
                (type: 4, pos: new Vector2(380f, -190f), size: 130f, rot: -12f),
                (type: 3, pos: new Vector2(250f, 380f), size: 110f, rot: 25f),
            };
            foreach (var spot in flowerSpots)
            {
                var flower = UiBuilder.CreateImage($"Flower {spot.type}", logo, s.Flowers[spot.type], Color.white);
                UiBuilder.Place(flower.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), spot.pos, new Vector2(spot.size, spot.size));
                flower.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spot.rot);
                flower.transform.SetAsFirstSibling(); // behind the words
                flower.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Bob, 12f, 1.5f + spot.type * 0.3f);
            }

            var play = UiBuilder.CreateButton("Play Button", root.transform, s.Button, BordoButton, "Oyna", Cream, 96);
            UiBuilder.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(600f, 200f));
            play.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Pulse, 0.045f, 3f);

            var title = root.gameObject.AddComponent<TitleScreen>();
            Wire(title, "playButton", play);
            root.gameObject.SetActive(false);
            return title;
        }

        // ---------- Final surprise ----------

        /// <summary>
        /// After the last level: bordo screen with turning rays, the message, and a big heart with a closed rose in
        /// it. "Gerçek çiçeği gör" swaps the rose for the front camera, cropped by the heart.
        /// </summary>
        private static FinalSurpriseScreen CreateFinalSurprise(Transform canvas, Sprites s)
        {
            var root = UiBuilder.CreateImage("Final Surprise", canvas, null, Bordo);
            root.raycastTarget = true;
            UiBuilder.Stretch(root.rectTransform);

            const float heartY = -60f;
            const float heartSize = 760f;
            var rays = UiBuilder.CreateImage("Rays", root.transform, s.Rays, new Color(1f, 0.85f, 0.88f, 0.35f));
            UiBuilder.Place(rays.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, heartY), new Vector2(1900f, 1900f));
            rays.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Rotate, 10f, 1f);

            var title = Ribbon(root.transform, s.Ribbon, "Tebrikler!", 720f, 820f);
            title.transform.parent.gameObject.AddComponent<PopIn>();

            var message = UiBuilder.Stroke(UiBuilder.CreateText("Message", root.transform, "", 56, Cream), BordoDark, 3f, dropShadow: false);
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiBuilder.Place(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 470f), new Vector2(960f, 190f));

            // Heart: a pink outline, then the heart-shaped window (a Mask) holding the rose and the camera picture.
            var heart = UiBuilder.CreateRect("Heart", root.transform);
            UiBuilder.Place(heart, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, heartY), new Vector2(heartSize, heartSize));
            heart.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Pulse, 0.025f, 2.2f);
            var outline = UiBuilder.CreateImage("Outline", heart, s.Heart, Pink);
            UiBuilder.Stretch(outline.rectTransform);

            var window = UiBuilder.CreateImage("Window", heart, s.Heart, Cream);
            UiBuilder.Stretch(window.rectTransform, 34f, 34f, 34f, 34f);
            window.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            var rose = UiBuilder.CreateImage("Closed Flower", window.transform, s.Flowers[0], Color.white);
            UiBuilder.Place(rose.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(380f, 380f));

            // Square picture, as big as the heart, so a rotated (sideways) camera still covers it.
            var mirror = UiBuilder.CreateRect("Camera Mirror", window.transform);
            UiBuilder.Place(mirror, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(heartSize, heartSize));
            var pictureRect = UiBuilder.CreateRect("Camera", mirror);
            UiBuilder.Stretch(pictureRect);
            var picture = pictureRect.gameObject.AddComponent<RawImage>();
            picture.raycastTarget = false;
            picture.gameObject.SetActive(false);

            // A few flowers around the heart.
            var spots = new[]
            {
                (type: 2, pos: new Vector2(-390f, 230f), size: 150f, rot: -14f),
                (type: 4, pos: new Vector2(395f, 250f), size: 140f, rot: 12f),
                (type: 1, pos: new Vector2(-360f, -330f), size: 130f, rot: 10f),
                (type: 3, pos: new Vector2(360f, -320f), size: 120f, rot: -18f),
            };
            foreach (var spot in spots)
            {
                var flower = UiBuilder.CreateImage($"Flower {spot.type}", root.transform, s.Flowers[spot.type], Color.white);
                UiBuilder.Place(flower.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), spot.pos + new Vector2(0f, heartY), new Vector2(spot.size, spot.size));
                flower.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spot.rot);
                flower.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Bob, 14f, 1.4f + spot.type * 0.25f);
            }

            var reveal = UiBuilder.CreateButton("Reveal Button", root.transform, s.Button, Pink, "Gerçek çiçeği gör", Bordo, 54);
            UiBuilder.Place((RectTransform)reveal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -600f), new Vector2(760f, 180f));
            reveal.gameObject.AddComponent<UiMotion>().EditorSetup(UiMotion.Kind.Pulse, 0.04f, 3f);
            var revealLabel = reveal.GetComponentInChildren<Text>();

            var map = UiBuilder.CreateButton("Map Button", root.transform, s.Button, SoftPink, "Harita", Bordo, 44);
            UiBuilder.Place((RectTransform)map.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -800f), new Vector2(320f, 140f));

            var screen = root.gameObject.AddComponent<FinalSurpriseScreen>();
            Wire(screen, "titleText", title);
            Wire(screen, "messageText", message);
            Wire(screen, "revealButton", reveal);
            Wire(screen, "revealLabel", revealLabel);
            Wire(screen, "mapButton", map);
            Wire(screen, "closedFlower", rose.rectTransform);
            Wire(screen, "cameraMirror", mirror);
            Wire(screen, "cameraImage", picture);
            root.gameObject.SetActive(false);
            return screen;
        }

        // ---------- Helpers ----------

        private static RectTransform HorizontalRow(string name, Transform parent, float spacing)
        {
            var rect = UiBuilder.CreateRect(name, parent);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = spacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rect;
        }

        private static Button RoundButton(string name, Transform parent, Sprite round, Color color, Sprite icon, float size)
        {
            var image = UiBuilder.CreateImage(name, parent, round, color);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var iconImage = UiBuilder.CreateImage("Icon", image.transform, icon, Color.white);
            UiBuilder.Place(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(size * 0.55f, size * 0.55f));
            return button;
        }
    }
}
