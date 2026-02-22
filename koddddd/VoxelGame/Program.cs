using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
namespace VoxelGame;
class Program
{
    static void Main()
    {
        var native = new NativeWindowSettings()
        {
            Size = new Vector2i(1280, 720),
            Title = "C# Voxel Prototype"
        };

        using var window = new Game(GameWindowSettings.Default, native);
        window.Run();
    }
}