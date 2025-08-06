using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board
{
    public enum eMatchDirection
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        ALL
    }

    private int boardSizeX;

    private int boardSizeY;

    private Cell[,] m_cells;

    private Transform m_root;

    private int m_matchMin;
    
    private static GameObject s_cachedCellPrefab;
    private Theme _theme;
    private Dictionary<NormalItem.eNormalType, GameObject> m_normalPrefabs;
    private Dictionary<BonusItem.eBonusType, GameObject> m_bonusPrefabs;

    public Board(Transform transform, GameSettings gameSettings, Theme theme)
    {
        m_root = transform;

        m_matchMin = gameSettings.MatchesMin;
        _theme = theme;
        LoadNormalPrefabs();
        LoadBonusPrefabs();
        this.boardSizeX = gameSettings.BoardSizeX;
        this.boardSizeY = gameSettings.BoardSizeY;

        m_cells = new Cell[boardSizeX, boardSizeY];

        CreateBoard();
    }


    private void LoadNormalPrefabs()
    {
        m_normalPrefabs = new Dictionary<NormalItem.eNormalType, GameObject>();

        foreach (NormalItem.eNormalType type in Enum.GetValues(typeof(NormalItem.eNormalType)))
        {
            var pf = Resources.Load<GameObject>($"prefabs/itemNormal{((int)type).ToString("D2")}");

            var themeItem = _theme.GetThemeItem(type);

            var pfThemed = GameObject.Instantiate(pf, Vector2.one * 1000000, Quaternion.identity); // dịch ra chỗ khác đỡ nhìn thấy
            
            Sprite newSprite = Sprite.Create(themeItem.Tex2D,
                new Rect(0, 0, themeItem.Tex2D.width, themeItem.Tex2D.height),
                new Vector2(0.5f, 0.5f),themeItem.PPU); 
            pfThemed.GetComponent<SpriteRenderer>().sprite = newSprite;
            m_normalPrefabs.Add(type, pfThemed);
        }
    }

    private void LoadBonusPrefabs()
    {
        m_bonusPrefabs = new Dictionary<BonusItem.eBonusType, GameObject>();
        foreach (BonusItem.eBonusType type in Enum.GetValues(typeof(BonusItem.eBonusType)))
        {
            var pf = Resources.Load<GameObject>($"prefabs/itemBonus{type}");
            m_bonusPrefabs.Add(type, pf);
        }
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
        //GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        if (s_cachedCellPrefab == null)
        {
            s_cachedCellPrefab = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        }
        GameObject prefabBG = s_cachedCellPrefab;
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = origin + new Vector3(x, y, 0f);
                go.transform.SetParent(m_root);

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y);

                m_cells[x, y] = cell;
            }
        }

        //set neighbours
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (y + 1 < boardSizeY) m_cells[x, y].NeighbourUp = m_cells[x, y + 1];
                if (x + 1 < boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
                if (y > 0) m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
                if (x > 0) m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
            }
        }

    }

    internal void Fill()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                NormalItem item = new NormalItem();

                List<NormalItem.eNormalType> types = new List<NormalItem.eNormalType>();
                if (cell.NeighbourBottom != null)
                {
                    NormalItem nitem = cell.NeighbourBottom.Item as NormalItem;
                    if (nitem != null)
                    {
                        types.Add(nitem.ItemType);
                    }
                }

                if (cell.NeighbourLeft != null)
                {
                    NormalItem nitem = cell.NeighbourLeft.Item as NormalItem;
                    if (nitem != null)
                    {
                        types.Add(nitem.ItemType);
                    }
                }

                item.SetType(Utils.GetRandomNormalTypeExcept(types.ToArray()));
                item.SetPrefab(m_normalPrefabs[item.ItemType]);
                item.SetView();
                item.SetViewRoot(m_root);

                cell.Assign(item);
                cell.ApplyItemPosition(false);
            }
        }
    }

    internal void Shuffle()
    {
        List<Item> list = new List<Item>();
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                list.Add(m_cells[x, y].Item);
                m_cells[x, y].Free();
            }
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                int rnd = UnityEngine.Random.Range(0, list.Count);
                m_cells[x, y].Assign(list[rnd]);
                m_cells[x, y].ApplyItemMoveToPosition();

                list.RemoveAt(rnd);
            }
        }
    }


    internal void FillGapsWithNewItems()
    {
        // for (int x = 0; x < boardSizeX; x++)
        // {
        //     for (int y = 0; y < boardSizeY; y++)
        //     {
        //         Cell cell = m_cells[x, y];
        //         if (!cell.IsEmpty) continue;
        //
        //         NormalItem item = new NormalItem();
        //
        //         item.SetType(Utils.GetRandomNormalType());
        //         item.SetTheme(_theme.GetThemeItem(item.ItemType));
        //         item.SetView();
        //         item.SetViewRoot(m_root);
        //
        //         cell.Assign(item);
        //         cell.ApplyItemPosition(true);
        //     }
        // }
        
        // đếm các loại item trên bàn
        Dictionary<NormalItem.eNormalType, int> typeCounts = new Dictionary<NormalItem.eNormalType, int>();
        foreach (var cell in m_cells)
        {
            if (cell.Item is NormalItem normalItem)
            {
                if (!typeCounts.ContainsKey(normalItem.ItemType))
                    typeCounts[normalItem.ItemType] = 0;
                typeCounts[normalItem.ItemType]++;
            }
        }
        
        // duyệt ma trận 
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (!cell.IsEmpty) continue;

                // lấy 4 item xung quanh nó
                HashSet<NormalItem.eNormalType> neighborTypes = new HashSet<NormalItem.eNormalType>();
                CheckAndAddType(x - 1, y,ref neighborTypes); // trái
                CheckAndAddType(x + 1, y,ref neighborTypes); // phải
                CheckAndAddType(x, y - 1,ref neighborTypes); // dưới 
                CheckAndAddType(x, y + 1,ref neighborTypes); // trên

                //Lấy tất cả item type hợp lệ khác xung quanh
                var allTypes = Enum.GetValues(typeof(NormalItem.eNormalType)).Cast<NormalItem.eNormalType>();
                var validTypes = allTypes.Where(t => !neighborTypes.Contains(t)).ToList();

                // Chọn item type ít nhất 
                NormalItem.eNormalType selectedType = validTypes
                    .OrderBy(t => typeCounts.ContainsKey(t) ? typeCounts[t] : 0)
                    .FirstOrDefault();

                // Tăng đếm cho vòng sau
                if (!typeCounts.ContainsKey(selectedType))
                    typeCounts[selectedType] = 0;
                typeCounts[selectedType]++;

                // tạo item
                NormalItem item = new NormalItem();
                item.SetType(selectedType);
                item.SetPrefab(m_normalPrefabs[item.ItemType]);
                item.SetView();
                item.SetViewRoot(m_root);

                cell.Assign(item);
                cell.ApplyItemPosition(true);
            }
        }
        void CheckAndAddType(int x, int y,ref HashSet<NormalItem.eNormalType> set)
        {
            if (x >= 0 && x < boardSizeX && y >= 0 && y < boardSizeY)
            {
                Cell neighbor = m_cells[x, y];
                if (!neighbor.IsEmpty && neighbor.Item is NormalItem ni)
                {
                    set.Add(ni.ItemType);
                }
            }
        }
        
    }

    internal void ExplodeAllItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.ExplodeItem();
            }
        }
    }

    public void Swap(Cell cell1, Cell cell2, Action callback)
    {
        Item item = cell1.Item;
        cell1.Free();
        Item item2 = cell2.Item;
        cell1.Assign(item2);
        cell2.Free();
        cell2.Assign(item);

        item.View.DOMove(cell2.transform.position, 0.3f);
        item2.View.DOMove(cell1.transform.position, 0.3f).OnComplete(() => { if (callback != null) callback(); });
    }

    public List<Cell> GetHorizontalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        //check horizontal match
        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourRight;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourLeft;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return list;
    }


    public List<Cell> GetVerticalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourUp;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourBottom;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return list;
    }

    internal void ConvertNormalToBonus(List<Cell> matches, Cell cellToConvert)
    {
        eMatchDirection dir = GetMatchDirection(matches);

        BonusItem item = new BonusItem();
        switch (dir)
        {
            case eMatchDirection.ALL:
                item.SetType(BonusItem.eBonusType.ALL);
                break;
            case eMatchDirection.HORIZONTAL:
                item.SetType(BonusItem.eBonusType.HORIZONTAL);
                break;
            case eMatchDirection.VERTICAL:
                item.SetType(BonusItem.eBonusType.VERTICAL);
                break;
        }

        if (item != null)
        {
            if (cellToConvert == null)
            {
                int rnd = UnityEngine.Random.Range(0, matches.Count);
                cellToConvert = matches[rnd];
            }

            item.SetPrefab(m_bonusPrefabs[item.ItemType]);
            item.SetView();
            item.SetViewRoot(m_root);

            cellToConvert.Free();
            cellToConvert.Assign(item);
            cellToConvert.ApplyItemPosition(true);
        }
    }


    internal eMatchDirection GetMatchDirection(List<Cell> matches)
    {
        if (matches == null || matches.Count < m_matchMin) return eMatchDirection.NONE;
        
        int referenceX = matches[0].BoardX;
        int sameXCount = 0;
        for (int i = 0; i < matches.Count; i++)
        {
            if (matches[i].BoardX == referenceX)
                sameXCount++;
        }
        
        if (sameXCount == matches.Count)
        {
            return eMatchDirection.VERTICAL;
        }
        
        int referenceY = matches[0].BoardY;
        int sameYCount = 0;
        for (int i = 0; i < matches.Count; i++)
        {
            if (matches[i].BoardY == referenceY)
                sameYCount++;
        }
        
        if (sameYCount == matches.Count)
        {
            return eMatchDirection.HORIZONTAL;
        }

        if (matches.Count > 5)
        {
            return eMatchDirection.ALL;
        }

        return eMatchDirection.NONE;
    }

    internal List<Cell> FindFirstMatch()
    {
        List<Cell> list = new List<Cell>();

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                var listhor = GetHorizontalMatches(cell);
                if (listhor.Count >= m_matchMin)
                {
                    list = listhor;
                    break;
                }

                var listvert = GetVerticalMatches(cell);
                if (listvert.Count >= m_matchMin)
                {
                    list = listvert;
                    break;
                }
            }
        }

        return list;
    }

    public List<Cell> CheckBonusIfCompatible(List<Cell> matches)
    {
        var dir = GetMatchDirection(matches);
        
        Cell bonusCell = null;
        for (int i = 0; i < matches.Count; i++)
        {
            if (matches[i].Item is BonusItem)
            {
                bonusCell = matches[i];
                break;
            }
        }
        
        if (bonusCell == null)
        {
            return matches;
        }

        List<Cell> result = new List<Cell>();
        switch (dir)
        {
            case eMatchDirection.HORIZONTAL:
                for (int i = 0; i < matches.Count; i++)
                {
                    BonusItem item = matches[i].Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.HORIZONTAL)
                    {
                        result.Add(matches[i]);
                    }
                }
                break;
            case eMatchDirection.VERTICAL:
                for (int i = 0; i < matches.Count; i++)
                {
                    BonusItem item = matches[i].Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.VERTICAL)
                    {
                        result.Add(matches[i]);
                    }
                }
                break;
            case eMatchDirection.ALL:
                for (int i = 0; i < matches.Count; i++)
                {
                    BonusItem item = matches[i].Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.ALL)
                    {
                        result.Add(matches[i]);
                    }
                }
                break;
        }

        return result;
    }

    internal List<Cell> GetPotentialMatches()
    {
        List<Cell> result = new List<Cell>();
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                //check right
                /* example *\
                  * * * * *
                  * * * * *
                  * * * ? *
                  * & & * ?
                  * * * ? *
                \* example  */

                if (cell.NeighbourRight != null)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourRight, cell.NeighbourRight.NeighbourRight);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check up
                /* example *\
                  * ? * * *
                  ? * ? * *
                  * & * * *
                  * & * * *
                  * * * * *
                \* example  */
                if (cell.NeighbourUp != null)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourUp, cell.NeighbourUp.NeighbourUp);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check bottom
                /* example *\
                  * * * * *
                  * & * * *
                  * & * * *
                  ? * ? * *
                  * ? * * *
                \* example  */
                if (cell.NeighbourBottom != null)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourBottom, cell.NeighbourBottom.NeighbourBottom);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check left
                /* example *\
                  * * * * *
                  * * * * *
                  * ? * * *
                  ? * & & *
                  * ? * * *
                \* example  */
                if (cell.NeighbourLeft != null)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourLeft, cell.NeighbourLeft.NeighbourLeft);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * * * * *
                  * * ? * *
                  * & * & *
                  * * ? * *
                \* example  */
                Cell neib = cell.NeighbourRight;
                if (neib != null && neib.NeighbourRight != null && neib.NeighbourRight.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellVertical(neib, cell);
                    if (second != null)
                    {
                        result.Add(cell);
                        result.Add(neib.NeighbourRight);
                        result.Add(second);
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * & * * *
                  ? * ? * *
                  * & * * *
                  * * * * *
                \* example  */
                neib = null;
                neib = cell.NeighbourUp;
                if (neib != null && neib.NeighbourUp != null && neib.NeighbourUp.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellHorizontal(neib, cell);
                    if (second != null)
                    {
                        result.Add(cell);
                        result.Add(neib.NeighbourUp);
                        result.Add(second);
                        break;
                    }
                }
            }

            if (result.Count > 0) break;
        }

        return result;
    }

    private List<Cell> GetPotentialMatch(Cell cell, Cell neighbour, Cell target)
    {
        List<Cell> result = new List<Cell>();

        if (neighbour != null && neighbour.IsSameType(cell))
        {
            Cell third = LookForTheThirdCell(target, neighbour);
            if (third != null)
            {
                result.Add(cell);
                result.Add(neighbour);
                result.Add(third);
            }
        }

        return result;
    }

    private Cell LookForTheSecondCellHorizontal(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look right
        Cell second = null;
        second = target.NeighbourRight;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look left
        second = null;
        second = target.NeighbourLeft;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheSecondCellVertical(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look up        
        Cell second = target.NeighbourUp;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look bottom
        second = null;
        second = target.NeighbourBottom;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheThirdCell(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look up
        Cell third = CheckThirdCell(target.NeighbourUp, main);
        if (third != null)
        {
            return third;
        }

        //look right
        third = null;
        third = CheckThirdCell(target.NeighbourRight, main);
        if (third != null)
        {
            return third;
        }

        //look bottom
        third = null;
        third = CheckThirdCell(target.NeighbourBottom, main);
        if (third != null)
        {
            return third;
        }

        //look left
        third = null;
        third = CheckThirdCell(target.NeighbourLeft, main); ;
        if (third != null)
        {
            return third;
        }

        return null;
    }

    private Cell CheckThirdCell(Cell target, Cell main)
    {
        if (target != null && target != main && target.IsSameType(main))
        {
            return target;
        }

        return null;
    }

    internal void ShiftDownItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            int shifts = 0;
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell.IsEmpty)
                {
                    shifts++;
                    continue;
                }

                if (shifts == 0) continue;

                Cell holder = m_cells[x, y - shifts];

                Item item = cell.Item;
                cell.Free();

                holder.Assign(item);
                item.View.DOMove(holder.transform.position, 0.3f);
            }
        }
    }

    public void Clear()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.Clear();

                GameObject.Destroy(cell.gameObject);
                m_cells[x, y] = null;
            }
        }
    }
}
