using System.Collections.Generic;
using UnityEngine;

public class Commodity
{
    public string Name, Local, Desc;
    public Color Col;
    public float BuyPrice, SellPrice;
    public int Held;
    public float Paid;
    public int WestPrice;
}

/// <summary>Cargo hold and a small dynamic economy: buying raises prices, selling lowers them.</summary>
public class Market
{
    public int Gold = 160;
    public int Capacity = 20;
    public readonly List<Commodity> Items = new List<Commodity>();
    public float Spent, Earned;
    public string PortName = "";
    public string PortNote = "";
    public bool CanBuy = true, CanSell = true;
    public string WestLabel = "in the west";

    public Market()
    {
        Items.Add(new Commodity { Name = "Black Pepper", Local = "kurumulaku", Desc = "The black gold of Malabar. Every kitchen from Alexandria to Rome craves it.", WestPrice = 31, Col = new Color(0.2f, 0.17f, 0.14f) });
        Items.Add(new Commodity { Name = "Cardamom", Local = "elakka", Desc = "Green pods from the misty Western Ghats. Light, fragrant, and costly.", WestPrice = 42, Col = new Color(0.45f, 0.6f, 0.28f) });
        Items.Add(new Commodity { Name = "Cinnamon", Local = "karuva", Desc = "Rolled bark bound for the perfumers and physicians of the west.", WestPrice = 27, Col = new Color(0.6f, 0.34f, 0.16f) });
    }

    public int Cargo { get { int c = 0; foreach (var i in Items) c += i.Held; return c; } }

    public void SetMuziris()
    {
        PortName = "MUZIRIS MARKET";
        PortNote = Game.I != null && Game.I.Tutorial ? "BEST: buy CARDAMOM first (biggest profit), then PEPPER until your gold or hold runs out. Then DONE." : "Spices are cheap at the source. Fill the hold — the west pays in silver.";
        CanBuy = true; CanSell = true;
        Items[0].BuyPrice = 9; Items[1].BuyPrice = 16; Items[2].BuyPrice = 12;
        foreach (var i in Items) i.SellPrice = Mathf.Round(i.BuyPrice * 0.8f);
    }

    public void SetOutpost()
    {
        PortName = "RAS AL-MIRA";
        PortNote = Game.I != null && Game.I.Tutorial ? "TRADE SHIFT: prices have soared. BEST: press SELL ALL on every spice, then DONE." : "TRADE SHIFT: the storm closed the northern routes. Spice prices have soared.";
        CanBuy = false; CanSell = true;
        Items[0].SellPrice = 31; Items[1].SellPrice = 42; Items[2].SellPrice = 27;
    }

    public void UseIncenseGoods()
    {
        Items.Clear();
        Items.Add(new Commodity { Name = "Frankincense", Local = "luban", Desc = "Pale resin of the Arabian hills. Every temple from Aden to Rome burns it.", Col = new Color(0.86f, 0.78f, 0.55f), WestPrice = 31 });
        Items.Add(new Commodity { Name = "Myrrh", Local = "murr", Desc = "Dark, bitter and precious: perfume, medicine and offering.", Col = new Color(0.55f, 0.25f, 0.12f), WestPrice = 38 });
        Items.Add(new Commodity { Name = "Dates", Local = "tamr", Desc = "Sweet and cheap. Sailors trade them everywhere.", Col = new Color(0.45f, 0.22f, 0.1f), WestPrice = 13 });
    }

    public void SetRasAlMira2()
    {
        PortName = "RAS AL-MIRA";
        WestLabel = "in Aden";
        PortNote = "BEST: buy MYRRH and FRANKINCENSE (the biggest profit in Aden), then DATES with what is left. Then DONE.";
        CanBuy = true; CanSell = true;
        Items[0].BuyPrice = 12; Items[1].BuyPrice = 15; Items[2].BuyPrice = 5;
        foreach (var i in Items) i.SellPrice = Mathf.Round(i.BuyPrice * 0.8f);
    }

    public void SetAden()
    {
        PortName = "ADEN";
        PortNote = "Aden's temples burn incense day and night. BEST: press SELL ALL on every good, then DONE.";
        CanBuy = false; CanSell = true;
        Items[0].SellPrice = 31; Items[1].SellPrice = 38; Items[2].SellPrice = 13;
    }

    public int BuyCost(int idx) => Mathf.RoundToInt(Items[idx].BuyPrice);
    public int SellValue(int idx) => Mathf.RoundToInt(Items[idx].SellPrice);

    public bool Buy(int idx)
    {
        var c = Items[idx];
        int cost = BuyCost(idx);
        if (!CanBuy || Gold < cost || Cargo >= Capacity) return false;
        Gold -= cost;
        Spent += cost;
        c.Held++;
        c.Paid += cost;
        c.BuyPrice *= 1.045f;
        c.SellPrice = Mathf.Min(c.SellPrice * 1.02f, c.BuyPrice * 0.85f);
        return true;
    }

    public bool Sell(int idx)
    {
        var c = Items[idx];
        if (!CanSell || c.Held <= 0) return false;
        int v = SellValue(idx);
        Gold += v;
        Earned += v;
        if (c.Held > 0) c.Paid -= c.Paid / c.Held;
        c.Held--;
        c.SellPrice *= 0.96f;
        return true;
    }
}
