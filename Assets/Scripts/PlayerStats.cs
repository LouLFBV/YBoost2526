using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : NetworkBehaviour
{
    [Header("Player Health")]
    [SerializeField] private int maxHealth = 100;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Components")]
    [SerializeField] private Image life;
    [SerializeField] private TextMeshProUGUI healthQuantity;
    public ScoreSystem scoreSystem;

    // On stocke l'ID de l'attaquant plutôt que le GameObject
    private ulong lastAttackerId;

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += (oldValue, newValue) => {
            UpdateHealthbar(newValue);
        };
        UpdateHealthbar(currentHealth.Value);
    }

    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDamageServerRpc(int damage, ulong attackerID)
    {
        TakeDamage(damage, attackerID); // Le serveur s'exécute
    }

    // Cette fonction sera appelée par ton script de tir (via un ServerRPC)
    public void TakeDamage(int damage, ulong attackerId)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;
        lastAttackerId = attackerId;

        Debug.Log($"[SERVER] Joueur {OwnerClientId} touché par {attackerId}. Vie : {currentHealth.Value}");

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!IsServer) return;

        // On cherche le tueur dans la liste des clients connectés pour lui donner le point
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(lastAttackerId, out var killerClient))
        {
            if (killerClient.PlayerObject.TryGetComponent<ScoreSystem>(out var killerScore))
            {
                killerScore.AddTuesServerRpc(); // Appel d'un RPC pour le score
            }
        }

        scoreSystem.AddMortsServerRpc();

        Debug.Log($"[SERVER] Joueur {OwnerClientId} est mort.");

        // On Despawn l'objet (il disparaît chez tout le monde)
        GetComponent<NetworkObject>().Despawn();
    }

    private void UpdateHealthbar(int value)
    {
        if (life != null) life.fillAmount = (float)value / maxHealth;
        if (healthQuantity != null) healthQuantity.text = $"{value}/{maxHealth}";
    }
}