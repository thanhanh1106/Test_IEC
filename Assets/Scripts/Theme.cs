using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class Theme : ScriptableObject
{
    
    [SerializeField] private List<ThemeItem>  items = new List<ThemeItem>();
    
    public ThemeItem GetThemeItem(NormalItem.eNormalType type)
    {
        return items.FirstOrDefault(item => item.ItemType == type);
    }
    
}

[Serializable]
public class ThemeItem
{
    public NormalItem.eNormalType ItemType;
    public Texture2D Tex2D;
    public int PPU;
    
}
