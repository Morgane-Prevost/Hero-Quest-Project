using UnityEngine;
using System.Collections.Generic;

public class HeroManager : MonoBehaviour
{
    [SerializeField] private List<HeroData> heroes;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void createCharacter()
    {
        foreach (HeroData heroData in heroes)
        {
            GameObject heroObject = Instantiate(heroData.prefab, Vector3.zero, Quaternion.identity);
            Hero heroComponent = heroObject.AddComponent<Hero>();
            heroComponent.Initialize(heroData, heroData.name); // To be modified to take the name from the character creation UI
        }
    }
}
