using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
    public class Item
    {
        public Data.ItemType type { get; }
        public Data.Range range { get; }
        public int effectVal { get; } //damage done / healing applied based on the type of weapon. TODO: Get better variable name
        public int ammo { get; set; }

        public Item() => type = Data.ItemType.Empty;

        public Item(Data.ItemType type, Data.Range range, int effectVal, int ammo)
        {
            this.type = type;
            this.range = range;
            this.effectVal = effectVal;
            this.ammo = ammo;
        }
    }
}
