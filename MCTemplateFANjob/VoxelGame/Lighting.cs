using OpenTK.Mathematics;
using System;

namespace VoxelGame;

/// <summary>
/// Gün/Gece döngüsü, ışık yayılması ve gölge sistemi
/// </summary>
public class Lighting
{
    private float timeOfDay = 0.5f; // 0-1 arasında (0=gece, 0.5=gündüz)
    private float dayLengthSeconds = 120f; // 2 dakika döngü
    private float elapsedTime = 0f;
    private bool fullbriteMode = false;

    // Gün/Gece ışık özellikleri
    public Vector3 LightDirection { get; private set; }
    public Vector3 LightColor { get; private set; }
    public Vector3 AmbientColor { get; private set; }
    public float LightIntensity { get; private set; }

    public Lighting()
    {
        UpdateLighting();
    }

    public void Update(float deltaTime)
    {
        elapsedTime += deltaTime;
        timeOfDay = (elapsedTime % dayLengthSeconds) / dayLengthSeconds;
        UpdateLighting();
    }

    private void UpdateLighting()
    {
        // Fullbrite modu - heryerde parlak
        if (fullbriteMode)
        {
            LightDirection = Vector3.UnitY;
            LightColor = Vector3.One;
            AmbientColor = Vector3.One;
            LightIntensity = 1.0f;
            return;
        }

        // Normal lighting döngüsü
        
        float sunRotation = timeOfDay * MathF.PI * 2f; // 0 ile 2π arasında
        
        // Güneş konumu
        float sunHeight = MathF.Sin(timeOfDay * MathF.PI - MathF.PI / 2f);
        float sunX = MathF.Sin(sunRotation) * 50f;
        float sunY = MathF.Max(sunHeight * 50f, -10f); // Gece bile bazı ışık
        float sunZ = MathF.Cos(sunRotation) * 50f;
        
        LightDirection = new Vector3(sunX, sunY, sunZ).Normalized();

        // Gün/Gece rengi ve yoğunluğu
        if (timeOfDay >= 0.2f && timeOfDay <= 0.8f) // Gündüz
        {
            float dayFactor = MathF.Sin((timeOfDay - 0.2f) / 0.6f * MathF.PI);
            dayFactor = MathHelper.Clamp(dayFactor, 0.3f, 1.0f);
            
            LightIntensity = dayFactor;
            
            // Gündüz: Sarı/Beyaz ışık
            LightColor = Vector3.Lerp(
                new Vector3(1.0f, 0.8f, 0.6f),  // Sabah/Akşam
                new Vector3(1.0f, 1.0f, 0.95f), // Öğle
                MathF.Abs(timeOfDay - 0.5f) * 2f
            ) * dayFactor;
        }
        else // Gece
        {
            LightIntensity = 0.65f; // Ay ışığı - daha güçlü
            
            // Gece: Mavi/Mor ay ışığı
            LightColor = new Vector3(0.5f, 0.6f, 1.0f);
        }

        // Ortamsal ışık - gece de yeterli aydınlık
        float ambientBase = MathHelper.Lerp(0.35f, 0.7f, MathF.Sin((timeOfDay - 0.2f) / 0.6f * MathF.PI));
        AmbientColor = new Vector3(
            0.95f * ambientBase,
            0.98f * ambientBase,
            1.0f * ambientBase
        );
    }

    public float GetTimeOfDay() => timeOfDay;
    
    public void SetDayLength(float seconds)
    {
        dayLengthSeconds = seconds;
    }

    public void ToggleFullbrite()
    {
        fullbriteMode = !fullbriteMode;
        UpdateLighting();
    }

    public bool IsFullbrite() => fullbriteMode;
}
