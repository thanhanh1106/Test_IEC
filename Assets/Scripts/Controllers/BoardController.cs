using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;
    private Theme _theme;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;
    
    private Vector3 m_lastMousePosition;
    private const float MIN_MOUSE_MOVEMENT = 0.1f;
    private const float INPUT_COOLDOWN = 0.05f;
    private float m_lastInputTime;

    public void StartGame(GameManager gameManager, GameSettings gameSettings,Theme theme)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;
        _theme = theme;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings,_theme);

        Fill();
    }
    
    

    private void Fill()
    {
        m_board.Fill();
        FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                StopHints();
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;
        if (!m_hintIsShown)
        {
            m_timeAfterFill += Time.deltaTime;
            if (m_timeAfterFill > m_gameSettings.TimeForHint)
            {
                m_timeAfterFill = 0f;
                ShowHint();
            }
        }

        HandleOptimizedInput();
    }
    
    private void HandleOptimizedInput()
    {
        if (Time.time - m_lastInputTime < INPUT_COOLDOWN)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            m_lastMousePosition = Input.mousePosition;
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(m_lastMousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                m_isDragging = true;
                m_hitCollider = hit.collider;
                m_lastInputTime = Time.time;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetRayCast();
            m_lastInputTime = Time.time;
        }

        if (Input.GetMouseButton(0) && m_isDragging)
        {
            Vector3 currentMousePos = Input.mousePosition;
            
            if (Vector3.Distance(currentMousePos, m_lastMousePosition) < MIN_MOUSE_MOVEMENT)
                return;
                
            m_lastMousePosition = currentMousePos;
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(currentMousePos), Vector2.zero);
            
            if (hit.collider != null)
            {
                if (m_hitCollider != null && m_hitCollider != hit.collider)
                {
                    StopHints();

                    Cell c1 = m_hitCollider.GetComponent<Cell>();
                    Cell c2 = hit.collider.GetComponent<Cell>();
                    
                    if (AreItemsNeighbor(c1, c2))
                    {
                        IsBusy = true;
                        SetSortingLayer(c1, c2);
                        m_board.Swap(c1, c2, () =>
                        {
                            FindMatchesAndCollapse(c1, c2);
                        });

                        ResetRayCast();
                    }
                }
            }
            else
            {
                ResetRayCast();
            }
            
            m_lastInputTime = Time.time;
        }
    }

    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            List<Cell> cells1 = GetMatches(cell1);
            List<Cell> cells2 = GetMatches(cell2);

            List<Cell> matches = new List<Cell>();
            matches.AddRange(cells1);
            matches.AddRange(cells2);
            matches = matches.Distinct().ToList();

            if (matches.Count < m_gameSettings.MatchesMin)
            {
                m_board.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(matches, cell2);
            }
        }
    }

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }
}
