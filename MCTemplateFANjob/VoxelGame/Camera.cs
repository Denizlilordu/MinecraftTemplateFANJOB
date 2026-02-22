using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
namespace VoxelGame;
public class Camera
{
    Vector3 position;
    float pitch = 0;
    float yaw = -90f;
    float speed = 10f;
    float mouseSensitivity = 0.2f;
    float sprintMultiplier = 1.8f;
    float gravity = -25f;
    float jumpSpeed = 8f;
    float velocityY = 0f;
    bool grounded = false;
    bool flyMode = false;
    float playerRadius = 0.3f;
    float playerHeight = 1.5f;  // Player is 1.5 blocks tall
    List<Chunk> chunks = null!;

    public Camera(Vector3 start)
    {
        position = start;
    }

    public void SetChunks(List<Chunk> chunkList)
    {
        chunks = chunkList;
    }

    bool IsBlockAtPosition(Vector3 pos)
    {
        if (chunks == null) return false;
        
        int x = (int)MathF.Floor(pos.X);
        int y = (int)MathF.Floor(pos.Y);
        int z = (int)MathF.Floor(pos.Z);

        foreach (var chunk in chunks)
        {
            if (chunk.HasBlockAt(x, y, z))
                return true;
        }
        return false;
    }

    bool CheckCollisionAtFeet(Vector3 newPos)
    {
        // Check only at feet level (position.Y - playerHeight)
        float checkRadius = playerRadius;
        float feetY = newPos.Y - playerHeight;
        
        // Check ground block below
        int groundY = (int)MathF.Floor(feetY);
        
        for (float dx = -checkRadius; dx <= checkRadius; dx += 0.2f)
        {
            for (float dz = -checkRadius; dz <= checkRadius; dz += 0.2f)
            {
                if (dx * dx + dz * dz <= checkRadius * checkRadius)
                {
                    if (IsBlockAtPosition(newPos + new Vector3(dx, -playerHeight, dz)))
                        return true;
                }
            }
        }
        return false;
    }
    
    Vector3 GetGroundPosition(Vector3 currentPos)
    {
        // Find the highest block below the player and position them on top
        float feetY = currentPos.Y - playerHeight;
        int checkY = (int)MathF.Floor(feetY);
        
        // Search downward from feet position
        for (int y = checkY; y >= checkY - 5; y--)
        {
            float checkRadius = playerRadius;
            bool hitBlock = false;
            
            for (float dx = -checkRadius; dx <= checkRadius; dx += 0.2f)
            {
                for (float dz = -checkRadius; dz <= checkRadius; dz += 0.2f)
                {
                    if (dx * dx + dz * dz <= checkRadius * checkRadius)
                    {
                        Vector3 checkPos = currentPos + new Vector3(dx, y - checkY, dz);
                        if (IsBlockAtPosition(checkPos))
                        {
                            hitBlock = true;
                            break;
                        }
                    }
                }
                if (hitBlock) break;
            }
            
            if (hitBlock)
            {
                // Position feet on top of this block
                return currentPos + new Vector3(0, y + 1 - checkY, 0);
            }
        }
        
        return currentPos;
    }

    bool CheckCollision(Vector3 newPos)
    {
        // Check collision cylinder: radius=0.3, height=2.5
        // Position is at eye level, so check from (position - playerHeight) to position
        float checkRadius = playerRadius;
        float feetY = newPos.Y - playerHeight;
        
        // Check at multiple heights along the player body
        for (float heightOffset = 0f; heightOffset <= playerHeight; heightOffset += 0.5f)
        {
            float checkY = feetY + heightOffset;
            
            // Check a circle at this height around the player position
            for (float dx = -checkRadius; dx <= checkRadius; dx += 0.2f)
            {
                for (float dz = -checkRadius; dz <= checkRadius; dz += 0.2f)
                {
                    if (dx * dx + dz * dz <= checkRadius * checkRadius)
                    {
                        if (IsBlockAtPosition(newPos + new Vector3(dx, heightOffset - playerHeight, dz)))
                            return true;
                    }
                }
            }
        }
        return false;
    }

    public void Update(KeyboardState k, MouseState m, float dt)
    {
        // Mouse look
        yaw += m.Delta.X * mouseSensitivity;
        pitch -= m.Delta.Y * mouseSensitivity;
        pitch = MathHelper.Clamp(pitch, -89f, 89f);

        // Movement on XZ plane
        var front = GetFront();
        var forward = new Vector3(front.X, 0f, front.Z).Normalized();
        var right = Vector3.Cross(forward, Vector3.UnitY).Normalized();

        float currentSpeed = speed * (k.IsKeyDown(Keys.LeftShift) ? sprintMultiplier : 1f);

        Vector3 move = Vector3.Zero;
        if (k.IsKeyDown(Keys.W)) move += forward;
        if (k.IsKeyDown(Keys.S)) move -= forward;
        if (k.IsKeyDown(Keys.A)) move -= right;
        if (k.IsKeyDown(Keys.D)) move += right;

        if (move.LengthSquared > 0)
            position += move.Normalized() * currentSpeed * dt;

        if (flyMode)
        {
            // In fly mode: no gravity, use Space/LeftShift/Q/E for vertical movement
            if (k.IsKeyDown(Keys.Space)) position.Y += currentSpeed * dt;
            if (k.IsKeyDown(Keys.LeftShift)) position.Y -= currentSpeed * dt;
            if (k.IsKeyDown(Keys.Q)) position.Y += currentSpeed * dt;
            if (k.IsKeyDown(Keys.E)) position.Y -= currentSpeed * dt;

            if (k.IsKeyPressed(Keys.Space)) System.Console.WriteLine("Fly: Space pressed (up)");
            if (k.IsKeyPressed(Keys.LeftShift)) System.Console.WriteLine("Fly: LeftShift pressed (down)");
            if (k.IsKeyPressed(Keys.Q)) System.Console.WriteLine("Fly: Q pressed (up)");
            if (k.IsKeyPressed(Keys.E)) System.Console.WriteLine("Fly: E pressed (down)");

            velocityY = 0f;
            grounded = false;
        }
        else
        {
            // Jump
            if (grounded && k.IsKeyPressed(Keys.Space))
            {
                velocityY = jumpSpeed;
                grounded = false;
            }

            // Gravity
            velocityY += gravity * dt;
            Vector3 newPos = position + new Vector3(0, velocityY * dt, 0);

            // Check collision at feet level
            if (velocityY < 0 && CheckCollisionAtFeet(newPos))
            {
                // Landing on a block - find exact ground position
                newPos = GetGroundPosition(position);
                velocityY = 0f;
                grounded = true;
            }
            else
            {
                grounded = false;
            }

            position = newPos;
        }

        // (Gravity/jump handled inside the non-fly branch)
    }

    public void ToggleFly()
    {
        flyMode = !flyMode;
        if (flyMode)
        {
            velocityY = 0f;
            grounded = false;
        }
        System.Console.WriteLine($"Fly mode: {flyMode}");
    }

    public bool IsFlyMode() => flyMode;
    
    public (Vector3 blockPos, Vector3 placePosi)? GetLookatBlock(float maxDistance = 10f)
    {
        Vector3 rayStart = position;
        Vector3 rayDir = GetFront();
        
        // Raycast to find block
        for (float dist = 0f; dist < maxDistance; dist += 0.1f)
        {
            Vector3 checkPos = rayStart + rayDir * dist;
            
            if (IsBlockAtPosition(checkPos))
            {
                // Found a block, return its grid position
                int blockX = (int)MathF.Floor(checkPos.X);
                int blockY = (int)MathF.Floor(checkPos.Y);
                int blockZ = (int)MathF.Floor(checkPos.Z);
                
                // Find which face we hit and placement position
                Vector3 blockCenter = new Vector3(blockX + 0.5f, blockY + 0.5f, blockZ + 0.5f);
                Vector3 hitNormal = checkPos - blockCenter;
                
                // Determine placement position (adjacent block)
                Vector3 placePos = new Vector3(blockX, blockY, blockZ);
                if (MathF.Abs(hitNormal.X) > MathF.Abs(hitNormal.Y) && MathF.Abs(hitNormal.X) > MathF.Abs(hitNormal.Z))
                {
                    placePos.X += hitNormal.X > 0 ? 1 : -1;
                }
                else if (MathF.Abs(hitNormal.Y) > MathF.Abs(hitNormal.Z))
                {
                    placePos.Y += hitNormal.Y > 0 ? 1 : -1;
                }
                else
                {
                    placePos.Z += hitNormal.Z > 0 ? 1 : -1;
                }
                
                return (new Vector3(blockX, blockY, blockZ), placePos);
            }
        }
        
        return null;
    }

    Vector3 GetFront()
    {
        return new Vector3(
            MathF.Cos(MathHelper.DegreesToRadians(yaw)) *
            MathF.Cos(MathHelper.DegreesToRadians(pitch)),
            MathF.Sin(MathHelper.DegreesToRadians(pitch)),
            MathF.Sin(MathHelper.DegreesToRadians(yaw)) *
            MathF.Cos(MathHelper.DegreesToRadians(pitch))
        ).Normalized();
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(position, position + GetFront(), Vector3.UnitY);
    }
}