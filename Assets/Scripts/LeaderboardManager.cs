using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel; // Le panel qui contient tout
    [SerializeField] private Transform entryContainer;    // L'objet avec le Vertical Layout Group
    [SerializeField] private GameObject entryPrefab;      // Le prefab d'une ligne de score

    // Cette fonction est appelée quand la partie se termine
    public void ShowLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        RefreshLeaderboard();
    }

    private void RefreshLeaderboard()
    {
        // 1. On nettoie les anciennes lignes
        foreach (Transform child in entryContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. On récupère tous les ScoreSystems de tous les joueurs connectés
        // NetworkManager.Singleton.ConnectedClients donne accès à tous les joueurs
        List<ScoreSystem> allScores = new List<ScoreSystem>();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<ScoreSystem>(out var ss))
            {
                allScores.Add(ss);
            }
        }

        // 3. On trie par score (du plus grand au plus petit)
        var sortedScores = allScores.OrderByDescending(s => s.score.Value).ToList();

        // 4. On crée les lignes dans l'UI
        for (int i = 0; i < sortedScores.Count; i++)
        {
            GameObject entry = Instantiate(entryPrefab, entryContainer);
            LeaderboardEntryUI entryUI = entry.GetComponent<LeaderboardEntryUI>();

            // On remplit les données
            // On peut utiliser l'OwnerClientId comme nom par défaut
            string playerName = $"Joueur {sortedScores[i].OwnerClientId}";
            if (sortedScores[i].IsLocalPlayer) playerName += " (Toi)";

            entryUI.Setup(
                (i + 1).ToString(),
                playerName,
                sortedScores[i].tues.Value.ToString(),
                sortedScores[i].morts.Value.ToString(),
                sortedScores[i].score.Value.ToString()
            );
        }
    }
}