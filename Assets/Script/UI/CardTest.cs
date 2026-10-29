using UnityEngine;

public class CardTest : MonoBehaviour
{
    [SerializeField] private CardView cardView;
    [SerializeField] private CardSO testCard;

    private void Start()
    {
        cardView.Show(testCard);
    }
}