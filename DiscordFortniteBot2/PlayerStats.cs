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
        public int totalTreesCut { get; set; } = 0;
        public int totalItemsDropped { get; set; } = 0;
        public int totalTrapsPlaced { get; set; } = 0;
        public int totalTrapsHit { get; set; } = 0;
        public int totalPlayersKilled { get; set; } = 0; //TODO: How to get this
    }
}
