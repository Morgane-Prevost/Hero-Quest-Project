using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TreasureDeck")]
public class TreasureDeckSO : ScriptableObject
{
    public List<TreasureCardSO> cards;
}
