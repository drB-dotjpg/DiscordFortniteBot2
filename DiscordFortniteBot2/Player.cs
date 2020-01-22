using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
    public class Player
    {
        public SocketUser discordUser { get; }
        public int health { get; set; }
        public Item[] inventory { get; set; } = new Item[5];

        public Player(SocketUser discordUser)
        {
            this.discordUser = discordUser;
            health = 100;

            for (int i = 0; i < inventory.Length; i++) inventory[i] = new Item();
        }
    }
}
