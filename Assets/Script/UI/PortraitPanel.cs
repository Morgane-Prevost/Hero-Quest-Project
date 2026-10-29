using UnityEngine;
using UnityEngine.UI;

public class PortraitPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage portraitImage;
    [SerializeField] private AspectRatioFitter ratioFitter;

    public void SetHero(HeroData hero)
    {
        if (hero == null || hero.texture == null)
        {
            if (portraitImage != null) portraitImage.enabled = false;
            return;
        }

        portraitImage.texture = hero.texture;
        portraitImage.enabled = true;

        if (ratioFitter != null && hero.texture.height > 0)
        {
            ratioFitter.aspectRatio = (float)hero.texture.width / hero.texture.height;
        }
    }
}