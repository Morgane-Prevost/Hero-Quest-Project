using UnityEngine;
using UnityEngine.UI;

public class PortraitPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage portraitImage;
    [SerializeField] private AspectRatioFitter ratioFitter;

    public void SetHero(HeroData hero)
    {
        portraitImage.texture = hero.texture;

        if (hero.texture  != null)
        {
            ratioFitter.aspectRatio = (float)hero.texture.width / hero.texture.height;
        }
    }
}