using TMPro;
using UnityEngine;

public class LeaderboardEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI deathsText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void Setup(string rank, string name, string kills, string deaths, string score)
    {
        rankText.text = rank;
        nameText.text = name;
        killsText.text = kills;
        deathsText.text = deaths;
        scoreText.text = score;
    }
}