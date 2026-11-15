using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>All screens and the in-game HUD, built in code with uGUI.</summary>
public class HUD : MonoBehaviour
{
    public static HUD I;
    RectTransform root, hud;
    CanvasGroup hudGroup;

    // HUD widgets
    RectTransform compass;
    readonly List<(Text t, float ang)> compassTicks = new List<(Text, float)>();
    Image compassObj, compassEnemy, compassShip;
    Text warnText, autoText;
    RectTransform windDial, windArrow, northMark;
    Text pointOfSail, speedText, sailLabel;
    readonly List<Image> sailPips = new List<Image>();
    Image hullFill; Text hullText;
    Image eraFill, eraRing, eraGlow; Text eraLabel, eraSub;
    Text goldText, cargoText;
    RectTransform objPanel; Text objTitle, objText, objDist;
    public Text NextText;
    RectTransform marker; Text markerText; Image markerIcon;
    RectTransform enemyBar; Image enemyFill; Text enemyName;
    RectTransform reticle; Image reticleFill, reticleRing;
    RectTransform prompt; Text promptText; CanvasGroup promptGroup;
    Text hintText; float hintTimer;
    RectTransform dialogue; CanvasGroup dialogueGroup; Text dlgSpeaker, dlgBody;
    RectTransform banner; CanvasGroup bannerGroup; Text bannerTitle, bannerSub; float bannerTimer;
    RectTransform toastRoot;
    readonly List<(Text t, float time)> toasts = new List<(Text, float)>();
    RectTransform lbTop, lbBottom; float letterbox, letterboxTarget;
    Image fade; float fadeTarget, fadeSpeed = 1f;
    Text readingText;
    RectTransform clickToSteer;
    Text fpsText; bool showFps; float fpsAcc = 60f;

    // screens
    CanvasGroup loading; Image loadFill; Text loadStatus;
    CanvasGroup title;
    CanvasGroup cards; Text cardTitle, cardBody;
    CanvasGroup market; RectTransform marketRows; Text marketTitle, marketNote, marketGold, marketCargo; Action marketClose;
    readonly List<(Text price, Text held, Button buy, Button sell, Button sell5)> marketRowUI = new List<(Text, Text, Button, Button, Button)>();
    CanvasGroup chart; RectTransform chartMap; RectTransform chartShip, chartObj; readonly List<(Text t, string key)> chartLabels = new List<(Text, string)>();
    CanvasGroup pause; Button qualityBtn, volBtn;
    CanvasGroup controls;
    CanvasGroup gameOver;
    CanvasGroup ending; RectTransform endRoute; Text endText, endStats;

    public bool MarketOpen, ChartOpen, PauseOpen, CardsOpen, GameOverOpen, EndingOpen, TitleOpen, ControlsOpen;
    public bool ModalOpen => MarketOpen || ChartOpen || PauseOpen || CardsOpen || GameOverOpen || EndingOpen || TitleOpen || ControlsOpen;

    // dialogue state
    readonly Queue<(string sp, string tx, float dur)> lines = new Queue<(string, string, float)>();
    string curSpeaker, curText; float curDur, curT; bool lineActive;
    public bool DialogueIdle => !lineActive && lines.Count == 0;

    // objective state
    public Vector3? MarkerTarget;
    public string MarkerLabel = "";
    public Ship EnemyTarget;
    public string EnemyName = "CARTOGRAPHER RAIDER";
    public bool ShowStealth;
    CanvasGroup stealthGroup; Image stealthFill; Text stealthLabel, lanternLabel;
    Button endNext;
    System.Action endNextAction;
    public bool ShowReticle;
    public Vector3 ReticleWorld;
    public float ReloadFrac = 1f;
    public string Reading = "";

    public void ForceCloseCards()
    {
        CardsOpen = false;
        cards.alpha = 0;
        cards.blocksRaycasts = false;
        cards.gameObject.SetActive(false);
    }

    public static HUD Create()
    {
        UIKit.Init();
        var go = new GameObject("UI");
        I = go.AddComponent<HUD>();
        I.Build();
        return I;
    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        root = (RectTransform)transform;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        var mod = es.AddComponent<InputSystemUIInputModule>();
        mod.AssignDefaultActions();

        hud = UIKit.Rect(root, "HUD");
        UIKit.Stretch(hud);
        hudGroup = hud.gameObject.AddComponent<CanvasGroup>();
        hudGroup.alpha = 0;
        hudGroup.blocksRaycasts = false;

        BuildCompass();
        BuildShipPanel();
        BuildEra();
        BuildTopRight();
        BuildObjective();
        BuildMarkers();
        BuildMessages();

        // letterbox
        lbTop = UIKit.Img(root, "LetterTop", ProcTex.Square, Color.black).rectTransform;
        UIKit.Place(lbTop, 0.5f, 1f, 0.5f, 1f, 0, 0, 4000, 0);
        lbBottom = UIKit.Img(root, "LetterBottom", ProcTex.Square, Color.black).rectTransform;
        UIKit.Place(lbBottom, 0.5f, 0f, 0.5f, 0f, 0, 0, 4000, 0);

        BuildDialogue();
        BuildBanner();
        BuildMarket();
        BuildChart();
        BuildPause();
        BuildControls();
        BuildGameOver();
        BuildEnding();
        BuildCards();
        BuildTitle();

        fade = UIKit.Img(root, "Fade", ProcTex.Square, new Color(0, 0, 0, 1));
        UIKit.Stretch(fade.rectTransform);
        BuildLoading();
    }

    // ------------------------------------------------------------------ builders
    void BuildCompass()
    {
        compass = UIKit.Rect(hud, "Compass");
        UIKit.Place(compass, 0.5f, 1f, 0.5f, 1f, 0, -26, 640, 58);
        var bg = UIKit.Img(compass, "Bg", ProcTex.Bar, new Color(0, 0, 0, 0.55f));
        UIKit.Stretch(bg.rectTransform);
        var line = UIKit.Img(compass, "Line", ProcTex.Bar, new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.6f));
        UIKit.Place(line.rectTransform, 0.5f, 0f, 0.5f, 0f, 0, 0, 640, 2);
        compass.gameObject.AddComponent<RectMask2D>();
        string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        for (int a = 0; a < 360; a += 15)
        {
            bool card = a % 45 == 0;
            string s = card ? names[a / 45] : "|";
            var t = UIKit.Txt(compass, "Tick" + a, s, card ? (a % 90 == 0 ? 26 : 20) : 13, card ? (a == 0 ? UIKit.Red : UIKit.Parch) : UIKit.Muted, TextAnchor.MiddleCenter, card ? FontStyle.Bold : FontStyle.Normal);
            UIKit.Place(t.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 2, 60, 40);
            compassTicks.Add((t, a));
        }
        compassObj = UIKit.Img(compass, "Obj", ProcTex.Diamond, UIKit.Gold);
        UIKit.Place(compassObj.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 6, 22, 22);
        compassShip = UIKit.Img(compass, "ShipHeading", ProcTex.ShipIcon, UIKit.Teal);
        UIKit.Place(compassShip.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 6, 26, 26);
        compassEnemy = UIKit.Img(compass, "Enemy", ProcTex.Diamond, UIKit.Red);
        UIKit.Place(compassEnemy.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 6, 18, 18);
        var center = UIKit.Img(hud, "CompassCenter", ProcTex.Arrow, UIKit.Gold);
        UIKit.Place(center.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -84, 18, 18);
        center.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
    }

    void BuildShipPanel()
    {
        var p = UIKit.Rect(hud, "ShipPanel");
        UIKit.Place(p, 0f, 0f, 0f, 0f, 36, 30, 560, 230);
        var glow = UIKit.Img(p, "Shade", ProcTex.Glow, new Color(0, 0, 0, 0.55f));
        UIKit.Place(glow.rectTransform, 0f, 0f, 0.5f, 0.5f, 110, 110, 420, 380);

        windDial = UIKit.Rect(p, "WindDial");
        UIKit.Place(windDial, 0f, 0f, 0.5f, 0.5f, 110, 112, 190, 190);
        var disc = UIKit.Img(windDial, "Disc", ProcTex.Circle, new Color(0.02f, 0.03f, 0.05f, 0.6f));
        UIKit.Stretch(disc.rectTransform);
        var ring = UIKit.Img(windDial, "Ring", ProcTex.Ring, new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.75f));
        UIKit.Stretch(ring.rectTransform);
        var ring2 = UIKit.Img(windDial, "Ring2", ProcTex.RingThin, new Color(1, 1, 1, 0.2f));
        UIKit.Place(ring2.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 130, 130);
        var ship = UIKit.Img(windDial, "Ship", ProcTex.ShipIcon, UIKit.Parch);
        UIKit.Place(ship.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 70, 70);
        windArrow = UIKit.Rect(windDial, "WindPivot");
        UIKit.Place(windArrow, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 10, 10);
        var arrow = UIKit.Img(windArrow, "Arrow", ProcTex.Arrow, UIKit.Teal);
        UIKit.Place(arrow.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -58, 40, 46);
        northMark = UIKit.Rect(windDial, "NorthPivot");
        UIKit.Place(northMark, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 10, 10);
        var n = UIKit.Txt(northMark, "N", "N", 20, UIKit.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(n.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 82, 30, 30);
        var wl = UIKit.Txt(p, "WindLabel", UIKit.Spaced("WIND"), 15, UIKit.GoldDim, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(wl.rectTransform, 0f, 0f, 0.5f, 0.5f, 110, 222, 200, 22);

        pointOfSail = UIKit.Txt(p, "PoS", "", 20, UIKit.Teal, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(pointOfSail.rectTransform, 0f, 0f, 0f, 0.5f, 232, 182, 340, 30);
        speedText = UIKit.Txt(p, "Speed", "", 34, UIKit.Parch, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(speedText.rectTransform, 0f, 0f, 0f, 0.5f, 232, 140, 340, 44);

        sailLabel = UIKit.Txt(p, "SailLabel", UIKit.Spaced("SAIL"), 15, UIKit.GoldDim, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(sailLabel.rectTransform, 0f, 0f, 0f, 0.5f, 232, 100, 200, 22);
        for (int i = 0; i < 3; i++)
        {
            var pip = UIKit.Img(p, "Pip" + i, ProcTex.PanelFill, UIKit.Gold, true);
            UIKit.Place(pip.rectTransform, 0f, 0f, 0f, 0.5f, 300 + i * 46, 100, 40, 14);
            sailPips.Add(pip);
        }
        var hl = UIKit.Txt(p, "HullLabel", UIKit.Spaced("HULL"), 15, UIKit.GoldDim, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(hl.rectTransform, 0f, 0f, 0f, 0.5f, 232, 66, 200, 22);
        var hb = UIKit.Img(p, "HullBg", ProcTex.Square, new Color(0, 0, 0, 0.6f));
        UIKit.Place(hb.rectTransform, 0f, 0f, 0f, 0.5f, 300, 66, 230, 12);
        hullFill = UIKit.Img(p, "HullFill", ProcTex.Square, new Color(0.85f, 0.7f, 0.4f));
        UIKit.Place(hullFill.rectTransform, 0f, 0f, 0f, 0.5f, 300, 66, 230, 12);
        hullFill.type = Image.Type.Filled;
        hullFill.fillMethod = Image.FillMethod.Horizontal;
        hullText = UIKit.Txt(p, "HullTxt", "", 15, UIKit.Parch, TextAnchor.MiddleLeft);
        UIKit.Place(hullText.rectTransform, 0f, 0f, 0f, 0.5f, 540, 66, 80, 22);
        var keys = UIKit.Txt(p, "Keys", "<color=#e8c57a>W/S</color> sail   <color=#e8c57a>A/D</color> steer   <color=#e8c57a>T</color> auto-steer   <color=#e8c57a>M</color> chart", 15, UIKit.Muted, TextAnchor.MiddleLeft);
        UIKit.Place(keys.rectTransform, 0f, 0f, 0f, 0.5f, 232, 30, 460, 24);
    }

    void BuildEra()
    {
        var p = UIKit.Rect(hud, "Era");
        UIKit.Place(p, 1f, 0f, 1f, 0f, -40, 34, 170, 200);
        eraGlow = UIKit.Img(p, "Glow", ProcTex.Glow, new Color(UIKit.Teal.r, UIKit.Teal.g, UIKit.Teal.b, 0f));
        UIKit.Place(eraGlow.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 112, 280, 280);
        var disc = UIKit.Img(p, "Disc", ProcTex.Circle, new Color(0.02f, 0.03f, 0.05f, 0.6f));
        UIKit.Place(disc.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 112, 136, 136);
        eraRing = UIKit.Img(p, "RingBg", ProcTex.Ring, new Color(1, 1, 1, 0.15f));
        UIKit.Place(eraRing.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 112, 136, 136);
        eraFill = UIKit.Img(p, "RingFill", ProcTex.Ring, UIKit.Teal);
        UIKit.Place(eraFill.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 112, 136, 136);
        eraFill.type = Image.Type.Filled;
        eraFill.fillMethod = Image.FillMethod.Radial360;
        eraFill.fillOrigin = (int)Image.Origin360.Top;
        eraFill.fillClockwise = true;
        eraLabel = UIKit.Txt(p, "Key", "SHIFT", 22, UIKit.Parch, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(eraLabel.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 118, 130, 30);
        var s2 = UIKit.Txt(p, "Compass", "compass", 14, UIKit.Muted, TextAnchor.MiddleCenter, FontStyle.Italic);
        UIKit.Place(s2.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 94, 130, 22);
        eraSub = UIKit.Txt(p, "Sub", "", 15, UIKit.Teal, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(eraSub.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 24, 260, 24);
    }

    void BuildTopRight()
    {
        var p = UIKit.Rect(hud, "Purse");
        UIKit.Place(p, 1f, 1f, 1f, 1f, -40, -30, 360, 90);
        goldText = UIKit.Txt(p, "Gold", "", 28, UIKit.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
        UIKit.Place(goldText.rectTransform, 1f, 1f, 1f, 1f, 0, 0, 360, 36);
        cargoText = UIKit.Txt(p, "Cargo", "", 18, UIKit.Parch, TextAnchor.MiddleRight);
        UIKit.Place(cargoText.rectTransform, 1f, 1f, 1f, 1f, 0, -38, 360, 26);
    }

    void BuildObjective()
    {
        objPanel = UIKit.Rect(hud, "Objective");
        UIKit.Place(objPanel, 0f, 1f, 0f, 1f, 40, -30, 560, 140);
        objPanel.anchoredPosition = new Vector2(40, -30);
        var bar = UIKit.Img(objPanel, "Accent", ProcTex.Square, UIKit.Gold);
        UIKit.Place(bar.rectTransform, 0f, 1f, 0f, 1f, 0, -4, 3, 74);
        objTitle = UIKit.Txt(objPanel, "Title", UIKit.Spaced("OBJECTIVE"), 15, UIKit.GoldDim, TextAnchor.UpperLeft, FontStyle.Bold);
        UIKit.Place(objTitle.rectTransform, 0f, 1f, 0f, 1f, 16, 0, 540, 22);
        objText = UIKit.Txt(objPanel, "Text", "", 25, UIKit.Parch, TextAnchor.UpperLeft, FontStyle.Bold);
        UIKit.Place(objText.rectTransform, 0f, 1f, 0f, 1f, 16, -24, 420, 60);
        objDist = UIKit.Txt(objPanel, "Dist", "", 17, UIKit.Muted, TextAnchor.UpperLeft, FontStyle.Italic);
        UIKit.Place(objDist.rectTransform, 0f, 1f, 0f, 1f, 16, -86, 540, 24);
        NextText = UIKit.Txt(hud, "Next", "", 21, UIKit.Teal, TextAnchor.UpperLeft, FontStyle.Bold);
        UIKit.Place(NextText.rectTransform, 0f, 1f, 0f, 1f, 56, -146, 700, 30);
    }

    void BuildMarkers()
    {
        marker = UIKit.Rect(hud, "Marker");
        UIKit.Place(marker, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 40, 40);
        markerIcon = UIKit.Img(marker, "Icon", ProcTex.Diamond, UIKit.Gold);
        UIKit.Place(markerIcon.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 34, 34);
        var g = UIKit.Img(marker, "Glow", ProcTex.Glow, new Color(1f, 0.8f, 0.4f, 0.35f));
        UIKit.Place(g.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 90, 90);
        markerText = UIKit.Txt(marker, "Dist", "", 17, UIKit.Parch, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(markerText.rectTransform, 0.5f, 0.5f, 0.5f, 1f, 0, -24, 220, 24);

        enemyBar = UIKit.Rect(hud, "EnemyBar");
        UIKit.Place(enemyBar, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 200, 40);
        enemyName = UIKit.Txt(enemyBar, "Name", "CARTOGRAPHER RAIDER", 15, UIKit.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(enemyName.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, 0, 300, 20);
        var eb = UIKit.Img(enemyBar, "Bg", ProcTex.Square, new Color(0, 0, 0, 0.6f));
        UIKit.Place(eb.rectTransform, 0.5f, 0f, 0.5f, 0f, 0, 6, 180, 8);
        enemyFill = UIKit.Img(enemyBar, "Fill", ProcTex.Square, UIKit.Red);
        UIKit.Place(enemyFill.rectTransform, 0.5f, 0f, 0.5f, 0f, 0, 6, 180, 8);
        enemyFill.type = Image.Type.Filled;
        enemyFill.fillMethod = Image.FillMethod.Horizontal;

        var sr = UIKit.Rect(hud, "Stealth");
        UIKit.Place(sr, 0.5f, 1f, 0.5f, 1f, 0, -100, 520, 80);
        stealthGroup = sr.gameObject.AddComponent<CanvasGroup>();
        stealthGroup.alpha = 0;
        stealthLabel = UIKit.Txt(sr, "Label", "", 20, UIKit.Teal, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(stealthLabel.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, 0, 520, 26);
        var sbg = UIKit.Img(sr, "Bg", ProcTex.Square, new Color(0, 0, 0, 0.6f));
        UIKit.Place(sbg.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -30, 360, 10);
        stealthFill = UIKit.Img(sr, "Fill", ProcTex.Square, UIKit.Red);
        UIKit.Place(stealthFill.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -30, 360, 10);
        stealthFill.type = Image.Type.Filled;
        stealthFill.fillMethod = Image.FillMethod.Horizontal;
        lanternLabel = UIKit.Txt(sr, "Lantern", "", 17, UIKit.Parch, TextAnchor.MiddleCenter, FontStyle.Italic);
        UIKit.Place(lanternLabel.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -52, 520, 24);

        reticle = UIKit.Rect(hud, "Reticle");
        UIKit.Place(reticle, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 70, 70);
        reticleRing = UIKit.Img(reticle, "Ring", ProcTex.RingThin, new Color(1f, 0.75f, 0.4f, 0.8f));
        UIKit.Stretch(reticleRing.rectTransform);
        reticleFill = UIKit.Img(reticle, "Fill", ProcTex.Ring, new Color(1f, 0.6f, 0.25f, 0.9f));
        UIKit.Stretch(reticleFill.rectTransform);
        reticleFill.type = Image.Type.Filled;
        reticleFill.fillMethod = Image.FillMethod.Radial360;
        reticleFill.fillOrigin = (int)Image.Origin360.Top;
    }

    void BuildMessages()
    {
        prompt = UIKit.Panel(hud, "Prompt", new Color(0.03f, 0.04f, 0.06f, 0.82f), new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.6f));
        UIKit.Place(prompt, 0.5f, 0f, 0.5f, 0.5f, 0, 300, 560, 54);
        promptGroup = prompt.gameObject.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0;
        promptText = UIKit.Txt(prompt, "Text", "", 24, UIKit.Parch, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Stretch(promptText.rectTransform);

        hintText = UIKit.Txt(hud, "Hint", "", 20, UIKit.Parch, TextAnchor.MiddleCenter, FontStyle.Italic);
        UIKit.Place(hintText.rectTransform, 0.5f, 0f, 0.5f, 0.5f, 0, 250, 1200, 30);

        warnText = UIKit.Txt(hud, "Warn", "", 30, UIKit.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(warnText.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 170, 1300, 44);
        autoText = UIKit.Txt(hud, "Auto", "", 20, UIKit.Teal, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(autoText.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -96, 600, 28);
        readingText = UIKit.Txt(hud, "Reading", "", 26, UIKit.Teal, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(readingText.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -130, 900, 40);

        fpsText = UIKit.Txt(root, "Fps", "", 18, UIKit.Teal, TextAnchor.UpperRight);
        UIKit.Place(fpsText.rectTransform, 1f, 1f, 1f, 1f, -12, -100, 200, 24);
        toastRoot = UIKit.Rect(hud, "Toasts");
        UIKit.Place(toastRoot, 1f, 1f, 1f, 1f, -40, -150, 600, 400);

        clickToSteer = UIKit.Panel(root, "ClickToSteer", new Color(0.03f, 0.04f, 0.06f, 0.75f), new Color(UIKit.Teal.r, UIKit.Teal.g, UIKit.Teal.b, 0.6f));
        UIKit.Place(clickToSteer, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 520, 70);
        var ct = UIKit.Txt(clickToSteer, "T", "CLICK TO TAKE THE HELM", 24, UIKit.Teal, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Stretch(ct.rectTransform);
        clickToSteer.gameObject.SetActive(false);
    }

    void BuildDialogue()
    {
        dialogue = UIKit.Rect(root, "Dialogue");
        UIKit.Place(dialogue, 0.5f, 0f, 0.5f, 0f, 0, 36, 1200, 170);
        dialogueGroup = dialogue.gameObject.AddComponent<CanvasGroup>();
        dialogueGroup.alpha = 0;
        var bg = UIKit.Img(dialogue, "Bg", ProcTex.Glow, new Color(0, 0, 0, 0.7f));
        UIKit.Place(bg.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1500, 300);
        dlgSpeaker = UIKit.Txt(dialogue, "Speaker", "", 20, UIKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
        UIKit.Place(dlgSpeaker.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -8, 1100, 28);
        dlgBody = UIKit.Txt(dialogue, "Body", "", 28, UIKit.Parch, TextAnchor.UpperCenter);
        UIKit.Place(dlgBody.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -42, 1100, 110);
    }

    void BuildBanner()
    {
        banner = UIKit.Rect(root, "Banner");
        UIKit.Place(banner, 0.5f, 0.5f, 0.5f, 0.5f, 0, 200, 1400, 200);
        bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.alpha = 0;
        var glow = UIKit.Img(banner, "Glow", ProcTex.Glow, new Color(0, 0, 0, 0.5f));
        UIKit.Place(glow.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1500, 360);
        var l1 = UIKit.Img(banner, "Line1", ProcTex.Bar, UIKit.Gold);
        UIKit.Place(l1.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 58, 900, 2);
        var l2 = UIKit.Img(banner, "Line2", ProcTex.Bar, UIKit.Gold);
        UIKit.Place(l2.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -52, 900, 2);
        var dia = UIKit.Img(banner, "Dia", ProcTex.Diamond, UIKit.Gold);
        UIKit.Place(dia.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 58, 18, 18);
        bannerTitle = UIKit.Txt(banner, "Title", "", 66, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(bannerTitle.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 6, 1400, 90);
        bannerSub = UIKit.Txt(banner, "Sub", "", 26, UIKit.Parch, TextAnchor.MiddleCenter, FontStyle.Italic);
        UIKit.Place(bannerSub.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -82, 1400, 40);
    }

    CanvasGroup MakeScreen(string name, Color bg)
    {
        var rt = UIKit.Rect(root, name);
        UIKit.Stretch(rt);
        var cg = rt.gameObject.AddComponent<CanvasGroup>();
        if (bg.a > 0)
        {
            var im = UIKit.Img(rt, "Bg", ProcTex.Square, bg);
            UIKit.Stretch(im.rectTransform);
            im.raycastTarget = true;
        }
        cg.alpha = 0;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        rt.gameObject.SetActive(false);
        return cg;
    }

    void Show(CanvasGroup cg, bool on)
    {
        if (on) cg.gameObject.SetActive(true);
        cg.blocksRaycasts = on;
        cg.interactable = on;
        StartCoroutine(FadeGroup(cg, on ? 1f : 0f, 0.35f));
    }

    IEnumerator FadeGroup(CanvasGroup cg, float target, float dur)
    {
        float start = cg.alpha;
        float t = 0;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        cg.alpha = target;
        if (target <= 0f) cg.gameObject.SetActive(false);
    }

    void BuildLoading()
    {
        loading = MakeScreen("Loading", new Color(0.02f, 0.03f, 0.04f, 1f));
        loading.gameObject.SetActive(true);
        loading.alpha = 1;
        var t = UIKit.Txt(loading.transform, "T", UIKit.Spaced("THE SPICE ROUTE"), 64, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(t.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 60, 1400, 90);
        var bgb = UIKit.Img(loading.transform, "BarBg", ProcTex.Square, new Color(1, 1, 1, 0.1f));
        UIKit.Place(bgb.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -30, 500, 3);
        loadFill = UIKit.Img(loading.transform, "BarFill", ProcTex.Square, UIKit.Gold);
        UIKit.Place(loadFill.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -30, 500, 3);
        loadFill.type = Image.Type.Filled;
        loadFill.fillMethod = Image.FillMethod.Horizontal;
        loadFill.fillAmount = 0;
        loadStatus = UIKit.Txt(loading.transform, "S", "", 20, UIKit.Muted, TextAnchor.MiddleCenter, FontStyle.Italic);
        UIKit.Place(loadStatus.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -70, 1000, 30);
    }

    public void SetLoading(float p, string s)
    {
        loadFill.fillAmount = p;
        loadStatus.text = s + "…";
    }

    public void HideLoading() => Show(loading, false);

    void BuildTitle()
    {
        title = MakeScreen("Title", new Color(0, 0, 0, 0));
        var shade = UIKit.Img(title.transform, "Shade", ProcTex.Glow, new Color(0, 0, 0, 0.95f));
        UIKit.Place(shade.rectTransform, 0f, 0.5f, 0.5f, 0.5f, 380, 0, 2200, 1700);
        var pre = UIKit.Txt(title.transform, "Pre", UIKit.Spaced("A VOYAGE FROM THE MALABAR COAST"), 18, UIKit.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(pre.rectTransform, 0f, 0.5f, 0f, 0.5f, 140, 250, 1200, 30);
        var t1 = UIKit.Txt(title.transform, "T1", UIKit.Spaced("THE SPICE ROUTE"), 92, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(t1.rectTransform, 0f, 0.5f, 0f, 0.5f, 132, 170, 1500, 120);
        var t2 = UIKit.Txt(title.transform, "T2", UIKit.Spaced("ACROSS SEVEN SEAS"), 30, UIKit.Parch, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(t2.rectTransform, 0f, 0.5f, 0f, 0.5f, 140, 92, 1200, 40);
        var line = UIKit.Img(title.transform, "Line", ProcTex.Bar, UIKit.Gold);
        UIKit.Place(line.rectTransform, 0f, 0.5f, 0f, 0.5f, 140, 60, 620, 2);
        var t3 = UIKit.Txt(title.transform, "T3", "Chapter I  —  The Malabar Expedition", 26, UIKit.Parch, TextAnchor.MiddleLeft, FontStyle.Italic);
        UIKit.Place(t3.rectTransform, 0f, 0.5f, 0f, 0.5f, 140, 26, 1200, 36);
        var q = UIKit.Txt(title.transform, "Q", "“The sea remembers every journey.”", 22, UIKit.Muted, TextAnchor.MiddleLeft, FontStyle.Italic);
        UIKit.Place(q.rectTransform, 0f, 0.5f, 0f, 0.5f, 140, -14, 1200, 30);

        var start = UIKit.Btn(title.transform, UIKit.Spaced("SET SAIL"), 26, () => onStart?.Invoke());
        UIKit.Place((RectTransform)start.transform, 0f, 0.5f, 0f, 0.5f, 140, -110, 320, 64);
        var tut = UIKit.Btn(title.transform, UIKit.Spaced("TUTORIAL VOYAGE"), 20, () => onTutorial?.Invoke());
        UIKit.Place((RectTransform)tut.transform, 0f, 0.5f, 0f, 0.5f, 140, -186, 320, 54);
        var tutNote = UIKit.Txt(title.transform, "TutNote", "guided lessons  ·  learn every control and win", 16, UIKit.Teal, TextAnchor.MiddleLeft, FontStyle.Italic);
        UIKit.Place(tutNote.rectTransform, 0f, 0.5f, 0f, 0.5f, 476, -186, 520, 30);
        var ch2 = UIKit.Btn(title.transform, "CHAPTER II  ·  THE GATE OF TEARS", 18, () => Game.I.LoadChapter(2));
        UIKit.Place((RectTransform)ch2.transform, 0f, 0.5f, 0f, 0.5f, 140, -252, 320, 54);
        var ch2Note = UIKit.Txt(title.transform, "Ch2Note", "the story continues  ·  stealth, fog and a flagship", 16, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Italic);
        UIKit.Place(ch2Note.rectTransform, 0f, 0.5f, 0f, 0.5f, 476, -252, 520, 30);
        var ctr = UIKit.Btn(title.transform, UIKit.Spaced("CONTROLS"), 20, () => ShowControls(true));
        UIKit.Place((RectTransform)ctr.transform, 0f, 0.5f, 0f, 0.5f, 140, -318, 320, 50);
        var qb = UIKit.Btn(title.transform, "GRAPHICS: HIGH", 20, null);
        qb.onClick.AddListener(() => { Game.I.ToggleQuality(); UIKit.SetLabel(qb, "GRAPHICS: " + (Game.I.HighQuality ? "HIGH" : "PERFORMANCE")); });
        UIKit.Place((RectTransform)qb.transform, 0f, 0.5f, 0f, 0.5f, 140, -380, 320, 50);

        var newbie = UIKit.Txt(title.transform, "Newbie", "First time playing?  Choose  TUTORIAL VOYAGE  — it teaches every button as you go.", 20, UIKit.Teal, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(newbie.rectTransform, 0f, 0.5f, 0f, 0.5f, 140, -436, 1200, 30);
        var foot = UIKit.Txt(title.transform, "Foot", "GitHub Game Off 2026  ·  Theme: SHIFT  ·  Made with Unity 6  ·  every model, texture, sound and note generated in code", 16, UIKit.Muted, TextAnchor.MiddleLeft);
        UIKit.Place(foot.rectTransform, 0f, 0f, 0f, 0f, 140, 40, 1600, 26);
    }

    Action onStart, onTutorial;
    public void ShowTitle(bool on, Action start = null, Action tutorial = null)
    {
        if (start != null) onStart = start;
        if (tutorial != null) onTutorial = tutorial;
        TitleOpen = on;
        Show(title, on);
    }

    void BuildControls()
    {
        controls = MakeScreen("Controls", new Color(0, 0, 0, 0.7f));
        var p = UIKit.Panel(controls.transform, "Panel", new Color(0.04f, 0.05f, 0.07f, 0.95f), new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.5f));
        UIKit.Place(p, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 900, 780);
        var h = UIKit.Txt(p, "H", UIKit.Spaced("CONTROLS"), 34, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(h.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -30, 800, 50);
        string[,] rows = {
            {"W / S", "Raise / lower the sail (three settings)"},
            {"A / D", "Work the steering oar"},
            {"Mouse", "Look around (click to take the helm)"},
            {"Scroll", "Camera distance"},
            {"SHIFT", "ERA SHIFT — see the sea as it once was"},
            {"T", "Auto-steer to the gold diamond (on / off)"},
            {"Q", "PERSPECTIVE SHIFT — helm view"},
            {"E", "Interact: trade, dock, read"},
            {"Left Click", "Loose a volley of fire-arrows (broadside)"},
            {"Right Click", "Aim the volley"},
            {"M", "Your father's chart"},
            {"Enter", "Skip dialogue"},
            {"P", "Pause"},
        };
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            var k = UIKit.Txt(p, "K" + i, rows[i, 0], 22, UIKit.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            UIKit.Place(k.rectTransform, 0.5f, 1f, 1f, 0.5f, -30, -110 - i * 42, 300, 36);
            var v = UIKit.Txt(p, "V" + i, rows[i, 1], 21, UIKit.Parch, TextAnchor.MiddleLeft);
            UIKit.Place(v.rectTransform, 0.5f, 1f, 0f, 0.5f, 0, -110 - i * 42, 500, 36);
        }
        var b = UIKit.Btn(p, UIKit.Spaced("BACK"), 20, () => ShowControls(false));
        UIKit.Place((RectTransform)b.transform, 0.5f, 0f, 0.5f, 0f, 0, 30, 240, 50);
    }

    public void ShowControls(bool on) { ControlsOpen = on; Show(controls, on); }

    void BuildCards()
    {
        cards = MakeScreen("Cards", new Color(0, 0, 0, 0.45f));
        var g = UIKit.Img(cards.transform, "Glow", ProcTex.Glow, new Color(0, 0, 0, 0.6f));
        UIKit.Place(g.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 2000, 900);
        cardTitle = UIKit.Txt(cards.transform, "T", "", 22, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(cardTitle.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 110, 1400, 40);
        cardBody = UIKit.Txt(cards.transform, "B", "", 36, UIKit.Parch, TextAnchor.MiddleCenter, FontStyle.Italic);
        UIKit.Place(cardBody.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1300, 200);
        var hint = UIKit.Txt(cards.transform, "H", "click to continue", 16, UIKit.Muted, TextAnchor.MiddleCenter, FontStyle.Italic);
        UIKit.Place(hint.rectTransform, 0.5f, 0f, 0.5f, 0f, 0, 60, 600, 30);
    }

    public IEnumerator PlayCards(string[][] list)
    {
        CardsOpen = true;
        cards.gameObject.SetActive(true);
        cards.alpha = 1;
        cards.blocksRaycasts = true;
        for (int i = 0; i < list.Length; i++)
        {
            cardTitle.text = UIKit.Spaced(list[i][0]);
            cardBody.text = list[i][1];
            float t = 0;
            Color c1 = cardBody.color, c0 = cardTitle.color;
            while (t < 1f) { t += Time.deltaTime; SetA(cardBody, t); SetA(cardTitle, t); yield return null; }
            float hold = Mathf.Max(3.5f, list[i][1].Length * 0.06f);
            t = 0;
            yield return null;
            while (t < hold && !In.LMBDown && !In.Down(Key.Enter) && !In.Down(Key.Space)) { t += Time.deltaTime; yield return null; }
            t = 1;
            while (t > 0f) { t -= Time.deltaTime * 2f; SetA(cardBody, t); SetA(cardTitle, t); yield return null; }
        }
        StartCoroutine(FadeGroup(cards, 0, 1f));
        CardsOpen = false;
    }

    static void SetA(Graphic g, float a) { var c = g.color; c.a = Mathf.Clamp01(a); g.color = c; }

    void BuildMarket()
    {
        market = MakeScreen("Market", new Color(0, 0, 0, 0.55f));
        var p = UIKit.Panel(market.transform, "Panel", new Color(0.04f, 0.05f, 0.07f, 0.95f), new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.55f));
        UIKit.Place(p, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1180, 700);
        marketTitle = UIKit.Txt(p, "Title", "", 40, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(marketTitle.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -26, 1100, 54);
        marketNote = UIKit.Txt(p, "Note", "", 20, UIKit.Teal, TextAnchor.MiddleCenter, FontStyle.Italic);
        UIKit.Place(marketNote.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -84, 1100, 30);
        string[] heads = { "COMMODITY", "PRICE", "IN HOLD" };
        float[] hx = { -330, 150, 270 };
        for (int i = 0; i < 3; i++)
        {
            var h = UIKit.Txt(p, "H" + i, UIKit.Spaced(heads[i]), 14, UIKit.GoldDim, i == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(h.rectTransform, 0.5f, 1f, 0.5f, 0.5f, hx[i], -136, i == 0 ? 500 : 160, 24);
        }
        marketRows = UIKit.Rect(p, "Rows");
        UIKit.Place(marketRows, 0.5f, 1f, 0.5f, 1f, 0, -160, 1100, 420);
        marketGold = UIKit.Txt(p, "Gold", "", 28, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(marketGold.rectTransform, 0f, 0f, 0f, 0f, 50, 34, 400, 44);
        marketCargo = UIKit.Txt(p, "Cargo", "", 22, UIKit.Parch, TextAnchor.MiddleLeft);
        UIKit.Place(marketCargo.rectTransform, 0f, 0f, 0f, 0f, 330, 38, 400, 36);
        var done = UIKit.Btn(p, UIKit.Spaced("DONE"), 22, () => CloseMarket());
        UIKit.Place((RectTransform)done.transform, 1f, 0f, 1f, 0f, -40, 30, 220, 56);
    }

    public void ShowMarket(Market m, Action onClose)
    {
        marketClose = onClose;
        MarketOpen = true;
        foreach (Transform c in marketRows) Destroy(c.gameObject);
        marketRowUI.Clear();
        marketTitle.text = UIKit.Spaced(m.PortName);
        marketNote.text = m.PortNote;
        for (int i = 0; i < m.Items.Count; i++)
        {
            int idx = i;
            var c = m.Items[i];
            var row = UIKit.Rect(marketRows, "Row" + i);
            UIKit.Place(row, 0.5f, 1f, 0.5f, 1f, 0, -i * 136, 1100, 124);
            var bg = UIKit.Img(row, "Bg", ProcTex.PanelFill, new Color(1, 1, 1, 0.04f), true);
            UIKit.Stretch(bg.rectTransform);
            var icon = UIKit.Img(row, "Icon", ProcTex.Circle, c.Col);
            UIKit.Place(icon.rectTransform, 0f, 0.5f, 0.5f, 0.5f, 60, 0, 64, 64);
            var ring = UIKit.Img(row, "IconRing", ProcTex.RingThin, UIKit.Gold);
            UIKit.Place(ring.rectTransform, 0f, 0.5f, 0.5f, 0.5f, 60, 0, 72, 72);
            var nm = UIKit.Txt(row, "Name", c.Name + "  <i><size=18><color=#b8ad94>" + c.Local + "</color></size></i>", 28, UIKit.Parch, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(nm.rectTransform, 0f, 0.5f, 0f, 0.5f, 112, 22, 520, 36);
            var ds = UIKit.Txt(row, "Desc", c.Desc, 17, UIKit.Muted, TextAnchor.UpperLeft);
            UIKit.Place(ds.rectTransform, 0f, 0.5f, 0f, 0.5f, 112, -18, 520, 44);
            var price = UIKit.Txt(row, "Price", "", 28, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(price.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 150, 0, 160, 40);
            if (m.CanBuy)
            {
                var west = UIKit.Txt(row, "West", "worth ~" + c.WestPrice + " g " + m.WestLabel, 15, UIKit.Teal, TextAnchor.MiddleCenter, FontStyle.Italic);
                UIKit.Place(west.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 150, -30, 220, 22);
            }
            var held = UIKit.Txt(row, "Held", "", 28, UIKit.Parch, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(held.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 270, 0, 120, 40);
            Button buy = null, sell = null, sell5 = null;
            if (m.CanBuy)
            {
                buy = UIKit.Btn(row, "BUY", 20, () => { if (m.Buy(idx)) AudioMan.I.Play("coin", 0.5f, 1.1f); else AudioMan.I.Play("click", 0.5f, 0.6f); RefreshMarket(m); });
                UIKit.Place((RectTransform)buy.transform, 1f, 0.5f, 1f, 0.5f, -150, 0, 120, 48);
                sell = UIKit.Btn(row, "SELL", 20, () => { if (m.Sell(idx)) AudioMan.I.Play("coin", 0.5f, 0.9f); RefreshMarket(m); });
                UIKit.Place((RectTransform)sell.transform, 1f, 0.5f, 1f, 0.5f, -20, 0, 120, 48);
            }
            else
            {
                sell = UIKit.Btn(row, "SELL", 20, () => { if (m.Sell(idx)) AudioMan.I.Play("coin", 0.6f, 1f); RefreshMarket(m); });
                UIKit.Place((RectTransform)sell.transform, 1f, 0.5f, 1f, 0.5f, -150, 0, 120, 48);
                sell5 = UIKit.Btn(row, "SELL ALL", 18, () => { int n = 0; while (m.Sell(idx)) n++; if (n > 0) AudioMan.I.Play("coin", 0.8f, 1f); RefreshMarket(m); });
                UIKit.Place((RectTransform)sell5.transform, 1f, 0.5f, 1f, 0.5f, -20, 0, 120, 48);
            }
            marketRowUI.Add((price, held, buy, sell, sell5));
        }
        RefreshMarket(m);
        Show(market, true);
    }

    void RefreshMarket(Market m)
    {
        for (int i = 0; i < marketRowUI.Count; i++)
        {
            var r = marketRowUI[i];
            r.price.text = (m.CanBuy ? m.BuyCost(i) : m.SellValue(i)) + " g";
            r.held.text = m.Items[i].Held.ToString();
            if (r.buy != null) r.buy.interactable = m.Gold >= m.BuyCost(i) && m.Cargo < m.Capacity;
            if (r.sell != null) r.sell.interactable = m.Items[i].Held > 0;
            if (r.sell5 != null) r.sell5.interactable = m.Items[i].Held > 0;
        }
        marketGold.text = m.Gold + " gold";
        marketCargo.text = "Hold  " + m.Cargo + " / " + m.Capacity;
    }

    public void CloseMarket()
    {
        if (!MarketOpen) return;
        MarketOpen = false;
        Show(market, false);
        var a = marketClose; marketClose = null;
        a?.Invoke();
    }

    void BuildChart()
    {
        chart = MakeScreen("Chart", new Color(0, 0, 0, 0.6f));
        var parch = UIKit.Raw(chart.transform, "Parch", null, Color.white);
        UIKit.Place(parch.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1240, 900);
        var h = UIKit.Txt(parch.transform, "H", UIKit.Spaced("THE CHART OF RAGHAVAN"), 30, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
        UIKit.Place(h.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -30, 1000, 44);
        var map = UIKit.Raw(parch.transform, "Map", null, Color.white);
        UIKit.Place(map.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -10, 1110, 780);
        chartMap = map.rectTransform;
        chartObj = UIKit.Img(chartMap, "Obj", ProcTex.Diamond, new Color(0.7f, 0.15f, 0.05f)).rectTransform;
        UIKit.Place(chartObj, 0f, 0f, 0.5f, 0.5f, 0, 0, 30, 30);
        chartShip = UIKit.Img(chartMap, "Ship", ProcTex.Arrow, UIKit.Ink).rectTransform;
        UIKit.Place(chartShip, 0f, 0f, 0.5f, 0.5f, 0, 0, 34, 34);
        var close = UIKit.Txt(parch.transform, "C", "[M] close", 18, UIKit.Ink, TextAnchor.MiddleRight, FontStyle.Italic, false);
        UIKit.Place(close.rectTransform, 1f, 0f, 1f, 0f, -40, 20, 300, 30);
        var rose = UIKit.Txt(chartMap, "Rose", "N\n↑", 26, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
        UIKit.Place(rose.rectTransform, 1f, 1f, 1f, 1f, -30, -20, 60, 80);
    }

    public void InitChart()
    {
        var parch = chart.transform.Find("Parch").GetComponent<RawImage>();
        parch.texture = Mats.Parchment;
        var map = chartMap.GetComponent<RawImage>();
        map.texture = World.ChartTex;
        map.color = new Color(1, 1, 1, 0.92f);
        void Label(string key, string text, Vector3 world, int size = 20)
        {
            var t = UIKit.Txt(chartMap, "L_" + key, text, size, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, false);
            Vector2 pos = ChartPos(world);
            UIKit.Place(t.rectTransform, 0f, 0f, 0.5f, 0.5f, pos.x, pos.y + 34, 300, 30);
            chartLabels.Add((t, key));
        }
        if (Game.I != null && Game.I.Chapter == 2)
        {
            KnownPlaces = new HashSet<string> { "ras", "arabia", "azania", "aden", "gate", "redsea", "fog" };
            Label("ras", "Ras al-Mira", World.P("dock_ras") + new Vector3(0, 0, 150), 22);
            Label("arabia", "ARABIA", new Vector3(-200, 0, 800), 30);
            Label("azania", "AZANIA", new Vector3(-1200, 0, -800), 30);
            Label("fog", "Reefs of the Fog", new Vector3(330, 0, -60));
            Label("aden", "Aden", World.P("dock_aden") + new Vector3(-120, 0, 120), 24);
            Label("gate", "Gate of Tears", new Vector3(-1700, 0, -260), 22);
            Label("redsea", "RED SEA", new Vector3(-2450, 0, 700), 26);
            return;
        }
        Label("muziris", "Muziris", World.P("dock_muziris") + new Vector3(140, 0, 0), 24);
        Label("malabar", "MALABAR", new Vector3(650, 0, 500), 30);
        Label("cape", "Cape of Lanterns", World.P("cape"));
        Label("kadal", "Kadal", new Vector3(-820, 0, -230));
        Label("echo", "Isle of Echoes", new Vector3(-1500, 0, -100));
        Label("shoals", "Shoals of Thorns", new Vector3(-2020, 0, -360));
        Label("outpost", "Ras al-Mira", World.P("dock_outpost") + new Vector3(-150, 0, 0), 22);
    }

    public HashSet<string> KnownPlaces = new HashSet<string> { "muziris", "malabar", "cape", "kadal" };

    Vector2 ChartPos(Vector3 w)
    {
        var r = World.ChartRect;
        return new Vector2((w.x - r.xMin) / r.width * chartMap.sizeDelta.x, (w.z - r.yMin) / r.height * chartMap.sizeDelta.y);
    }

    public void ToggleChart()
    {
        ChartOpen = !ChartOpen;
        Show(chart, ChartOpen);
    }

    void BuildPause()
    {
        pause = MakeScreen("Pause", new Color(0, 0, 0, 0.6f));
        var p = UIKit.Panel(pause.transform, "Panel", new Color(0.04f, 0.05f, 0.07f, 0.95f), new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.5f));
        UIKit.Place(p, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 520, 520);
        var h = UIKit.Txt(p, "H", UIKit.Spaced("PAUSED"), 40, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(h.rectTransform, 0.5f, 1f, 0.5f, 1f, 0, -40, 500, 60);
        var r = UIKit.Btn(p, UIKit.Spaced("RESUME"), 22, () => Game.I.SetPause(false));
        UIKit.Place((RectTransform)r.transform, 0.5f, 1f, 0.5f, 1f, 0, -130, 340, 58);
        var c = UIKit.Btn(p, UIKit.Spaced("CONTROLS"), 20, () => ShowControls(true));
        UIKit.Place((RectTransform)c.transform, 0.5f, 1f, 0.5f, 1f, 0, -205, 340, 52);
        qualityBtn = UIKit.Btn(p, "GRAPHICS: HIGH", 20, null);
        qualityBtn.onClick.AddListener(() => { Game.I.ToggleQuality(); UIKit.SetLabel(qualityBtn, "GRAPHICS: " + (Game.I.HighQuality ? "HIGH" : "PERFORMANCE")); });
        UIKit.Place((RectTransform)qualityBtn.transform, 0.5f, 1f, 0.5f, 1f, 0, -272, 340, 52);
        volBtn = UIKit.Btn(p, "VOLUME: 100%", 20, null);
        volBtn.onClick.AddListener(() =>
        {
            var a = AudioMan.I;
            a.MasterVolume = a.MasterVolume > 0.8f ? 0.55f : a.MasterVolume > 0.4f ? 0.25f : a.MasterVolume > 0.1f ? 0f : 0.85f;
            UIKit.SetLabel(volBtn, "VOLUME: " + Mathf.RoundToInt(a.MasterVolume / 0.85f * 100f) + "%");
        });
        UIKit.Place((RectTransform)volBtn.transform, 0.5f, 1f, 0.5f, 1f, 0, -339, 340, 52);
        var cp = UIKit.Btn(p, "RESTART FROM CHECKPOINT", 18, () => { Game.I.SetPause(false); Game.I.Respawn(); });
        UIKit.Place((RectTransform)cp.transform, 0.5f, 1f, 0.5f, 1f, 0, -406, 340, 52);
    }

    public void ShowPause(bool on) { PauseOpen = on; Show(pause, on); }

    void BuildGameOver()
    {
        gameOver = MakeScreen("GameOver", new Color(0.05f, 0, 0, 0.55f));
        var t = UIKit.Txt(gameOver.transform, "T", UIKit.Spaced("THE KAVERI IS LOST"), 64, UIKit.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Place(t.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 80, 1400, 90);
        var s = UIKit.Txt(gameOver.transform, "S", "The sea remembers every journey — even the ones that end too soon.", 26, UIKit.Parch, TextAnchor.MiddleCenter, FontStyle.Italic);
        UIKit.Place(s.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1400, 40);
        var b = UIKit.Btn(gameOver.transform, "RETURN TO LAST CHECKPOINT", 22, () => { ShowGameOver(false); Game.I.Respawn(); });
        UIKit.Place((RectTransform)b.transform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -100, 460, 60);
    }

    public void ShowGameOver(bool on) { GameOverOpen = on; Show(gameOver, on); }

    void BuildEnding()
    {
        ending = MakeScreen("Ending", new Color(0.02f, 0.02f, 0.03f, 0.88f));
        var parch = UIKit.Raw(ending.transform, "Parch", null, Color.white);
        UIKit.Place(parch.rectTransform, 0.5f, 0.5f, 1f, 0.5f, -40, 30, 760, 560);
        endRoute = UIKit.Rect(parch.transform, "Route");
        UIKit.Stretch(endRoute);
        var ch = UIKit.Txt(parch.transform, "Cap", "the passage beyond the edge of every map", 20, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Italic, false);
        UIKit.Place(ch.rectTransform, 0.5f, 0f, 0.5f, 0f, 0, 26, 700, 30);
        endText = UIKit.Txt(ending.transform, "Text", "", 26, UIKit.Parch, TextAnchor.UpperLeft, FontStyle.Italic);
        UIKit.Place(endText.rectTransform, 0.5f, 0.5f, 0f, 0.5f, 40, 120, 760, 340);
        endStats = UIKit.Txt(ending.transform, "Stats", "", 20, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
        UIKit.Place(endStats.rectTransform, 0.5f, 0.5f, 0f, 0.5f, 40, -140, 760, 200);
        var b = UIKit.Btn(ending.transform, UIKit.Spaced("SAIL AGAIN"), 22, () => Game.I.Restart());
        UIKit.Place((RectTransform)b.transform, 0.5f, 0f, 0.5f, 0f, -190, 60, 340, 60);
        endNext = UIKit.Btn(ending.transform, "NEXT", 22, () => endNextAction?.Invoke());
        UIKit.Place((RectTransform)endNext.transform, 0.5f, 0f, 0.5f, 0f, 190, 60, 340, 60);
        var credit = UIKit.Txt(ending.transform, "Credit", "THE SPICE ROUTE: ACROSS SEVEN SEAS  ·  A Game Off 2026 entry  ·  Thank you for playing", 16, UIKit.Muted, TextAnchor.MiddleCenter);
        UIKit.Place(credit.rectTransform, 0.5f, 0f, 0.5f, 0f, 0, 20, 1600, 26);
    }

    public IEnumerator PlayEnding(string text, string stats, string nextLabel = null, System.Action next = null)
    {
        endNextAction = next;
        endNext.gameObject.SetActive(next != null);
        if (nextLabel != null) UIKit.SetLabel(endNext, UIKit.Spaced(nextLabel));
        EndingOpen = true;
        var parch = ending.transform.Find("Parch").GetComponent<RawImage>();
        parch.texture = Mats.Parchment;
        endText.text = "";
        endStats.text = "";
        Show(ending, true);
        yield return new WaitForSeconds(0.8f);
        // draw an unknown passage across the parchment
        var pts = new List<Vector2>();
        var rng = new System.Random(77);
        Vector2 p = new Vector2(70, 120);
        for (int i = 0; i < 24; i++)
        {
            pts.Add(p);
            p += new Vector2(26f + (float)rng.NextDouble() * 6f, (float)(Math.Sin(i * 0.55) * 22.0) + 7f);
        }
        for (int i = 0; i < 6; i++)
        {
            var isl = UIKit.Img(endRoute, "Isle", ProcTex.Circle, new Color(0.55f, 0.42f, 0.25f, 0.8f));
            UIKit.Place(isl.rectTransform, 0f, 0f, 0.5f, 0.5f, 90 + i * 110 + (float)rng.NextDouble() * 30f, 100 + (float)rng.NextDouble() * 300f, 30 + (float)rng.NextDouble() * 50f, 20 + (float)rng.NextDouble() * 30f);
        }
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 a = pts[i], c = pts[i + 1];
            var seg = UIKit.Img(endRoute, "Seg", ProcTex.Square, new Color(0.85f, 0.45f, 0.08f, 1f));
            var rt = seg.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = a;
            rt.sizeDelta = new Vector2((c - a).magnitude * 0.7f, 4);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(c.y - a.y, c.x - a.x) * Mathf.Rad2Deg);
            AudioMan.I.Play("click", 0.15f, 1.5f);
            yield return new WaitForSeconds(0.08f);
        }
        var star = UIKit.Img(endRoute, "End", ProcTex.Diamond, new Color(0.8f, 0.2f, 0.05f));
        UIKit.Place(star.rectTransform, 0f, 0f, 0.5f, 0.5f, pts[pts.Count - 1].x, pts[pts.Count - 1].y, 36, 36);
        var glow = UIKit.Img(endRoute, "Glow", ProcTex.Glow, new Color(1f, 0.6f, 0.2f, 0.6f));
        UIKit.Place(glow.rectTransform, 0f, 0f, 0.5f, 0.5f, pts[pts.Count - 1].x, pts[pts.Count - 1].y, 140, 140);
        AudioMan.I.Play("discovery", 0.9f);
        for (int i = 0; i <= text.Length; i++)
        {
            endText.text = text.Substring(0, i);
            if (In.LMBDown || In.Down(Key.Enter)) { endText.text = text; break; }
            yield return new WaitForSeconds(0.022f);
        }
        yield return new WaitForSeconds(0.6f);
        endStats.text = stats;
        AudioMan.I.Play("gong", 0.6f);
    }

    // ------------------------------------------------------------------ API
    float hudTarget, hudSpeed = 2f;
    public bool HudShowing => hud.gameObject.activeInHierarchy && hudGroup.alpha > 0.5f;
    public void SetHudVisible(bool on, float speed = 2f) { hudTarget = on ? 1f : 0f; hudSpeed = speed; hud.gameObject.SetActive(true); }

    public void Letterbox(bool on) => letterboxTarget = on ? 1f : 0f;

    public void FadeTo(float a, float seconds) { fadeTarget = a; fadeSpeed = 1f / Mathf.Max(0.01f, seconds); }

    public void SetObjective(string text)
    {
        if (objText.text == text && objPanel.gameObject.activeSelf == !string.IsNullOrEmpty(text)) return;
        objText.text = text;
        objPanel.gameObject.SetActive(!string.IsNullOrEmpty(text));
        if (!string.IsNullOrEmpty(text)) StartCoroutine(Pulse(objPanel));
    }

    IEnumerator Pulse(RectTransform rt)
    {
        float t = 0;
        while (t < 0.5f) { t += Time.deltaTime; rt.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t / 0.5f * Mathf.PI)); yield return null; }
        rt.localScale = Vector3.one;
    }

    public void Banner(string t, string sub, float dur = 5f)
    {
        bannerTitle.text = UIKit.Spaced(t);
        bannerSub.text = sub;
        bannerTimer = dur;
        if (AudioMan.I != null) AudioMan.I.Play("stinger", 0.55f);
    }

    public void Toast(string msg, Color c)
    {
        var t = UIKit.Txt(toastRoot, "Toast", msg, 22, c, TextAnchor.MiddleRight, FontStyle.Bold);
        UIKit.Place(t.rectTransform, 1f, 1f, 1f, 1f, 0, 0, 600, 30);
        toasts.Insert(0, (t, 0f));
        if (toasts.Count > 5) { Destroy(toasts[toasts.Count - 1].t.gameObject); toasts.RemoveAt(toasts.Count - 1); }
    }

    public void Hint(string s, float dur = 6f) { hintText.text = s; hintTimer = dur; }

    public void Prompt(string s)
    {
        if (string.IsNullOrEmpty(s)) { promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, 0, Time.deltaTime * 4f); return; }
        promptText.text = s;
        promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, 1, Time.deltaTime * 4f);
    }

    public void Say(string speaker, string text, float dur = -1f)
    {
        if (dur < 0) dur = Mathf.Max(2.6f, text.Length * 0.056f + 1.3f);
        lines.Enqueue((speaker, text, dur));
    }

    public void ClearDialogue() { lines.Clear(); lineActive = false; }

    // ------------------------------------------------------------------ per-frame
    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        // fade
        var fc = fade.color;
        fc.a = Mathf.MoveTowards(fc.a, fadeTarget, dt * fadeSpeed);
        fade.color = fc;
        fade.raycastTarget = fc.a > 0.5f;
        hudGroup.alpha = Mathf.MoveTowards(hudGroup.alpha, hudTarget, dt * hudSpeed);
        // letterbox
        letterbox = Mathf.MoveTowards(letterbox, letterboxTarget, dt * 1.2f);
        float lb = letterbox * letterbox * (3f - 2f * letterbox) * 120f;
        lbTop.sizeDelta = new Vector2(4000, lb);
        lbBottom.sizeDelta = new Vector2(4000, lb);
        // banner
        if (bannerTimer > 0)
        {
            bannerTimer -= dt;
            float a = Mathf.Clamp01(Mathf.Min(bannerTimer, 1f)) * Mathf.Clamp01((5.2f - bannerTimer) * 2f);
            bannerGroup.alpha = Mathf.MoveTowards(bannerGroup.alpha, bannerTimer > 0.8f ? 1f : 0f, dt * 2.5f);
            banner.localScale = Vector3.one * (1f + 0.03f * (1f - a));
        }
        else bannerGroup.alpha = Mathf.MoveTowards(bannerGroup.alpha, 0, dt * 2f);
        // toasts
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            var t = toasts[i];
            t.time += dt;
            toasts[i] = t;
            t.t.rectTransform.anchoredPosition = Vector2.Lerp(t.t.rectTransform.anchoredPosition, new Vector2(0, -i * 34f), dt * 10f);
            SetA(t.t, Mathf.Clamp01(4f - t.time));
            if (t.time > 4f) { Destroy(t.t.gameObject); toasts.RemoveAt(i); }
        }
        // hint
        hintTimer -= dt;
        SetA(hintText, ModalOpen ? 0f : Mathf.Clamp01(hintTimer));
        // dialogue
        UpdateDialogue(Time.deltaTime);
        readingText.text = Reading;
        if (In.Down(Key.F3)) showFps = !showFps;
        fpsAcc = Mathf.Lerp(fpsAcc, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);
        fpsText.text = showFps ? Mathf.RoundToInt(fpsAcc) + " FPS" : "";

        if (Game.I == null) return;
        UpdateHud(dt);
    }

    void UpdateDialogue(float dt)
    {
        if (!lineActive && lines.Count > 0)
        {
            var l = lines.Dequeue();
            curSpeaker = l.sp; curText = l.tx; curDur = l.dur; curT = 0; lineActive = true;
            dlgSpeaker.text = UIKit.Spaced(curSpeaker.ToUpperInvariant());
            dlgSpeaker.color = curSpeaker.Contains("echo") ? UIKit.Teal : UIKit.Gold;
            dlgBody.fontStyle = curSpeaker.Contains("echo") ? FontStyle.Italic : FontStyle.Normal;
        }
        if (lineActive)
        {
            curT += dt;
            int n = Mathf.Min(curText.Length, Mathf.FloorToInt(curT * 55f));
            dlgBody.text = curText.Substring(0, n);
            dialogueGroup.alpha = Mathf.MoveTowards(dialogueGroup.alpha, 1, dt * 5f);
            if (In.Down(Key.Enter) || In.Down(Key.NumpadEnter))
            {
                if (n < curText.Length) curT = curText.Length / 55f; else curT = curDur;
            }
            if (curT >= curDur) lineActive = false;
        }
        else dialogueGroup.alpha = Mathf.MoveTowards(dialogueGroup.alpha, 0, dt * 3f);
    }

    void UpdateHud(float dt)
    {
        var g = Game.I;
        var ship = g.Player;
        var cam = CameraRig.I;
        if (ship == null || cam == null) return;
        // compass
        float yaw = cam.Yaw;
        foreach (var (t, ang) in compassTicks)
        {
            float d = Mathf.DeltaAngle(yaw, ang);
            float x = d * 4.4f;
            t.rectTransform.anchoredPosition = new Vector2(x, 2);
            SetA(t, Mathf.Clamp01(1f - Mathf.Abs(x) / 320f) * 1.2f);
        }
        if (MarkerTarget.HasValue)
        {
            Vector3 to = MarkerTarget.Value - ship.transform.position;
            float b = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float x = Mathf.Clamp(Mathf.DeltaAngle(yaw, b) * 4.4f, -310f, 310f);
            compassObj.enabled = true;
            compassObj.rectTransform.anchoredPosition = new Vector2(x, 6);
        }
        else compassObj.enabled = false;
        if (EnemyTarget != null && !EnemyTarget.Dead && EnemyTarget.gameObject.activeInHierarchy)
        {
            Vector3 to = EnemyTarget.transform.position - ship.transform.position;
            float b = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            compassEnemy.enabled = true;
            compassEnemy.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(Mathf.DeltaAngle(yaw, b) * 4.4f, -310f, 310f), 6);
        }
        else compassEnemy.enabled = false;
        compassShip.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(Mathf.DeltaAngle(yaw, ship.Heading) * 4.4f, -310f, 310f), 6);
        warnText.text = g.ShoreWarning ? "SHALLOW WATER AHEAD!  Turn away (A / D)  or slow down (S)" : "";
        if (g.ShoreWarning) SetA(warnText, 0.6f + 0.4f * Mathf.Sin(Time.time * 8f));
        autoText.text = g.AutoSteer && g.State == Game.GameState.Sailing ? "AUTO-STEER ON  ·  press T to steer yourself" : "";

        // wind dial
        float rel = ship.WindAngle;
        windArrow.localRotation = Quaternion.Euler(0, 0, -rel);
        northMark.localRotation = Quaternion.Euler(0, 0, ship.Heading);
        northMark.GetChild(0).localRotation = Quaternion.Euler(0, 0, -ship.Heading);
        pointOfSail.text = Ship.PointOfSail(rel) + "  " + Mathf.RoundToInt(ship.WindEff * 100f) + "%";
        pointOfSail.color = Color.Lerp(UIKit.Red, UIKit.Teal, ship.WindEff);
        speedText.text = (ship.Speed * 1.94f).ToString("0.0") + " <size=20>KNOTS</size>";
        for (int i = 0; i < 3; i++) sailPips[i].color = i < ship.SailLevel ? UIKit.Gold : new Color(1, 1, 1, 0.12f);
        float hp = ship.HP / ship.MaxHP;
        hullFill.fillAmount = Mathf.Lerp(hullFill.fillAmount, hp, dt * 6f);
        hullFill.color = Color.Lerp(UIKit.Red, new Color(0.9f, 0.75f, 0.45f), hp * 1.4f - 0.2f);
        hullText.text = Mathf.CeilToInt(ship.HP).ToString();

        // era
        var era = EraShift.I;
        eraFill.fillAmount = era.Energy;
        eraFill.color = era.Unlocked ? (era.Active ? new Color(1f, 0.8f, 0.4f) : UIKit.Teal) : new Color(1, 1, 1, 0.15f);
        eraLabel.color = era.Unlocked ? UIKit.Parch : UIKit.Muted;
        eraSub.text = !era.Unlocked ? "LOCKED" : era.Active ? UIKit.Spaced("ERA SHIFT") : (era.Energy > 0.12f ? "READY" : "RECOVERING");
        var gc = eraGlow.color;
        gc.a = era.Unlocked ? (era.Active ? 0.35f + 0.15f * Mathf.Sin(Time.time * 4f) : g.EchoNearby ? 0.2f + 0.2f * Mathf.Sin(Time.time * 6f) : 0f) : 0f;
        eraGlow.color = gc;

        goldText.text = g.Market.Gold + " <size=18>GOLD</size>";
        cargoText.text = "Cargo  " + g.Market.Cargo + " / " + g.Market.Capacity;

        if (MarkerTarget.HasValue)
        {
            float dist = Vector3.Distance(new Vector3(ship.transform.position.x, 0, ship.transform.position.z), new Vector3(MarkerTarget.Value.x, 0, MarkerTarget.Value.z));
            objDist.text = MarkerLabel + (dist > 1000 ? (dist / 1000f).ToString("0.0") + " km" : Mathf.RoundToInt(dist) + " m");
            PlaceOnScreen(marker, MarkerTarget.Value + Vector3.up * 8f, true, out bool _);
            markerText.text = dist > 1000 ? (dist / 1000f).ToString("0.0") + " km" : Mathf.RoundToInt(dist) + " m";
            marker.gameObject.SetActive(!ModalOpen);
        }
        else { objDist.text = ""; marker.gameObject.SetActive(false); }

        if (EnemyTarget != null && !EnemyTarget.Dead && EnemyTarget.gameObject.activeInHierarchy)
        {
            bool vis = PlaceOnScreen(enemyBar, EnemyTarget.transform.position + Vector3.up * 17f, false, out bool front);
            enemyBar.gameObject.SetActive(vis && front);
            enemyFill.fillAmount = EnemyTarget.HP / EnemyTarget.MaxHP;
            enemyName.text = EnemyName;
        }
        else enemyBar.gameObject.SetActive(false);

        if (ShowReticle)
        {
            reticle.gameObject.SetActive(PlaceOnScreen(reticle, ReticleWorld, false, out bool f2) && f2);
            reticleFill.fillAmount = ReloadFrac;
            reticleRing.color = ReloadFrac >= 1f ? new Color(1f, 0.8f, 0.4f, 0.9f) : new Color(1, 1, 1, 0.3f);
        }
        else reticle.gameObject.SetActive(false);

        stealthGroup.alpha = Mathf.MoveTowards(stealthGroup.alpha, ShowStealth && !ModalOpen ? 1f : 0f, dt * 3f);
        if (ShowStealth && Stealth.I != null)
        {
            var st = Stealth.I;
            stealthFill.fillAmount = st.Detection;
            stealthFill.color = Color.Lerp(UIKit.Gold, UIKit.Red, st.Detection);
            stealthLabel.text = st.Spotted ? "SPOTTED!" : st.InLight ? "IN THE LIGHT OF A " + st.Source.ToUpperInvariant() + "  ·  GET OUT!" : (st.Detection > 0.05f ? "THEY ARE LOOKING..." : "UNSEEN");
            stealthLabel.color = st.Spotted || st.InLight ? UIKit.Red : UIKit.Teal;
            lanternLabel.text = ship.LanternOn ? "Your lantern is LIT  ·  press L to douse it" : "Lantern dark  ·  you are hard to see";
            lanternLabel.color = ship.LanternOn ? UIKit.Gold : UIKit.Parch;
        }
        clickToSteer.gameObject.SetActive(g.State == Game.GameState.Sailing && !ModalOpen && Cursor.lockState != CursorLockMode.Locked && g.WantsLock && !g.BotControl);
    }

    bool PlaceOnScreen(RectTransform rt, Vector3 world, bool clampEdges, out bool inFront)
    {
        var cam = CameraRig.I.Cam;
        Vector3 sp = cam.WorldToScreenPoint(world);
        inFront = sp.z > 0;
        if (!inFront) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }
        bool on = inFront && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;
        if (clampEdges)
        {
            float m = 60f;
            if (!inFront) sp.y = m;
            sp.x = Mathf.Clamp(sp.x, m, Screen.width - m);
            sp.y = Mathf.Clamp(sp.y, m + 120f, Screen.height - m - 80f);
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out Vector2 local);
        rt.anchoredPosition = local;
        return on || clampEdges;
    }

    public void UpdateChart()
    {
        if (!ChartOpen || Game.I == null || Game.I.Player == null) return;
        var s = Game.I.Player;
        chartShip.anchoredPosition = ChartPos(s.transform.position);
        chartShip.localRotation = Quaternion.Euler(0, 0, -s.Heading);
        if (MarkerTarget.HasValue) { chartObj.gameObject.SetActive(true); chartObj.anchoredPosition = ChartPos(MarkerTarget.Value); }
        else chartObj.gameObject.SetActive(false);
        foreach (var (t, key) in chartLabels) t.gameObject.SetActive(KnownPlaces.Contains(key));
    }

    void LateUpdate() => UpdateChart();
}
