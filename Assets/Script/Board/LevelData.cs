using System;

namespace HeroQuest.Board
{
    /// <summary>
    /// One level = one JSON file. Convention: x grows East, y grows North, (0,0) = bottom-left cell.
    /// Zero / empty values mean "use the default", interpreted by the loader.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int schemaVersion = 1;
        public string levelId = "level";
        public string name = "";
        public string gridMap = "";              // informative: id of the grid map this level targets

        public int[] unusedRooms;                // rooms (1..22) not used by this level: floor hidden, not playable
        public RevealRules reveal = new RevealRules();
        public WallRules walls = new WallRules();
        public StairsData stairs = new StairsData();
        public DoorData[] doors;
        public BlockedData[] blocked;
        public FurnitureData[] furniture;        // furniture AND chests (type "Chest")
        public MonsterData[] monsters;
        public TrapData[] traps;
    }

    [Serializable]
    public class RevealRules
    {
        public int[] initialZones;               // rooms visible to heroes at start (default: the stairs room)
    }

    [Serializable]
    public class WallRules
    {
        // "zoneBoundaries": one wall on every border between two different zones
        // "explicit":       no wall at start, everything comes from addEdges
        // "none":           no wall at all
        public string mode = "zoneBoundaries";
        public bool includeBoardBorder = true;
        public EdgeRef[] addEdges;
        public EdgeRef[] removeEdges;
    }

    /// <summary>An edge described from a cell: "the {side} side of cell (x,y)".</summary>
    [Serializable]
    public class EdgeRef { public int x; public int y; public string side; }

    /// <summary>Start/exit staircase. x,y = bottom-left corner. Missing w,h = 2x2.</summary>
    [Serializable]
    public class StairsData { public int x; public int y; public int w; public int h; }

    /// <summary>Door on the {side} side of cell (x,y). hidden = secret door.</summary>
    [Serializable]
    public class DoorData { public string id; public string type; public int x; public int y; public string side; public bool hidden; public string note; }

    /// <summary>Blocked square(s). x,y = bottom-left corner, w,h = footprint (default 1x1).</summary>
    [Serializable]
    public class BlockedData { public string id; public string type; public int x; public int y; public int w; public int h; public bool hidden; public string note; }

    /// <summary>Furniture / chest. w,h = REAL footprint on the grid (default 1x1). facing = front side of the model.</summary>
    [Serializable]
    public class FurnitureData
    {
        public string id; public string type;
        public int x; public int y; public int w; public int h;
        public string facing;
        public bool hidden;     // invisible to heroes until RevealElement
        public bool trapped;    // gameplay info only (no visual)
        public string note;
    }

    [Serializable]
    public class MonsterData { public string id; public string type; public int x; public int y; public string facing; public bool hidden; public string note; }

    /// <summary>Trap: always hidden at start (gold marker for Zargon only).</summary>
    [Serializable]
    public class TrapData { public string id; public string type; public int x; public int y; public string note; }
}