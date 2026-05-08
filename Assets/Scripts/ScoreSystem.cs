using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreSystem : NetworkBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI scoreText;

    // On synchronise les 3 valeurs
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> tues = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> morts = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // On s'abonne aux changements pour mettre à jour l'UI localement
        score.OnValueChanged += (oldVal, newVal) => UpdateText();
        tues.OnValueChanged += (oldVal, newVal) => UpdateText();
        morts.OnValueChanged += (oldVal, newVal) => UpdateText();

        // Premier affichage
        UpdateText();
    }

    // --- LES ACTIONS (Appelées par le serveur) ---

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone )]
    public void AddTuesServerRpc()
    {
        tues.Value += 1;
        score.Value += 25;
        // Pas besoin d'appeler UpdateText ici, OnValueChanged s'en occupe pour tout le monde
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddMortsServerRpc()
    {
        morts.Value += 1;
        score.Value -= 15;
    }

    // --- L'AFFICHAGE (S'exécute chez tout le monde) ---

    private void UpdateText()
    {
        // On n'affiche le score QUE si c'est notre propre personnage
        // Sinon, on verrait le score des autres sur notre propre écran
        if (!IsOwner) return;

        if (scoreText != null)
            scoreText.text = $"Tuées/Morts/Score \n{tues.Value}/{morts.Value}/{score.Value}";
    }
}