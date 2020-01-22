using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
    public static class Data
    {
    
    }

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
}
