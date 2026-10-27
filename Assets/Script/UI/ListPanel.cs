using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ListPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button arrowButton;
    [SerializeField] private TMP_Text arrowText;

    [Header("Lists")]
    [SerializeField] private Transform monsterGrid;
    [SerializeField] private Transform heroGrid;
    [SerializeField] private Button characterButtonPrefab;

    [Header("Animation")]
    [SerializeField] private float slideSpeed = 8f;

    [Header("Test values")]
    [SerializeField] private string[] monsterNames =
    {
        "Gobelin", "Squelette", "Zombie", "Orc", "Abomination", "Momie", "Gargouille", "Guerrier de la terreur"
    };
    [SerializeField] private string[] heroNames =
    {
        "Nain", "Enchanteur", "Elfe", "Barbare"
    };

    private bool isOpen;
    private float panelWidth;

    private void Awake()
    {
        panelWidth = panel.rect.width;
        panel.anchoredPosition = new Vector2(-panelWidth, panel.anchoredPosition.y);
        arrowText.text = ">>";

        arrowButton.onClick.AddListener(OnToggle);

        CreateButtons(monsterNames, monsterGrid);
        CreateButtons(heroNames, heroGrid);
    }

    private void OnToggle()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            arrowText.text = "<<";
        }
        else
        {
            arrowText.text = ">>";
        }
    }

    private void CreateButtons(string[] names, Transform grid)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Button button = Instantiate(characterButtonPrefab, grid);
            button.GetComponentInChildren<TMP_Text>().text = names[i];
        }
    }

    // Update is called once per frame
    void Update()
    {
        float targetX = isOpen ? 0f : -panelWidth;

        float newX = Mathf.Lerp(panel.anchoredPosition.x, targetX, slideSpeed * Time.deltaTime);
        panel.anchoredPosition = new Vector2(newX, panel.anchoredPosition.y);
    }
}
