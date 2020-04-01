using Discord;
using System;

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
        Empty, Weapon, Health, Shield, HealAll, Trap
    }
    public enum Range
    {
        None, Short, Medium, Far
    }

    //Player turn data
    public enum Action
    {
        Move, Use, Build, Loot, Equip, Drop, Info, None
    }
    public enum Direction
    {
        Left, Right, Up, Down, None
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
        public static Emoji leaveGame = new Emoji("👈");

        //ingame
        public static Emoji[] arrowEmojis =
        {
            new Emoji("⬅️"),
            new Emoji("➡️"),
            new Emoji("⬆️"),
            new Emoji("⬇️")
        };

        public static Emoji[] slotEmojis =
        {
            new Emoji("1️⃣"),
            new Emoji("2️⃣"),
            new Emoji("3️⃣"),
            new Emoji("4️⃣"),
            new Emoji("5️⃣")
        };

        public static Emoji[] actionEmojis =
        {
            new Emoji("👣"),
            new Emoji("✋"),
            new Emoji("🔨"),
            new Emoji("💼"),
            new Emoji("🔄"),
            new Emoji("🗑️"),
            new Emoji("ℹ")
        };

        public static Emoji sprintButton = new Emoji("🚶");
        public static Emoji sprintFastButton = new Emoji("🏃‍♂️");

        public static Emoji lootAllButton = new Emoji("↕️");
    }

    //Items that can be created and looted in game
    public static class Spawnables
    {
        public static Item GetRandomSpawnable()
        {
            Random random = new Random();
            switch (random.Next(14))
            {
                case 0: return new Item("Tactical Shotgun", ItemType.Weapon, Range.Short, 80, 3);
                case 1: return new Item("Pump Shotgun", ItemType.Weapon, Range.Short, 100, 2);
                case 2: return new Item("Pistol", ItemType.Weapon, Range.Medium, 50, 6);
                case 3: return new Item("Burst Assault Rifle", ItemType.Weapon, Range.Medium, 70, 5);
                case 4: return new Item("Assault Rifle", ItemType.Weapon, Range.Medium, 75, 4);
                case 5: return new Item("Sniper Rifle", ItemType.Weapon, Range.Far, 80, 3);
                case 6: return new Item("Rocket Launcher", ItemType.Weapon, Range.Far, 100, 1);
                case 7: return new Item("Bandages", ItemType.Health, Range.None, 30, 3);
                case 8: return new Item("Medkit", ItemType.Health, Range.None, 50, 2);
                case 9: return new Item("Small Shield Potion", ItemType.Shield, Range.None, 25, 2);
                case 10: return new Item("Shield Potion", ItemType.Shield, Range.None, 50, 2);
                case 11: return new Item("Slurp Juice", ItemType.HealAll, Range.None, 50, 2);
                case 12: return new Item("Chug Jug", ItemType.HealAll, Range.None, 200, 1);
                case 13: return new Item("Spike Trap", ItemType.Trap, Range.None, 75, 1);
            }
            return new Item();
        }
    }
}
