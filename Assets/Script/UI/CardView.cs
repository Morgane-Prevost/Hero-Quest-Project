using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RawImage artworkImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject priceObject;
    [SerializeField] private TMP_Text priceText;

    public void Show(CardSO card)
    {
        titleText.text = card.displayName;
        descriptionText.text = card.description;

        Texture2D picture = card.artwork != null ? card.artwork : card.icon;
        artworkImage.texture = picture;
        artworkImage.enabled = picture != null;

        bool hasPrice = card.goldCost > 0;
        priceObject.SetActive(hasPrice);

        if (hasPrice)
        {
            priceText.text = card.goldCost + " gold";
        }
    }
}