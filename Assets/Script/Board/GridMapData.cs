using System;

namespace HeroQuest.Board
{
    // DTO compatibles JsonUtility (champs publics, pas de dictionnaires).
    // La grid map ne décrit QUE le sol : zones (salles / couloirs) et styles de sol.
    [Serializable]
    public class GridMapData
    {
        public int schemaVersion = 1;
        public string id = "board";
        public int width = 26;
        public int height = 19;
        public int expectedRoomCount = 22;   // 0 = ne pas contrôler (plateaux d'extension)
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
        public string rooms = "abcdefghijklmnopqrstuv"; // le caractère i = salle (i+1)
    }

    [Serializable]
    public class RoomStyleOverride
    {
        public int zone;
        public string floorStyle;
        public string name;
        public string theme; // indice pour le Room Dressing (étape 3)
    }

    [Serializable]
    public class FloorStyleData
    {
        public string id = "default";
        public string pattern = "flagstone"; // flagstone | checker | planks | cracked
        public string colorA = "#9C8F7A";
        public string colorB = "#8A7D69";
        public string lineColor = "#3A332B";
        public int lineWidthPx = 2;          // par côté de case : la ligne visible fait 2x cette valeur
        public int tileTexturePx = 128;
        public int variants = 3;
        public string texturePath = "";      // optionnel : PNG relatif à StreamingAssets (extensions)
    }
}