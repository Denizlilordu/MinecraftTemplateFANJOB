using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
namespace VoxelGame;
public class Game : GameWindow
{
    Shader shader = null!;
    Shader crosshairShader = null!;
    List<Chunk> chunks = new();
    Camera camera;
    Inventory inventory = new();
    Lighting lighting = new();
    int texture;
    int crosshairVao;
    int crosshairVbo;

    // FPS Counter
    double fpsCounter = 0;
    int frameCount = 0;
    double fpsUpdateInterval = 0.5; // update every 0.5 seconds

    public Game(GameWindowSettings g, NativeWindowSettings n) : base(g, n) { }

    protected override void OnLoad()
    {
        
        GL.ClearColor(0.5f, 0.7f, 1.0f, 1.0f);
        GL.Enable(EnableCap.DepthTest);

        string vertex = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec2 aTexCoord;
        layout (location = 2) in vec3 aNormal;
        layout (location = 3) in float aLight;

        uniform mat4 model;
        uniform mat4 view;
        uniform mat4 projection;

        out vec2 TexCoord;
        out vec3 Normal;
        out vec3 FragPos;
        out float Light;

        void main()
        {
            TexCoord = aTexCoord;
            Normal = normalize(mat3(transpose(inverse(model))) * aNormal);
            FragPos = vec3(model * vec4(aPos, 1.0));
            Light = aLight;
            gl_Position = projection * view * model * vec4(aPos, 1.0);
        }";

        string fragment = @"
        #version 330 core
        out vec4 FragColor;
        in vec2 TexCoord;
        in vec3 Normal;
        in vec3 FragPos;
        in float Light;
        
        uniform sampler2D tex;
        uniform vec3 lightDir;
        uniform vec3 lightColor;
        uniform vec3 ambientColor;
        uniform float lightIntensity;

        void main()
        {
            vec4 texColor = texture(tex, TexCoord);
            
            vec3 norm = normalize(Normal);
            float diff = max(dot(norm, normalize(lightDir)), 0.0);
            
            vec3 diffuse = (diff * 0.7 + 0.2) * lightColor * lightIntensity;
            
            vec3 ambient = ambientColor * (Light / 15.0);
            
            ambient += ambientColor * 0.2;
            
            vec3 lighting = ambient + diffuse * 0.8;
            
            FragColor = vec4(texColor.rgb * lighting, texColor.a);
        }";

        shader = new Shader(vertex, fragment);

        // Create crosshair shader
        string crosshairVert = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;
        
        uniform mat4 model;
        uniform mat4 view;
        uniform mat4 projection;
        
        void main()
        {
            gl_Position = projection * view * model * vec4(aPos, 1.0);
        }";

        string crosshairFrag = @"
        #version 330 core
        out vec4 FragColor;
        
        void main()
        {
            FragColor = vec4(1.0, 1.0, 1.0, 1.0);
        }";

        crosshairShader = new Shader(crosshairVert, crosshairFrag);

        // Create a 3x3 chunk area centered on 0,0
        for (int cx = -1; cx <= 1; cx++)
        for (int cz = -1; cz <= 1; cz++)
        {
            chunks.Add(new Chunk(cx, cz));
        }

        camera = new Camera(new Vector3(8, 10, 25));
        camera.SetChunks(chunks);

        // Give each chunk a reference to the full chunk list so they can query neighbors
        foreach (var ch in chunks)
            ch.SetChunksReference(chunks);

        // Create a 4x4 cell atlas (256x256 px) programmatically with high detail
        texture = GL.GenTexture();
        GL.BindTexture(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, texture);

        int atlasCells = 4;
        int cellSize = 64; // each cell 64x64 px => atlas 256x256
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

        // Helper to add procedural noise/details to a block of pixels
        void FillCellWithDetail(int cellX, int cellY, byte r, byte g, byte b)
        {
            int sx = cellX * cellSize;
            int sy = cellY * cellSize;
            System.Random rand = new(cellX * 1000 + cellY);

            for (int py = 0; py < cellSize; py++)
            {
                for (int px = 0; px < cellSize; px++)
                {
                    // Add slight color variation for texture details
                    int variation = rand.Next(-20, 20);
                    byte rr = (byte)System.Math.Clamp(r + variation, 0, 255);
                    byte gg = (byte)System.Math.Clamp(g + variation, 0, 255);
                    byte bb = (byte)System.Math.Clamp(b + variation, 0, 255);

                    // Add subtle dot pattern for extra detail
                    if (rand.Next(100) < 3)
                    {
                        rr = (byte)System.Math.Clamp(rr - 30, 0, 255);
                        gg = (byte)System.Math.Clamp(gg - 30, 0, 255);
                        bb = (byte)System.Math.Clamp(bb - 30, 0, 255);
                    }

                    SetPixel(sx + px, sy + py, rr, gg, bb, 255);
                }
            }
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

        // Fill cells with detailed textures
        FillCellWithDetail(0, 0, grassTop.r, grassTop.g, grassTop.b);
        FillCellWithDetail(1, 0, dirt.r, dirt.g, dirt.b);
        FillCellWithDetail(2, 0, grassSide.r, grassSide.g, grassSide.b);
        FillCellWithDetail(3, 0, stone.r, stone.g, stone.b);
        FillCellWithDetail(0, 1, dirtGreen.r, dirtGreen.g, dirtGreen.b);
        FillCellWithDetail(1, 1, stoneGreen.r, stoneGreen.g, stoneGreen.b);
        
        // Fill remaining filler cells
        for (int y = 0; y < cellSize * 2; y++)
        {
            for (int x = 0; x < cellSize * 2; x++)
            {
                SetPixel(cellSize * 2 + x, cellSize * 2 + y, filler.r, filler.g, filler.b, 255);
            }
        }

        GL.TexImage2D(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, 0,
            OpenTK.Graphics.OpenGL4.PixelInternalFormat.Rgba, atlasSize, atlasSize, 0,
            OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, OpenTK.Graphics.OpenGL4.PixelType.UnsignedByte, texData);
        GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
        GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureWrapT, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
        GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureMinFilter, (int)OpenTK.Graphics.OpenGL4.TextureMinFilter.Nearest);
        GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureMagFilter, (int)OpenTK.Graphics.OpenGL4.TextureMagFilter.Nearest);

        // Enable anisotropic filtering for better texture quality at angles
        GL.GetFloat((GetPName)0x84FF, out float maxAnisotropy); // GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT
        if (maxAnisotropy > 0)
        {
            GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, 
                (OpenTK.Graphics.OpenGL4.TextureParameterName)0x84FE, // GL_TEXTURE_MAX_ANISOTROPY_EXT
                maxAnisotropy);
        }

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
        // Update lighting
        lighting.Update((float)e.Time);
        
        // Toggle fullbrite with F
        if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F))
        {
            lighting.ToggleFullbrite();
        }
        
        // Toggle creative fly mode with C
        if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.C))
        {
            camera.ToggleFly();
        }
        
        // Hotbar slot selection (1-9 keys)
        for (int i = 0; i < 9; i++)
        {
            var key = (OpenTK.Windowing.GraphicsLibraryFramework.Keys)('1' + i);
            if (KeyboardState.IsKeyPressed(key))
                inventory.SelectSlot(i);
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
                    if (chunk.BreakBlock((int)blockPos.X, (int)blockPos.Y, (int)blockPos.Z, out int brokenBlockType))
                    {
                        // Add broken block to inventory
                        if (brokenBlockType != 0)
                            inventory.AddItem(brokenBlockType, 1);
                        break;
                    }
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
                if (inventory.TryUseSelected(out int blockType))
                {
                    foreach (var chunk in chunks)
                    {
                        if (chunk.PlaceBlock((int)placePos.X, (int)placePos.Y, (int)placePos.Z, blockType))
                            break;
                    }
                }
            }
        }

        camera.Update(KeyboardState, MouseState, (float)e.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        // Update FPS counter and display inventory
        fpsCounter += e.Time;
        frameCount++;
        if (fpsCounter >= fpsUpdateInterval)
        {
            double fps = frameCount / fpsCounter;
            int selectedBlockType = inventory.GetSelectedBlockType();
            int selectedCount = inventory.GetSelectedCount();
            string blockName = selectedBlockType switch 
            {
                1 => "Grass",
                2 => "Dirt", 
                3 => "Stone",
                _ => "None"
            };
            Title = $"C# Voxel Prototype - FPS: {fps:F1} | Slot: {selectedBlockType} {blockName} x{selectedCount}";
            fpsCounter = 0;
            frameCount = 0;
        }

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
        
        // Lighting uniforms
        shader.SetVector3("lightDir", lighting.LightDirection);
        shader.SetVector3("lightColor", lighting.LightColor);
        shader.SetVector3("ambientColor", lighting.AmbientColor);
        shader.SetFloat("lightIntensity", lighting.LightIntensity);

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
        
        crosshairShader.Use();
        crosshairShader.SetMatrix4("projection", orthoProj);
        crosshairShader.SetMatrix4("view", view);
        crosshairShader.SetMatrix4("model", model);
        
        GL.BindVertexArray(crosshairVao);
        GL.LineWidth(2f);
        GL.DrawArrays(PrimitiveType.Lines, 0, 4);
        GL.LineWidth(1f);
        
        GL.Enable(EnableCap.DepthTest);
    }
}