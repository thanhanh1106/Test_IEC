using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalItem : Item
{
    public enum eNormalType
    {
        TYPE_ONE = 1,
        TYPE_TWO = 2,
        TYPE_THREE = 3,
        TYPE_FOUR = 4,
        TYPE_FIVE = 5,
        TYPE_SIX = 6,
        TYPE_SEVEN = 7
    }

    public eNormalType ItemType;
    
    public void SetType(eNormalType type)
    {
        ItemType = type;
    }
    

    internal override bool IsSameType(Item other)
    {
        NormalItem it = other as NormalItem;

        return it != null && it.ItemType == this.ItemType;
    }
}
