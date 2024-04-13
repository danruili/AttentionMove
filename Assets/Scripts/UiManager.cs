using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    public static UiManager instance;

    public bool showPath = false;
    public bool showAgentForce = true;
    public bool showText = false;
    public bool showWall= false;

    public bool showAttentionScore = false;
    public static bool displayAttention = false;

    private void Awake()
    {
        instance = this;
        displayAttention = showAttentionScore;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    public void ShowText(string text)
    {
        if (showText) timeText.text = text;
    }
}
