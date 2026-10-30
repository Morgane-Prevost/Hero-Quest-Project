using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackToMenuButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string menuSceneName = "MainMenu";

    private void Start()
    {
        button.onClick.AddListener(GoToMenu);
    }

    private void GoToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}