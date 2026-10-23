
using System.Collections.Generic;

public class TreasureCardData
{
    public string Id;                   //ex : "potion_healing
    public string DispayName;           
    public bool ReturnsToPile;          // true pour piege et monstre errant
    public string EffectId;             // ex : "gain_gold", "wandering_monster"

    public Dictionary<string, int> Parameters = new Dictionary<string, int>();
}
