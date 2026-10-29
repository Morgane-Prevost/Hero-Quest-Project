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
        if (overlay != null) overlay.SetActive(false);
        if (portraitButton != null) portraitButton.onClick.AddListener(Open);
        if (overlayButton != null) overlayButton.onClick.AddListener(Close);
    }

    public void SetHero(HeroData hero)
    {
        if (hero == null)
        {
            currentCard = null;
            return;
        }

        currentCard = hero.cardTexture != null ? hero.cardTexture : hero.texture;
    }

    private void Open()
    {
        if (currentCard == null || cardImage == null || overlay == null) return;

        cardImage.texture = currentCard;
        if (ratioFitter != null && currentCard.height > 0)
        {
            ratioFitter.aspectRatio = (float)currentCard.width / currentCard.height;
        }

        overlay.SetActive(true);
    }

    private void Close()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (overlay != null && overlay.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
#endif
    }
}