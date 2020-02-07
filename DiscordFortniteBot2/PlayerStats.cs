using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
    public class PlayerStats
    {
        //these are all get set so I can see their references
        public int totalDmgTaken { get; set; } = 0;
        public int totalDmgHealed { get; set; } = 0;
        public int totalTilesMoved { get; set; } = 0;
        public int totalTurnsAlive { get; set; } = 0;
        public int totalWallsPlaced { get; set; } = 0;
        public int totalWallsDestroyed { get; set; } = 0;
        public int totalItemsUsed { get; set; } = 0;
        public int totalItemsLooted { get; set; } = 0;
        public int totalItemsDropped { get; set; } = 0;
        public int totalTreesCut { get; set; } = 0;
        public int totalTrapsPlaced { get; set; } = 0;
        public int totalTrapsHit { get; set; } = 0;
        public int totalPlayersKilled { get; set; } = 0; //TODO: How to get this

        public string GetAllStats()
        {
            return "```"+
                $"Total Damage Taken.....{totalDmgTaken}\n" +
                $"Total Damage Healed....{totalDmgHealed}\n" +
                $"Total Turns Alive......{totalTurnsAlive}\n" +
                $"Total Walls Placed.....{totalWallsPlaced}\n" +
                $"Total Walls Destroyed..{totalWallsDestroyed}\n" +
                $"Total Items Used.......{totalItemsUsed}\n" +
                $"Total Items Looted.....{totalItemsLooted}\n" +
                $"Total Items Dropped....{totalItemsDropped}\n" +
                $"Total Trees Cut........{totalTreesCut}\n" +
                $"Total Traps Placed.....{totalTrapsHit}\n" +
                $"Total Traps Hit........{totalTrapsHit}\n" +
                $"Total Players Killed...{totalPlayersKilled}" +
                "```";
        }
    }
}
