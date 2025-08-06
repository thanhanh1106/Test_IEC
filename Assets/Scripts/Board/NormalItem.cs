using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalItem : Item
{
    public enum eNormalType
    {
        TYPE_ONE,
        TYPE_TWO,
        TYPE_THREE,
        TYPE_FOUR,
        TYPE_FIVE,
        TYPE_SIX,
        TYPE_SEVEN
    }

    public eNormalType ItemType;
    
    private ThemeItem _themeItem;

    public void SetType(eNormalType type)
    {
        ItemType = type;
    }

    public void SetTheme(ThemeItem theme)
    {
        _themeItem = theme;
    }

    public override void SetView()
    {
        string prefabname = GetPrefabName();

        if (!string.IsNullOrEmpty(prefabname))
        {
            // Load prefab from Resources
            m_prefab = Resources.Load<GameObject>(prefabname);
            if (m_prefab)
            {
                Sprite newSprite = Sprite.Create(_themeItem.Tex2D,
                    new Rect(0, 0, _themeItem.Tex2D.width, _themeItem.Tex2D.height),
                    new Vector2(0.5f, 0.5f),_themeItem.PPU); 
                m_prefab.GetComponent<SpriteRenderer>().sprite = newSprite;
                GameObject pooledObject = ObjectPool.Instance.SpawnFromPool(m_prefab, Vector3.zero, Quaternion.identity);
                if (pooledObject != null)
                {
                    View = pooledObject.transform;
                    return;
                }
                
                // Fallback to direct instantiation if pool fails
                View = GameObject.Instantiate(m_prefab).transform;
            }
        }
    }

    protected override string GetPrefabName()
    {
        string prefabname = string.Empty;
        switch (ItemType)
        {
            case eNormalType.TYPE_ONE:
                prefabname = Constants.PREFAB_NORMAL_TYPE_ONE;
                break;
            case eNormalType.TYPE_TWO:
                prefabname = Constants.PREFAB_NORMAL_TYPE_TWO;
                break;
            case eNormalType.TYPE_THREE:
                prefabname = Constants.PREFAB_NORMAL_TYPE_THREE;
                break;
            case eNormalType.TYPE_FOUR:
                prefabname = Constants.PREFAB_NORMAL_TYPE_FOUR;
                break;
            case eNormalType.TYPE_FIVE:
                prefabname = Constants.PREFAB_NORMAL_TYPE_FIVE;
                break;
            case eNormalType.TYPE_SIX:
                prefabname = Constants.PREFAB_NORMAL_TYPE_SIX;
                break;
            case eNormalType.TYPE_SEVEN:
                prefabname = Constants.PREFAB_NORMAL_TYPE_SEVEN;
                break;
        }

        return prefabname;
    }

    internal override bool IsSameType(Item other)
    {
        NormalItem it = other as NormalItem;

        return it != null && it.ItemType == this.ItemType;
    }
}
