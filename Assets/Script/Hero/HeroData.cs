using UnityEngine;

[CreateAssetMenu(fileName = "HeroData", menuName = "GameDatas/HeroData")]
public class HeroData : ScriptableObject
{
    public GameObject prefab;
    public Texture2D texture;

    public string name;

    public int bodyPoints;
    public int mindPoints;
    public int attackDices;
    public int defenseDices;

    public int movementDices;

    public Equipement weapon;
    public Equipement armor;
}
