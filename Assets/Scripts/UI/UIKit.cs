using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class UIKit
{
    public static Font Font;
    public static readonly Color Gold = new Color(0.95f, 0.78f, 0.45f);
    public static readonly Color GoldDim = new Color(0.72f, 0.58f, 0.34f);
    public static readonly Color Parch = new Color(0.96f, 0.92f, 0.82f);
    public static readonly Color Muted = new Color(0.7f, 0.67f, 0.6f);
    public static readonly Color Dark = new Color(0.03f, 0.04f, 0.06f, 0.78f);
    public static readonly Color Teal = new Color(0.45f, 0.95f, 0.9f);
    public static readonly Color Red = new Color(1f, 0.42f, 0.32f);
    public static readonly Color Ink = new Color(0.22f, 0.15f, 0.08f);

    public static void Init()
    {
        if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ProcTex.InitSprites();
    }

    public static RectTransform Rect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>Anchor at (ax,ay) with pivot (px,py), offset pos and size.</summary>
    public static RectTransform Place(RectTransform rt, float ax, float ay, float px, float py, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
        rt.pivot = new Vector2(px, py);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    public static RectTransform Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
        return rt;
    }

    public static Image Img(Transform parent, string name, Sprite sp, Color c, bool sliced = false)
    {
        var rt = Rect(parent, name);
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite = sp;
        im.color = c;
        im.raycastTarget = false;
        if (sliced) im.type = Image.Type.Sliced;
        return im;
    }

    public static RawImage Raw(Transform parent, string name, Texture tex, Color c)
    {
        var rt = Rect(parent, name);
        var im = rt.gameObject.AddComponent<RawImage>();
        im.texture = tex;
        im.color = c;
        im.raycastTarget = false;
        return im;
    }

    public static Text Txt(Transform parent, string name, string s, int size, Color c, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, bool shadow = true)
    {
        var rt = Rect(parent, name);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Font;
        t.text = s;
        t.fontSize = size;
        t.color = c;
        t.alignment = align;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.supportRichText = true;
        if (shadow)
        {
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.75f);
            sh.effectDistance = new Vector2(1.5f, -1.5f);
        }
        return t;
    }

    public static string Spaced(string s, int gap = 1)
    {
        var sb = new System.Text.StringBuilder();
        string g = new string(' ', gap);
        for (int i = 0; i < s.Length; i++)
        {
            sb.Append(s[i]);
            if (i < s.Length - 1) sb.Append(s[i] == ' ' ? g + g : g);
        }
        return sb.ToString();
    }

    public static RectTransform Panel(Transform parent, string name, Color fill, Color border)
    {
        var bg = Img(parent, name, ProcTex.PanelFill, fill, true);
        var b = Img(bg.transform, "Border", ProcTex.PanelBorder, border, true);
        Stretch(b.rectTransform);
        return bg.rectTransform;
    }

    public static Button Btn(Transform parent, string label, int size, UnityAction onClick, Color? fill = null)
    {
        var rt = Panel(parent, "Btn_" + label, fill ?? new Color(0.06f, 0.07f, 0.09f, 0.85f), new Color(Gold.r, Gold.g, Gold.b, 0.55f));
        var img = rt.GetComponent<Image>();
        img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.6f, 1.45f, 1.1f, 1f);
        cb.pressedColor = new Color(0.8f, 0.75f, 0.6f, 1f);
        cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        cb.colorMultiplier = 1.5f;
        cb.fadeDuration = 0.12f;
        b.colors = cb;
        var t = Txt(rt, "Label", label, size, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(t.rectTransform);
        b.onClick.AddListener(() => { if (AudioMan.I != null) AudioMan.I.Play("click", 0.6f); });
        if (onClick != null) b.onClick.AddListener(onClick);
        return b;
    }

    public static void SetLabel(Button b, string s)
    {
        var t = b.GetComponentInChildren<Text>();
        if (t != null) t.text = s;
    }
}
