using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HeroData", menuName = "GameDatas/HeroData")]
public class HeroData : ScriptableObject
{
    public GameObject prefab;
    public Texture2D texture;

    public string name;

    public HeroType type;

    public int bodyPoints;
    public int mindPoints;
    public int attackDices;
    public int defenseDices;

    public int movementDices;

    public WeaponData weapon;
    public List<ArmorData> armor;
    public int gold;
}
