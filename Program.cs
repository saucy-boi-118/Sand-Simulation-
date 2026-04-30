using System;
using Raylib_cs;
using System.Numerics;
using static Global;

class Global
{
    public const int WINWIDTH = 500;
    public const int WINHEIGHT = 500;
    public const int CELLSIZE = 1; // also sand size
    public static readonly Vector2 SANDSIZE = new(CELLSIZE, CELLSIZE); // sand size --> same as cell
    public const int MAX_PARTICLES = (int)((0.75f*WINWIDTH)*(0.75f*WINHEIGHT)); // 75% the screen
    public const int MAX_BLOCKS = 3000; // max blocks on screen

    public static readonly Color DesertYellow = new(237, 201, 175);
    public static readonly Color DesertRed = new(194, 88, 60);

    public static readonly int buffer = 10;

    public static int[,] DefineMap()
    {
        int[,] ints = new int[WINHEIGHT + buffer/CELLSIZE,WINWIDTH + buffer/CELLSIZE];
        for (int y = 0; y < WINHEIGHT + buffer/CELLSIZE; y++)
        {
            for (int x = 0; x < WINWIDTH + buffer/CELLSIZE; x++)
            {
                if (y == buffer || y == WINHEIGHT/CELLSIZE || x == buffer || x == WINWIDTH/CELLSIZE - buffer)
                {
                    ints[y,x] = 1;
                }
                else {ints[y,x] = 0;}
            }
        }
        return ints;
    }

    // map
    public static int[,] map = DefineMap();
}
class Program
{
    struct Sand()
    {
        public Vector2 Position;
        public Vector2 Cell;
        public bool Alive = false;
        public Color color;
    }
    public static void CALCULATECELLID(ref Vector2 Cell, Vector2 Position)
    {
        map[(int)Cell.Y,(int)Cell.X] = 0;
        Cell = new(Position.X/CELLSIZE, Position.Y/CELLSIZE);
        map[(int)Cell.Y,(int)Cell.X] = 1;
    }
    public static Color LerpColor(Color a, Color b, float t)
    {
        // x + t * (y - x) == lerp function
        return new(
            (byte)(a.R + t * (b.R - a.R)), // Red
            (byte)(a.G + t * (b.G - a.G)), // Green
            (byte)(a.B + t * (b.B - a.B)) // Blue
            );
    }
    public static void Main()
    {
        // init window
        Raylib.InitWindow(WINWIDTH, WINHEIGHT, "Sand Simulation");  
        Image windowIcon = Raylib.LoadImage("pyramid.png");
        Raylib.SetWindowIcon(windowIcon);
        Raylib.SetTargetFPS(120);
        float dt = 0;

        // Sand
        Sand[] sands = new Sand[MAX_PARTICLES];
        int sandCount = 0;
        Random rng = new();

        // blocks

        Vector2[] blocks = new Vector2[MAX_BLOCKS];
        int blockCount = 0;


        // brush
        int radius = 5;
        int mRadius = 10;
        float brushTimer = 0;
        int speed = CELLSIZE;
        string radiusText;

        while (!Raylib.WindowShouldClose())
        {
            dt = Raylib.GetFrameTime(); // delta time

            // control draw size
            radius = (radius <= mRadius && radius > 0)?  radius + (int)Raylib.GetMouseWheelMove(): mRadius;
            if (Raylib.GetMouseWheelMove() != 0){brushTimer = 1;}

            if(Raylib.IsMouseButtonDown(MouseButton.Left) && sandCount < MAX_PARTICLES && Raylib.GetMouseX() > buffer*2 && Raylib.GetMouseX() < WINWIDTH-(buffer*1.5f))
            {
                // spawn brush
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx * dx + dy * dy <= radius * radius && map[(int)Raylib.GetMouseX() + dx, (int)Raylib.GetMouseY() + dy] != 1)
                        {
                            sands[sandCount].Position = new(Raylib.GetMouseX() + dx, Raylib.GetMouseY() + dy); // Change Position
                            sands[sandCount].Alive = true; // Activate the particle
                            sands[sandCount].color = DesertYellow; // Desert Yellow
                            CALCULATECELLID(ref sands[sandCount].Cell, sands[sandCount].Position); // Set the Cell
                            sandCount++; // Increase Count
                            if (sandCount >= sands.Length) {sandCount--;} // Cap the Count
                        }
                    }
                }
            } // spawn blocks
            else if(Raylib.IsMouseButtonPressed(MouseButton.Right) && Raylib.GetMouseX() > buffer*2 && Raylib.GetMouseX() < WINWIDTH-(buffer*1.5f))
            {
                // spawn blocks == CIRCLE
                for (int bx = -radius; bx <= radius; bx++)
                {
                    for (int by = -radius; by <= radius; by++)
                    {
                        if (bx * bx + by * by <= radius * radius && map[(int)(Raylib.GetMouseY()/CELLSIZE) + bx, (int)(Raylib.GetMouseX()/CELLSIZE) + by] != 1)
                        {
                            blocks[blockCount] = new((Raylib.GetMouseX()/CELLSIZE) + bx, (Raylib.GetMouseY()/CELLSIZE) + by);
                            map[(int)(Raylib.GetMouseY()/CELLSIZE) + bx, (int)(Raylib.GetMouseX()/CELLSIZE) + by] = 1;
                            blockCount++;
                            if (blockCount >= blocks.Length) {blockCount--;}
                        }
                    }
                }
            }

            // Drawing
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // Draw info
            Raylib.DrawFPS(10,10);
            Raylib.DrawText("Particles: " + sandCount, 10, 40, 20, Color.White);


            // scroll wheel and brush text
            if (brushTimer > 0) 
            {
                brushTimer -= dt; // decrease timer
                radiusText = "Brush Size: " + radius;
                Raylib.DrawCircleV(Raylib.GetMousePosition(), radius, Color.LightGray);
                Raylib.DrawText(radiusText, Raylib.GetMouseX() - (Raylib.MeasureText(radiusText, 20)/2), Raylib.GetMouseY() - 30, 20, Color.White);
            }
            

            // update blocks
            for (int b = 0; b < blockCount; b++)
            {
                Raylib.DrawRectangleV(blocks[b], SANDSIZE, Color.White);
            }

            // update sand
            for (int i = 0; i < sandCount; i++)
            {
                // If the sand particle is active and the big 3 checks 
                if (sands[i].Alive == true)
                {                   
                    // The big three checks
                    if (map[(int)sands[i].Cell.Y + 1, (int)sands[i].Cell.X] == 0)
                    {
                        sands[i].Position.Y += speed ; // gravity
                        CALCULATECELLID(ref sands[i].Cell, sands[i].Position); 

                        // Changing the Color
                        sands[i].color = LerpColor(DesertYellow, DesertRed, (float)sands[i].Position.Y / WINHEIGHT);
                    }
                    else if (map[(int)sands[i].Cell.Y + 1, (int)sands[i].Cell.X + 1] == 0 && rng.NextSingle() < 0.7f)
                    {
                        sands[i].Position.Y += speed; // gravity
                        sands[i].Position.X += speed; // To the right
                        CALCULATECELLID(ref sands[i].Cell, sands[i].Position);
                    }
                    else if (map[(int)sands[i].Cell.Y + 1, (int)sands[i].Cell.X - 1] == 0&& rng.NextSingle() < 0.7f)
                    {
                        sands[i].Position.Y += speed; // gravity
                        sands[i].Position.X -= speed; // To the left
                        CALCULATECELLID(ref sands[i].Cell, sands[i].Position);
                    }
                    else {}

                }

                // Drawing
                Raylib.DrawRectangleV(sands[i].Position, SANDSIZE, sands[i].color);
                
            }

            Raylib.EndDrawing();
        }

        // Unloading
        Raylib.UnloadImage(windowIcon);
        //Raylib.CloseWindow();

    }
}