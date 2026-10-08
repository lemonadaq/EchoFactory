using System;

namespace EchoFactory.Core
{
    // Prices move with supply: selling floods the market (cheaper), buying drains it (dearer).
    // Supply is stored per resource in StrategicState.MarketSupply and drifts back to 0 every turn.
    public static class MarketSystem
    {
        public const int MaxSupply = 10;
        public const int PercentPerSupply = 5;
        public const int BuySpreadPercent = 110;
        public const int SellSpreadPercent = 90;

        public static int BasePrice(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Steel: return 40;
                case ResourceKind.Energy: return 25;
                case ResourceKind.Bitumen: return 35;
                case ResourceKind.Scrap: return 10;
                case ResourceKind.Electronics: return 90;
                case ResourceKind.FinishedGoods: return 120;
                default: return 0;
            }
        }

        public static bool CanBuyKind(ResourceKind kind) { return kind == ResourceKind.Steel || kind == ResourceKind.Energy || kind == ResourceKind.Bitumen || kind == ResourceKind.Electronics; }
        public static bool CanSellKind(ResourceKind kind) { return kind == ResourceKind.FinishedGoods || kind == ResourceKind.Scrap; }

        // Mid price for a given supply level: -50%..+50% around the base price.
        static int MidPrice(ResourceKind kind, int supply)
        {
            supply = Math.Max(-MaxSupply, Math.Min(MaxSupply, supply));
            return Math.Max(1, BasePrice(kind) * (100 - supply * PercentPerSupply) / 100);
        }

        public static int BuyPrice(StrategicState state, ResourceKind kind) { return BuyPriceAt(kind, state.GetSupply(kind)); }
        public static int SellPrice(StrategicState state, ResourceKind kind) { return SellPriceAt(kind, state.GetSupply(kind)); }
        static int BuyPriceAt(ResourceKind kind, int supply) { return Math.Max(1, (MidPrice(kind, supply) * BuySpreadPercent + 99) / 100); }
        static int SellPriceAt(ResourceKind kind, int supply) { return Math.Max(1, MidPrice(kind, supply) * SellSpreadPercent / 100); }

        // Total cost of buying `amount` units (price rises after each unit). 0 if the kind cannot be bought.
        public static int BuyQuote(StrategicState state, ResourceKind kind, int amount)
        {
            if (state == null || !CanBuyKind(kind) || amount <= 0) return 0;
            int supply = state.GetSupply(kind), total = 0;
            for (int i = 0; i < amount; i++) { total += BuyPriceAt(kind, supply); supply = Math.Max(-MaxSupply, supply - 1); }
            return total;
        }

        // Total payout for selling `amount` units (price falls after each unit).
        public static int SellQuote(StrategicState state, ResourceKind kind, int amount)
        {
            if (state == null || !CanSellKind(kind) || amount <= 0) return 0;
            int supply = state.GetSupply(kind), total = 0;
            for (int i = 0; i < amount; i++) { total += SellPriceAt(kind, supply); supply = Math.Min(MaxSupply, supply + 1); }
            return total;
        }

        public static bool CanBuy(StrategicState state, ResourceKind kind, int amount)
        {
            return state != null && state.Phase == StrategicPhase.Planning && amount > 0 && CanBuyKind(kind) && state.Credits >= BuyQuote(state, kind, amount);
        }

        public static bool CanSell(StrategicState state, ResourceKind kind, int amount)
        {
            return state != null && state.Phase == StrategicPhase.Planning && amount > 0 && CanSellKind(kind) && state.GetStock(kind) >= amount;
        }

        public static bool Buy(StrategicState state, ResourceKind kind, int amount)
        {
            if (!CanBuy(state, kind, amount)) return false;
            state.Credits -= BuyQuote(state, kind, amount);
            state.AddStock(kind, amount);
            state.SetSupply(kind, state.GetSupply(kind) - amount);
            return true;
        }

        public static bool Sell(StrategicState state, ResourceKind kind, int amount)
        {
            if (!CanSell(state, kind, amount)) return false;
            state.Credits += SellQuote(state, kind, amount);
            state.AddStock(kind, -amount);
            state.SetSupply(kind, state.GetSupply(kind) + amount);
            return true;
        }

        // Called once per turn: every price level moves one step back toward its base.
        public static void Recover(StrategicState state)
        {
            if (state == null) return;
            var keys = new System.Collections.Generic.List<ResourceKind>(state.MarketSupply.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                int s = state.MarketSupply[keys[i]];
                state.SetSupply(keys[i], s > 0 ? s - 1 : s < 0 ? s + 1 : 0);
            }
        }
    }
}
