using System;

namespace HeroQuest.Board
{
    // JsonUtility compatible DTOs (public fields, no dictionaries).
    // The grid map describes ONLY the floor: zones (rooms / corridors) and floor styles.
    [Serializable]
    public class GridMapData
    {
        public int schemaVersion = 1;
        public string id = "board";
        public int width = 26;
        public int height = 19;
        public int expectedRoomCount = 22;   // 0 = do not validate (expansion boards)
        public BoardSymbols symbols = new BoardSymbols();
        public string[] rows;
        public string corridorStyle = "corridor_stone";
        public string[] roomStylePalette;
        public RoomStyleOverride[] roomOverrides;
        public FloorStyleData[] floorStyles;
    }

    [Serializable]
    public class BoardSymbols
    {
        public string corridor = ".";
        public string voidCell = "#";
        public string rooms = "abcdefghijklmnopqrstuv"; // character index = room (index + 1)
    }

    [Serializable]
    public class RoomStyleOverride
    {
        public int zone;
        public string floorStyle;
        public string name;
        public string theme; // index for Room Dressing (step 3)
    }

    [Serializable]
    public class FloorStyleData
    {
        public string id = "default";
        public string pattern = "flagstone"; // flagstone | checker | planks | cracked
        public string colorA = "#9C8F7A";
        public string colorB = "#8A7D69";
        public string lineColor = "#3A332B";
        public int lineWidthPx = 2;          // per tile side: the visible line is 2x this value
        public int tileTexturePx = 128;
        public int variants = 3;
        public string texturePath = "";      // optional: PNG relative to StreamingAssets (extensions)
    }
}