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
        public const int mapWidth = 50;
        public const int mapHeight = 50;


        const int houseCount = 20;
        const int treeCount = 150;
        const int riverCount = 3; //river count is randomized, this is a cap to the amount of rivers generated.

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

            //Add River(s)
            int riverAmount = random.Next(riverCount) + 1;
            for (int i = 0; i < riverAmount; i++) GenerateRiver();

            //Add Buildings
            GenerateBuildings();

            if (debug) PrintMap();
        }

        void RandomlyAddTile(int amount, TileType type)
        {
            int numberLeft = amount;
            while (numberLeft > 0)
            {
                int x = random.Next(0, mapWidth);
                int y = random.Next(0, mapHeight);

                //If the randomly chosen tile is a grass tile, place new item, otherwise try again.
                if (mapGrid[x, y].Type == TileType.Grass)
                {
                    mapGrid[x, y] = new Tile(type);
                    numberLeft--;
                }
            }
        }

        void GenerateRiver() //TODO: Array bounds error, might be fixed but the issue shows up at random times
        {
            bool vertical = random.Next(2) == 0; // 50% chance the river is vertically facing
            int noise = random.Next(5); //how smooth the river is

            if (vertical)
            {
                int width = (int)Math.Ceiling(mapWidth / 15.0); //get how wide the river is based on map size
                int drawPoint = random.Next(mapWidth - width); //the point on the x axis the river starts drawing on

                for (int y = 0; y < mapHeight; y++) //for each collum of map tiles
                {
                    for (int i = 0; i < width - 1; i++) //loop iterates the number of times width is worth.
                    {
                        mapGrid[drawPoint + i, y] = new Tile(TileType.Water);
                    }

                    if (random.Next(noise) == 0) //random chance the river will shift leftward or rightward
                    {
                        int oldpoint = drawPoint;

                        drawPoint = random.Next(2) == 0
                            ? drawPoint + 1
                            : drawPoint - 1;

                        if (drawPoint > mapWidth - width || drawPoint < 0) drawPoint = oldpoint;
                    }
                }
            }
            else //code below is very similar to above
            {
                int height = (int)Math.Ceiling(mapHeight / 15.0);
                int drawPoint = random.Next(mapWidth - height);

                for (int x = 0; x < mapHeight; x++)
                {
                    for (int i = 0; i < height - 1; i++)
                    {
                        mapGrid[x, drawPoint + i] = new Tile(TileType.Water);
                    }

                    if (random.Next(noise) == 0)
                    {
                        int oldPoint = drawPoint;

                        drawPoint = random.Next(2) == 0
                            ? drawPoint + 1
                            : drawPoint - 1;

                        if (drawPoint > mapHeight - height || drawPoint < 0) drawPoint = oldPoint;
                    }
                }
            }
        }

        void GenerateBuildings()
        {
            int numberLeft = houseCount;
            while (numberLeft > 0)
            {
                bool safe = true;
                int x = 0;
                int y = 0;

                do
                {
                    safe = true;

                    x = random.Next(1, mapWidth - 1);
                    y = random.Next(1, mapHeight - 1);

                    for (int hor = x; hor < x + 2; hor++)
                    {
                        for (int vert = y; vert < y + 2; vert++)
                        {
                            if (mapGrid[hor, vert].Type == TileType.Chest || mapGrid[hor, vert].Type == TileType.Wall)
                                safe = false;
                        }
                    }
                } while (!safe);

                int opening = random.Next(4);

                //Sorry about this (it makes the building)
                mapGrid[x - 1, y - 1] = new Tile(TileType.Wall);
                if (opening != 0) mapGrid[x, y - 1] = new Tile(TileType.Wall);
                mapGrid[x + 1, y - 1] = new Tile(TileType.Wall);
                if (opening != 1) mapGrid[x - 1, y] = new Tile(TileType.Wall);
                mapGrid[x, y] = new Tile(GenerateItems());
                if (opening != 2) mapGrid[x + 1, y] = new Tile(TileType.Wall);
                mapGrid[x - 1, y + 1] = new Tile(TileType.Wall);
                if (opening != 3) mapGrid[x, y + 1] = new Tile(TileType.Wall);
                mapGrid[x + 1, y + 1] = new Tile(TileType.Wall);

                numberLeft--;
            }
        }

        Item[] GenerateItems()
        {
            Item[] items = new Item[random.Next(4)];

            for (int i = 0; i < items.Length; i++)
            {
                items[i] = Spawnables.GetRandomSpawnable();
            }

            return items;
        }

        void PrintMap() //debug function
        {
            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    Console.Write((int)mapGrid[i, j].Type);
                }
                Console.WriteLine();
            }
        }

        public struct Tile //tiles represent a space on the map
        {
            public TileType Type { get; }
            public Item[] Items { get; }

            public Tile(TileType type)
            {
                this.Type = type;
                Items = new Item[0];
            }

            public Tile(Item[] items)
            {
                Type = TileType.Chest;
                Items = items;
            }
        }
    }
}
