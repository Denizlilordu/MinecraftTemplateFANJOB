# VoxelGame Template (Minecraft-like Engine)

C#/.NET tabanlı basit voxel oyun motoru şablonu. Minecraft benzeri oyunlar yapmak için başlangıç noktası.

## Özellikler
- Voxel blok sistemi
- Chunk yükleme / unloading
- Basit procedural generation
- **✨ Gün/Gece döngüsü**
- **✨ Dinamik ışık yayılması (Light Propagation)**
- **✨ Gölge sistemi**
- Opentk ile 3d rendering sistemi

## Yeni Özelllikler (eklenen)

### Gün/Gece Döngüsü
- Otomatik gün/gece geçişi
- Güneş konumuna göre dinamik aydınlatma
- Gece ışığı (ay ışığı - mavi renk)
- Bağlamsal aydınlatma (ambient lighting)

### Işık Yayılması (Light Propagation)
- Chunk başına ışık seviye hesabı (0-15)
- Dikey ışık yayılması (güneşten yukarıdan)
- Horizontal ışık yayılması
- Gölge oluşturması

### Gölge Sistemi
- Normal vektöre dayalı gölgeler
- Işık yönüne göre gölge hesabı
- Smooth gölge geçişleri

## Kurulum
1. Git clone https://github.com/denizlilordu/MinecraftTemplateFANJOB.git
2. cd MinecraftTemplateFANJOB/MCTemplateFANjob/VoxelGame
3. dotnet restore
4. dotnet run

## Kontroller
- **WASD**: Hareket
- **SPACE**: Zıpla
- **LeftCtrl**: Fare kilidi
- **ESC**: Fare kilidini aç
- **C**: Creative Fly Modu aç/kapat
- **1-9**: Envanterden blok seç
- **Sol Tık**: Blok kır
- **Sağ Tık**: Blok koy

## FPS Counter
Pencere başlığında FPS ve mevcut blok bilgisi gösterilir.

---
⚠️ Bu bir engine template i bir oyun değil - geri kalanı senin özgür geliştiriciliğine bağlıdır!



