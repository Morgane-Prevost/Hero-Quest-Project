using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>Reads and validates the grid map (project TextAsset or extension file).</summary>
    public static class GridMapLoader
    {
        public static bool TryParse(string json, string sourceName, out GridMapData data, out string error)
        {
            data = null; error = null;
            if (string.IsNullOrEmpty(json)) { error = "Empty JSON: " + sourceName; return false; }
            try
            {
                data = JsonUtility.FromJson<GridMapData>(json);
                if (data == null) { error = "Invalid JSON: " + sourceName; return false; }
                return true;
            }
            catch (System.Exception ex) { error = "Unreadable JSON (" + sourceName + "): " + ex.Message; return false; }
        }

        /// <summary>Reads a file from StreamingAssets (client extensions). PC target.</summary>
        public static bool TryLoad(string relativePath, out GridMapData data, out string error)
        {
            data = null; error = null;
            if (string.IsNullOrEmpty(relativePath)) { error = "No TextAsset or StreamingAssets path provided for the grid map."; return false; }
            string path = Path.Combine(Application.streamingAssetsPath, relativePath);
            if (!File.Exists(path)) { error = "File not found: " + path; return false; }
            return TryParse(File.ReadAllText(path), path, out data, out error);
        }

        public static bool TryBuildModel(GridMapData d, out BoardModel model,
                                        List<string> errors, List<string> warnings)
        {
            model = null;
            if (d == null) { errors.Add("Null data."); return false; }
            if (d.width <= 0 || d.height <= 0) { errors.Add("Invalid width/height."); return false; }
            if (d.rows == null || d.rows.Length != d.height)
            {
                errors.Add("rows must contain " + d.height + " lines (found " + (d.rows == null ? 0 : d.rows.Length) + ").");
                return false;
            }

            var sym = d.symbols ?? new BoardSymbols();
            if (string.IsNullOrEmpty(sym.corridor) || string.IsNullOrEmpty(sym.voidCell) || string.IsNullOrEmpty(sym.rooms))
            { errors.Add("Incomplete symbols."); return false; }

            char cCor = sym.corridor[0], cVoid = sym.voidCell[0];
            var roomIndex = new Dictionary<char, int>();
            for (int i = 0; i < sym.rooms.Length; i++)
            {
                char ch = sym.rooms[i];
                if (ch == cCor || ch == cVoid || roomIndex.ContainsKey(ch))
                { errors.Add("Conflicting room symbol: '" + ch + "'."); continue; }
                roomIndex[ch] = i + 1;
            }

            int w = d.width, h = d.height;
            var zone = new int[w, h];
            var present = new HashSet<int>();

            for (int r = 0; r < h; r++)
            {
                string row = d.rows[r];
                if (row == null || row.Length != w)
                { errors.Add("Row " + r + ": " + (row == null ? 0 : row.Length) + " characters instead of " + w + "."); continue; }
                int y = h - 1 - r; // rows[0] = North
                for (int x = 0; x < w; x++)
                {
                    char ch = row[x];
                    int z;
                    if (ch == cCor) z = 0;
                    else if (ch == cVoid) z = -1;
                    else if (!roomIndex.TryGetValue(ch, out z))
                    { errors.Add("Unknown character '" + ch + "' at row " + r + ", column " + x + "."); z = -1; }
                    zone[x, y] = z;
                    if (z > 0) present.Add(z);
                }
            }
            if (errors.Count > 0) return false;

            if (d.expectedRoomCount > 0 && present.Count != d.expectedRoomCount)
                errors.Add("Rooms present: " + present.Count + " (expected " + d.expectedRoomCount + ").");

            // Connectivity of each zone (a room or the corridor network in a single block)
            var seen = new bool[w, h];
            var comps = new Dictionary<int, int>();
            var stack = new Stack<Vector2Int>();
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (seen[x, y] || zone[x, y] < 0) continue;
                    int z = zone[x, y];
                    int c; comps.TryGetValue(z, out c); comps[z] = c + 1;
                    seen[x, y] = true; stack.Push(new Vector2Int(x, y));
                    while (stack.Count > 0)
                    {
                        var p = stack.Pop();
                        for (int k = 0; k < 4; k++)
                        {
                            int nx = p.x + DirUtil.DX[k], ny = p.y + DirUtil.DY[k];
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h || seen[nx, ny] || zone[nx, ny] != z) continue;
                            seen[nx, ny] = true; stack.Push(new Vector2Int(nx, ny));
                        }
                    }
                }
            foreach (var kv in comps)
                if (kv.Value > 1)
                    warnings.Add((kv.Key == 0 ? "Corridors" : "Room " + kv.Key) + " in " + kv.Value + " unconnected pieces.");

            if (errors.Count > 0) return false;
            model = new BoardModel(d.id, zone, sym.rooms.Length);
            return true;
        }
    }
}