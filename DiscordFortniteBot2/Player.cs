using Discord;
using Discord.WebSocket;
using Discord.Rest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordFortniteBot2
{
    public class Player
    {
        public SocketUser discordUser { get; }
        public int health { get; set; }
        public int shield { get; set; }
        public Item[] inventory { get; set; } = new Item[5];
        public int equipped { get; set; }
        public int materials { get; set; } //building materials
        public int x { get; set; }
        public int y { get; set; }
        public Emoji icon { get; }
        public List<RestUserMessage> currentMessages { get; set; }

        public Action turnAction { get; set; }
        public Direction turnDirection { get; set; }
        public int turnIndex { get; set; }
        public bool sprinting { get; set; }

        public RestUserMessage turnMessage { get; set; }

        public Player(SocketUser discordUser, Emoji icon)
        {
            this.discordUser = discordUser;
            this.icon = icon;

            health = 100;
            shield = 0;
            equipped = 0;

            x = 20; //TODO: Remove this because it is temporary.
            y = 20;

            for (int i = 0; i < inventory.Length; i++) inventory[i] = Spawnables.GetRandomSpawnable();

            currentMessages = new List<RestUserMessage>();
        }

        public void Move(int sprintAmount, Map map)
        {
            int movementMultiplier = sprinting ? sprintAmount : 1;

            for (int i = 0; i < movementMultiplier; i++)
            {
                switch (turnDirection)
                {
                    case Direction.Right:
                        if (x < Map.mapWidth - 1
                            && map.mapGrid[x + 1, y].Type != TileType.Wall)
                            x++;
                        break;

                    case Direction.Left:
                        if (x > 0
                            && map.mapGrid[x - 1, y].Type != TileType.Wall)
                            x--;
                        break;

                    case Direction.Up:
                        if (y < Map.mapHeight - 1
                            && map.mapGrid[x, y - 1].Type != TileType.Wall)
                            y--;
                        break;

                    case Direction.Down:
                        if (y > 0
                            && map.mapGrid[x, y + 1].Type != TileType.Wall)
                            y++;
                        break;
                }
            }
            sprinting = false;
        }
    }
}
