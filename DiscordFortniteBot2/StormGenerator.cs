using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordFortniteBot2
{
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
            x = rand.Next(width / 2) + width / 4;
            y = rand.Next(height / 2) + height / 4;

            Console.WriteLine($"Map x = {x}; y = {y}");

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

            for (int i = 0; i < DELAY; i++)
                turnSizes[i] = maxDiameter + 1;

            for (int i = DELAY; i < SPEED; i++)
            {
                turnSizes[i] = (int)diameter;
                diameter -= rate;
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

            float d = turnSizes[turn];

            while (d >= 0)
            {
                float xDraw = 0f;
                float yDraw = 0f;

                while (xDraw < LIMIT - PRECISION && yDraw < LIMIT - PRECISION)
                {
                    xDraw += PRECISION;
                    yDraw += PRECISION;

                    int xGrid = (int)Math.Round(Math.Sin(xDraw) * d / 2) + y; //use WAVES to make circles
                    int yGrid = (int)Math.Round(Math.Cos(yDraw) * d / 2) + x;

                    //Console.WriteLine($"xGrid = {xGrid}; yGrid = {yGrid}");

                    if (xGrid >= width || xGrid < 0 || yGrid >= height || yGrid < 0)
                        continue;

                    storm[xGrid, yGrid] = false;
                }

                d -= PRECISION * 100;
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
