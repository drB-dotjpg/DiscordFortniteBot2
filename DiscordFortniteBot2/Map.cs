using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
    public enum TileType
    {
        Grass,
        House,
        Chest,
        Water,
        Structure,
    }
    public class Map
    {
        public const int mapWidth = 10;
        public const int mapHeight = 10;

        const int houseCount = 15;

        const int waterCount = 20;

        public Tile[,] mapGrid = new Tile[mapWidth, mapHeight];

        Random random = new Random();

        public Map()
        {
            GenerateMap();
        }

        void GenerateMap()
        {
            //Fill the map with grass tiles to start
            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    mapGrid[i, j] = new Tile(TileType.Grass);
                }
            }

            //Add water
            RandomlyAddTile(waterCount, TileType.Water);

            //Add houses
            RandomlyAddTile(houseCount, TileType.House);

        }

        void RandomlyAddTile(int amount, TileType type)
        {
            int numberLeft = amount;
            while (numberLeft > 0)
            {
                int x = random.Next(0, mapWidth);
                int y = random.Next(0, mapHeight);

                //If the randomly chosen tile is a grass tile, place a house, otherwise try again.
                if (mapGrid[x, y].type == TileType.Grass)
                {
                    mapGrid[x, y] = new Tile(type);
                    numberLeft--;
                }
            }
        }

        void PrintMap() //debug function
        {
            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    switch (mapGrid[i, j].type)
                    {
                        case TileType.Grass:
                            Console.Write(0);
                            break;
                        case TileType.Water:
                            Console.Write(1);
                            break;
                        case TileType.House:
                            Console.Write(2);
                            break;
                    }
                }
                Console.WriteLine();
            }
        }

        public struct Tile
        {
            public TileType type { get; }

            public Tile(TileType type)
            {
                this.type = type;
            }
        }
    }
}
