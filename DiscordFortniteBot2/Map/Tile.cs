using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
    public struct Tile //tiles represent a space on the map
    {
        public TileType Type;

        public Item[] Items { get; }

        public Trap trap;

        public Tile(TileType type)
        {
            Type = type;
            Items = new Item[5];
            for (int i = 0; i < Items.Length; i++) Items[i] = new Item();
            trap = null;
        }

        public Tile(Item[] items)
        {
            Type = TileType.Chest;
            Items = new Item[5];

            for (int i = 0; i < Items.Length; i++)
            {
                Items[i] = new Item();
            }
            for (int i = 0; i < items.Length; i++)
            {
                Items[i] = items[i];
            }

            Items = items;
            trap = null;
        }

        public bool AddChestItem(Item item) //returns true if item was added
        {
            bool added = false;
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].type == ItemType.Empty)
                {
                    added = true;
                    Items[i] = item;
                    break;
                }
            }
            if (added) Type = TileType.Chest;
            return added;
        }

        public bool IsEmpty()
        {
            foreach (Item item in Items)
            {
                if (item.type != ItemType.Empty) return false;
            }
            return true;
        }
    }
}
