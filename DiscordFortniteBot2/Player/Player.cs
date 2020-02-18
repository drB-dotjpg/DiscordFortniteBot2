using Discord;
using Discord.WebSocket;
using Discord.Rest;
using System.Collections.Generic;

namespace DiscordFortniteBot2
{
    public class Player
    {
        //player user data
        public SocketUser discordUser { get; }
        public PlayerStats stats = new PlayerStats();

        //player game data
        public int health;
        public int shield;
        public Item[] inventory = new Item[5];
        public int equipped;
        public int materials; //building materials
        public int x;
        public int y;
        public Emoji icon { get; }

        //player state data
        public List<RestUserMessage> currentMessages;
        public bool ready;
        public string briefing; //briefing is edited during a turn while currentBriefing keeps its value
        public string currentBriefing;
        public int inactiveTurns;
        public Action turnAction;
        public Direction turnDirection;
        public int turnIndex;
        public int movementSpeed;
        public RestUserMessage turnMessage;

        public Player(SocketUser discordUser, Emoji icon)
        {
            this.discordUser = discordUser;
            this.icon = icon;

            health = 100;
            shield = 0;
            equipped = 0;
            inactiveTurns = 0;

            x = -1;
            y = -1;
            materials = 0;

            for (int i = 0; i < inventory.Length; i++) inventory[i] = new Item();

            ready = false;

            currentMessages = new List<RestUserMessage>();
        }

        public void Move(Map map)
        {
            for (int i = 0; i < movementSpeed; i++)
            {
                switch (turnDirection)
                {
                    case Direction.Right:
                        if (x < Map.MAPWIDTH - 1
                            && map.mapGrid[y, x + 1].Type != TileType.Wall)
                        {
                            x++;
                            stats.UpdateStat(PlayerStats.Stat.TilesMoved);
                        }

                        break;

                    case Direction.Left:
                        if (x > 0
                            && map.mapGrid[y, x - 1].Type != TileType.Wall)
                        {
                            x--;
                            stats.UpdateStat(PlayerStats.Stat.TilesMoved);
                        }

                        break;

                    case Direction.Up:
                        if (y > 0
                            && map.mapGrid[y - 1, x].Type != TileType.Wall)
                        {
                            y--;
                            stats.UpdateStat(PlayerStats.Stat.TilesMoved);
                        }

                        break;

                    case Direction.Down:
                        if (y < Map.MAPHEIGHT - 1
                            && map.mapGrid[y + 1, x].Type != TileType.Wall)
                        {
                            y++;
                            stats.UpdateStat(PlayerStats.Stat.TilesMoved);
                        }

                        break;
                }
                if (map.mapGrid[y, x].trap != null && map.mapGrid[y, x].trap.placedBy != this) //Check if the player has walked on another person's trap
                {
                    TakeDamage(map.mapGrid[y, x].trap.trapType.effectVal);
                    map.mapGrid[y, x].trap = null;

                    stats.UpdateStat(PlayerStats.Stat.TrapsHit);
                }
            }

            movementSpeed = 1;
        }

        public Map Build(Map map)
        {
            if (materials <= 0) return map;

            switch (turnDirection)
            {
                case Direction.Right:
                    if (x < Map.MAPWIDTH - 1
                        && map.mapGrid[y, x + 1].Type != TileType.Wall)
                    {
                        materials -= 10;
                        map.mapGrid[y, x + 1] = new Tile(TileType.Wall);
                        briefing += "\n" + $"You built a wall on your right.";
                        stats.UpdateStat(PlayerStats.Stat.WallsDestroyed);
                    }
                    else
                        briefing += "\n" + $"Cannot build a wall here!";

                    break;

                case Direction.Left:
                    if (x > 0
                        && map.mapGrid[y, x - 1].Type != TileType.Wall)
                    {
                        materials -= 10;
                        map.mapGrid[y, x - 1] = new Tile(TileType.Wall);
                        briefing += "\n" + $"You built a wall on your left.";
                        stats.UpdateStat(PlayerStats.Stat.WallsDestroyed);
                    }
                    else
                        briefing += "\n" + $"Cannot build a wall here!";

                    break;

                case Direction.Up:
                    if (y > 0
                        && map.mapGrid[y - 1, x].Type != TileType.Wall)
                    {
                        materials -= 10;
                        map.mapGrid[y - 1, x] = new Tile(TileType.Wall);
                        briefing += "\n" + $"You built a wall upward.";
                        stats.UpdateStat(PlayerStats.Stat.WallsDestroyed);
                    }
                    else
                        briefing += "\n" + $"Cannot build a wall here!";

                    break;

                case Direction.Down:
                    if (y < Map.MAPHEIGHT - 1
                        && map.mapGrid[y + 1, x].Type != TileType.Wall)
                    {
                        materials -= 10;
                        map.mapGrid[y + 1, x] = new Tile(TileType.Wall);
                        briefing += "\n" + $"You built a wall downward.";
                        stats.UpdateStat(PlayerStats.Stat.WallsDestroyed);
                    }
                    else
                        briefing += "\n" + $"Cannot build a wall here!";

                    break;
            }

            return map;
        }

        public void UseHealingItem(int slot)
        {
            Item item = inventory[slot];

            int oldHeath = health;
            int oldShield = shield;

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

            briefing += "\n" + $"Used {item.name} and healed {health - oldHeath} health and {shield - oldShield} shield.";

            stats.UpdateStat(PlayerStats.Stat.DamageHealed, item.effectVal);
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

                    string a = "a ";
                    if (newItem.name.StartsWith('a'))
                        a = "an ";
                    else if (newItem.name.EndsWith('s'))
                        a = "";

                    briefing += "\n" + $"You picked up {a}{newItem.name}.";

                    stats.UpdateStat(PlayerStats.Stat.ItemsLooted);

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
                    if (x < Map.MAPWIDTH - 1
                        && map.mapGrid[y, x + 1].Type != TileType.Wall && map.mapGrid[x + 1, y].Type != TileType.Water)
                    {
                        map.mapGrid[y, x + 1].trap = trap;
                        briefing += "\n" + $"You placed a trap to your right.";
                    }
                    else
                        briefing += "\n" + "You cannot place a trap here!";
                    break;

                case Direction.Left:
                    if (x > 0
                        && map.mapGrid[y, x - 1].Type != TileType.Wall && map.mapGrid[x - 1, y].Type != TileType.Water)
                    {
                        map.mapGrid[y, x - 1].trap = trap;
                        briefing += "\n" + $"You placed a trap to your left.";
                    }
                    else
                        briefing += "\n" + "You cannot place a trap here!";
                    break;

                case Direction.Up:
                    if (y < Map.MAPHEIGHT - 1
                        && map.mapGrid[y - 1, x].Type != TileType.Wall && map.mapGrid[x, y - 1].Type != TileType.Water)
                    {
                        map.mapGrid[y - 1, x].trap = trap;
                        briefing += "\n" + $"You placed a trap upward.";
                    }
                    else
                        briefing += "\n" + "You cannot place a trap here!";
                    break;

                case Direction.Down:
                    if (y > 0
                        && map.mapGrid[y + 1, x].Type != TileType.Wall && map.mapGrid[x, y + 1].Type != TileType.Water)
                    {
                        map.mapGrid[y + 1, x].trap = trap;
                        briefing += "\n" + $"You placed a trap downward.";
                    }
                    else
                        briefing += "\n" + "You cannot place a trap here!";
                    break;
            }
            inventory[slot].ammo--;

            if (inventory[slot].ammo <= 0)
            {
                RemoveItem(equipped);
            }

            stats.UpdateStat(PlayerStats.Stat.TrapsPlaced);
        }

        public void TakeDamage(int amount)
        {
            if (amount >= shield)
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

            stats.UpdateStat(PlayerStats.Stat.DamageTaken, amount);
        }

        private void RemoveItem(int slot)
        {
            inventory[slot] = new Item();
        }
    }
}
