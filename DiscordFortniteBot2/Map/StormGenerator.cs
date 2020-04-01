using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
    public class StormGenerator
    {
        
        private const float LIMIT = (float)Math.PI * 2; //pi times 2 makes a full circle (decreasing this makes a cool pie chart tho)
        private const float PRECISION = .001f; //keep at 0.01f or lower

        private int width; //map width
        private int height; //map height
        private int x; //where the storm is centered on
        private int y;
        public int speed { get; }
        public int delay { get; } //how many turns before the storm starts closing in
        private int[] turnSizes; //how big the storm is on the index representing the turn number

        public StormGenerator(int width, int height, int numPlayers)
        {
            this.width = width; //set width and height
            this.height = height;

            Random rand = new Random(); //mark the center of the storm
            x = rand.Next(width / 2) + width / 4;
            y = rand.Next(height / 2) + height / 4;

            if (numPlayers > 1)
            {
                speed = (int)(30.0 / Math.Log(7) * Math.Log(numPlayers - 1) + 30); //logarithmic function determines storm speed based on number of players (assuming number of players is <1)
                delay = (int)(4.0 / 3.0 * (numPlayers - 2) + 14); //linear function ranging from (2,20) to (8,30)
            }
            else
            {
                speed = 20;
                delay = 14;
            }

            //Console.WriteLine($"Map x = {x}; y = {y}");

            GenerateTurnSizes();
        }

        private void GenerateTurnSizes()
        {
            turnSizes = new int[delay + speed];

            int maxWidth = Math.Max(x, Math.Abs(width - x)); //get the longest distance from the center to the edge of the screen
            int maxHeight = Math.Max(y, Math.Abs(height - y));
            int maxDiameter = (int)Math.Ceiling(Math.Sqrt(Math.Pow(maxWidth, 2) + Math.Pow(maxHeight, 2))) * 2; //using TRIANGLES
            double diameter = maxDiameter;
            double rate = maxDiameter / (double)speed;

            for (int i = 0; i < delay; i++) //keep the storm circle at max diameter (just outside the map) for as many turns the delay const is worth
                turnSizes[i] = maxDiameter + 1;

            for (int i = delay; i < speed + delay; i++) //shrink the storm at a constant rate so it takes the amount of turns the speed const is worth
            {
                turnSizes[i] = (int)diameter;
                diameter -= rate;
            }

            //foreach (int i in turnSizes)
            //    Console.Write($"{i}, ");
        }

        private void FloodFill(int startX, int startY, bool[,] storm)
        {
            if (startX >= width || startX < 0 || startY >= height || startY < 0) return;
            if (!storm[startY, startX]) return;
            else storm[startY, startX] = false;
            FloodFill(startX + 1, startY, storm);
            FloodFill(startX - 1, startY, storm);
            FloodFill(startX, startY - 1, storm);
            FloodFill(startX, startY + 1, storm);

        }

        public bool[,] GetStormCircle(int turn) //oh boy here I go commenting this mess
        {
            bool[,] storm = new bool[height, width]; //this function returns a 2d array of booleon types, true is storm, false is not storm

            for (int i = 0; i < height; i++) //make all the values true, for now.
                for (int j = 0; j < width; j++)
                    storm[i, j] = true;

            float d = turnSizes[turn]; //d is diameter, the value is grabbed from the turnsizes array created in the generateturnsizes function


            while (d >= 0) //while the diameter is above 0 (d shrinks each loop)
            {
                float xDraw = 0f;
                float yDraw = 0f;

                while (xDraw < LIMIT - PRECISION && yDraw < LIMIT - PRECISION)
                {
                    xDraw += PRECISION;
                    yDraw += PRECISION;

                    int xGrid = (int)Math.Round(Math.Sin(xDraw) * d / 2) + y; //Calculate circle coordinates
                    int yGrid = (int)Math.Round(Math.Cos(yDraw) * d / 2) + x; //The coordinates of any point on a circle can be expressed as (cos(x) * r, sin(x) * r) 

                    //Console.WriteLine($"xGrid = {xGrid}; yGrid = {yGrid}");

                    if (xGrid >= width || xGrid < 0 || yGrid >= height || yGrid < 0)
                        continue;

                    storm[xGrid, yGrid] = false;
                }

                d -= PRECISION * 100;
            }

            //FloodFill(x, y, storm); //Fill in the circle

            /*
            string draw = "";
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                    draw += (storm[i, j] ? "#" : ".");
                draw += "\n";
            }
            
            Console.WriteLine(draw);
            */


            return storm;
        }
    }
}
