using System;
using System.Collections.Generic;

namespace VoxelGame;

/// <summary>
/// Chunk içinde ışık yayılması hesaplaması (Sunlight propagation)
/// </summary>
public class LightPropagation
{
    const int SIZE = 64;
    const int MAX_LIGHT = 15; // 0-15 ışık seviyeleri
    const int SUN_LIGHT = 15; // Güneş ışığının başlangıç seviyesi
    
    private byte[,,] sunLight = new byte[SIZE, SIZE, SIZE]; // Güneş ışığı
    private byte[,,] blockLight = new byte[SIZE, SIZE, SIZE]; // Blok ışığı (gelecek)
    private int[,,] blocks; // Blok verisi referansı
    private int baseX, baseZ;
    private List<Chunk>? allChunks;

    public LightPropagation(int[,,] blockData, int chunkX, int chunkZ)
    {
        blocks = blockData;
        baseX = chunkX;
        baseZ = chunkZ;
        CalculateLighting();
    }

    public void SetChunksReference(List<Chunk> chunks)
    {
        allChunks = chunks;
        RecalculateLighting();
    }

    private void CalculateLighting()
    {
        // Arrayları sıfırla
        Array.Clear(sunLight, 0, sunLight.Length);
        Array.Clear(blockLight, 0, blockLight.Length);

        // Üstten başlayarak güneş ışığını yayıl
        for (int x = 0; x < SIZE; x++)
        for (int z = 0; z < SIZE; z++)
        {
            // Üstten aşağıya ışık yayıl
            for (int y = SIZE - 1; y >= 0; y--)
            {
                if (blocks[x, y, z] != 0) // Dolu blok
                {
                    // Işık doluluğa engel olur
                    sunLight[x, y, z] = 0;
                    break; // Altındaki blocklarla engellenir
                }
                else
                {
                    // Boş blok = maksimum güneş ışığı
                    sunLight[x, y, z] = SUN_LIGHT;
                }
            }
        }

        // Horizontal ışık yayılması (bir kez)
        PropagateLightHorizontal();
    }

    private void PropagateLightHorizontal()
    {
        byte[,,] tempLight = (byte[,,])sunLight.Clone();

        for (int y = 0; y < SIZE; y++)
        for (int x = 0; x < SIZE; x++)
        for (int z = 0; z < SIZE; z++)
        {
            if (blocks[x, y, z] != 0) continue; // Dolu bloklar ışığı engeller

            byte maxLight = tempLight[x, y, z];

            // Komşu blocklardan ışık al
            maxLight = Math.Max(maxLight, GetNeighborLight(x + 1, y, z, tempLight));
            maxLight = Math.Max(maxLight, GetNeighborLight(x - 1, y, z, tempLight));
            maxLight = Math.Max(maxLight, GetNeighborLight(x, y, z + 1, tempLight));
            maxLight = Math.Max(maxLight, GetNeighborLight(x, y, z - 1, tempLight));
            maxLight = Math.Max(maxLight, GetNeighborLight(x, y + 1, z, tempLight));
            maxLight = Math.Max(maxLight, GetNeighborLight(x, y - 1, z, tempLight));

            // Işık mesafesel olarak azalır
            if (maxLight > 0)
                sunLight[x, y, z] = (byte)Math.Max(sunLight[x, y, z], maxLight - 1);
        }
    }

    private byte GetNeighborLight(int x, int y, int z, byte[,,] lightMap)
    {
        if (x < 0 || x >= SIZE || y < 0 || y >= SIZE || z < 0 || z >= SIZE)
            return 0;

        if (blocks[x, y, z] != 0) // Dolu blok ışık yaymaz
            return 0;

        return lightMap[x, y, z];
    }

    private void RecalculateLighting()
    {
        CalculateLighting();
    }

    /// <summary>
    /// Verilen pozisyonda ışık seviyesi (0-15)
    /// </summary>
    public byte GetLightLevel(int x, int y, int z)
    {
        if (x < 0 || x >= SIZE || y < 0 || y >= SIZE || z < 0 || z >= SIZE)
            return 0;

        byte light = sunLight[x, y, z];
        byte bLight = blockLight[x, y, z];
        return (byte)Math.Max(light, bLight);
    }

    /// <summary>
    /// 0-1 normalizlenmiş ışık değeri
    /// </summary>
    public float GetNormalizedLight(int x, int y, int z)
    {
        byte light = GetLightLevel(x, y, z);
        return light / (float)MAX_LIGHT;
    }

    /// <summary>
    /// Blok ışığı ekle (torch, lava vb)
    /// </summary>
    public void SetBlockLight(int x, int y, int z, byte level)
    {
        if (x >= 0 && x < SIZE && y >= 0 && y < SIZE && z >= 0 && z < SIZE)
            blockLight[x, y, z] = (byte)Math.Min((int)level, MAX_LIGHT);
    }

    public byte[,,] GetSunLightMap() => sunLight;
    public byte[,,] GetBlockLightMap() => blockLight;
}
