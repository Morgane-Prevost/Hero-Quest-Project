using HeroQuest.Board;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CellHighlight : MonoBehaviour
{
    public Vector2Int cell;
}

/// <summary>
/// Highlights a list of cells and waits for the player to click one of them.
/// Reusable for movement, attacks, spells, etc.
/// </summary>
public class CellPicker : MonoBehaviour
{
    public static CellPicker Instance { get; private set; }

    [SerializeField] private BoardBuilder board;
    [SerializeField] private GameObject defaultHighlightPrefab; // quad/plane with a collider
    [SerializeField] private LayerMask highlightMask;           // layer of the highlights
    [SerializeField] private float highlightHeight = 0.2f;
    [SerializeField] private LevelBuilder levelBuilder;

    private readonly List<GameObject> activeHighlights = new();
    private Action<Vector2Int> onChosen;
    private Action onCancelled;
    private bool isPicking;
    private Camera cam;

    public bool IsPicking => isPicking;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (levelBuilder == null)
        {
            levelBuilder = FindObjectOfType<LevelBuilder>();
        }
    }

    // ───────── Public API ─────────

    /// <summary>
    /// Highlights the given cells and waits for a click on one of them.
    /// A new Pick() cancels the previous one (and calls its onCancelled).
    /// </summary>
    public void Pick(IEnumerable<Vector2Int> cells,
                     Action<Vector2Int> onChosen,
                     Action onCancelled = null,
                     GameObject highlightPrefab = null)
    {
        Cancel();

        this.onChosen = onChosen;
        this.onCancelled = onCancelled;
        isPicking = true;

        GameObject prefab = highlightPrefab != null ? highlightPrefab : defaultHighlightPrefab;
        foreach (Vector2Int cell in cells)
            SpawnHighlight(cell, prefab);
    }

    /// <summary>Cancels the current selection and calls onCancelled.</summary>
    public void Cancel()
    {
        if (!isPicking) return;

        Action cb = onCancelled;
        Clear();
        cb?.Invoke();
    }

    // ───────── Internal ─────────

    private void Clear()
    {
        foreach (GameObject h in activeHighlights)
            if (h != null) Destroy(h);

        activeHighlights.Clear();
        isPicking = false;
        onChosen = null;
        onCancelled = null;
    }

    private void SpawnHighlight(Vector2Int cell, GameObject prefab)
    {
        Vector3 pos = board.CellToWorld(cell.x, cell.y) + Vector3.up * highlightHeight;
        GameObject go = Instantiate(prefab, pos, Quaternion.identity, transform);
        go.AddComponent<CellHighlight>().cell = cell;
        activeHighlights.Add(go);
    }

    private void Update()
    {
        if (!isPicking) return;

        // Cancel: right click or Escape
        bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (rightClick || escape)
        {
            Cancel();
            return;
        }

        if (Mouse.current == null) return;
        if (!ActionManager.Instance.leftClick.action.WasPressedThisFrame()) return;

        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[CellPicker] Camera.main is null (MainCamera tag missing).");
            return;
        }

        Debug.Log("1. Left click detected");

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, highlightMask, QueryTriggerInteraction.Collide))
        {
            Debug.Log("2. Raycast hit no highlight (collider / layer / mask)");
            return;
        }
        Debug.Log($"2. Hit {hit.collider.name} (layer {LayerMask.LayerToName(hit.collider.gameObject.layer)})");

        PlacedElement pe = hit.collider.GetComponentInParent<PlacedElement>();
        if (pe != null && pe.Def.kind == ElementKind.Door)
        {
            if (levelBuilder != null)
            {
                levelBuilder.OpenDoor(pe.Def.id);
                Debug.Log("[CellPicker] Ouverture de la porte : " + pe.Def.id);
                return;
            }
        }

        CellHighlight h = hit.collider.GetComponentInParent<CellHighlight>();
        if (h == null) return;

        // Clear BEFORE the callback: if it starts a new Pick(), it won't be wiped.
        Action<Vector2Int> cb = onChosen;
        Clear();
        cb?.Invoke(h.cell);
    }
    public IEnumerator SelectTarget(List<GameObject> targets, Action<GameObject> onSelected)
    {
        if (targets == null || targets.Count == 0)
        {
            Debug.Log("No valid target for this attack.");
            onSelected(null);
            yield break;
        }

        var targetsByCell = new Dictionary<Vector2Int, GameObject>();
        foreach (GameObject t in targets)
            targetsByCell[t.GetComponent<GridPawn>().Cell] = t;

        GameObject chosen = null;
        bool cancelled = false;

        CellPicker.Instance.Pick(
            targetsByCell.Keys,
            cell => chosen = targetsByCell[cell],
            () => cancelled = true,
            defaultHighlightPrefab);

        yield return new WaitUntil(() => chosen != null || cancelled);

        onSelected(cancelled ? null : chosen);
    }
}
