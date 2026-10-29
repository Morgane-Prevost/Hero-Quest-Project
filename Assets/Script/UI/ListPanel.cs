using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ListPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button arrowButton;
    [SerializeField] private TMP_Text arrowText;
    [SerializeField] private FichePanel fichePanel;
    [SerializeField] private QuestPanel questPanel;
    [SerializeField] private InventoryPanel inventoryPanel;

    [Header("Lists")]
    [SerializeField] private Transform monsterGrid;
    [SerializeField] private Transform heroGrid;
    [SerializeField] private Button characterButtonPrefab;

    [Header("Data")]
    [SerializeField] private MonsterData[] monsters;
    [SerializeField] private HeroData[] heroes;

    [Header("Animation")]
    [SerializeField] private float slideSpeed = 8f;

    private bool isOpen;
    private float panelWidth;
    private Button selectedButton;

    private void Awake()
    {
        panelWidth = panel.rect.width;
        panel.anchoredPosition = new Vector2(-panelWidth, panel.anchoredPosition.y);
        arrowText.text = ">>";

        arrowButton.onClick.AddListener(OnToggle);

        CreateMonsterButtons();
        CreateHeroButtons();
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

    private void CreateMonsterButtons()
    {
        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterData data = monsters[i];

            Button button = Instantiate(characterButtonPrefab, monsterGrid);
            button.GetComponentInChildren<TMP_Text>().text = data.monsterName;

            button.onClick.AddListener(() => OnMonsterClicked(data, button));
        }
    }

    private void CreateHeroButtons()
    {
        for (int i = 0; i < heroes.Length; i++)
        {
            HeroData data = heroes[i];

            Button button = Instantiate(characterButtonPrefab, heroGrid);
            button.GetComponentInChildren<TMP_Text>().text = data.name;

            button.onClick.AddListener(() => OnHeroClicked(data, button));
        }
    }

    private void OnMonsterClicked(MonsterData data, Button button)
    {
        if (SelectButton(button))
        {
            fichePanel.ShowMonster(data);
        }
    }

    private void OnHeroClicked(HeroData data, Button button)
    {
        if (SelectButton(button))
        {
            fichePanel.ShowHero(data);
        }
    }

    private bool SelectButton(Button button)
    {
        if (selectedButton == button)
        {
            ClearSelection();
            return false;
        }

        if (selectedButton != null)
        {
            SetUnderline(selectedButton, false);
        }

        selectedButton = button;
        SetUnderline(selectedButton, true);

        questPanel.Close();
        inventoryPanel.Close();
        return true;
    }

    public void ClearSelection()
    {
        if (selectedButton != null)
        {
            SetUnderline(selectedButton, false);
            selectedButton = null;
        }

        fichePanel.Hide();
    }

    private void SetUnderline(Button button, bool underline)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();

        if (underline)
        {
            text.fontStyle = FontStyles.Underline;
        }
        else
        {
            text.fontStyle = FontStyles.Normal;
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
