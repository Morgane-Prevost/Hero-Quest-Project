using UnityEngine;
using System.Collections.Generic;
using HeroQuest.Board;
using UnityEngine.UI;

public class HeroManager : MonoBehaviour
{
    [SerializeField] private BoardBuilder board;
    [SerializeField] private List<HeroData> heroes;


    public static HeroManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CreateCharacter(List<Vector2Int> startCells)
    {
        foreach (HeroData heroData in heroes)
        {
            GameObject heroObject = Instantiate(heroData.prefab, Vector3.zero, Quaternion.identity);
            GridPawn gridPawnComponent = heroObject.AddComponent<GridPawn>();
            gridPawnComponent.board = board;
            Hero heroComponent = heroObject.AddComponent<Hero>();
            Vector2Int startCell = startCells.Count > 0 ? startCells[0] : new Vector2Int(0, 0); // Default starting cell, can be modified later
            heroComponent.Initialize(heroData, heroData.name, startCell); // To be modified to take the name from the character creation UI
        }
    }
}
