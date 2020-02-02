using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
    public class Trap
    {
        public Player placedBy { get; } //The player that placed the trap

        public Item trapType { get; } //The type of trap (ex: Spike Trap)

        public Trap(Player player, Item type)
        {
            placedBy = player;
            trapType = type;
        }
    }
}
