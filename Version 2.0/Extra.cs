using System;
using Raylib_cs;
using System.Numerics;
using static Global;

class Global
{
    public const int WINW = 1024;
    public const int WINH = 512;
    public static Color[] PixelBuffer = new Color[WINW * WINH];
    public static Image canvas = Raylib.GenImageColor(WINW, WINH, Color.Black);
    public static Texture2D canvasTex = Raylib.LoadTextureFromImage(canvas);
    public const short SANDSIZE = 1;
    public const int MAX_PARTICLES = 250000 / SANDSIZE; // Total Screen


    // Grid
    public static int Rows = WINH/SANDSIZE, CurrentRow = 0;
    public static int Columns = WINW/SANDSIZE, CurrentColumn = 0;
    public static byte[] Grid = new byte[Rows * Columns]; // access it by Grid[CurrentColumn + CurrentRow * Columns]

    public static void DefineGrid()
    {
        // current rows and columns
        int curColl = 0;
        int curRow = 0;

        for(int i = 0; i < Grid.Length; i++)
        {
            // make a border
            if (curRow == 0 || curRow == Rows - 1 || curColl == 0 || curColl == Columns - 1)
            {
                Grid[curColl + curRow * Columns] = 1;
            }

            // updating columns
            curColl++;
            if (curColl == Columns) {curRow++; curColl = 0;}
        }
    }

}
class Program
{

    struct Sands()
    {
        public short[] Y = new short[MAX_PARTICLES]; // 2 bytes each
        public short[] X = new short[MAX_PARTICLES]; // 2 bytes each
        public bool[] ALIVE = new bool[MAX_PARTICLES]; // 1 byte each
        public short[] Type = new short[MAX_PARTICLES]; // 0 for empty, 1 for sand, 2 for water
    }
    public static void Main()
    {
        // init window
        Raylib.InitWindow(WINW, WINH, "Sand Simulation");  
        Raylib.SetTargetFPS(120);
        float dt = 0;


        DefineGrid(); // define grid
        
        Sands s = new(); // sands
        int sandCounter = 0; // count the sand

        Random rng = new(); // randomizer

        Color emptyColor = new(0,0,0,0);

        // hitbox for screen
        float padding = 10;
        Vector2 screenHitSize = new(WINW - (padding * 2), WINH - (padding * 2));
        Vector2 screenHitPosition = new(padding, padding);
        Rectangle screenHitbox = new(screenHitPosition, screenHitSize);


        // drawing brush
        int radius = 5;
        int mRadius = 10;
        float brushTimer = 0;
        string radiusText;

        // types
        int curTypeIndex = 0;
        short[] types = [1, 2, 3];
        string[] typesString = ["SAND", "WATER", "ROCK"];
        short currentType = types[curTypeIndex];

        // directions for water
        Vector2[] directions = [
            new(-1,-1), new(-1,0), new(-1,1), // left column
            new(0,-1), new(0,1), // middle column
            new(1,-1), new(1,0), new(1,1) // right column
            ];
        Vector2 currentDirection = new();
        while (!Raylib.WindowShouldClose())
        {
            // update
            dt = Raylib.GetFrameTime(); // delta time

            // control draw size
            radius = (radius <= mRadius && radius > 0)?  radius + (int)Raylib.GetMouseWheelMove(): mRadius;
            if (Raylib.GetMouseWheelMove() != 0){brushTimer = 1;}

            // spawning sand
            if(Raylib.IsMouseButtonDown(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), screenHitbox))
            {
                // spawn brush
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx * dx + dy * dy <= radius * radius && // circle 
                            sandCounter < MAX_PARTICLES && // cap particle count
                            Grid[(int)(Raylib.GetMouseX() + dx) + (int)(Raylib.GetMouseY() + dy) * Columns] == 0 // mouse is in range
                           )
                        {
                            // Create Sand
                            s.X[sandCounter] = (short)(Raylib.GetMouseX() + dx);
                            s.Y[sandCounter] = (short)(Raylib.GetMouseY() + dy);
                            s.ALIVE[sandCounter] = true;
                            s.Type[sandCounter] = currentType;

                            sandCounter++; // Increase Count
                        }
                    }
                }
            }
            else if (Raylib.IsMouseButtonPressed(MouseButton.Right)) // type changing
            {
                curTypeIndex++;
                if(curTypeIndex == types.Length){curTypeIndex = 0;}
                currentType = types[curTypeIndex];
            }

            // update sand
            for (int i = 0; i < sandCounter; i++)
            {
                if (s.ALIVE[i] == true)
                {
                    Grid[s.X[i] + (s.Y[i]) * Columns] = 0; // reset the Grid
                    PixelBuffer[s.X[i] + (s.Y[i]) * Columns] = emptyColor;

                    switch (s.Type[i])
                    {
                        case 1: // sand

                        if (Grid[s.X[i] + (s.Y[i]+1) * Columns] == 0) // gravity
                        {
                            s.Y[i] += SANDSIZE;
                        }

                        if (Grid[(s.X[i]+1) + (s.Y[i]+1) * Columns] == 0 && rng.NextSingle() <= 0.5f) // right
                        {
                            s.X[i] += SANDSIZE;
                            s.Y[i] += SANDSIZE;
                        }
                        else if (Grid[(s.X[i]-1) + (s.Y[i]+1) * Columns] == 0) // left
                        {
                            s.X[i] -= SANDSIZE;
                            s.Y[i] += SANDSIZE;
                        }
                        else {}

                        // set grid and color to pixel buffer
                        Grid[s.X[i] + s.Y[i] * Columns] = 1; // set the grid to new cell
                        PixelBuffer[s.X[i] + (s.Y[i]) * Columns] = Color.Yellow; // set the pixel to yellow
                        break;
                        case 2: // water
                        if (Grid[s.X[i] + (s.Y[i]+1) * Columns] == 0) // gravity
                        {
                            s.Y[i] += SANDSIZE;
                        }
                        
                        // flailing
                        currentDirection = directions[Raylib.GetRandomValue(0,directions.Length-1)];

                        if (Grid[(s.X[i]+((short)(currentDirection.X))) + (s.Y[i]+((short)(currentDirection.Y))) * Columns] == 0)
                        {
                            s.X[i] += (short)(currentDirection.X * SANDSIZE);
                            s.Y[i] += (short)(currentDirection.Y * SANDSIZE);
                        }

                        // sliding
                        if (Grid[(s.X[i]+1) + (s.Y[i]) * Columns] == 0 && rng.NextSingle() < 0.5f) // right
                        {
                            s.X[i] += SANDSIZE;
                        }
                        else if (Grid[(s.X[i]-1) + (s.Y[i]) * Columns] == 0) // left
                        {
                            s.X[i] -= SANDSIZE;
                        }
                        else {}

                        // set grid and color to pixel buffer
                        Grid[s.X[i] + (s.Y[i]) * Columns] = 2; // set the grid to new cell --> 2 is sand
                        PixelBuffer[s.X[i] + (s.Y[i]) * Columns] = Color.Blue; // set the pixel to yellow
                        break;
                        case 3: // rock
                        if (Grid[s.X[i] + (s.Y[i]+1) * Columns] == 0) // gravity
                        {
                            s.Y[i] += SANDSIZE;
                        }
                        else {}

                        // set grid and color to pixel buffer
                        Grid[s.X[i] + s.Y[i] * Columns] = 1; // set the grid to new cell
                        if (rng.NextSingle() < 0.5f)
                        {
                            PixelBuffer[s.X[i] + (s.Y[i]) * Columns] = Color.DarkGray; // set the pixel to gray    
                        }
                        else
                        {
                            PixelBuffer[s.X[i] + (s.Y[i]) * Columns] = Color.LightGray; // set the pixel to lighter gray
                        }
                        break;
                    }

                    
                }
            }


            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // Draw info
            Raylib.DrawFPS(10,10);
            Raylib.DrawText("Particles: " + sandCounter, 10, 40, 20, Color.White); // particle num
            Raylib.DrawText("Type: " + typesString[curTypeIndex], 10, 60, 20, Color.White); // types
            

            // Drawing all the sand
            Raylib.UpdateTexture(canvasTex, PixelBuffer); // update the texture -- > Built in function
            Raylib.DrawTexture(canvasTex, 0, 0, Color.White); // draw full texture

            // scroll wheel and brush text
            if (brushTimer > 0) 
            {
                brushTimer -= dt; // decrease timer
                radiusText = "Brush Size: " + radius;
                Raylib.DrawCircleV(Raylib.GetMousePosition(), radius, Color.LightGray);
                Raylib.DrawText(radiusText, Raylib.GetMouseX() - (Raylib.MeasureText(radiusText, 20)/2), Raylib.GetMouseY() - 30, 20, Color.White);
            }

            Raylib.EndDrawing();
            
        }

        // Unloading
        
        // Pixel Buffer Image and Texture
        Raylib.UnloadImage(canvas);
        Raylib.UnloadTexture(canvasTex);

        // closing
        Raylib.CloseWindow();

    }
}
