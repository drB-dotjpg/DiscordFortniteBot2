using System;
using System.Collections.Generic;

namespace DiscordFortniteBot2
{
    public enum TileType
    {
        Grass,
        Chest,
        Water,
        Wall,
        Tree,
        Storm,
        Bounds
    }

    public class Map
    {
        public int width { get; }
        public int height { get; }

        //Amount of things that will be generated
        private int houseCount;
        private int treeCount;
        private int riverCount;


        public Tile[,] mapGrid;

        private Random random = new Random();
        private StormGenerator stormGen;

        public Map(bool debug, int numPlayers)
        {
            int baseMapSize = Math.Max(20, (int)(25 / Math.Log(7) * Math.Log(numPlayers - 1) + 20));
            if (debug) Console.WriteLine($"Map is {baseMapSize}^2 tiles");
            width = baseMapSize;
            height = baseMapSize;

            houseCount = width * height / 70;
            treeCount = width * height / 11;
            riverCount = (width * height / 800) + 1; //river count is randomized, this is a cap to the amount of rivers generated.

            mapGrid = new Tile[width, height];

            GenerateMap(debug);
            stormGen = new StormGenerator(width, height, numPlayers);
        }

        private void GenerateMap(bool debug)
        {
            //Fill the map with grass tiles to start
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
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

        private void RandomlyAddTile(int amount, TileType type)
        {
            int numberLeft = amount;
            while (numberLeft > 0)
            {
                int x = random.Next(0, width);
                int y = random.Next(0, height);

                //If the randomly chosen tile is a grass tile, place new item, otherwise try again.
                if (mapGrid[x, y].Type == TileType.Grass)
                {
                    mapGrid[x, y] = new Tile(type);
                    numberLeft--;
                }
            }
        }

        private void GenerateRiver()
        {
            bool vertical = random.Next(2) == 0; // 50% chance the river is vertically facing
            int noise = random.Next(5); //how smooth the river is

            if (vertical)
            {
                int size = (int)Math.Ceiling(width / 15.0); //get how wide the river is based on map size
                int drawPoint = random.Next(width - size); //the point on the x axis the river starts drawing on

                for (int y = 0; y < height; y++) //for each column of map tiles
                {
                    for (int i = 0; i < size - 1; i++) //loop iterates the number of times width is worth.
                    {
                        mapGrid[drawPoint + i, y] = new Tile(TileType.Water);
                    }

                    if (random.Next(noise) == 0) //random chance the river will shift leftward or rightward
                    {
                        int oldpoint = drawPoint;

                        drawPoint = random.Next(2) == 0
                            ? drawPoint + 1
                            : drawPoint - 1;

                        if (drawPoint > width - size || drawPoint < 0) drawPoint = oldpoint;
                    }
                }
            }
            else //code below is very similar to above
            {
                int size = (int)Math.Ceiling(height / 15.0);
                int drawPoint = random.Next(height - size);

                for (int x = 0; x < width; x++)
                {
                    for (int i = 0; i < size - 1; i++)
                    {
                        mapGrid[x, drawPoint + i] = new Tile(TileType.Water);
                    }

                    if (random.Next(noise) == 0)
                    {
                        int oldPoint = drawPoint;

                        drawPoint = random.Next(2) == 0
                            ? drawPoint + 1
                            : drawPoint - 1;

                        if (drawPoint > height - size || drawPoint < 0) drawPoint = oldPoint;
                    }
                }
            }
        }

        private void GenerateBuildings()
        {
            int numberLeft = houseCount;
            while (numberLeft > 0)
            {
                int y;
                int x;
                bool safe;

                do
                {
                    safe = true;

                    x = random.Next(1, width - 1);  //Pick random set of coords to generate the building at
                    y = random.Next(1, height - 1);

                    //Check if the building can generate (cannot generate on chests or walls)
                    for (int hor = x; hor < x + 2; hor++)
                    {
                        for (int vert = y; vert < y + 2; vert++)
                        {
                            if (mapGrid[hor, vert].Type == TileType.Chest
                                || mapGrid[hor, vert].Type == TileType.Wall
                                || mapGrid[hor, vert].Type == TileType.Water)
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

        public void DropSupplyDrop() //Drop a chest on a random grass tile
        {
            while (true)
            {
                int randomX = random.Next(0, width);
                int randomY = random.Next(0, height);

                if (mapGrid[randomY, randomX].Type == TileType.Grass)
                {
                    mapGrid[randomY, randomX] = new Tile(GenerateItems());
                    //Console.WriteLine($"Supply drop dropped at ({randomY},{randomX})");
                    break;
                }

            }
        }

        private Item[] GenerateItems() //Generate items for chests
        {
            Item[] items = new Item[5];

            int amount = random.Next(2, items.Length - 1);

            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new Item();
            }
            for (int i = 0; i < amount; i++)
            {
                items[i] = Spawnables.GetRandomSpawnable();
            }

            return items;
        }

        void PrintMap() //debug function
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Console.Write((int)mapGrid[i, j].Type);
                }
                Console.WriteLine();
            }
        }

        private Tile[,] GetMapArea(int x, int y) //get info on 7x7 area around the map. x and y represent the center of the 7x7 grid
        {
            //Console.WriteLine($"GetMapArea (x = {x}, y = {y})");

            Tile[,] mapArea = new Tile[7, 7];

            int xTrack = x - 3; //start the x axis scan 3 slots away from the center.
            int yTrack = y - 3;

            int xCap = xTrack + 7; //the limit to how far the scan can go.
            int xReset = xCap - 7;
            if (xCap >= width) xCap = width - 1; //make sure its not out of bounds.

            int yCap = yTrack + 7;
            if (yCap >= height) yCap = height - 1;

            int yMap = 0;
            while (yMap < 7) //start the scan.
            {
                int xMap = 0;
                xTrack = xReset;

                while (xMap < 7)
                {
                    //Console.Write($"\nxTrack = {xTrack}; yTrack = {yTrack}; xMap = {xMap}; yMap = {yMap}; ");
                    //Console.WriteLine($"mapGrid[xTrack, yTrack] = {mapGrid[xTrack, yTrack].Type.ToString()}");

                    if (xTrack >= 0 && yTrack >= 0 && xTrack <= xCap && yTrack <= yCap) //if its not out of bounds.
                        mapArea[yMap, xMap] = mapGrid[yTrack, xTrack];
                    else
                        mapArea[yMap, xMap] = new Tile(TileType.Bounds);

                    xMap++;
                    xTrack++;
                }
                yMap++;
                yTrack++;
            }

            return mapArea;
        }

        public string GetMapAreaString(int x, int y, List<Player> players)
        {
            Tile[,] mapArea = GetMapArea(x, y); //get a 7x7 area of the map around the x and y coodnates
            string mapString = "";

            x -= 3;
            y -= 3;
            int xStore = x;

            for (int i = 0; i < 7; i++) //all assuming GetMapArea returns a 7x7 array
            {
                x = xStore;

                for (int j = 0; j < 7; j++)
                {
                    //Console.Write($"x={x};y={y}|");

                    bool playerFound = false;
                    foreach (Player player in players) //Check for players in the area
                    {
                        if (player.x == x && player.y == y)
                        {
                            mapString += player.icon;
                            playerFound = true;
                            break;
                        }
                    }

                    if (playerFound) { x++; continue; }

                    switch (mapArea[i, j].Type)
                    {
                        case TileType.Chest:
                            mapString += "🟨"; //yellow block
                            break;
                        case TileType.Grass:
                            mapString += "🟩"; //green block
                            break;
                        case TileType.Tree:
                            mapString += "🟫"; //brown block
                            break;
                        case TileType.Wall:
                            mapString += "🟥"; //red block
                            break;
                        case TileType.Water:
                            mapString += "🟦"; //blue block
                            break;
                        case TileType.Storm:
                            mapString += "⛈️"; //thundercloud
                            break;
                        case TileType.Bounds:
                            mapString += "⬛"; //black block
                            break;
                    }

                    x++;
                }

                //Console.WriteLine();

                y++;
                mapString += "\n";
            }

            return mapString;
        }

        public string GetWorldMapString(Player player = null)
        {
            string mapString = $"World Map (1/2 scale)\n{(player != null ? "0 = You | " : "")}. = Ground | # = Water | ! = Storm\n\n";

            for (int i = 0; i < width; i += 2)
            {
                for (int j = 0; j < height; j += 2)
                {
                    if (player != null)
                    {
                        if (player.x == j && player.y == i
                            || player.x == j - 1 && player.y == i
                            || player.x == j && player.y == i - 1
                            || player.x == j - 1 && player.y == i - 1)
                        {
                            mapString += "0 ";
                            continue;
                        }
                    }

                    switch (mapGrid[i, j].Type)
                    {

                        case TileType.Water:
                            mapString += "# "; //blue block
                            break;
                        case TileType.Storm:
                            mapString += "! "; //thundercloud
                            break;
                        default:
                            mapString += ". "; //green block
                            break;
                    }
                }
                mapString += "\n";
            }

            return mapString;
        }

        public void UpdateStorm(int turn)
        {
            if (turn >= stormGen.delay + stormGen.speed) return;

            bool[,] storm = stormGen.GetStormCircle(turn);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (storm[j, i])
                        mapGrid[i, j] = new Tile(TileType.Storm);
                }
            }
        }

        public int GetStormSpeed() => stormGen.speed;

        public int GetStormDelay() => stormGen.delay;
    }
}
