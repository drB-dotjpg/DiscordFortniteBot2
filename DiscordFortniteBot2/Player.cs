using Discord.WebSocket;

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

        public Player(SocketUser discordUser)
        {
            this.discordUser = discordUser;
            health = 100;
            shield = 0;

            for (int i = 0; i < inventory.Length; i++) inventory[i] = new Item();
        }
    }
}
