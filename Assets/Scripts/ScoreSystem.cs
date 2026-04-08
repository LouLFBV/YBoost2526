using TMPro;
using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
    [Header("Paramètres du Score")]
    public int Score { get => score; private set => score = value; }
    [SerializeField] private int score = 0;
    [SerializeField] private TextMeshProUGUI scoreText;

    public int Tues { get => tues; private set => tues = value; }
    [SerializeField] private int tues = 0;

    public int Morts { get => morts; private set => morts = value; }
    [SerializeField] private int morts = 0;

    private void Start()
    {
        UpdateText();
    }
    public void AddTues()
    {
        tues += 1;
        score += 25;
        UpdateText();
    }
    public void AddMorts()
    {
        morts += 1;
        score -= 15;
        UpdateText();
    }

    private void UpdateText()
    {
        if (scoreText != null) 
        scoreText.text = $"Tuées/Morts/Score \n{tues}/{morts}/{score}";
    }
}

