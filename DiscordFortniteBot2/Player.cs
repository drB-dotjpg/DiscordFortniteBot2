using Discord;
using Discord.WebSocket;
using Discord.Rest;

namespace DiscordFortniteBot2
{
    public class Player
    {
        public SocketUser discordUser { get; }
        public int health { get; set; }
        public int shield { get; set; }
        public Item[] inventory { get; set; } = new Item[5];
        public int materials { get; set; } //building materials
        public int x { get; set; }
        public int y { get; set; }
        public Emoji icon { get; }
        public RestUserMessage localMap { get; set; }

        public Player(SocketUser discordUser, Emoji icon)
        {
            this.discordUser = discordUser;
            this.icon = icon;

            health = 100;
            shield = 0;

            x = 20; //TODO: Remove this because it is temporary.
            y = 20;

            for (int i = 0; i < inventory.Length; i++) inventory[i] = new Item();
        }
    }
}
