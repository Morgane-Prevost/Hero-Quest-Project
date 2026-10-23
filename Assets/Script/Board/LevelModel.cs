using System.Collections.Generic;

namespace HeroQuest.Board
{
    /// <summary>
    /// État des murs d'un niveau, en lecture seule. C'est CE modèle (et non BoardModel)
    /// que le Lead Dev interroge pour savoir si deux cases voisines sont séparées par un mur.
    /// </summary>
    public sealed class LevelModel
    {
        public readonly string id, name;
        readonly HashSet<EdgeKey> set;
        readonly List<EdgeKey> list;

        public IReadOnlyList<EdgeKey> WallEdges { get { return list; } }

        public LevelModel(string id, string name, List<EdgeKey> edges)
        {
            this.id = id; this.name = name;
            list = edges;
            set = new HashSet<EdgeKey>(edges);
        }

        public bool HasWall(EdgeKey e) { return set.Contains(e); }
        public bool HasWall(int x, int y, Dir side) { return set.Contains(EdgeKey.FromCell(x, y, side)); }
    }
}