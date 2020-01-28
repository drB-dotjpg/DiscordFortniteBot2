using Discord;
using Discord.WebSocket;
using Discord.Rest;
using System.Collections.Generic;

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
        public bool ready { get; set; }

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

            x = 20; //TODO: Remove these because they are temporary.
            y = 20;
            materials = 10;

            for (int i = 0; i < inventory.Length; i++) inventory[i] = Spawnables.GetRandomSpawnable();

            ready = false;

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
                if(map.mapGrid[x,y].trap != null && map.mapGrid[x,y].trap.placedBy != this) //Check if the player has walked on another person's trap
                {
                    TakeDamage(map.mapGrid[x, y].trap.trapType.effectVal);
                    map.mapGrid[x, y].trap = null;
                }
            }

            sprinting = false;
        }

        public Map Build(Map map)
        {
            if (materials <= 0) return map;

            switch (turnDirection)
            {
                case Direction.Right:
                    if (x < Map.mapWidth - 1
                        && map.mapGrid[x + 1, y].Type != TileType.Wall)
                    {
                        materials -= 10;
                        map.mapGrid[x + 1, y] = new Map.Tile(TileType.Wall);
                    }

                    break;

                case Direction.Left:
                    if (x > 0
                        && map.mapGrid[x - 1, y].Type != TileType.Wall)
                    {
                        materials -= 10;
                        map.mapGrid[x - 1, y] = new Map.Tile(TileType.Wall);
                    }
                    break;

                case Direction.Up:
                    if (y < Map.mapHeight - 1
                        && map.mapGrid[x, y - 1].Type != TileType.Wall)
                    {
                        materials -= 10;
                        map.mapGrid[x, y - 1] = new Map.Tile(TileType.Wall);
                    }
                    break;

                case Direction.Down:
                    if (y > 0
                        && map.mapGrid[x, y + 1].Type != TileType.Wall)
                    {
                        materials -= 10;
                        map.mapGrid[x, y + 1] = new Map.Tile(TileType.Wall);
                    }
                    break;
            }

            return map;
        }

        public void Use(int slot)
        {
            Item item = inventory[slot];
            switch (item.type)
            {
                case ItemType.Health:
                    if (health + item.effectVal > 100) health = 100;
                    else health += item.effectVal;
                    break;
                case ItemType.Shield:
                    if (shield + item.effectVal > 100) shield = 100;
                    else shield += item.effectVal;
                    break;
                case ItemType.HealAll:
                    if (health + item.effectVal > 100) health = 100;
                    else health += item.effectVal;

                    if (shield + item.effectVal > 100) shield = 100;
                    else shield += item.effectVal;
                    break;
                default:
                    return;
            }
            item.ammo--;

            if (inventory[equipped].ammo <= 0)
            {
                RemoveItem(equipped);
            }
            
        }

        public bool Loot(Item newItem) //returns true if the loot was successful
        {
            bool canLoot = false;

            for (int i = 0; i < inventory.Length; i++)
            {
                if (inventory[i].type == ItemType.Empty)
                {
                    canLoot = true;
                    inventory[i] = newItem;
                    break;
                }
            }

            return canLoot;
        }

        public void PlaceTrap(Map map, int slot)
        {
            Trap trap = new Trap(this, inventory[slot]);
            switch (turnDirection)
            {
                case Direction.Right:
                    if (x < Map.mapWidth - 1
                        && map.mapGrid[x + 1, y].Type != TileType.Wall && map.mapGrid[x + 1, y].Type != TileType.Water)
                    {
                        map.mapGrid[x + 1, y].trap = trap;
                    }
                    break;

                case Direction.Left:
                    if (x > 0
                        && map.mapGrid[x - 1, y].Type != TileType.Wall && map.mapGrid[x - 1, y].Type != TileType.Water)
                    {
                        map.mapGrid[x - 1, y].trap = trap;
                    }
                    break;

                case Direction.Up:
                    if (y < Map.mapHeight - 1
                        && map.mapGrid[x, y - 1].Type != TileType.Wall && map.mapGrid[x, y - 1].Type != TileType.Water)
                    {
                        map.mapGrid[x, y - 1].trap = trap;
                    }
                    break;

                case Direction.Down:
                    if (y > 0
                        && map.mapGrid[x, y + 1].Type != TileType.Wall && map.mapGrid[x, y + 1].Type != TileType.Water)
                    {
                        map.mapGrid[x, y + 1].trap = trap;
                    }
                    break;
            }
            inventory[slot].ammo--;
            
            if(inventory[slot].ammo <= 0)
            {
                RemoveItem(equipped);
            }
        }

        public void TakeDamage(int amount)
        {
            if(amount >= shield)
            {
                amount -= shield; //If damage is greater than shield, substract shield from damage and set shield to 0
                shield = 0;
            }
            else
            {
                shield -= amount; //If damage is less than shield, don't need to go forward with the hp path so just return
                return;
            }
            health -= amount;
        }


        private void RemoveItem(int slot)
        {
            inventory[slot] = new Item();
        }
    }
}
