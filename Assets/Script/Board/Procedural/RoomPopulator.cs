using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>
    /// 3rd stage of the pipeline (Layout -> Builder -> POPULATOR).
    /// The layout (GridMap + Quest JSON) and the walls/elements (LevelBuilder) already exist: this class only DRESSES the rooms.
    ///
    /// FIDELITY RULES (rulebook, "interface adaptation" in the deviation log: no game result is changed):
    ///  - decor has NO collider (rays of Hero / MovementSystem ignore it) and NO GridPawn (never occupies a cell);
    ///  - it never uses the shared IRandomPicker (dice / cards): it has its OWN System.Random, seeded
    ///    => recorded games replay with the same dice, and the same level always gets the same decor;
    ///  - it never leaks hidden information: a room's decor is only active once the room is revealed,
    ///    traps and hidden elements (secret doors, hidden treasure) are NOT avoided (avoiding them would reveal them);
    ///  - themes must not contain anything that looks like treasure (gold, gems, chests).
    /// </summary>
    public class RoomPopulator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelBuilder level;

        [Header("Themes")]
        [SerializeField] private List<RoomThemeSO> roomThemes = new List<RoomThemeSO>();
        [Tooltip("Force a theme for a given room (zone number). Others are spread automatically.")]
        [SerializeField] private List<ZoneTheme> zoneOverrides = new List<ZoneTheme>();

        [Header("Corridors (torches only)")]
        [SerializeField] private RoomThemeSO corridorTheme;
        [SerializeField] private int corridorTorchSpacing = 6;
        [SerializeField] private int maxCorridorTorches = 12;

        [Header("Determinism")]
        [SerializeField] private int seed = 1234;

        [Serializable]
        public class ZoneTheme { public int zone; public RoomThemeSO theme; }

        [Header("Auto-Detection Rules")]
        [SerializeField] private List<ThemeRule> themeRules = new List<ThemeRule>();

        [Serializable]
        public class ThemeRule
        {
            public string furnitureType;
            public RoomThemeSO theme;  
        }

        private struct CellInfo
        {
            public Vector2Int cell;
            public List<Dir> walls;
        }

        private static readonly Dir[] AllDirs = (Dir[])Enum.GetValues(typeof(Dir));

        private BoardBuilder board;
        private Transform decorRoot;
        private readonly Dictionary<int, GameObject> zoneRoots = new Dictionary<int, GameObject>();
        private readonly HashSet<EdgeKey> walls = new HashSet<EdgeKey>();
        private readonly HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();

        // --- Life cycle ---

        private void OnEnable()
        {
            if (level == null) { Debug.LogError("[Decor] No LevelBuilder assigned."); return; }
            board = level.board;
            level.LevelBuilt += OnLevelBuilt;
            level.ElementOpened += OnElementOpened;
            if (board != null) board.BoardCleared += Clear;
        }

        private void OnDisable()
        {
            if (level != null)
            {
                level.LevelBuilt -= OnLevelBuilt;
                level.ElementOpened -= OnElementOpened;
            }
            if (board != null) board.BoardCleared -= Clear;
        }

        private void Start()
        {
            if (level != null && level.Model != null && decorRoot == null) Populate();
        }

        private void OnDestroy() { Clear(); }

        private void OnLevelBuilt(LevelBuilder built) { Populate(); }

        private void OnElementOpened(ElementDef def, bool open)
        {
            if (open && def.kind == ElementKind.Door) StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            RefreshVisibility();
        }

        public void RefreshVisibility()
        {
            foreach (KeyValuePair<int, GameObject> kv in zoneRoots)
                if (kv.Value != null) kv.Value.SetActive(level.IsZoneRevealed(kv.Key));
        }

        public void Clear()
        {
            if (decorRoot != null) Destroy(decorRoot.gameObject);
            decorRoot = null;
            zoneRoots.Clear();
        }

        // --- Generation ---

        [ContextMenu("Populate")]
        public void Populate()
        {
            Clear();
            if (level == null || level.Model == null || board == null || board.Model == null) return;

            LevelModel lm = level.Model;
            BoardModel bm = board.Model;

            walls.Clear();
            foreach (EdgeKey e in lm.WallEdges) walls.Add(e);

            HashSet<Vector2Int> doorCells = BuildDoorCells(lm);
            BuildBlocked(lm, bm);

            decorRoot = new GameObject("DecorGenerated").transform;
            decorRoot.SetParent(board.transform, false);
            decorRoot.gameObject.layer = board.BoardLayer;

            int levelHash = Fnv(lm.id);

            Dictionary<int, RoomThemeSO> autoThemes = new Dictionary<int, RoomThemeSO>();
            if (lm.Elements != null)
            {
                foreach (var element in lm.Elements)
                {
                    if (element.kind == ElementKind.Furniture)
                    {
                        int zone = bm.ZoneAt(element.x, element.y);
                        if (zone > 0)
                        {
                            foreach (var rule in themeRules)
                            {
                                if (rule != null && rule.theme != null &&
                                    string.Equals(element.type, rule.furnitureType, StringComparison.OrdinalIgnoreCase))
                                {
                                    autoThemes[zone] = rule.theme;
                                    break;
                                }
                            }
                        }
                    }
                }
            }


            List<RoomThemeSO> order = new List<RoomThemeSO>();
            foreach (RoomThemeSO t in roomThemes) if (t != null) order.Add(t);
            Shuffle(order, new System.Random(Mix(seed, levelHash, -1)));

            int index = 0;
            for (int zone = 1; zone <= bm.roomCount; zone++)
            {
                if (lm.IsUnusedZone(zone)) continue;

                // Ordre de priorité des thèmes :
                // 1. Override manuel (Inspector)
                RoomThemeSO theme = FindOverride(zone);

                // 2. Détection automatique par meuble (JSON)
                if (theme == null && autoThemes.ContainsKey(zone))
                {
                    theme = autoThemes[zone];
                }

                // 3. Pool aléatoire général par défaut
                if (theme == null && order.Count > 0)
                {
                    theme = order[index++ % order.Count];
                }

                if (theme == null) continue;

                GameObject root = new GameObject("Decor_Zone_" + zone);
                root.transform.SetParent(decorRoot, false);
                root.layer = board.BoardLayer;

                System.Random rng = new System.Random(Mix(seed, levelHash, zone));
                PopulateRoom(root.transform, zone, bm, theme, doorCells, rng);

                zoneRoots[zone] = root;
            }

            if (corridorTheme != null)
                PopulateCorridors(bm, doorCells, new System.Random(Mix(seed, levelHash, 0)));

            RefreshVisibility();
            Debug.Log("[Decor] Level '" + lm.id + "' decorated (" + zoneRoots.Count + " rooms).");
        }

        private RoomThemeSO FindOverride(int zone)
        {
            foreach (ZoneTheme z in zoneOverrides) if (z != null && z.zone == zone && z.theme != null) return z.theme;
            return null;
        }

        private static HashSet<Vector2Int> BuildDoorCells(LevelModel lm)
        {
            HashSet<Vector2Int> set = new HashSet<Vector2Int>();
            foreach (ElementDef d in lm.Elements)
            {
                if (d.kind != ElementKind.Door || d.hidden) continue;
                set.Add(d.edge.CellA);
                set.Add(d.edge.CellB);
            }
            return set;
        }

        private void BuildBlocked(LevelModel lm, BoardModel bm)
        {
            blocked.Clear();
            List<ElementDef> solids = new List<ElementDef>();
            foreach (ElementDef d in lm.Elements)
                if (!d.hidden && d.kind != ElementKind.Trap && d.kind != ElementKind.Door) solids.Add(d);

            for (int x = 0; x < bm.width; x++)
                for (int y = 0; y < bm.height; y++)
                    foreach (ElementDef d in solids)
                        if (d.Covers(x, y)) { blocked.Add(new Vector2Int(x, y)); break; }
        }

        // --- Rooms ---

        private void PopulateRoom(Transform root, int zone, BoardModel bm, RoomThemeSO theme,
                                  HashSet<Vector2Int> doorCells, System.Random rng)
        {
            IReadOnlyList<Vector2Int> roomCells = bm.RoomCells(zone);
            if (roomCells.Count == 0) return;

            List<CellInfo> free = new List<CellInfo>();
            foreach (Vector2Int c in roomCells)
            {
                if (blocked.Contains(c) || doorCells.Contains(c)) continue;
                free.Add(new CellInfo { cell = c, walls = GetWallSides(c) });
            }

            HashSet<Vector2Int> used = new HashSet<Vector2Int>();

            // 1) Fill light
            AddFillLight(root, roomCells, theme);

            // 2) Torches
            List<CellInfo> wallCells = free.FindAll(c => c.walls.Count > 0);
            Shuffle(wallCells, rng);
            int torchCount = rng.Next(theme.minTorches, Mathf.Max(theme.minTorches, theme.maxTorches) + 1);
            List<Vector2Int> torches = new List<Vector2Int>();
            foreach (CellInfo c in wallCells)
            {
                if (torches.Count >= torchCount) break;
                if (!FarEnough(c.cell, torches, theme.torchMinSpacing)) continue;
                PlaceTorch(root, c, theme, rng);
                torches.Add(c.cell);
                used.Add(c.cell);
            }

            // 3) Props
            if (theme.props == null || theme.props.Count == 0) return;

            List<CellInfo> wallPool = free.FindAll(c => c.walls.Count > 0 && !used.Contains(c.cell));
            List<CellInfo> cornerPool = free.FindAll(c => IsCorner(c.walls) && !used.Contains(c.cell));
            List<CellInfo> floorPool = free.FindAll(c => !used.Contains(c.cell));
            Shuffle(wallPool, rng); Shuffle(cornerPool, rng); Shuffle(floorPool, rng);

            int target = Mathf.Clamp(Mathf.RoundToInt(free.Count * theme.density), theme.minProps, theme.maxProps);
            int placed = 0, tries = 0;
            while (placed < target && tries++ < target * 4)
            {
                PropEntry entry = PickEntry(theme.props, rng);
                if (entry == null) break;

                List<CellInfo> pool = entry.slot == PropSlot.Wall ? wallPool
                                    : entry.slot == PropSlot.Corner ? cornerPool : floorPool;
                if (!Take(pool, used, out CellInfo c)) continue;

                List<Dir> sides = null;
                if (entry.slot == PropSlot.Wall) sides = new List<Dir> { c.walls[rng.Next(c.walls.Count)] };
                else if (entry.slot == PropSlot.Corner) sides = CornerSides(c.walls);

                float yaw = entry.randomYaw ? (float)rng.NextDouble() * 360f : 90f * rng.Next(4);

                // Utilisation de DecorPlacer pour respecter la modularité
                if (DecorPlacer.Place(board, level.wallThickness, entry.prefab, root, c.cell, sides, entry.size, 0f, yaw) != null)
                    placed++;
            }
        }

        // --- Corridors ---

        private void PopulateCorridors(BoardModel bm, HashSet<Vector2Int> doorCells, System.Random rng)
        {
            GameObject root = new GameObject("Decor_Corridors");
            root.transform.SetParent(decorRoot, false);
            root.layer = board.BoardLayer;

            List<CellInfo> pool = new List<CellInfo>();
            for (int x = 0; x < bm.width; x++)
                for (int y = 0; y < bm.height; y++)
                {
                    Vector2Int c = new Vector2Int(x, y);
                    if (bm.ZoneAt(x, y) != 0 || blocked.Contains(c) || doorCells.Contains(c)) continue;
                    List<Dir> w = GetWallSides(c);
                    if (w.Count > 0) pool.Add(new CellInfo { cell = c, walls = w });
                }
            Shuffle(pool, rng);

            HashSet<Vector2Int> used = new HashSet<Vector2Int>();

            // 1) Torches dans les couloirs
            List<Vector2Int> torches = new List<Vector2Int>();
            foreach (CellInfo c in pool)
            {
                if (torches.Count >= maxCorridorTorches) break;
                if (!FarEnough(c.cell, torches, corridorTorchSpacing)) continue;
                PlaceTorch(root.transform, c, corridorTheme, rng);
                torches.Add(c.cell);
                used.Add(c.cell);
            }

            // 2) Props dans les couloirs (si le thème en contient)
            if (corridorTheme.props != null && corridorTheme.props.Count > 0)
            {
                List<CellInfo> wallPool = pool.FindAll(c => c.walls.Count > 0 && !used.Contains(c.cell));
                List<CellInfo> floorPool = pool.FindAll(c => !used.Contains(c.cell));
                Shuffle(wallPool, rng);
                Shuffle(floorPool, rng);

                int target = Mathf.Clamp(Mathf.RoundToInt(pool.Count * corridorTheme.density), corridorTheme.minProps, corridorTheme.maxProps);
                int placed = 0, tries = 0;

                while (placed < target && tries++ < target * 4)
                {
                    PropEntry entry = PickEntry(corridorTheme.props, rng);
                    if (entry == null) break;

                    List<CellInfo> subPool = entry.slot == PropSlot.Wall ? wallPool : floorPool;
                    if (!Take(subPool, used, out CellInfo c)) continue;

                    List<Dir> sides = null;
                    if (entry.slot == PropSlot.Wall && c.walls.Count > 0)
                        sides = new List<Dir> { c.walls[rng.Next(c.walls.Count)] };

                    float yaw = entry.randomYaw ? (float)rng.NextDouble() * 360f : 90f * rng.Next(4);

                    if (DecorPlacer.Place(board, level.wallThickness, entry.prefab, root.transform, c.cell, sides, entry.size, 0f, yaw) != null)
                        placed++;
                }
            }
        }
        // --- Lights & Torches Placement ---

        private void PlaceTorch(Transform root, CellInfo c, RoomThemeSO theme, System.Random rng)
        {
            Dir side = c.walls[rng.Next(c.walls.Count)];
            float y = theme.torchHeight * level.wallHeight;
            Vector2 inward = new Vector2(-DirUtil.DX[(int)side], -DirUtil.DY[(int)side]);

            Transform torch = null;
            if (theme.torchPrefab != null)
            {
                torch = DecorPlacer.Place(board, level.wallThickness, theme.torchPrefab, root, c.cell, new List<Dir> { side }, theme.torchSize, y, 0f);
            }

            Vector3 lightPos;
            if (torch != null)
            {
                Bounds b = DecorPlacer.LocalBounds(torch.gameObject, root);
                lightPos = new Vector3(b.center.x, b.max.y - 0.2f, b.center.z);
            }
            else
            {
                Vector3 center = board.CellToLocal(c.cell.x, c.cell.y);
                float gap = board.tileSize * 0.5f - level.wallThickness * 0.5f;
                lightPos = center + new Vector3(-inward.x * gap, y + 0.2f, -inward.y * gap);
            }

            lightPos += new Vector3(inward.x, 0f, inward.y) * (0.05f * board.tileSize);

            if (torch != null && theme.flameFxPrefab != null)
            {
                GameObject fx = Instantiate(theme.flameFxPrefab, root);
                fx.transform.localPosition = lightPos;
                fx.transform.localRotation = Quaternion.identity;
                fx.transform.localScale = Vector3.one;

                foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = ps.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(theme.torchColor);

                    var colorOverLifetime = ps.colorOverLifetime;
                    if (colorOverLifetime.enabled)
                    {
                        Gradient grad = new Gradient();
                        grad.SetKeys(
                            new GradientColorKey[] { new GradientColorKey(theme.torchColor, 0f), new GradientColorKey(theme.torchColor, 1f) },
                            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
                        );
                        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);
                    }
                }
            }

            AddLight(root, lightPos, theme.torchColor, theme.torchIntensity, theme.torchRange, true);
        }

        private void AddFillLight(Transform root, IReadOnlyList<Vector2Int> cells, RoomThemeSO theme)
        {
            if (theme.fillIntensity <= 0f) return;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (Vector2Int c in cells)
            {
                minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
            }
            Vector3 center = board.CellToLocal(minX, minY) + (board.CellToLocal(maxX, maxY) - board.CellToLocal(minX, minY)) * 0.5f;
            float radius = Mathf.Max(maxX - minX + 1, maxY - minY + 1) * 0.75f;
            AddLight(root, new Vector3(center.x, level.wallHeight * 1.3f, center.z), theme.fillColor, theme.fillIntensity, radius, false);
        }

        private void AddLight(Transform parent, Vector3 localPos, Color color, float intensity, float rangeInCells, bool flicker)
        {
            GameObject go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.layer = board.BoardLayer;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = rangeInCells * board.tileSize;
            l.shadows = LightShadows.None;
            if (flicker) go.AddComponent<TorchFlicker>();
        }

        // --- Grid helpers ---

        private static EdgeKey EdgeOf(Vector2Int c, Dir d)
        {
            switch (d)
            {
                case Dir.N: return new EdgeKey(c.x, c.y + 1, true);
                case Dir.S: return new EdgeKey(c.x, c.y, true);
                case Dir.E: return new EdgeKey(c.x + 1, c.y, false);
                default: return new EdgeKey(c.x, c.y, false);   // W
            }
        }

        private List<Dir> GetWallSides(Vector2Int c)
        {
            List<Dir> list = new List<Dir>(4);
            foreach (Dir d in AllDirs) if (walls.Contains(EdgeOf(c, d))) list.Add(d);
            return list;
        }

        private static bool IsVertical(Dir d) { return d == Dir.N || d == Dir.S; }

        private static bool IsCorner(List<Dir> w)
        {
            bool v = false, h = false;
            foreach (Dir d in w) { if (IsVertical(d)) v = true; else h = true; }
            return v && h;
        }

        private static List<Dir> CornerSides(List<Dir> w)
        {
            Dir? v = null, h = null;
            foreach (Dir d in w) { if (IsVertical(d)) { if (!v.HasValue) v = d; } else if (!h.HasValue) h = d; }
            return new List<Dir> { v.Value, h.Value };
        }

        private static bool FarEnough(Vector2Int c, List<Vector2Int> others, int spacing)
        {
            foreach (Vector2Int o in others)
                if (Mathf.Abs(o.x - c.x) + Mathf.Abs(o.y - c.y) < spacing) return false;
            return true;
        }

        private static bool Take(List<CellInfo> pool, HashSet<Vector2Int> used, out CellInfo result)
        {
            while (pool.Count > 0)
            {
                CellInfo c = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
                if (used.Add(c.cell)) { result = c; return true; }
            }
            result = default;
            return false;
        }

        private static PropEntry PickEntry(List<PropEntry> entries, System.Random rng)
        {
            float total = 0f;
            foreach (PropEntry e in entries) if (e != null && e.prefab != null) total += Mathf.Max(0f, e.weight);
            if (total <= 0f) return null;

            float roll = (float)rng.NextDouble() * total;
            foreach (PropEntry e in entries)
            {
                if (e == null || e.prefab == null) continue;
                roll -= Mathf.Max(0f, e.weight);
                if (roll <= 0f) return e;
            }
            return entries[entries.Count - 1];
        }

        // --- Deterministic random ---

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        private static int Mix(int seed, int levelHash, int zone)
        {
            unchecked { return ((17 * 31 + seed) * 31 + levelHash) * 31 + zone; }
        }

        private static int Fnv(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                if (s != null) foreach (char ch in s) { h ^= ch; h *= 16777619; }
                return (int)h;
            }
        }
    }
}