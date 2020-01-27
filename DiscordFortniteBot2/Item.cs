namespace DiscordFortniteBot2
{
    public class Item
    {
        public string name { get; }
        public ItemType type { get; }
        public Range range { get; }
        public int effectVal { get; } //damage done / healing applied based on the type of weapon.
        public int ammo { get; set; }

        public Item()
        {
            type = ItemType.Empty;
            ammo = 0;
        }

        public Item(string name, ItemType type, Range range, int effectVal, int ammo)
        {
            this.name = name;
            this.type = type;
            this.range = range;
            this.effectVal = effectVal;
            this.ammo = ammo;
        }
    }
}
