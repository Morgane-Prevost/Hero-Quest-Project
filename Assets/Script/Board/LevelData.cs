using System;

namespace HeroQuest.Board
{
    /// <summary>A level = a JSON. Convention: x increases towards the East, y towards the North, (0,0) = bottom-left corner.</summary>
    /// <summary>No "magic" default values: 0 / empty = default value interpreted by the loader.</summary>
    [Serializable]
    public class LevelData
    {
        public int schemaVersion = 1;
        public string levelId = "level";
        public string name = "";
        public string gridMap = "";              // target grid map id (informative check)

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
        public int[] initialZones;               // rooms visible to heroes initially (default: stairs room)
    }

    [Serializable]
    public class WallRules
    {
        // "zoneBoundaries" : a wall at each boundary between two grid map zones
        // "explicit"       : no starting walls, everything comes from addEdges
        // "none"           : no walls
        public string mode = "zoneBoundaries";
        public bool includeBoardBorder = true;
        public EdgeRef[] addEdges;
        public EdgeRef[] removeEdges;
    }

    /// <summary>An edge, described from a cell: "the {side} side of cell (x,y)".</summary>
    [Serializable]
    public class EdgeRef { public int x; public int y; public string side; }

    /// <summary>Starting stairs. x,y = bottom-left corner. missing w,h = 2x2.</summary>
    [Serializable]
    public class StairsData { public int x; public int y; public int w; public int h; }

    /// <summary>Door placed on the {side} side of cell (x,y). hidden = secret door.</summary>
    [Serializable]
    public class DoorData { public string id; public string type; public int x; public int y; public string side; public bool hidden; }

    /// <summary>Obstructed cell(s). x,y = bottom-left corner, w,h = footprint (default 1x1).</summary>
    [Serializable]
    public class BlockedData { public string id; public string type; public int x; public int y; public int w; public int h; public bool hidden; }

    /// <summary>Furniture / chest. w,h = ACTUAL footprint on the grid (default 1x1). facing = "front side" direction.</summary>
    [Serializable]
    public class FurnitureData
    {
        public string id; public string type;
        public int x; public int y; public int w; public int h;
        public string facing;
        public bool hidden;      // invisible to heroes until RevealElement
        public bool trapped;     // information for game logic (no visual)
    }

    [Serializable]
    public class MonsterData { public string id; public string type; public int x; public int y; public string facing; public bool hidden; }

    /// <summary>Trap: always hidden initially (gold marker for Zargon only).</summary>
    [Serializable]
    public class TrapData { public string id; public string type; public int x; public int y; }
}