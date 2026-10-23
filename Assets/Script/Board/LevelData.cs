using System;

namespace HeroQuest.Board
{
    /// <summary>
    /// Un niveau = un JSON. Pour l'instant : les murs.
    /// L'étape 2 y ajoutera stairs, doors, furniture, monsters, traps, reveal, dressing…
    /// (JsonUtility ignore les clés inconnues : les équipes peuvent déjà les rédiger.)
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int schemaVersion = 1;
        public string levelId = "level";
        public string name = "";
        public string gridMap = "";            // id de la grid map visée (contrôle informatif)
        public WallRules walls = new WallRules();
    }

    [Serializable]
    public class WallRules
    {
        // "zoneBoundaries" : un mur à chaque frontière entre deux zones de la grid map
        // "explicit"       : aucun mur de départ, tout vient de addEdges
        // "none"           : aucun mur
        public string mode = "zoneBoundaries";
        public bool includeBoardBorder = true; // mode zoneBoundaries : murs sur le pourtour / contre le vide
        public EdgeRef[] addEdges;             // appliqué après le mode
        public EdgeRef[] removeEdges;          // appliqué en dernier
    }

    /// <summary>Une arête, décrite depuis une case : "le côté {side} de la case (x,y)".</summary>
    [Serializable]
    public class EdgeRef
    {
        public int x;
        public int y;
        public string side; // N, E, S, W (ou O pour Ouest)
    }
}