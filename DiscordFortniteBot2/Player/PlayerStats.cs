using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiscordFortniteBot2
{
    public class PlayerStats
    {
        public enum Stat
        {
            DamageTaken,
            DamageHealed,
            TilesMoved,
            TurnsAlive,
            WallsPlaced,
            WallsDestroyed,
            ItemsUsed,
            ItemsLooted,
            ItemsDropped,
            TreesCut,
            TrapsPlaced,
            TrapsHit,
            PlayersKilled
        }

        private int[] statValues = new int[Enum.GetNames(typeof(Stat)).Length];

        public int GetStat(Stat stat)
        {
            return statValues[(int)stat];
        }

        public void UpdateStat(Stat stat, int increment = 1)
        {
            statValues[(int)stat] += increment;
        }

        public static string GetStatName(Stat stat)
        {
            return Regex.Replace(stat.ToString(), "(\\B[A-Z])", " $1"); //I got this off SO it just adds spaces to a camel-cased string
        }

        public string GetAllStats()
        {
            string builder = "```";
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                string name = GetStatName(stat); 
                builder += $"Total {name}";
                builder += string.Concat(Enumerable.Repeat(".", 23 - builder.Length)) + statValues[(int)stat] + "\n";
            }
            builder += "```";

            return builder;
        }
    }
}
