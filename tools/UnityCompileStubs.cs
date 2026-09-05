// Compile-only surface for validating project C# without the Unity editor.
// This file is never included by Unity because it lives outside Assets/.
using System;

namespace UnityEngine
{
    public class Object
    {
        public static T FindFirstObjectByType<T>() where T : Object, new() => default(T);
        public static void DontDestroyOnLoad(Object target) { }
        public static void Destroy(Object target) { }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; set; }
        public Transform transform { get; set; }
        public T GetComponent<T>() where T : Component, new() => new T();
        public T GetComponentInChildren<T>() where T : Component, new() => new T();
    }

    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }

    public class GameObject : Object
    {
        public GameObject(string name, params Type[] components) { }
        public T AddComponent<T>() where T : Component, new() => new T();
        public T GetComponent<T>() where T : Component, new() => new T();
    }

    public class Transform : Component
    {
        public int childCount { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public Transform GetChild(int index) => new Transform();
        public void SetAsFirstSibling() { }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
    }

    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float xValue, float yValue) { x = xValue; y = yValue; }
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public static Vector2 zero => new Vector2(0f, 0f);
        public static Vector2 one => new Vector2(1f, 1f);
        public static Vector2 operator +(Vector2 left, Vector2 right) => new Vector2(left.x + right.x, left.y + right.y);
        public static Vector2 operator -(Vector2 left, Vector2 right) => new Vector2(left.x - right.x, left.y - right.y);
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float xValue, float yValue, float zValue) { x = xValue; y = yValue; z = zValue; }
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;
        public Color(float red, float green, float blue, float alpha = 1f) { r = red; g = green; b = blue; a = alpha; }
        public static Color white => new Color(1f, 1f, 1f, 1f);
    }

    public class Texture : Object { }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point, Bilinear }
    public enum TextureWrapMode { Repeat, Clamp }

    public class Texture2D : Texture
    {
        public Texture2D() { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public void SetPixels(Color[] pixels) { }
        public void Apply() { }
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
    }

    public class Sprite : Object
    {
        public Rect rect => new Rect(0, 0, 1536, 1024);
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) => new Sprite();
    }
    public class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string[] names, int size) => new Font();
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object, new() => new T();
        public static T GetBuiltinResource<T>(string path) where T : Object, new() => new T();
    }

    public static class Application
    {
        public static string persistentDataPath => "/tmp";
        public static void Quit() { }
    }

    public static class Debug
    {
        public static void LogWarning(object message) { }
        public static bool isDebugBuild => true;
    }

    public static class Time
    {
        public static float unscaledDeltaTime => 1f / 60f;
    }

    public static class Input
    {
        public static bool GetKeyDown(KeyCode key) => false;
    }

    public static class Mathf
    {
        public const float Rad2Deg = 57.2957795f;
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static int RoundToInt(float value) => (int)Math.Round(value);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sqrt(float value) => (float)Math.Sqrt(value);
        public static float Clamp01(float value) => Clamp(value, 0f, 1f);
        public static float Floor(float value) => (float)Math.Floor(value);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
    }

    public static class JsonUtility
    {
        public static string ToJson(object value, bool prettyPrint = false) => "{}";
        public static T FromJson<T>(string json) => default(T);
    }

    public enum KeyCode
    {
        Space,
        P,
        N,
        L,
        C,
        Return,
        KeypadEnter,
        Escape,
        F,
        S,
        R,
        M,
        Alpha1 = 49,
        Alpha2,
        Alpha3,
        Alpha4,
        Alpha5,
        Alpha6,
        Alpha7,
        Alpha8,
        Alpha9,
        Z
    }
    public enum RenderMode { ScreenSpaceOverlay }
    public enum FontStyle { Normal, Bold }
    public enum TextAnchor { MiddleCenter, MiddleLeft, MiddleRight, UpperLeft, LowerLeft, UpperCenter, UpperRight, LowerCenter, LowerRight }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }

    public enum RuntimeInitializeLoadType { AfterSceneLoad }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
    }

    public class CanvasRenderer : Component { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequireComponent : Attribute
    {
        public RequireComponent(Type component) { }
    }

    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize }
        public enum ScreenMatchMode { MatchWidthOrHeight }
        public ScaleMode uiScaleMode { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
        public Vector2 referenceResolution { get; set; }
    }

    public class GraphicRaycaster : Behaviour { }
}

namespace UnityEngine.Events
{
    public class UnityEvent
    {
        public void AddListener(Action action) { }
    }

    public class UnityEvent<T>
    {
        public void AddListener(Action<T> action) { }
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : UnityEngine.MonoBehaviour { }
    public class StandaloneInputModule : UnityEngine.MonoBehaviour { }
}

namespace UnityEngine.UI
{
    using UnityEngine.Events;

    public class Graphic : UnityEngine.Behaviour
    {
        public UnityEngine.Color color { get; set; }
        public bool raycastTarget { get; set; }
        public UnityEngine.RectTransform rectTransform => new UnityEngine.RectTransform();
    }

    public class Image : Graphic
    {
        public UnityEngine.Sprite sprite { get; set; }
        public bool preserveAspect { get; set; }
    }

    public class MaskableGraphic : Graphic
    {
        protected virtual void OnPopulateMesh(VertexHelper mesh) { }
        protected UnityEngine.Rect GetPixelAdjustedRect() => new UnityEngine.Rect();
    }
    public class RectMask2D : UnityEngine.Behaviour { }

    public class VertexHelper
    {
        public int currentVertCount => 0;
        public void Clear() { }
        public void AddVert(UnityEngine.Vector3 position, UnityEngine.Color color, UnityEngine.Vector2 uv) { }
        public void AddTriangle(int a, int b, int c) { }
    }

    public class RawImage : Graphic
    {
        public UnityEngine.Texture texture { get; set; }
    }

    public class Text : Graphic
    {
        public UnityEngine.Font font { get; set; }
        public int fontSize { get; set; }
        public UnityEngine.FontStyle fontStyle { get; set; }
        public string text { get; set; }
        public UnityEngine.TextAnchor alignment { get; set; }
        public UnityEngine.HorizontalWrapMode horizontalOverflow { get; set; }
        public UnityEngine.VerticalWrapMode verticalOverflow { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public int resizeTextMinSize { get; set; }
        public int resizeTextMaxSize { get; set; }
    }

    public struct ColorBlock
    {
        public UnityEngine.Color normalColor;
        public UnityEngine.Color highlightedColor;
        public UnityEngine.Color pressedColor;
        public UnityEngine.Color disabledColor;
        public float fadeDuration;
    }

    public class Selectable : UnityEngine.Behaviour
    {
        public bool interactable { get; set; } = true;
        public Graphic targetGraphic { get; set; }
        public ColorBlock colors { get; set; }
    }

    public class Button : Selectable
    {
        public UnityEvent onClick { get; } = new UnityEvent();
    }

    public class InputField : Selectable
    {
        public enum LineType { SingleLine }
        public Text textComponent { get; set; }
        public Graphic placeholder { get; set; }
        public string text { get; set; }
        public LineType lineType { get; set; }
        public int characterLimit { get; set; }
        public UnityEvent<string> onValueChanged { get; } = new UnityEvent<string>();
    }
}
