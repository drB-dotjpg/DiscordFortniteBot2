using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
    public enum TileType
    {
        Grass,
        Chest,
        Water,
        Wall,
        Tree
    }
    public class Map
    {
        public const int mapWidth = 10;
        public const int mapHeight = 10;

        //                                                    v----edit this
        const int houseCount = (int)(mapWidth * mapHeight * 0.03); //if value is .03, then there are 3 houses per 100 tiles
        const int treeCount = (int)(mapWidth * mapHeight * 0.30);
        const int waterCount = (int)(mapWidth * mapHeight * 0.20);

        public Tile[,] mapGrid = new Tile[mapWidth, mapHeight];

        Random random = new Random();

        public Map(bool debug)
        {
            GenerateMap(debug);
        }

        void GenerateMap(bool debug)
        {
            //Fill the map with grass tiles to start
            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    mapGrid[i, j] = new Tile(TileType.Grass);
                }
            }

            //Add Trees
            RandomlyAddTile(treeCount, TileType.Tree);

            //Add River
            GenerateRiver();
            GenerateRiver();

            if (debug) PrintMap();
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

        void GenerateRiver()
        {
            Random random = new Random();

            bool vertical = random.Next(2) == 0; // 50% chance the river is vertically facing

            if (vertical)
            {
                int width = mapWidth / 10;
                int drawPoint = random.Next(mapWidth - width);

                for (int y = 0; y < mapHeight; y++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        mapGrid[drawPoint + i, y] = new Tile(TileType.Water);
                    }

                    if (random.Next(4) == 0)
                    {
                        drawPoint = random.Next(2) == 0
                            ? drawPoint + 1
                            : drawPoint - 1;
                    }
                }
            }
            else
            {
                int height = mapHeight / 10;
                int drawPoint = random.Next(mapWidth - height);

                for (int x = 0; x < mapHeight; x++)
                {
                    for (int i = 0; i < height; i++)
                    {
                        mapGrid[x, drawPoint + i] = new Tile(TileType.Water);
                    }

                    if (random.Next(4) == 0)
                    {
                        drawPoint = random.Next(2) == 0
                            ? drawPoint + 1
                            : drawPoint - 1;
                    }
                }
            }
        }

        void PrintMap() //debug function
        {
            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    Console.Write((int)mapGrid[i, j].type);
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
