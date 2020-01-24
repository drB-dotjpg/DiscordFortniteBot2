using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordFortniteBot2
{

    //Game Logic
    public enum Phase
    {
        Pregame, Ingame, Postgame
    }

    //Item related
    public enum ItemType
    {
        Empty, Weapon, Health, Shield, HealAll
    }
    public enum Range
    {
        Short, Medium, Far, None
    }

    //Emotes used in reactions & throughout the game
    public static class Emotes
    {
        //player icons
        public static Emoji[] playerIcons = {
            new Emoji("😀"),
            new Emoji("😎"),
            new Emoji("😔"),
            new Emoji("😜"),
            new Emoji("😳"),
            new Emoji("😂"),
            new Emoji("🤔"),
            new Emoji("😤"),
        };

        //pregame
        public static Emoji joinGame = new Emoji("🎮"); //used for join button
    }

    //Items that can be created and looted in game
    public static class Spawnables
    {
        public static Item[] allItems = new Item[]
        {
            //weapons (in order of range then damage)
            //       v-Name-----------v  v-Item type---v  v-Range---v  v-damage, ammo
            new Item("Tactical Shotgun", ItemType.Weapon, Range.Short, 80, 3),
            new Item("Pump Shotgun", ItemType.Weapon, Range.Short, 100, 2),
            new Item("Pistol", ItemType.Weapon, Range.Medium, 50, 6),
            new Item("Burst Assault Rifle", ItemType.Weapon, Range.Medium, 70, 5),
            new Item("Assault Rifle", ItemType.Weapon, Range.Medium, 75, 4),
            new Item("Sniper Rifle", ItemType.Weapon, Range.Far, 80, 3),
            new Item("Rocket Launcher", ItemType.Weapon, Range.Far, 100, 1),

            //health
            new Item("Bandages", ItemType.Health, Range.None, 30, 3),
            new Item("Medkit", ItemType.Health, Range.None, 50, 2),

            //shield
            new Item("Small Shield Potion", ItemType.Shield, Range.None, 25, 2),
            new Item("Shield Potion", ItemType.Shield, Range.None, 50, 2),

            //health+shield
            new Item("Slurp Juice", ItemType.HealAll, Range.None, 50, 2),
            new Item("Chug Jug", ItemType.HealAll, Range.None, 200, 1)
        };

        public static Item GetRandomSpawnable()
        {
            Random random = new Random();
            return allItems[random.Next(allItems.Length)];
        }
    }
}
