using System.Collections.Generic;

namespace VoxelGame;

public class Inventory
{
    const int HOTBAR_SIZE = 9;
    const int MAX_STACK = 64;
    
    // Inventory slots: (blockType, count)
    // blockType: 1=grass, 2=dirt, 3=stone
    Dictionary<int, int> items = new();
    int selectedSlot = 0;

    public Inventory()
    {
        // Initialize hotbar slots with 0 items
        for (int i = 0; i < HOTBAR_SIZE; i++)
        {
            items[i] = 0;
        }
    }

    public void AddItem(int blockType, int count = 1)
    {
        // Find slot with same block type or empty slot
        for (int slot = 0; slot < HOTBAR_SIZE; slot++)
        {
            if (!items.ContainsKey(slot)) items[slot] = 0;
            
            int currentType = GetBlockTypeAt(slot);
            int currentCount = GetCountAt(slot);
            
            // If slot is empty or has same block type and space available
            if (currentType == 0 || currentType == blockType)
            {
                int canAdd = MAX_STACK - currentCount;
                int toAdd = System.Math.Min(canAdd, count);
                
                if (toAdd > 0)
                {
                    items[slot] = currentType == blockType ? 
                        (items[slot] >> 16) << 16 | (currentCount + toAdd) : 
                        (blockType << 16) | toAdd;
                    
                    count -= toAdd;
                    if (count <= 0) return;
                }
            }
        }
    }

    public int GetSelectedBlockType()
    {
        return GetBlockTypeAt(selectedSlot);
    }

    public int GetSelectedCount()
    {
        return GetCountAt(selectedSlot);
    }

    public bool TryUseSelected(out int blockType)
    {
        blockType = GetSelectedBlockType();
        if (blockType == 0) return false;

        int count = GetCountAt(selectedSlot);
        if (count <= 0) return false;

        // Decrease count
        int newCount = count - 1;
        if (newCount == 0)
        {
            items[selectedSlot] = 0;
        }
        else
        {
            items[selectedSlot] = (blockType << 16) | newCount;
        }

        return true;
    }

    public void SelectSlot(int slot)
    {
        if (slot >= 0 && slot < HOTBAR_SIZE)
            selectedSlot = slot;
    }

    public int GetSelectedSlot() => selectedSlot;

    public int GetBlockTypeAt(int slot)
    {
        if (!items.ContainsKey(slot)) return 0;
        return (items[slot] >> 16) & 0xFFFF;
    }

    public int GetCountAt(int slot)
    {
        if (!items.ContainsKey(slot)) return 0;
        return items[slot] & 0xFFFF;
    }

    public string GetHotbarDisplay()
    {
        string display = "";
        for (int i = 0; i < HOTBAR_SIZE; i++)
        {
            if (i == selectedSlot) display += "[";
            
            int type = GetBlockTypeAt(i);
            int count = GetCountAt(i);
            
            if (type == 0)
                display += " - ";
            else
                display += $"{type}:{count}";
            
            if (i == selectedSlot) display += "]";
            display += " ";
        }
        return display;
    }
}
