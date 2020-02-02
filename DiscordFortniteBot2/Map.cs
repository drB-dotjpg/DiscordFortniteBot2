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
        public const int MAPWIDTH = 40;
        public const int MAPHEIGHT = 40;

        //Amount of things that will be generated
        const int houseCount = 18;
        const int treeCount = 150;
        const int riverCount = 3; //river count is randomized, this is a cap to the amount of rivers generated.

        public Tile[,] mapGrid = new Tile[MAPWIDTH, MAPHEIGHT];

        Random random = new Random();

        private StormGenerator stormGen;

        public Map(bool debug)
        {
            GenerateMap(debug);
            stormGen = new StormGenerator(MAPWIDTH, MAPHEIGHT);
        }

        void GenerateMap(bool debug)
        {
            //Fill the map with grass tiles to start
            for (int i = 0; i < MAPWIDTH; i++)
            {
                for (int j = 0; j < MAPHEIGHT; j++)
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
                int x = random.Next(0, MAPWIDTH);
                int y = random.Next(0, MAPHEIGHT);

                //If the randomly chosen tile is a grass tile, place new item, otherwise try again.
                if (mapGrid[x, y].Type == TileType.Grass)
                {
                    mapGrid[x, y] = new Tile(type);
                    numberLeft--;
                }
            }
        }

        void GenerateRiver()
        {
            bool vertical = random.Next(2) == 0; // 50% chance the river is vertically facing
            int noise = random.Next(5); //how smooth the river is

            if (vertical)
            {
                int width = (int)Math.Ceiling(MAPWIDTH / 15.0); //get how wide the river is based on map size
                int drawPoint = random.Next(MAPWIDTH - width); //the point on the x axis the river starts drawing on

                for (int y = 0; y < MAPHEIGHT; y++) //for each column of map tiles
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

                        if (drawPoint > MAPWIDTH - width || drawPoint < 0) drawPoint = oldpoint;
                    }
                }
            }
            else //code below is very similar to above
            {
                int height = (int)Math.Ceiling(MAPHEIGHT / 15.0);
                int drawPoint = random.Next(MAPWIDTH - height);

                for (int x = 0; x < MAPHEIGHT; x++)
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

                        if (drawPoint > MAPHEIGHT - height || drawPoint < 0) drawPoint = oldPoint;
                    }
                }
            }
        }

        void GenerateBuildings()
        {
            int numberLeft = houseCount;
            while (numberLeft > 0)
            {
                bool safe = true; //If the building can generate
                int x = 0;
                int y = 0;

                do
                {
                    safe = true;

                    x = random.Next(1, MAPWIDTH - 1);  //Pick random set of coords to generate the building at
                    y = random.Next(1, MAPHEIGHT - 1);

                    //Check if the building can generate (cannot generate on chests or walls)
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

        Item[] GenerateItems() //Generate items for chests
        {
            Item[] items = new Item[5];

            int amount = random.Next(items.Length);

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
            for (int i = 0; i < MAPWIDTH; i++)
            {
                for (int j = 0; j < MAPHEIGHT; j++)
                {
                    Console.Write((int)mapGrid[i, j].Type);
                }
                Console.WriteLine();
            }
        }

        Tile[,] GetMapArea(int x, int y) //get info on 7x7 area around the map. x and y represent the center of the 7x7 grid
        {
            Tile[,] mapArea = new Tile[7, 7];

            int xTrack = x - 3; //start the x axis scan 3 slots away from the center.
            int yTrack = y - 3;

            int xCap = xTrack + 7; //the limit to how far the scan can go.
            if (xCap >= MAPWIDTH) xCap = MAPWIDTH - 1; //make sure its not out of bounds.

            int yCap = yTrack + 7;
            if (yCap >= MAPHEIGHT) yCap = MAPHEIGHT - 1;

            int yMap = 0;
            while (yMap < 7) //start the scan.
            {
                int xMap = 0;
                xTrack = xCap - 7;

                while (xMap < 7)
                {
                    //Console.Write($"\nxTrack = {xTrack}; yTrack = {yTrack}; xMap = {xMap}; yMap = {yMap}; ");
                    //Console.WriteLine($"mapGrid[xTrack, yTrack] = {mapGrid[xTrack, yTrack].Type.ToString()}");

                    if (xTrack >= 0 && yTrack >= 0 && xTrack < xCap && yTrack < yCap) //if its not out of bounds.
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
                    //Console.WriteLine($"GetMapAreaString: x = {x}; y = {y}");

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

                y++;
                mapString += "\n";
            }

            return mapString;
        }

        public string GetWorldMapString(Player player = null)
        {
            string mapString = "World Map (1/2 scale)\n0 = You | . = Ground | # = Water | ! = Storm\n\n";

            for (int i = 0; i < MAPWIDTH; i += 2)
            {
                for (int j = 0; j < MAPHEIGHT; j += 2)
                {
                    if (player.x == j && player.y == i || player.x == j - 1 && player.y == i - 1)
                    {
                        mapString += "0 ";
                        continue;
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
            bool[,] storm = stormGen.GetStormCircle(turn);

            for (int i = 0; i < MAPWIDTH; i++)
            {
                for (int j = 0; j < MAPHEIGHT; j++)
                {
                    if (storm[j, i]) mapGrid[j,i] = new Tile(TileType.Storm);
                }
            }
        }

        public struct Tile //tiles represent a space on the map
        {
            public TileType Type { get; set; }
            public Item[] Items { get; }

            public Trap trap { get; set; }

            public Tile(TileType type)
            {
                Type = type;
                Items = new Item[5];
                for (int i = 0; i < Items.Length; i++) Items[i] = new Item();
                trap = null;
            }

            public Tile(Item[] items)
            {
                Type = TileType.Chest;
                Items = new Item[5];

                for (int i = 0; i < Items.Length; i++)
                {
                    Items[i] = new Item();
                }
                for (int i = 0; i < items.Length; i++)
                {
                    Items[i] = items[i];
                }

                Items = items;
                trap = null;
            }

            public bool AddChestItem(Item item) //returns true if item was added
            {
                bool added = false;
                for (int i = 0; i < Items.Length; i++)
                {
                    if (Items[i].type == ItemType.Empty)
                    {
                        added = true;
                        Items[i] = item;
                        break;
                    }
                }
                if (added) Type = TileType.Chest;
                return added;
            }
        }
    }

    public class Trap
    {
        public Player placedBy { get; } //The player that placed the trap

        public Item trapType { get; } //The type of trap (ex: Spike Trap)

        public Trap(Player player, Item type)
        {
            placedBy = player;
            trapType = type;
        }
    }

    public class StormGenerator
    {
        const int DELAY = 3; //how many turns before the storm starts closing in
        const int SPEED = 15; //how many turns it takes to close fully
        const float LIMIT = (float)Math.PI * 2; //pi times 2 makes a full circle (decreasing this makes a cool pie chart tho)
        const float PRECISION = .01f; //keep at 0.01f or lower

        int width { get; } //map width
        int height { get; } //map height
        private int x; //where the storm is centered on
        private int y;
        private int[] turnSizes; //how big the storm is on the index representing the turn number

        public StormGenerator(int width, int height)
        {
            this.width = width; //set width and height
            this.height = height;

            Random rand = new Random(); //mark the center of the storm
            x = rand.Next(width / 2) + width / 2;
            y = rand.Next(height / 2) + height / 2;

            GenerateTurnSizes();
        }

        private void GenerateTurnSizes()
        {
            turnSizes = new int[DELAY + SPEED];

            int maxWidth = Math.Max(x - width, width - x); //get the longest distance from the center to the edge of the screen
            int maxHeight = Math.Max(y - height, height - y);
            int maxSize = (int)Math.Sqrt(Math.Pow(maxWidth, 2) + Math.Pow(maxHeight, 2)); //using TRIANGLES
            double radius = maxSize;
            double rate = maxSize / SPEED;

            for (int i = 0; i < DELAY; i++)
                turnSizes[i] = maxSize + 1;

            for (int i = DELAY; i < SPEED; i++)
            {
                turnSizes[i] = (int)radius;
                radius -= rate;
            }

            foreach (int i in turnSizes)
                Console.Write($"{i}, ");
        }

        public bool[,] GetStormCircle(int turn)
        {
            bool[,] storm = new bool[height, width];

            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                    storm[i, j] = true;

            float r = turnSizes[turn];

            while (r >= 0)
            {
                float xDraw = 0f;
                float yDraw = 0f;

                while (xDraw < LIMIT - PRECISION && yDraw < LIMIT - PRECISION)
                {
                    xDraw += PRECISION;
                    yDraw += PRECISION;

                    int xGrid = (int)Math.Round(Math.Sin(xDraw) * r / 2) + y; //use WAVES to make circles
                    int yGrid = (int)Math.Round(Math.Cos(yDraw) * r / 2) + x;

                    //Console.WriteLine($"xGrid = {xGrid}; yGrid = {yGrid}");

                    if (xGrid >= width || xGrid < 0 || yGrid >= height || yGrid < 0)
                        continue;

                    storm[xGrid, yGrid] = false;
                }

                r -= PRECISION*100;
            }

            string draw = "";
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                    draw += (storm[i, j] ? "#" : ".");

                draw += "\n";
            }

            Console.WriteLine(draw);

            return storm;
        }
    }
}
