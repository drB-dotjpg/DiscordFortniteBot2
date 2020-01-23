using Discord;
using System;
using System.Collections.Generic;
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
        Empty, Weapon, Health, Shield
    }
    public enum Range
    {
        Short, Medium, Far
    }

    public static class Emotes
    {
        public static Emoji joinGame = new Emoji("🎮");
    }
}
