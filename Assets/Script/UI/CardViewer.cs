using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CardViewer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button portraitButton;
    [SerializeField] private GameObject overlay;
    [SerializeField] private Button overlayButton;
    [SerializeField] private RawImage cardImage;
    [SerializeField] private AspectRatioFitter ratioFitter;

    private Texture2D currentCard;

    private void Awake()
    {
        overlay.SetActive(false);

        portraitButton.onClick.AddListener(Open);
        overlayButton.onClick.AddListener(Close);
    }

    public void SetHero(HeroData hero)
    {
        currentCard = hero.cardTexture;

        // Si la carte n'est pas encore faite, on affiche le portrait à la place
        if (currentCard == null)
        {
            currentCard = hero.texture;
        }
    }

    private void Open()
    {
        if (currentCard == null)
            return;

        cardImage.texture = currentCard;
        ratioFitter.aspectRatio = (float)currentCard.width / currentCard.height;

        overlay.SetActive(true);
    }

    private void Close()
    {
        overlay.SetActive(false);
    }

    private void Update()
    {
        // Échap ferme aussi la carte
        if (overlay.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }
}