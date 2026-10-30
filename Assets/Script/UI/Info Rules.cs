using UnityEngine;

public class InfoRules : MonoBehaviour
{
    [SerializeField] GameObject infoPanel;

    void Start()
    {
        infoPanel.gameObject.SetActive(false);
    }

    public void DisplayInfo()
    {
        if (infoPanel.activeSelf)
        {
            infoPanel.SetActive(false);
        }
        else
        {
            infoPanel.SetActive(true);
        }
    }
}
