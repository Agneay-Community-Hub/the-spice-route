using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>The Captain's Ledger: skill trees, crew and stations, ship upgrades and classes (TAB).</summary>
public class Ledger : MonoBehaviour
{
    public static Ledger I;
    public static bool IsOpen;
    CanvasGroup cg;
    RectTransform panel, content;
    Text head, note;
    readonly Button[] tabs = new Button[3];
    int tab;
    bool atWright;
    string tavernPort;
    int openedFrame;
    float prevScale = 1f;
    static readonly Color Fill = new Color(0.05f, 0.06f, 0.08f, 0.94f);
    static Color Line => new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.35f);

    public static void Create()
    {
        if (I != null || HUD.I == null) return;
        var rt = UIKit.Rect(HUD.I.transform, "Ledger");
        UIKit.Stretch(rt);
        I = rt.gameObject.AddComponent<Ledger>();
        I.Build(rt);
    }

    void Build(RectTransform rt)
    {
        cg = rt.gameObject.AddComponent<CanvasGroup>();
        var bg = UIKit.Img(rt, "Bg", ProcTex.Square, new Color(0, 0, 0, 0.62f));
        UIKit.Stretch(bg.rectTransform);
        bg.raycastTarget = true;
        panel = UIKit.Panel(rt, "Panel", Fill, new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.5f));
        UIKit.Place(panel, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1600, 940);
        var t = UIKit.Txt(panel, "Title", UIKit.Spaced("CAPTAIN'S LEDGER"), 34, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(t.rectTransform, 0f, 1f, 0f, 1f, 40, -22, 700, 50);
        head = UIKit.Txt(panel, "Head", "", 22, UIKit.Parch, TextAnchor.MiddleRight, FontStyle.Bold);
        UIKit.Place(head.rectTransform, 1f, 1f, 1f, 1f, -230, -26, 760, 40);
        var close = UIKit.Btn(panel, UIKit.Spaced("CLOSE"), 18, Close);
        UIKit.Place((RectTransform)close.transform, 1f, 1f, 1f, 1f, -36, -22, 170, 48);
        string[] names = { "SKILLS", "CREW", "SHIP" };
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            tabs[i] = UIKit.Btn(panel, UIKit.Spaced(names[i]), 20, () => { tab = k; Refresh(); });
            UIKit.Place((RectTransform)tabs[i].transform, 0f, 1f, 0f, 1f, 40 + i * 230, -86, 215, 50);
        }
        note = UIKit.Txt(panel, "Note", "", 19, UIKit.Teal, TextAnchor.MiddleLeft, FontStyle.Italic);
        UIKit.Place(note.rectTransform, 0f, 1f, 0f, 1f, 750, -92, 810, 40);
        content = UIKit.Rect(panel, "Content");
        UIKit.Place(content, 0f, 1f, 0f, 1f, 0, -150, 1600, 790);
        cg.alpha = 0; cg.blocksRaycasts = false; cg.interactable = false;
        rt.gameObject.SetActive(false);
    }

    public void Open(int startTab, bool shipwright = false, string tavern = null)
    {
        if (IsOpen) return;
        tab = startTab; atWright = shipwright; tavernPort = tavern;
        IsOpen = true;
        openedFrame = Time.frameCount;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        cg.alpha = 1; cg.blocksRaycasts = true; cg.interactable = true;
        prevScale = Time.timeScale > 0 ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        cg.alpha = 0; cg.blocksRaycasts = false; cg.interactable = false;
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        Progress.Save();
    }

    void Update()
    {
        if (IsOpen && Time.frameCount != openedFrame && (In.Down(Key.Escape) || In.Down(Key.Tab))) Close();
    }

    // ------------------------------------------------------------ helpers
    Text T(Transform p, string s, int size, Color c, float x, float y, float w, float h, TextAnchor a = TextAnchor.UpperLeft, FontStyle st = FontStyle.Normal)
    {
        var t = UIKit.Txt(p, "t", s, size, c, a, st);
        UIKit.Place(t.rectTransform, 0f, 1f, 0f, 1f, x, y, w, h);
        return t;
    }

    RectTransform Card(float x, float y, float w, float h, bool bright = false)
    {
        var r = UIKit.Panel(content, "Card", new Color(0.08f, 0.09f, 0.12f, bright ? 0.98f : 0.8f), bright ? new Color(UIKit.Teal.r, UIKit.Teal.g, UIKit.Teal.b, 0.6f) : Line);
        UIKit.Place(r, 0f, 1f, 0f, 1f, x, y, w, h);
        return r;
    }

    Button B(Transform p, string label, float x, float y, float w, float h, System.Action act, bool enabled = true, int size = 17)
    {
        var b = UIKit.Btn(p, label, size, () => { act(); Refresh(); });
        UIKit.Place((RectTransform)b.transform, 0f, 1f, 0f, 1f, x, y, w, h);
        b.interactable = enabled;
        if (!enabled) b.GetComponentInChildren<Text>().color = UIKit.Muted;
        return b;
    }

    void Bar(Transform p, float x, float y, float w, float frac, Color c)
    {
        var bg = UIKit.Img(p, "BarBg", ProcTex.Square, new Color(1, 1, 1, 0.12f));
        UIKit.Place(bg.rectTransform, 0f, 1f, 0f, 1f, x, y, w, 8);
        var f = UIKit.Img(p, "BarFill", ProcTex.Square, c);
        UIKit.Place(f.rectTransform, 0f, 1f, 0f, 1f, x, y, Mathf.Max(2f, w * Mathf.Clamp01(frac)), 8);
    }

    static string Stars(int n) => n <= 0 ? "-" : new string('*', n);

    public void Refresh()
    {
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
        var d = Progress.D;
        var m = Game.I.Market;
        head.text = m.Gold + " GOLD      " + d.Points + " SKILL POINT" + (d.Points == 1 ? "" : "S");
        for (int i = 0; i < 3; i++) tabs[i].GetComponentInChildren<Text>().color = i == tab ? UIKit.Teal : UIKit.Gold;
        if (tab == 0) Skills(); else if (tab == 1) Crew(); else ShipTab();
    }

    // ------------------------------------------------------------ skills
    void Skills()
    {
        var d = Progress.D;
        note.text = d.Points > 0 ? "You have points to spend. Click LEARN on any skill that is not locked." : "Earn skill points by finishing story steps, sinking enemies and reading inscriptions on land.";
        for (int tr = 0; tr < 3; tr++)
        {
            float x = 40 + tr * 510;
            T(content, UIKit.Spaced(Progress.TreeNames[tr]), 24, UIKit.Gold, x, -6, 480, 36, TextAnchor.MiddleLeft, FontStyle.Bold);
            string[] blurb = { "sail faster, read the weather, master the compass", "better prices, a bigger hold, cheaper shipwrights", "more arrows, faster volleys, a tougher hull" };
            T(content, blurb[tr], 16, UIKit.Muted, x, -40, 480, 26, TextAnchor.MiddleLeft, FontStyle.Italic);
            for (int k = 0; k < 4; k++)
            {
                int id = tr * 4 + k;
                bool has = d.Skills[id], can = Progress.CanLearn(id);
                bool locked = !has && k > 0 && !d.Skills[id - 1];
                var c = Card(x, -76 - k * 172, 480, 160, can);
                T(c, Progress.SkillNames[id], 23, has ? UIKit.Teal : UIKit.Parch, 20, -14, 440, 32, TextAnchor.MiddleLeft, FontStyle.Bold);
                T(c, Progress.SkillDesc[id], 18, UIKit.Parch, 20, -50, 440, 52);
                string label = has ? "LEARNED" : locked ? "LOCKED  ·  learn the one above first" : "LEARN  ·  " + Progress.SkillCost[k] + (Progress.SkillCost[k] == 1 ? " point" : " points");
                B(c, label, 20, -106, 440, 42, () => Progress.Learn(id), can);
            }
        }
    }

    // ------------------------------------------------------------ crew
    void Crew()
    {
        var d = Progress.D;
        bool tav = tavernPort != null;
        note.text = tav ? "You are at the tavern: hire crew, pay wages, or throw a feast." : "Click a station to move a sailor. Visit a tavern on land (press F at a port) to hire and to pay wages.";
        T(content, UIKit.Spaced("YOUR CREW") + "    " + d.Crew.Count + " / 6", 22, UIKit.Gold, 40, -6, 600, 34, TextAnchor.MiddleLeft, FontStyle.Bold);
        T(content, "A sailor helps only at a station they are skilled in (stars). Loyal sailors work harder; sailors with no loyalty leave.", 16, UIKit.Muted, 40, -40, 960, 26, TextAnchor.MiddleLeft, FontStyle.Italic);
        for (int i = 0; i < d.Crew.Count; i++)
        {
            var cm = d.Crew[i];
            var c = Card(40, -76 - i * 116, 960, 106);
            T(c, cm.Name, 23, UIKit.Parch, 20, -10, 300, 32, TextAnchor.MiddleLeft, FontStyle.Bold);
            T(c, "of " + cm.Origin + "  ·  wage " + cm.Wage + " g", 16, UIKit.Muted, 20, -42, 320, 24);
            T(c, cm.Trait, 16, UIKit.Parch, 20, -68, 560, 30, TextAnchor.UpperLeft, FontStyle.Italic);
            Color lc = cm.Loyalty > 60 ? UIKit.Teal : cm.Loyalty > 25 ? UIKit.Gold : UIKit.Red;
            T(c, "LOYALTY  " + Mathf.RoundToInt(cm.Loyalty) + (cm.Loyalty > 60 ? "  ·  devoted" : cm.Loyalty > 25 ? "  ·  steady" : "  ·  about to leave!"), 16, lc, 350, -12, 280, 24, TextAnchor.MiddleLeft, FontStyle.Bold);
            Bar(c, 350, -42, 230, cm.Loyalty / 100f, lc);
            string st = cm.Station < 0 ? "RESTING" : Progress.StationNames[cm.Station] + "  " + Stars(cm.Skill[cm.Station]);
            B(c, st + "   >", 640, -12, 300, 44, () => Progress.CycleStation(cm));
            string best = "";
            for (int s = 0; s < 6; s++) if (cm.Skill[s] > 0) best += (best.Length > 0 ? "   " : "") + Progress.StationNames[s].ToLower() + " " + Stars(cm.Skill[s]);
            T(c, best, 15, UIKit.Muted, 640, -64, 310, 36);
        }
        // right column: stations summary and the tavern
        float x = 1030;
        T(content, UIKit.Spaced("STATIONS"), 22, UIKit.Gold, x, -6, 520, 34, TextAnchor.MiddleLeft, FontStyle.Bold);
        for (int s = 0; s < 6; s++)
        {
            var h = Progress.StationHolder(s);
            T(content, Progress.StationNames[s] + "  ·  " + Progress.StationDesc[s], 16, UIKit.Parch, x, -46 - s * 28, 330, 26, TextAnchor.MiddleLeft);
            T(content, h == null ? "empty" : h.Name + " " + Stars(h.Skill[s]), 16, h == null ? UIKit.Muted : (h.Skill[s] > 0 ? UIKit.Teal : UIKit.Red), x + 335, -46 - s * 28, 200, 26, TextAnchor.MiddleLeft, FontStyle.Bold);
        }
        float y = -232;
        T(content, UIKit.Spaced("TAVERN"), 22, UIKit.Gold, x, y, 520, 34, TextAnchor.MiddleLeft, FontStyle.Bold);
        if (!tav)
        {
            T(content, "Go ashore at a port and walk to the tavern (gold beam) to hire sailors, pay wages and feast the crew.", 17, UIKit.Muted, x, y - 40, 520, 80, TextAnchor.UpperLeft, FontStyle.Italic);
            return;
        }
        var m = Game.I.Market;
        int bill = Progress.WageBill();
        B(content, "PAY WAGES  ·  " + bill + " g  ·  loyalty +18", x, y - 44, 530, 46, () => { if (!Progress.PayWages()) HUD.I.Toast("Not enough gold for wages.", UIKit.Red); }, m.Gold >= bill);
        B(content, "THROW A FEAST  ·  30 g  ·  loyalty +10", x, y - 98, 530, 46, () => { if (!Progress.Feast()) HUD.I.Toast("Not enough gold.", UIKit.Red); }, m.Gold >= 30);
        var cand = Progress.Candidates(tavernPort);
        if (cand.Count == 0) T(content, "No one else here is looking for a ship.", 17, UIKit.Muted, x, y - 160, 520, 30, TextAnchor.UpperLeft, FontStyle.Italic);
        for (int i = 0; i < cand.Count && i < 3; i++)
        {
            var cm = cand[i];
            var c = Card(x, y - 156 - i * 132, 530, 124, true);
            T(c, cm.Name + "  of " + cm.Origin, 20, UIKit.Parch, 16, -8, 500, 28, TextAnchor.MiddleLeft, FontStyle.Bold);
            T(c, cm.Trait, 15, UIKit.Parch, 16, -36, 500, 24, TextAnchor.UpperLeft, FontStyle.Italic);
            string best = "";
            for (int s = 0; s < 6; s++) if (cm.Skill[s] > 0) best += (best.Length > 0 ? "   " : "") + Progress.StationNames[s].ToLower() + " " + Stars(cm.Skill[s]);
            T(c, best, 15, UIKit.Teal, 16, -60, 500, 22);
            int cost = Progress.Price(cm.HireCost);
            bool full = d.Crew.Count >= 6;
            B(c, full ? "CREW FULL" : "HIRE  ·  " + cost + " g", 16, -84, 498, 34, () => { if (!Progress.Hire(cm)) HUD.I.Toast("Not enough gold to hire " + cm.Name + ".", UIKit.Red); }, !full && m.Gold >= cost, 16);
        }
    }

    // ------------------------------------------------------------ ship
    void ShipTab()
    {
        var d = Progress.D;
        var m = Game.I.Market;
        note.text = atWright ? "You are at the shipwright: buy upgrades or change your ship's class." : "To buy, go ashore at a port (press F) and walk to the shipwright (gold beam).";
        T(content, UIKit.Spaced("UPGRADES"), 22, UIKit.Gold, 40, -6, 600, 34, TextAnchor.MiddleLeft, FontStyle.Bold);
        for (int u = 0; u < 5; u++)
        {
            int lv = d.Upgrades[u];
            var c = Card(40, -46 - u * 112, 700, 104);
            T(c, Progress.UpgradeNames[u], 22, UIKit.Parch, 20, -10, 300, 30, TextAnchor.MiddleLeft, FontStyle.Bold);
            T(c, Progress.UpgradeDesc[u], 16, UIKit.Muted, 20, -44, 420, 24);
            for (int k = 0; k < 3; k++)
            {
                var pip = UIKit.Img(c, "Pip", ProcTex.Square, k < lv ? UIKit.Teal : new Color(1, 1, 1, 0.14f));
                UIKit.Place(pip.rectTransform, 0f, 1f, 0f, 1f, 20 + k * 46, -76, 38, 10);
            }
            if (lv >= 3) T(c, "FULLY UPGRADED", 17, UIKit.Teal, 450, -32, 230, 40, TextAnchor.MiddleCenter, FontStyle.Bold);
            else
            {
                int cost = Progress.Price(Progress.UpgradeCost[lv]);
                int uu = u;
                B(c, atWright ? "BUY  ·  " + cost + " g" : cost + " g  ·  at a shipwright", 450, -28, 230, 48, () => { if (!Progress.BuyUpgrade(uu)) HUD.I.Toast("Not enough gold.", UIKit.Red); }, atWright && m.Gold >= cost, 16);
            }
        }
        T(content, UIKit.Spaced("SHIP CLASS"), 22, UIKit.Gold, 780, -6, 600, 34, TextAnchor.MiddleLeft, FontStyle.Bold);
        for (int k = 0; k < 5; k++)
        {
            bool cur = d.Class == k, own = d.ClassOwned[k];
            var c = Card(780, -46 - k * 112, 780, 104, cur);
            T(c, Progress.ClassNames[k], 22, cur ? UIKit.Teal : UIKit.Parch, 20, -10, 400, 30, TextAnchor.MiddleLeft, FontStyle.Bold);
            T(c, Progress.ClassDesc[k], 16, UIKit.Muted, 20, -42, 520, 56);
            int cost = Progress.Price(Progress.ClassCost[k]);
            int kk = k;
            if (cur) T(c, "YOUR SHIP", 17, UIKit.Teal, 560, -28, 200, 48, TextAnchor.MiddleCenter, FontStyle.Bold);
            else B(c, own ? (atWright ? "REFIT TO THIS" : "owned  ·  at a shipwright") : (atWright ? "BUY  ·  " + cost + " g" : cost + " g"), 560, -28, 200, 48, () => { if (!Progress.BuyOrSelectClass(kk)) HUD.I.Toast("Not enough gold.", UIKit.Red); }, atWright && (own || m.Gold >= cost), 16);
        }
        var s = Progress.S;
        string stats = "HULL " + Mathf.RoundToInt(s.MaxHP) + "     SPEED " + Mathf.RoundToInt(s.SpeedMul * 100f) + "%     TURNING " + Mathf.RoundToInt(s.TurnMul * 100f) + "%     CARGO " + Mathf.Max(0, s.Cargo) + "     ARROWS " + s.Volley + " x " + Mathf.RoundToInt(s.VolleyDmgMul * 100f) + "%     ERA SHIFT " + Mathf.RoundToInt(100f / s.EraDrainMul) + "%";
        T(content, stats, 19, UIKit.Gold, 40, -620, 1520, 34, TextAnchor.MiddleCenter, FontStyle.Bold);
        B(content, "RESET ALL PROGRESS", 40, -700, 260, 40, () => Progress.ResetAll(), true, 14);
        T(content, "Progress is kept between chapters and saved in this browser.", 15, UIKit.Muted, 320, -700, 700, 40, TextAnchor.MiddleLeft, FontStyle.Italic);
    }
}
