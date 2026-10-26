using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>Reads and validates a level JSON. All error messages cite the faulty element.</summary>
    public static class LevelLoader
    {
        public static bool TryParse(string json, string sourceName, out LevelData data, out string error)
        {
            data = null; error = null;
            if (string.IsNullOrEmpty(json)) { error = "Empty JSON: " + sourceName; return false; }
            try
            {
                data = JsonUtility.FromJson<LevelData>(json);
                if (data == null) { error = "Invalid JSON: " + sourceName; return false; }
                return true;
            }
            catch (System.Exception ex) { error = "Unreadable JSON (" + sourceName + "): " + ex.Message; return false; }
        }

        public static bool TryLoad(string relativePath, out LevelData data, out string error)
        {
            data = null; error = null;
            if (string.IsNullOrEmpty(relativePath)) { error = "No TextAsset or StreamingAssets path provided for the level."; return false; }
            string path = Path.Combine(Application.streamingAssetsPath, relativePath);
            if (!File.Exists(path)) { error = "File not found: " + path; return false; }
            return TryParse(File.ReadAllText(path), path, out data, out error);
        }

        public static bool TryParseSide(string s, out Dir d)
        {
            d = Dir.N;
            if (string.IsNullOrEmpty(s)) return false;
            switch (char.ToUpperInvariant(s[0]))
            {
                case 'N': d = Dir.N; return true;
                case 'E': d = Dir.E; return true;
                case 'S': d = Dir.S; return true;
                case 'W': case 'O': d = Dir.W; return true;
            }
            return false;
        }

        sealed class Ctx
        {
            public BoardModel board;
            public HashSet<EdgeKey> walls;
            public HashSet<string> ids = new HashSet<string>();
            public Dictionary<Vector2Int, string> solid = new Dictionary<Vector2Int, string>();
            public List<ElementDef> elements = new List<ElementDef>();
            public List<string> errors, warnings;
        }

        public static bool TryBuildModel(LevelData d, BoardModel board, out LevelModel model,
                                         List<string> errors, List<string> warnings)
        {
            model = null;
            if (d == null) { errors.Add("Null level."); return false; }
            if (board == null) { errors.Add("Missing grid map."); return false; }
            if (!string.IsNullOrEmpty(d.gridMap) && d.gridMap != board.id)
                warnings.Add("The level targets grid map '" + d.gridMap + "' but '" + board.id + "' is loaded.");

            // ---------- Walls ----------
            var rules = d.walls ?? new WallRules();
            var set = new HashSet<EdgeKey>();
            var order = new List<EdgeKey>();

            string mode = (rules.mode ?? "").Trim().ToLowerInvariant();
            if (mode == "zoneboundaries")
            {
                foreach (var e in board.ZoneBoundaryEdges)
                {
                    if (!rules.includeBoardBorder)
                    {
                        var a = e.CellA; var b = e.CellB;
                        if (board.ZoneAt(a.x, a.y) < 0 || board.ZoneAt(b.x, b.y) < 0) continue;
                    }
                    if (set.Add(e)) order.Add(e);
                }
            }
            else if (mode != "explicit" && mode != "none")
            {
                errors.Add("Unknown walls.mode: '" + rules.mode + "' (zoneBoundaries | explicit | none).");
                return false;
            }

            if (rules.addEdges != null)
                for (int i = 0; i < rules.addEdges.Length; i++)
                {
                    EdgeKey e;
                    if (!TryResolve(rules.addEdges[i], board, "walls.addEdges[" + i + "]", errors, out e)) continue;
                    if (set.Add(e)) order.Add(e);
                }

            if (rules.removeEdges != null)
                for (int i = 0; i < rules.removeEdges.Length; i++)
                {
                    EdgeKey e;
                    if (!TryResolve(rules.removeEdges[i], board, "walls.removeEdges[" + i + "]", errors, out e)) continue;
                    if (!set.Remove(e)) warnings.Add("walls.removeEdges[" + i + "]: no wall at this location (" + e + ").");
                }

            // ---------- Elements ----------
            var ctx = new Ctx { board = board, walls = set, errors = errors, warnings = warnings };
            BuildElements(d, ctx);
            List<int> initial = ResolveInitialZones(d, board, ctx.elements, errors, warnings);

            if (errors.Count > 0) return false;

            var final = new List<EdgeKey>(set.Count);
            foreach (var e in order) if (set.Contains(e)) final.Add(e);
            model = new LevelModel(d.levelId, d.name, final, ctx.elements, initial);
            return true;
        }

        // =====================================================================

        static void BuildElements(LevelData d, Ctx c)
        {
            // --- Stairs (present if at least one field is set) ---
            var st = d.stairs;
            if (st != null && (st.x != 0 || st.y != 0 || st.w != 0 || st.h != 0))
            {
                string id = ResolveId(c, "", "stairs", 0, "stairs");
                if (id != null)
                    AddFootprint(c, ElementKind.Stairs, id, "Stairs", st.x, st.y,
                                 st.w > 0 ? st.w : 2, st.h > 0 ? st.h : 2, Dir.N, false, false, "stairs", true);
            }

            // --- Doors ---
            var doorEdges = new HashSet<EdgeKey>();
            if (d.doors != null)
                for (int i = 0; i < d.doors.Length; i++)
                {
                    var dd = d.doors[i]; string label = "doors[" + i + "]";
                    if (dd == null) continue;
                    Dir side;
                    if (!TryParseSide(dd.side, out side)) { c.errors.Add(label + ": invalid side '" + dd.side + "' (N, E, S, W/O)."); continue; }
                    if (!c.board.IsWalkable(dd.x, dd.y)) { c.errors.Add(label + ": cell (" + dd.x + "," + dd.y + ") out of bounds or void."); continue; }
                    string id = ResolveId(c, dd.id, "door", i, label);
                    if (id == null) continue;

                    EdgeKey edge = EdgeKey.FromCell(dd.x, dd.y, side);
                    if (!doorEdges.Add(edge)) { c.errors.Add(label + " ('" + id + "'): two doors on the same edge " + edge + "."); continue; }
                    if (!c.walls.Contains(edge)) c.warnings.Add(label + " ('" + id + "'): no wall at this location (" + edge + "): the door will float.");

                    var def = new ElementDef
                    {
                        id = id,
                        type = string.IsNullOrEmpty(dd.type) ? "Door" : dd.type,
                        kind = ElementKind.Door,
                        x = dd.x,
                        y = dd.y,
                        w = 1,
                        h = 1,
                        side = side,
                        facing = side,
                        edge = edge,
                        hidden = dd.hidden
                    };
                    foreach (var cell in new[] { edge.CellA, edge.CellB })
                        if (c.board.IsWalkable(cell.x, cell.y))
                        {
                            def.cells.Add(cell);
                            int z = c.board.ZoneAt(cell.x, cell.y);
                            if (!def.zones.Contains(z)) def.zones.Add(z);
                        }
                    c.elements.Add(def);
                }

            // --- Blocked cells ---
            if (d.blocked != null)
                for (int i = 0; i < d.blocked.Length; i++)
                {
                    var b = d.blocked[i]; string label = "blocked[" + i + "]";
                    if (b == null) continue;
                    string id = ResolveId(c, b.id, "blocked", i, label);
                    if (id == null) continue;
                    AddFootprint(c, ElementKind.Blocked, id, string.IsNullOrEmpty(b.type) ? "Blocked" : b.type,
                                 b.x, b.y, b.w > 0 ? b.w : 1, b.h > 0 ? b.h : 1, Dir.N, b.hidden, false, label, true);
                }

            // --- Furniture / chests ---
            if (d.furniture != null)
                for (int i = 0; i < d.furniture.Length; i++)
                {
                    var f = d.furniture[i]; string label = "furniture[" + i + "]";
                    if (f == null) continue;
                    Dir facing;
                    if (!TryFacing(c, f.facing, label, out facing)) continue;
                    string id = ResolveId(c, f.id, "furniture", i, label);
                    if (id == null) continue;
                    AddFootprint(c, ElementKind.Furniture, id, string.IsNullOrEmpty(f.type) ? "Furniture" : f.type,
                                 f.x, f.y, f.w > 0 ? f.w : 1, f.h > 0 ? f.h : 1, facing, f.hidden, f.trapped, label, true);
                }

            // --- Monsters ---
            if (d.monsters != null)
                for (int i = 0; i < d.monsters.Length; i++)
                {
                    var m = d.monsters[i]; string label = "monsters[" + i + "]";
                    if (m == null) continue;
                    Dir facing;
                    if (!TryFacing(c, m.facing, label, out facing)) continue;
                    string id = ResolveId(c, m.id, "monster", i, label);
                    if (id == null) continue;
                    AddFootprint(c, ElementKind.Monster, id, string.IsNullOrEmpty(m.type) ? "Monster" : m.type,
                                 m.x, m.y, 1, 1, facing, m.hidden, false, label, true);
                }

            // --- Traps (after others: can warn if placed on an occupied cell) ---
            if (d.traps != null)
                for (int i = 0; i < d.traps.Length; i++)
                {
                    var t = d.traps[i]; string label = "traps[" + i + "]";
                    if (t == null) continue;
                    string id = ResolveId(c, t.id, "trap", i, label);
                    if (id == null) continue;
                    var def = AddFootprint(c, ElementKind.Trap, id, string.IsNullOrEmpty(t.type) ? "Trap" : t.type,
                                           t.x, t.y, 1, 1, Dir.N, true, false, label, false);
                    string other;
                    if (def != null && c.solid.TryGetValue(new Vector2Int(t.x, t.y), out other))
                        c.warnings.Add(label + " ('" + id + "'): trap on a cell occupied by '" + other + "'.");
                }
        }

        static string ResolveId(Ctx c, string given, string prefix, int index, string label)
        {
            string id = string.IsNullOrEmpty(given) ? prefix + "_" + (index + 1) : given;
            if (!c.ids.Add(id)) { c.errors.Add(label + ": duplicate id '" + id + "'."); return null; }
            return id;
        }

        static bool TryFacing(Ctx c, string s, string label, out Dir d)
        {
            d = Dir.N;
            if (string.IsNullOrEmpty(s)) return true;
            if (TryParseSide(s, out d)) return true;
            c.errors.Add(label + ": invalid facing '" + s + "' (N, E, S, W/O).");
            return false;
        }

        static ElementDef AddFootprint(Ctx c, ElementKind kind, string id, string type, int x, int y, int w, int h,
                                       Dir facing, bool hidden, bool trapped, string label, bool solidCells)
        {
            var def = new ElementDef
            {
                id = id,
                type = type,
                kind = kind,
                x = x,
                y = y,
                w = w,
                h = h,
                facing = facing,
                hidden = hidden,
                trapped = trapped
            };
            bool ok = true;
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    int cx = x + i, cy = y + j;
                    if (!c.board.IsWalkable(cx, cy))
                    { c.errors.Add(label + " ('" + id + "'): cell (" + cx + "," + cy + ") out of bounds or void."); ok = false; continue; }

                    var p = new Vector2Int(cx, cy);
                    def.cells.Add(p);
                    int z = c.board.ZoneAt(cx, cy);
                    if (!def.zones.Contains(z)) def.zones.Add(z);

                    if (solidCells)
                    {
                        string other;
                        if (c.solid.TryGetValue(p, out other))
                        { c.errors.Add(label + " ('" + id + "') overlaps '" + other + "' at (" + cx + "," + cy + ")."); ok = false; }
                        else c.solid[p] = id;
                    }
                }
            if (!ok) return null;
            if (def.zones.Count > 1)
                c.warnings.Add(label + " ('" + id + "') spans multiple zones (" + string.Join(",", def.zones) + ").");
            c.elements.Add(def);
            return def;
        }

        static List<int> ResolveInitialZones(LevelData d, BoardModel board, List<ElementDef> elements,
                                           List<string> errors, List<string> warnings)
        {
            var res = new List<int>();
            int[] cfg = d.reveal != null ? d.reveal.initialZones : null;
            if (cfg != null && cfg.Length > 0)
            {
                foreach (int z in cfg)
                {
                    if (z < 0 || z > board.roomCount) { errors.Add("reveal.initialZones: invalid zone " + z + " (0 to " + board.roomCount + ")."); continue; }
                    if (!res.Contains(z)) res.Add(z);
                }
                return res;
            }
            foreach (var e in elements)
                if (e.kind == ElementKind.Stairs && e.cells.Count > 0)
                {
                    int z = board.ZoneAt(e.cells[0].x, e.cells[0].y);
                    if (z > 0) { res.Add(z); return res; }
                }
            warnings.Add("No zones revealed initially (neither reveal.initialZones nor stairs in a room).");
            return res;
        }

        static bool TryResolve(EdgeRef r, BoardModel board, string label, List<string> errors, out EdgeKey edge)
        {
            edge = default(EdgeKey);
            if (r == null) { errors.Add(label + ": null entry."); return false; }
            Dir side;
            if (!TryParseSide(r.side, out side)) { errors.Add(label + ": invalid side '" + r.side + "' (N, E, S, W/O)."); return false; }
            if (!board.InBounds(r.x, r.y)) { errors.Add(label + ": cell (" + r.x + "," + r.y + ") out of bounds."); return false; }
            edge = EdgeKey.FromCell(r.x, r.y, side);
            return true;
        }
    }
}