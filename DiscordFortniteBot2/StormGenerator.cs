using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
    public class StormGenerator
    {
        public const int DELAY = 1; //how many turns before the storm starts closing in
        const int SPEED = 20; //how many turns it takes to close fully
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
            x = rand.Next(width / 2) + width / 4;
            y = rand.Next(height / 2) + height / 4;

            //Console.WriteLine($"Map x = {x}; y = {y}");

            GenerateTurnSizes();
        }

        private void GenerateTurnSizes()
        {
            turnSizes = new int[DELAY + SPEED];

            int maxWidth = Math.Max(x, Math.Abs(width - x)); //get the longest distance from the center to the edge of the screen
            int maxHeight = Math.Max(y, Math.Abs(height - y));
            int maxDiameter = (int)Math.Ceiling(Math.Sqrt(Math.Pow(maxWidth, 2) + Math.Pow(maxHeight, 2))) * 2; //using TRIANGLES
            double diameter = maxDiameter;
            double rate = maxDiameter / (double)SPEED;

            for (int i = 0; i < DELAY; i++) //keep the storm circle at max diameter (just outside the map) for as many turns the delay const is worth
                turnSizes[i] = maxDiameter + 1;

            for (int i = DELAY; i < SPEED + DELAY; i++) //shrink the storm at a constant rate so it takes the amount of turns the speed const is worth
            {
                turnSizes[i] = (int)diameter;
                diameter -= rate;
            }

            //foreach (int i in turnSizes)
            //    Console.Write($"{i}, ");
        }

        void FloodFill(int startX, int startY, bool[,] storm)
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

            FloodFill(x, y, storm); //Fill in the circle

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
