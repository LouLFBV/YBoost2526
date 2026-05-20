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

        if (TryGetComponent<FirstPersonController_Networked>(out var controller))
        {
            controller.Die();
        }

        bool isSuicide = (lastAttackerId == OwnerClientId);

        if (!isSuicide && lastAttackerId != 999 && NetworkManager.Singleton.ConnectedClients.TryGetValue(lastAttackerId, out var killerClient))
        {
            if (killerClient.PlayerObject.TryGetComponent<ScoreSystem>(out var killerScore))
            {
                // On donne le point uniquement si c'est un ENNEMI qui nous a tué
                killerScore.AddTuesServerRpc();
            }
        }

        if (scoreSystem != null) scoreSystem.AddMortsServerRpc();

        // --- Logique de Respawn ---
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        TogglePlayerStateRpc(false);
        yield return new WaitForSeconds(3f);

        Vector3 nextPos = Vector3.zero;

        if (NetworkPlayerSpawner.Instance != null)
        {
            nextPos = NetworkPlayerSpawner.Instance.GetSpawnPoint();
            TeleportPlayerRpc(nextPos);
        }

        currentHealth.Value = maxHealth;
        lastAttackerId = 999;

        if (TryGetComponent<FirstPersonController_Networked>(out var controller))
        {
            controller.RespawnPlayer(nextPos);
        }

        TogglePlayerStateRpc(true);
    }

    [Rpc(SendTo.Everyone)]
    private void TeleportPlayerRpc(Vector3 targetPos)
    {
        // 1. Désactiver la physique pour éviter les conflits
        if (TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;

        // 2. Appliquer la position
        transform.position = targetPos;

        // 3. Si on est l'Owner (celui qui a l'autorité) OU le Serveur, on valide la position
        if (TryGetComponent<NetworkTransform>(out var nt))
        {
            // On ne téléporte que si on a l'autorité (l'Owner) ou si on est le Serveur (si pas d'Owner)
            if (IsOwner || IsServer)
            {
                // Note: On n'utilise plus .Teleport() ici car transform.position 
                // suffit quand c'est fait du côté autoritaire
            }
        }

        // 4. Réactiver la physique
        if (TryGetComponent<CharacterController>(out var cc2)) cc2.enabled = true;
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