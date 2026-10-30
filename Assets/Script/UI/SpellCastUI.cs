using System.Collections.Generic;
using HeroQuest.Board;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Spell menu + target choice (rulebook p.14). The panel is built by hand: this script only fills it.
// Put it on an ACTIVE object (GameUI), never on the panel it hides.
public class SpellCastUI : MonoBehaviour
{
    public static SpellCastUI Instance { get; private set; }

    [Header("Panel (built by hand)")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform listRoot;          // object with a Vertical Layout Group
    [SerializeField] private Button spellButtonPrefab;    // hand-made button (a prefab), with a TMP child
    [SerializeField] private Button cancelButton;

    private const float EdgeMargin = 0.02f;
    private Hero caster;
    private readonly List<GameObject> spawned = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
    }

    public void Open(Hero hero)
    {
        if (hero == null || !hero.CanCastSpells || SpellManager.Instance == null) return;
        if (panel == null || titleText == null || listRoot == null || spellButtonPrefab == null)
        { Debug.LogError("[Spell] SpellCastUI: panel fields are not assigned."); return; }

        caster = hero;
        if (TurnManager.Instance != null) TurnManager.Instance.Locked = true;
        ShowMenu();
    }

    private void ShowMenu()
    {
        Clear();
        List<SpellCardSO> offered = Offered();
        if (offered.Count == 0)
        {
            Debug.Log("[Spell] " + caster.heroName + " has no spell to cast.");
            Close();
            return;
        }

        titleText.text = caster.heroName + ": choose a spell";
        foreach (SpellCardSO spell in offered)
        {
            Button b = Instantiate(spellButtonPrefab, listRoot);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = spell.displayName;
            SpellCardSO chosen = spell;
            b.onClick.AddListener(() => OnSpellChosen(chosen));
            spawned.Add(b.gameObject);
        }
        panel.SetActive(true);
    }

    // Spells that can really be cast now (door targets and unwired effects are left out, see the PR notes)
    private List<SpellCardSO> Offered()
    {
        var list = new List<SpellCardSO>();
        foreach (SpellCardSO s in SpellManager.Instance.GetAvailableSpells(caster))
            if (s.EffectImplemented && (s.Targets & SpellTargets.Door) == 0) list.Add(s);
        return list;
    }

    private void OnSpellChosen(SpellCardSO spell)
    {
        panel.SetActive(false);

        var byCell = new Dictionary<Vector2Int, object>();
        if ((spell.Targets & SpellTargets.Hero) != 0)
            foreach (Hero h in HeroTargets()) byCell[h.Cell] = h;
        else
            foreach (Monster m in MonsterTargets(spell)) byCell[m.cell] = m;

        if (byCell.Count == 0)
        {
            Debug.Log("[Spell] No valid target for " + spell.displayName + ".");
            ShowMenu();
            return;
        }

        // Right click / Escape: back to the menu, nothing is spent
        CellPicker.Instance.Pick(byCell.Keys, cell => Cast(spell, byCell[cell]), ShowMenu);
    }

    private void Cast(SpellCardSO spell, object target)
    {
        bool ok = false;
        if (target is Hero h) ok = SpellManager.Instance.CastOnHero(caster, spell, h);
        else if (target is Monster m) ok = SpellManager.Instance.CastOnMonster(caster, spell, m);

        if (!ok)
        {
            Debug.LogWarning("[Spell] " + spell.displayName + " could not be cast.");
            ShowMenu();
            return;
        }

        Debug.Log("[Spell] " + caster.heroName + " casts " + spell.displayName);
        if (TurnManager.Instance != null) TurnManager.Instance.MarkActionUsed();
        Close();
    }

    public void Close()
    {
        Clear();
        if (panel != null) panel.SetActive(false);
        caster = null;
        if (TurnManager.Instance != null) TurnManager.Instance.Locked = false;
    }

    private void Clear()
    {
        foreach (GameObject g in spawned) if (g != null) Destroy(g);
        spawned.Clear();
    }

    // A hero may target himself or any visible hero
    private List<Hero> HeroTargets()
    {
        var result = new List<Hero> { caster };
        var candidates = new List<GameObject>();
        foreach (Hero h in HeroManager.Instance.Heroes)
            if (h != null && h != caster && !h.IsAlly && !h.IsDead && !h.HasEscaped && h.gameObject.activeSelf)
                candidates.Add(h.gameObject);
        foreach (GameObject g in Visible(candidates)) result.Add(g.GetComponent<Hero>());
        return result;
    }

    // Monsters the heroes can see, and that the spell may target (sleep: not the undead)
    private List<Monster> MonsterTargets(SpellCardSO spell)
    {
        var candidates = new List<GameObject>();
        foreach (Monster m in MonsterManager.Instance.ActiveMonsters)
            if (m != null && !m.IsDead && m.revealedToHeroes && spell.CanTarget(m)) candidates.Add(m.gameObject);

        var result = new List<Monster>();
        foreach (GameObject g in Visible(candidates)) result.Add(g.GetComponent<Monster>());
        return result;
    }

    // Rulebook p.14: the line must cross no wall, closed door, hero or monster
    private List<GameObject> Visible(List<GameObject> candidates)
    {
        var result = new List<GameObject>();
        foreach (GameObject g in caster.VisibleAmong(candidates))   // walls
            if (!BlockedByPawn(g)) result.Add(g);                   // heroes and monsters in between
        return result;
    }

    private bool BlockedByPawn(GameObject target)
    {
        GridPawn targetPawn = target.GetComponent<GridPawn>();
        if (targetPawn == null || !targetPawn.HasCell || !caster.HasCell) return true;

        Vector2 a = (Vector2)caster.Cell + Vector2.one * 0.5f;
        Vector2 b = (Vector2)targetPawn.Cell + Vector2.one * 0.5f;

        foreach (Hero h in HeroManager.Instance.Heroes)
            if (h != null && h != caster && h.Pawn != targetPawn && !h.IsDead && !h.HasEscaped && h.HasCell
                && SegmentCrossesCell(a, b, h.Cell)) return true;

        foreach (Monster m in MonsterManager.Instance.ActiveMonsters)
            if (m != null && m.Pawn != targetPawn && m.Pawn != null && m.Pawn.HasCell
                && SegmentCrossesCell(a, b, m.Pawn.Cell)) return true;

        return false;
    }

    // Segment centre to centre against the square of a cell (Liang-Barsky, square slightly shrunk)
    private static bool SegmentCrossesCell(Vector2 a, Vector2 b, Vector2Int cell)
    {
        float xMin = cell.x + EdgeMargin, xMax = cell.x + 1 - EdgeMargin;
        float yMin = cell.y + EdgeMargin, yMax = cell.y + 1 - EdgeMargin;
        Vector2 d = b - a;
        float t0 = 0f, t1 = 1f;
        return Clip(-d.x, a.x - xMin, ref t0, ref t1) && Clip(d.x, xMax - a.x, ref t0, ref t1)
            && Clip(-d.y, a.y - yMin, ref t0, ref t1) && Clip(d.y, yMax - a.y, ref t0, ref t1);
    }

    private static bool Clip(float p, float q, ref float t0, ref float t1)
    {
        if (Mathf.Approximately(p, 0f)) return q >= 0f;
        float r = q / p;
        if (p < 0f) { if (r > t1) return false; if (r > t0) t0 = r; }
        else { if (r < t0) return false; if (r < t1) t1 = r; }
        return true;
    }
}