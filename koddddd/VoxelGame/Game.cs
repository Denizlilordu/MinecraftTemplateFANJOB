using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
namespace VoxelGame;
public class Game : GameWindow
{
    Shader shader = null!;
    List<Chunk> chunks = new();
    Camera camera;
    int texture;
    int crosshairVao;
    int crosshairVbo;

    public Game(GameWindowSettings g, NativeWindowSettings n) : base(g, n) { }

    protected override void OnLoad()
    {
        
        GL.ClearColor(0.5f, 0.7f, 1.0f, 1.0f);
        GL.Enable(EnableCap.DepthTest);

        string vertex = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec2 aTexCoord;

        uniform mat4 model;
        uniform mat4 view;
        uniform mat4 projection;

        out vec2 TexCoord;

        void main()
        {
            TexCoord = aTexCoord;
            gl_Position = projection * view * model * vec4(aPos, 1.0);
        }";

        string fragment = @"
        #version 330 core
        out vec4 FragColor;
        in vec2 TexCoord;
        uniform sampler2D tex;

        void main()
        {
            FragColor = texture(tex, TexCoord);
        }";

        shader = new Shader(vertex, fragment);

        // Create a 3x3 chunk area centered on 0,0
        for (int cx = -1; cx <= 1; cx++)
        for (int cz = -1; cz <= 1; cz++)
        {
            chunks.Add(new Chunk(cx, cz));
        }

        camera = new Camera(new Vector3(8, 10, 25));
        camera.SetChunks(chunks);

        // Create a 4x4 cell atlas (16x16 px) programmatically
        texture = GL.GenTexture();
        GL.BindTexture(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, texture);

        int atlasCells = 4;
        int cellSize = 4; // each cell 4x4 px => atlas 16x16
        int atlasSize = atlasCells * cellSize;
        byte[] texData = new byte[atlasSize * atlasSize * 4];

        // helper to set pixel
        void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            int idx = (y * atlasSize + x) * 4;
            texData[idx + 0] = r;
            texData[idx + 1] = g;
            texData[idx + 2] = b;
            texData[idx + 3] = a;
        }

        // define cell colors: (cellX,cellY)
        // (0,0): grass top, (1,0): dirt, (2,0): grass side, (3,0): stone
        // (0,1): dirt green, (1,1): stone green
        (byte r,byte g,byte b) grassTop = (34,139,34);
        (byte r,byte g,byte b) dirt = (134,96,67);
        (byte r,byte g,byte b) grassSide = (106,76,50);
        (byte r,byte g,byte b) stone = (120,120,120);
        (byte r,byte g,byte b) dirtGreen = (50,150,50);
        (byte r,byte g,byte b) stoneGreen = (80,140,80);
        (byte r,byte g,byte b) filler = (255,0,255);

        for (int cy = 0; cy < atlasCells; cy++)
        for (int cx = 0; cx < atlasCells; cx++)
        {
            (byte r,byte g,byte b) col = filler;
            if (cx == 0 && cy == 0) col = grassTop;
            if (cx == 1 && cy == 0) col = dirt;
            if (cx == 2 && cy == 0) col = grassSide;
            if (cx == 3 && cy == 0) col = stone;
            if (cx == 0 && cy == 1) col = dirtGreen;
            if (cx == 1 && cy == 1) col = stoneGreen;

            int sx = cx * cellSize;
            int sy = cy * cellSize;
            for (int py = 0; py < cellSize; py++)
            for (int px = 0; px < cellSize; px++)
                SetPixel(sx + px, sy + py, col.r, col.g, col.b, 255);
        }

        GL.TexImage2D(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, 0,
            OpenTK.Graphics.OpenGL4.PixelInternalFormat.Rgba, atlasSize, atlasSize, 0,
            OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, OpenTK.Graphics.OpenGL4.PixelType.UnsignedByte, texData);
        GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
        GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureWrapT, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
        GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureMinFilter, (int)OpenTK.Graphics.OpenGL4.TextureMinFilter.Nearest);
        GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureMagFilter, (int)OpenTK.Graphics.OpenGL4.TextureMagFilter.Nearest);

        // Create crosshair
        float[] crosshairVerts = new float[]
        {
            -0.02f, 0f, 0f,
            0.02f, 0f, 0f,
            0f, -0.02f, 0f,
            0f, 0.02f, 0f
        };
        
        crosshairVao = GL.GenVertexArray();
        crosshairVbo = GL.GenBuffer();
        
        GL.BindVertexArray(crosshairVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, crosshairVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, crosshairVerts.Length * sizeof(float), crosshairVerts, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindVertexArray(0);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        // Toggle creative fly mode with C
        if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.C))
        {
            camera.ToggleFly();
        }
        
        camera.SetChunks(chunks);
        // Lock cursor while Ctrl is held, free it on Escape
        if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftControl) ||
            KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightControl))
        {
            CursorState = OpenTK.Windowing.Common.CursorState.Grabbed;
        }

        if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
        {
            CursorState = OpenTK.Windowing.Common.CursorState.Normal;
        }

        // Block breaking (left mouse button - single click)
        if (MouseState.IsButtonPressed(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left))
        {
            var lookat = camera.GetLookatBlock();
            if (lookat.HasValue)
            {
                var blockPos = lookat.Value.blockPos;
                foreach (var chunk in chunks)
                {
                    if (chunk.BreakBlock((int)blockPos.X, (int)blockPos.Y, (int)blockPos.Z))
                        break;
                }
            }
        }

        // Block placing (right mouse button - single click)
        if (MouseState.IsButtonPressed(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right))
        {
            var lookat = camera.GetLookatBlock();
            if (lookat.HasValue)
            {
                var placePos = lookat.Value.placePosi;
                foreach (var chunk in chunks)
                {
                    if (chunk.PlaceBlock((int)placePos.X, (int)placePos.Y, (int)placePos.Z, 2))
                        break;
                }
            }
        }

        camera.Update(KeyboardState, MouseState, (float)e.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var view = camera.GetViewMatrix();
        
        var projection = Matrix4.CreatePerspectiveFieldOfView(
        

        MathHelper.DegreesToRadians(70f),

        Size.X / (float)Size.Y,
        0.1f, 100f);

        shader.Use();

        shader.SetMatrix4("view", view);
        shader.SetMatrix4("projection", projection);
        shader.SetMatrix4("model", Matrix4.Identity);

        // Bind texture unit 0
        OpenTK.Graphics.OpenGL4.GL.ActiveTexture(OpenTK.Graphics.OpenGL4.TextureUnit.Texture0);
        OpenTK.Graphics.OpenGL4.GL.BindTexture(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, texture);
        shader.SetInt("tex", 0);

        foreach (var c in chunks)
            c.Render();

        RenderCrosshair();

        SwapBuffers();
    }

    void RenderCrosshair()
    {
        GL.Disable(EnableCap.DepthTest);
        
        var orthoProj = Matrix4.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1);
        var view = Matrix4.Identity;
        var model = Matrix4.CreateTranslation(Size.X / 2f, Size.Y / 2f, 0);
        
        shader.Use();
        shader.SetMatrix4("projection", orthoProj);
        shader.SetMatrix4("view", view);
        shader.SetMatrix4("model", model);
        
        GL.BindVertexArray(crosshairVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, 4);
        
        GL.Enable(EnableCap.DepthTest);
    }
}