using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDamageServerRpc(int damage, ulong attackerID)
    {
        TakeDamage(damage, attackerID);
    }

    // Cette fonction sera appelée par ton script de tir (via un ServerRPC)
    public void TakeDamage(int damage, ulong attackerId)
    {
        // Sécurité : Seul le serveur a le droit de modifier une NetworkVariable 
        // configurée avec NetworkVariableWritePermission.Server
        if (!IsServer) return;

        // On évite de descendre en dessous de 0
        if (currentHealth.Value <= 0) return;

        currentHealth.Value -= damage;
        lastAttackerId = attackerId;

        Debug.Log($"[SERVER] Joueur {OwnerClientId} vie actuelle : {currentHealth.Value}");

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!IsServer) return;

        // --- Logique de Score ---
        if (lastAttackerId != 999 && NetworkManager.Singleton.ConnectedClients.TryGetValue(lastAttackerId, out var killerClient))
        {
            if (killerClient.PlayerObject.TryGetComponent<ScoreSystem>(out var killerScore))
            {
                killerScore.AddTuesServerRpc();
            }
        }

        if (scoreSystem != null) scoreSystem.AddMortsServerRpc();

        // --- Logique de Respawn ---
        // SURTOUT PAS DE DESPAWN ICI sinon l'objet disparaît et la coroutine s'arrête
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        TogglePlayerStateRpc(false);
        yield return new WaitForSeconds(3f);

        if (NetworkPlayerSpawner.Instance != null)
        {
            // 1. On récupère une position aléatoire
            Vector3 nextPos = NetworkPlayerSpawner.Instance.GetSpawnPoint();

            // 2. Désactive TOUT ce qui gère la position
            SetCharacterControllerStateRpc(false);

            // 3. Téléportation physique
            transform.position = nextPos;

            // 4. Petite sécurité : On synchronise immédiatement la position pour Netcode
            // Si tu as un NetworkTransform, cela force la mise à jour
            if (TryGetComponent<NetworkTransform>(out var nt))
            {
                // Selon ta version de Netcode, transform.position suffit 
                // mais certains préfèrent nt.Teleport(nextPos, transform.rotation, transform.localScale);
            }

            SetCharacterControllerStateRpc(true);
        }

        currentHealth.Value = maxHealth;
        lastAttackerId = 999; // <--- RESET de l'attaquant ici !
        TogglePlayerStateRpc(true);
    }

    [Rpc(SendTo.Everyone)]
    private void TogglePlayerStateRpc(bool isAlive)
    {
        // Sécurité pour le Find
        Transform graphics = transform.Find("Graphics");
        if (graphics != null) graphics.gameObject.SetActive(isAlive);

        if (TryGetComponent<CharacterController>(out var cc)) cc.enabled = isAlive;

        if (IsOwner)
        {
            // C'est ici que tu pourrais activer un écran "VOUS ETES MORT"
            // Debug.Log(isAlive ? "De retour au combat !" : "Vous êtes mort...");
        }
    }
    private void UpdateHealthbar(int value)
    {
        // On ne met à jour l'UI que si c'est NOTRE personnage
        // Sinon, on modifierait l'écran du joueur A quand le joueur B est touché.
        if (!IsOwner) return;

        if (life != null) life.fillAmount = (float)value / maxHealth;
        if (healthQuantity != null) healthQuantity.text = $"{value}/{maxHealth}";
    }

    [Rpc(SendTo.Everyone)]
    private void SetCharacterControllerStateRpc(bool state)
    {
        // On désactive le CC pour permettre la téléportation sans conflit physique
        if (TryGetComponent<CharacterController>(out var cc))
        {
            cc.enabled = state;
        }
    }
}