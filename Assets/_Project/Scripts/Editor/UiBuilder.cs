using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Flowers.Editor
{
    /// <summary>Small helpers so the Setup Scene menu can build uGUI objects in a few readable lines.</summary>
    public static class UiBuilder
    {
        /// <summary>M PLUS Rounded 1c ExtraBold (Google Fonts, OFL). Chunky and rounded, with all Turkish letters.</summary>
        public const string FontPath = "Assets/_Project/Fonts/MPLUSRounded1c-ExtraBold.ttf";

        private static Font _font;
        private static bool _usingGameFont;

        public static Font DefaultFont
        {
            get
            {
                if (_font != null) return _font;
                _font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                _usingGameFont = _font != null;
                if (!_usingGameFont)
                {
                    Debug.LogWarning($"[Flowers] Font not found at {FontPath}, using Unity's built-in font.");
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Anchors and pivot at one point; positioned by offset from that point.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Fills the parent, minus the given padding on each side.</summary>
        public static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool sliced = false)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced && sprite != null;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(string name, Transform parent, string text, int fontSize, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = DefaultFont;
            label.text = text;
            label.fontSize = fontSize;
            // The game font is already ExtraBold; Unity's fake bold on top would smear the letters together.
            label.fontStyle = _usingGameFont ? FontStyle.Normal : style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// Outline (and optional drop shadow) around text so it reads on busy, colorful backgrounds.
        /// The usual "game title" look: cream letters with a dark bordo edge.
        /// </summary>
        public static Text Stroke(Text text, Color outline, float width, bool dropShadow = true)
        {
            // An Outline copies the text to 4 diagonal offsets only (±x, ±y). With a thick width that leaves
            // notches straight left/right/up/down, which reads as a doubled edge. So: one diagonal outline
            // plus one horizontal and one vertical = copies in 8 directions, an even edge all around.
            float diagonal = width * 0.71f;
            AddOutline(text, outline, new Vector2(diagonal, diagonal));
            AddOutline(text, outline, new Vector2(width, 0f));
            AddOutline(text, outline, new Vector2(0f, width));
            if (dropShadow)
            {
                var s = text.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(outline.r, outline.g, outline.b, 0.3f);
                s.effectDistance = new Vector2(0f, -width * 0.9f);
            }
            return text;
        }

        private static void AddOutline(Text text, Color color, Vector2 distance)
        {
            var o = text.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = distance;
            o.useGraphicAlpha = true;
        }

        public static Button CreateButton(string name, Transform parent, Sprite background, Color backgroundColor,
            string label, Color labelColor, int fontSize)
        {
            var image = CreateImage(name, parent, background, backgroundColor, sliced: true);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;

            var text = CreateText("Label", image.transform, label, fontSize, labelColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            // Kenney buttons have a darker "depth" band at the bottom; center the label on the face above it.
            Stretch(text.rectTransform, bottom: 14f);
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -3f);
            return button;
        }
    }
}
