using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AetherArk.Runtime
{
    public sealed class UiFactory
    {
        public static readonly Color Ink = new Color(0.025f, 0.04f, 0.075f, 0.96f);
        public static readonly Color Panel = new Color(0.045f, 0.08f, 0.13f, 0.94f);
        public static readonly Color PanelSoft = new Color(0.07f, 0.12f, 0.18f, 0.88f);
        public static readonly Color Brass = new Color(0.92f, 0.68f, 0.27f, 1f);
        public static readonly Color Aether = new Color(0.3f, 0.88f, 0.86f, 1f);
        public static readonly Color Violet = new Color(0.57f, 0.32f, 0.86f, 1f);
        public static readonly Color Danger = new Color(0.9f, 0.27f, 0.28f, 1f);
        public static readonly Color Success = new Color(0.37f, 0.82f, 0.48f, 1f);
        public static readonly Color TextPrimary = new Color(0.94f, 0.95f, 0.92f, 1f);
        public static readonly Color TextMuted = new Color(0.65f, 0.72f, 0.75f, 1f);

        public Canvas Canvas { get; }
        public RectTransform Root { get; }
        public Font Font { get; }
        private readonly CanvasScaler scaler;

        public UiFactory(float uiScale)
        {
            EnsureEventSystem();
            Font = CreateFont();
            var canvasObject = new GameObject("AetherArkCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(canvasObject);
            Canvas = canvasObject.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 50;
            scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Root = canvasObject.GetComponent<RectTransform>();
            SetScale(uiScale);
        }

        public void SetScale(float uiScale)
        {
            var safe = Mathf.Clamp(uiScale, 0.8f, 1.25f);
            scaler.referenceResolution = new Vector2(1920f / safe, 1080f / safe);
        }

        public void Clear()
        {
            for (var i = Root.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(Root.GetChild(i).gameObject);
        }

        public RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public RectTransform Stretch(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public Image Image(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public Image Icon(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color? tint = null)
        {
            var rect = Rect(name, parent, position, size);
            var image = Image(rect, tint ?? Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public RectTransform PanelRect(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(name, parent, position, size);
            Image(rect, color);
            return rect;
        }

        public Text Text(string name, Transform parent, string value, int fontSize, Color color, TextAnchor alignment,
            Vector2 position, Vector2 size, FontStyle style = FontStyle.Normal)
        {
            var rect = Rect(name, parent, position, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.text = value;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public Button Button(string name, Transform parent, string label, Action action, Vector2 position, Vector2 size,
            Color? background = null, Color? foreground = null, int fontSize = 18)
        {
            var rect = PanelRect(name, parent, position, size, background ?? PanelSoft);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.65f);
            // The combat view is rebuilt every refresh tick; a non-zero fade makes every rebuilt
            // disabled button flash from its normal colour to its disabled colour.
            colors.fadeDuration = 0f;
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            var text = Text(name + "Label", rect, label, fontSize, foreground ?? TextPrimary, TextAnchor.MiddleCenter,
                Vector2.zero, size, FontStyle.Bold);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;
            return button;
        }

        public RectTransform Bar(string name, Transform parent, float value, Vector2 position, Vector2 size, Color fill,
            Color? background = null)
        {
            var back = PanelRect(name, parent, position, size, background ?? new Color(0.015f, 0.025f, 0.045f, 0.94f));
            back.GetComponent<Image>().raycastTarget = false;
            var clamped = Mathf.Clamp(value, 0f, 1f);
            var inset = 2f;
            var fillRect = Rect(name + "Fill", back, new Vector2(inset, inset),
                new Vector2((float)Math.Max(0f, (size.x - inset * 2f) * clamped), (float)Math.Max(0f, size.y - inset * 2f)));
            Image(fillRect, fill).raycastTarget = false;
            return back;
        }

        public InputField Input(string name, Transform parent, string value, string placeholder, Vector2 position, Vector2 size)
        {
            var rect = PanelRect(name, parent, position, size, new Color(0.03f, 0.05f, 0.08f, 0.95f));
            var field = rect.gameObject.AddComponent<InputField>();
            var text = Text(name + "Text", rect, value, 22, TextPrimary, TextAnchor.MiddleLeft,
                new Vector2(16f, 0f), new Vector2(size.x - 32f, size.y));
            var placeholderText = Text(name + "Placeholder", rect, placeholder, 22, TextMuted, TextAnchor.MiddleLeft,
                new Vector2(16f, 0f), new Vector2(size.x - 32f, size.y));
            field.textComponent = text;
            field.placeholder = placeholderText;
            field.text = value;
            field.lineType = InputField.LineType.SingleLine;
            field.characterLimit = 20;
            return field;
        }

        public void Background(Texture texture, Color overlay)
        {
            var backgroundRect = Stretch("SkyBackground", Root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var raw = backgroundRect.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.color = Color.white;
            raw.raycastTarget = false;
            var overlayRect = Stretch("BackgroundOverlay", Root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image(overlayRect, overlay).raycastTarget = false;
        }

        public void Line(Transform parent, Vector2 start, Vector2 end, float width, Color color)
        {
            var direction = end - start;
            var length = direction.magnitude;
            var rect = Rect("RouteLine", parent, start, new Vector2(length, width));
            rect.pivot = new Vector2(0f, 0.5f);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            Image(rect, color).raycastTarget = false;
            rect.SetAsFirstSibling();
        }

        private static Sprite circleSprite;

        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite != null) return circleSprite;
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color[size * size];
                var radius = size / 2f - 1f;
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt((x + 0.5f - size / 2f) * (x + 0.5f - size / 2f) + (y + 0.5f - size / 2f) * (y + 0.5f - size / 2f));
                    var alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply();
                circleSprite = Sprite.Create(texture, new UnityEngine.Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
                return circleSprite;
            }
        }

        public Image Circle(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(name, parent, position, size);
            var image = Image(rect, color);
            image.sprite = CircleSprite;
            return image;
        }

        public Button CircleButton(string name, Transform parent, Vector2 position, Vector2 size, Color color, Action action)
        {
            var image = Circle(name, parent, position, size, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            colors.fadeDuration = 0f;
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            return button;
        }

        public RectTransform Rotated(string name, Transform parent, Vector2 center, Vector2 size, float degrees, Color color)
        {
            var rect = Rect(name, parent, center, size);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localEulerAngles = new Vector3(0f, 0f, degrees);
            Image(rect, color).raycastTarget = false;
            return rect;
        }

        public void Outline(string name, Transform parent, Vector2 position, Vector2 size, float thickness, Color color)
        {
            var edges = new[]
            {
                new UnityEngine.Rect(position.x, position.y + size.y - thickness, size.x, thickness),
                new UnityEngine.Rect(position.x, position.y, size.x, thickness),
                new UnityEngine.Rect(position.x, position.y, thickness, size.y),
                new UnityEngine.Rect(position.x + size.x - thickness, position.y, thickness, size.y)
            };
            for (var i = 0; i < edges.Length; i++)
            {
                var rect = PanelRect(name + "_" + i, parent, new Vector2(edges[i].x, edges[i].y), new Vector2(edges[i].width, edges[i].height), color);
                rect.GetComponent<Image>().raycastTarget = false;
            }
        }

        private static Font CreateFont()
        {
            var candidates = new[] { "Apple SD Gothic Neo", "Malgun Gothic", "Arial Unicode MS", "Arial" };
            try { return Font.CreateDynamicFontFromOSFont(candidates, 22); }
            catch { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var gameObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
        }
    }
}
