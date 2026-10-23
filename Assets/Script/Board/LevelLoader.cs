using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>Lecture + validation d'un JSON de niveau, et calcul de l'ensemble final des murs.</summary>
    public static class LevelLoader
    {
        public static bool TryParse(string json, string sourceName, out LevelData data, out string error)
        {
            data = null; error = null;
            if (string.IsNullOrEmpty(json)) { error = "JSON vide : " + sourceName; return false; }
            try
            {
                data = JsonUtility.FromJson<LevelData>(json);
                if (data == null) { error = "JSON invalide : " + sourceName; return false; }
                return true;
            }
            catch (System.Exception ex) { error = "JSON illisible (" + sourceName + ") : " + ex.Message; return false; }
        }

        public static bool TryLoad(string relativePath, out LevelData data, out string error)
        {
            data = null; error = null;
            if (string.IsNullOrEmpty(relativePath)) { error = "Aucun TextAsset ni chemin StreamingAssets renseigné pour le niveau."; return false; }
            string path = Path.Combine(Application.streamingAssetsPath, relativePath);
            if (!File.Exists(path)) { error = "Fichier introuvable : " + path; return false; }
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

        public static bool TryBuildModel(LevelData d, BoardModel board, out LevelModel model,
                                         List<string> errors, List<string> warnings)
        {
            model = null;
            if (d == null) { errors.Add("Niveau nul."); return false; }
            if (board == null) { errors.Add("Grid map absente."); return false; }
            if (!string.IsNullOrEmpty(d.gridMap) && d.gridMap != board.id)
                warnings.Add("Le niveau vise la grid map '" + d.gridMap + "' mais '" + board.id + "' est chargée.");

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
                errors.Add("walls.mode inconnu : '" + rules.mode + "' (zoneBoundaries | explicit | none).");
                return false;
            }

            if (rules.addEdges != null)
                for (int i = 0; i < rules.addEdges.Length; i++)
                {
                    EdgeKey e;
                    if (!TryResolve(rules.addEdges[i], board, "addEdges[" + i + "]", errors, out e)) continue;
                    if (set.Add(e)) order.Add(e);
                }

            if (rules.removeEdges != null)
                for (int i = 0; i < rules.removeEdges.Length; i++)
                {
                    EdgeKey e;
                    if (!TryResolve(rules.removeEdges[i], board, "removeEdges[" + i + "]", errors, out e)) continue;
                    if (!set.Remove(e)) warnings.Add("removeEdges[" + i + "] : aucun mur à cet endroit (" + e + ").");
                }

            if (errors.Count > 0) return false;

            var final = new List<EdgeKey>(set.Count);
            foreach (var e in order) if (set.Contains(e)) final.Add(e);
            model = new LevelModel(d.levelId, d.name, final);
            return true;
        }

        static bool TryResolve(EdgeRef r, BoardModel board, string label, List<string> errors, out EdgeKey edge)
        {
            edge = default(EdgeKey);
            if (r == null) { errors.Add(label + " : entrée nulle."); return false; }
            Dir side;
            if (!TryParseSide(r.side, out side)) { errors.Add(label + " : side invalide '" + r.side + "' (N, E, S, W/O)."); return false; }
            if (!board.InBounds(r.x, r.y)) { errors.Add(label + " : case (" + r.x + "," + r.y + ") hors plateau."); return false; }
            edge = EdgeKey.FromCell(r.x, r.y, side);
            return true;
        }
    }
}