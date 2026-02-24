using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace VoxelGame;

public class Chunk
{
    const int SIZE = 64;
    int[,,] blocks = new int[SIZE, SIZE, SIZE];

    int vao;
    int vbo;
    int vertexCount;

    int baseX;
    int baseZ;
    List<Chunk>? allChunks;

    public Chunk(int chunkX, int chunkZ)
    {
        baseX = chunkX;
        baseZ = chunkZ;

        // Generate terrain heights using noise
        for (int x = 0; x < SIZE; x++)
        for (int z = 0; z < SIZE; z++)
        {
            int gx = chunkX * SIZE + x;
            int gz = chunkZ * SIZE + z;
            float n = Noise.FractalNoise2D(gx * 0.08f, gz * 0.08f, 4);
            int height = (int)(n * 6f) + 6; // adjust for nicer hills
            height = Math.Clamp(height, 0, SIZE-1);

            for (int y = 0; y <= height; y++)
            {
                if (y == height) blocks[x,y,z] = 1; // grass top
                else if (y >= height-3) blocks[x,y,z] = 2; // dirt (green)
                else blocks[x,y,z] = 3; // stone (green)
            }
        }

        BuildMesh();
    }

    // Called by the world to provide access to other chunks for neighbor checks
    public void SetChunksReference(List<Chunk> chunks)
    {
        allChunks = chunks;
    }

    void BuildMesh()
    {
        List<float> vertices = new();

        for (int x = 0; x < SIZE; x++)
        for (int y = 0; y < SIZE; y++)
        for (int z = 0; z < SIZE; z++)
        {
            if (blocks[x,y,z] == 0) continue;

            Vector3 pos = new(baseX * SIZE + x, y, baseZ * SIZE + z);

            int blockType = blocks[x,y,z];
            
            // Üst
            bool aboveOccupied;
            if (y+1 < SIZE) aboveOccupied = blocks[x,y+1,z] != 0;
            else aboveOccupied = IsGlobalOccupied(baseX * SIZE + x, y+1, baseZ * SIZE + z);
            if (!aboveOccupied)
                AddTop(vertices, pos, blockType);

            // Alt
            bool belowOccupied;
            if (y-1 >= 0) belowOccupied = blocks[x,y-1,z] != 0;
            else belowOccupied = IsGlobalOccupied(baseX * SIZE + x, y-1, baseZ * SIZE + z);
            if (!belowOccupied)
                AddBottom(vertices, pos, blockType);

            // Sağ
            bool rightOccupied;
            if (x+1 < SIZE) rightOccupied = blocks[x+1,y,z] != 0;
            else rightOccupied = IsGlobalOccupied(baseX * SIZE + x+1, y, baseZ * SIZE + z);
            if (!rightOccupied)
                AddRight(vertices, pos, blockType);

            // Sol
            bool leftOccupied;
            if (x-1 >= 0) leftOccupied = blocks[x-1,y,z] != 0;
            else leftOccupied = IsGlobalOccupied(baseX * SIZE + x-1, y, baseZ * SIZE + z);
            if (!leftOccupied)
                AddLeft(vertices, pos, blockType);

            // Ön
            bool frontOccupied;
            if (z+1 < SIZE) frontOccupied = blocks[x,y,z+1] != 0;
            else frontOccupied = IsGlobalOccupied(baseX * SIZE + x, y, baseZ * SIZE + z+1);
            if (!frontOccupied)
                AddFront(vertices, pos, blockType);

            // Arka
            bool backOccupied;
            if (z-1 >= 0) backOccupied = blocks[x,y,z-1] != 0;
            else backOccupied = IsGlobalOccupied(baseX * SIZE + x, y, baseZ * SIZE + z-1);
            if (!backOccupied)
                AddBack(vertices, pos, blockType);
        }

        vertexCount = vertices.Count / 5;

        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();

        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

        GL.BufferData(BufferTarget.ArrayBuffer,
            vertices.Count * sizeof(float),
            vertices.ToArray(),
            BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5*sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5*sizeof(float), 3*sizeof(float));
        GL.EnableVertexAttribArray(1);
    }

    (float u, float v) GetTexCoord(int blockType, int face, int corner)
    {
        // Each cell in atlas is 0.25x0.25
        float cellSize = 0.25f;
        float u = 0f, v = 0f;

        // Assign UV cells based on block type and face
        // Atlas mapping (see Game.cs):
        // (0,0): grass top, (1,0): dirt, (2,0): grass side, (3,0): stone
        if (blockType == 1) // grass
        {
            if (face == 0) { u = 0f; v = 0f; }            // top -> (0,0)
            else if (face == 1) { u = cellSize * 1; v = 0f; } // bottom -> (1,0)
            else { u = cellSize * 2; v = 0f; }            // sides -> (2,0)
        }
        else if (blockType == 2) // dirt
        {
            u = cellSize * 1; v = 0f; // (1,0)
        }
        else if (blockType == 3) // stone
        {
            u = cellSize * 3; v = 0f; // (3,0)
        }
        else
        {
            u = 0f; v = 0f;
        }

        // Add corner offset within the selected atlas cell
        float[] cornerU = { 0f, cellSize, cellSize, 0f };
        float[] cornerV = { 0f, 0f, cellSize, cellSize };
        int ci = corner % 4;
        u += cornerU[ci];
        v += cornerV[ci];

        return (u, v);
    }
    
    void AddQuad(List<float> v, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int blockType, int face)
    {
        // 2 triangles
        AddTri(v, a, b, c, blockType, face, 0, 1, 2);
        AddTri(v, c, d, a, blockType, face, 2, 3, 0);
    }
    
    void AddVertex(List<float> verts, Vector3 pos, float u, float tv)
    {
        verts.Add(pos.X);
        verts.Add(pos.Y);
        verts.Add(pos.Z);
        verts.Add(u);
        verts.Add(tv);
    }

    void AddTri(List<float> v, Vector3 a, Vector3 b, Vector3 c, int blockType, int face, int c0, int c1, int c2)
    {  
        var (u0, v0) = GetTexCoord(blockType, face, c0);
        var (u1, v1) = GetTexCoord(blockType, face, c1);
        var (u2, v2) = GetTexCoord(blockType, face, c2);
        
        AddVertex(v, a, u0, v0);
        AddVertex(v, b, u1, v1);
        AddVertex(v, c, u2, v2);
    }
    void AddTop(List<float> v, Vector3 p, int blockType)
    {
        AddQuad(v,
            p + new Vector3(-0.5f,0.5f,-0.5f),
            p + new Vector3( 0.5f,0.5f,-0.5f),
            p + new Vector3( 0.5f,0.5f, 0.5f),
            p + new Vector3(-0.5f,0.5f, 0.5f), blockType, 0);
    }

    void AddBottom(List<float> v, Vector3 p, int blockType)
    {
        AddQuad(v,
            p + new Vector3(-0.5f,-0.5f,-0.5f),
            p + new Vector3( 0.5f,-0.5f,-0.5f),
            p + new Vector3( 0.5f,-0.5f, 0.5f),
            p + new Vector3(-0.5f,-0.5f, 0.5f), blockType, 1);
    }

    void AddRight(List<float> v, Vector3 p, int blockType)
    {
        AddQuad(v,
            p + new Vector3(0.5f,-0.5f,-0.5f),
            p + new Vector3(0.5f,-0.5f, 0.5f),
            p + new Vector3(0.5f, 0.5f, 0.5f),
            p + new Vector3(0.5f, 0.5f,-0.5f), blockType, 2);
    }

    void AddLeft(List<float> v, Vector3 p, int blockType)
    {
        AddQuad(v,
            p + new Vector3(-0.5f,-0.5f,-0.5f),
            p + new Vector3(-0.5f,-0.5f, 0.5f),
            p + new Vector3(-0.5f, 0.5f, 0.5f),
            p + new Vector3(-0.5f, 0.5f,-0.5f), blockType, 2);
    }

    void AddFront(List<float> v, Vector3 p, int blockType)
    {
        AddQuad(v,
            p + new Vector3(-0.5f,-0.5f,0.5f),
            p + new Vector3( 0.5f,-0.5f,0.5f),
            p + new Vector3( 0.5f, 0.5f,0.5f),
            p + new Vector3(-0.5f, 0.5f,0.5f), blockType, 2);
    }

    void AddBack(List<float> v, Vector3 p, int blockType)
    {
        AddQuad(v,
            p + new Vector3(-0.5f,-0.5f,-0.5f),
            p + new Vector3( 0.5f,-0.5f,-0.5f),
            p + new Vector3( 0.5f, 0.5f,-0.5f),
            p + new Vector3(-0.5f, 0.5f,-0.5f), blockType, 2);
    }

    public void Render()
    {
        GL.BindVertexArray(vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
    }

    public bool HasBlockAt(int globalX, int globalY, int globalZ)
    {
        // Convert global coordinates to chunk-local coordinates
        int localX = globalX - baseX * SIZE;
        int localZ = globalZ - baseZ * SIZE;

        // Check if out of bounds for this chunk
        if (localX < 0 || localX >= SIZE || globalY < 0 || globalY >= SIZE || localZ < 0 || localZ >= SIZE)
            return false;

        int blockType = blocks[localX, globalY, localZ];
        // Solid blocks are types 1 (grass), 2 (dirt), 3 (stone)
        return blockType == 1 || blockType == 2 || blockType == 3;
    }

    public bool BreakBlock(int globalX, int globalY, int globalZ, out int blockType)
    {
        int localX = globalX - baseX * SIZE;
        int localZ = globalZ - baseZ * SIZE;

        blockType = 0;

        if (localX < 0 || localX >= SIZE || globalY < 0 || globalY >= SIZE || localZ < 0 || localZ >= SIZE)
            return false;

        blockType = blocks[localX, globalY, localZ];
        if (blockType != 0)
        {
            blocks[localX, globalY, localZ] = 0;
            BuildMesh();
            return true;
        }
        return false;
    }

    public bool PlaceBlock(int globalX, int globalY, int globalZ, int blockType = 1)
    {
        int localX = globalX - baseX * SIZE;
        int localZ = globalZ - baseZ * SIZE;

        if (localX < 0 || localX >= SIZE || globalY < 0 || globalY >= SIZE || localZ < 0 || localZ >= SIZE)
            return false;

        if (blocks[localX, globalY, localZ] == 0)
        {
            blocks[localX, globalY, localZ] = blockType;
            BuildMesh();
            return true;
        }
        return false;
    }

    // Return block id at given global coordinates relative to this chunk (0 if outside bounds)
    public int GetBlockAtGlobal(int globalX, int globalY, int globalZ)
    {
        int localX = globalX - baseX * SIZE;
        int localZ = globalZ - baseZ * SIZE;

        if (localX < 0 || localX >= SIZE || globalY < 0 || globalY >= SIZE || localZ < 0 || localZ >= SIZE)
            return 0;

        return blocks[localX, globalY, localZ];
    }

    bool IsGlobalOccupied(int globalX, int globalY, int globalZ)
    {
        if (globalY < 0 || globalY >= SIZE) return false;
        if (allChunks == null) return false;

        foreach (var c in allChunks)
        {
            if (c.GetBlockAtGlobal(globalX, globalY, globalZ) != 0)
                return true;
        }
        return false;
    }
}