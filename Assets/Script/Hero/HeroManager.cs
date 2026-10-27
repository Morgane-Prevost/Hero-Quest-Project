using UnityEngine;
using System.Collections.Generic;
using HeroQuest.Board;

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

    public void CreateCharacter()
    {
        foreach (HeroData heroData in heroes)
        {
            GameObject heroObject = Instantiate(heroData.prefab, Vector3.zero, Quaternion.identity);
            Hero heroComponent = heroObject.AddComponent<Hero>();
            heroComponent.Initialize(heroData, heroData.name); // To be modified to take the name from the character creation UI
            GridPawn gridPawnComponent = heroObject.AddComponent<GridPawn>();
            gridPawnComponent.board = board;
        }
    }
}
