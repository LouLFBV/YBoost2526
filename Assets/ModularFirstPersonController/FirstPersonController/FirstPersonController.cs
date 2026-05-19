using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.Net;
#endif

public class FirstPersonController_Networked : NetworkBehaviour
{
    private Rigidbody rb;

    [SerializeField] private AttackBehaviour attackBehaviour;

    [Header("Death System")]
    // Utilisation de NetworkVariableWritePermission.Server (tu avais écrit Server, c'est parfait)
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private MeshRenderer capsuleRenderer; // Glisse le MeshRenderer de ta capsule ici
    [SerializeField] private Palette playerPalette;

    [Header("UI Elements")]
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private GameObject pauseMenu;

    #region Camera Movement Variables
    [Header("Camera Settings")]
    public Camera playerCamera;

    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    [Header("Crosshair")]
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;

    private float yaw = 0.0f;
    private float pitch = 0.0f;
    [SerializeField] private Image crosshairObject;

    #region Camera Zoom Variables
    [Header("Zoom")]
    public bool enableZoom = true;
    public bool holdToZoom = false;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;

    private bool isZoomed = false;

    [Header("Viseur de sniper")]
    [SerializeField] private GameObject sniperViseur;
    [SerializeField] private GameObject sniperCurseur;
    [SerializeField] private GameObject sniperVisual;
    [SerializeField] private float sniperZoomFOV = 30f;
    [SerializeField] private Vector3 sniperVisualOffset;
    [SerializeField] private Vector3 sniperVisualOriginalPosition;
    #endregion
    #endregion

    #region Movement Variables
    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;

    private bool isWalking = false;

    #region Sprint
    public bool unlimitedSprint = false;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    [Header("Sprint Bar")]
    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private float sprintRemaining;
    private float sprintBarWidth;
    private float sprintBarHeight;
    private bool isSprintCooldown = false;
    #endregion

    #region Jump
    public bool enableJump = true;
    public float jumpPower = 5f;
    private bool isGrounded = false;
    #endregion

    #region Crouch
    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;

    private bool isCrouched = false;
    private Vector3 originalScale;
    #endregion
    #endregion

    #region Head Bob
    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    private Vector3 jointOriginalPos;
    private float timer = 0;
    #endregion

    #region Input 
    [SerializeField] private PlayerInput playerInput;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool sprintHeld;
    private bool crouchPressed;
    private bool zoomPressed;

    private bool inputEnabled = false;
    private float baseWalkSpeed;

    private void EnableInput()
    {
        if (playerInput == null) return;
        inputEnabled = true;

        playerInput.actions.Enable();

        playerInput.actions["Move"].Enable();
        playerInput.actions["Look"].Enable();
        playerInput.actions["Jump"].Enable();
        playerInput.actions["Sprint"].Enable();
        playerInput.actions["Zoom"].Enable();
        playerInput.actions["Crouch"].Enable();

        playerInput.actions["Move"].performed += OnMove;
        playerInput.actions["Move"].canceled += OnMove;
        playerInput.actions["Look"].performed += OnLook;
        playerInput.actions["Look"].canceled += OnLook;
        playerInput.actions["Jump"].performed += OnJump;
        playerInput.actions["Sprint"].performed += OnSprintStart;
        playerInput.actions["Sprint"].canceled += OnSprintStop;
        playerInput.actions["Crouch"].performed += OnCrouch;
        playerInput.actions["Zoom"].performed += OnZoomStart;
        playerInput.actions["Zoom"].canceled += OnZoomStop;
    }

    private void DisableInput()
    {
        if (!inputEnabled || playerInput == null) return;

        playerInput.actions["Move"].performed -= OnMove;
        playerInput.actions["Move"].canceled -= OnMove;
        playerInput.actions["Look"].performed -= OnLook;
        playerInput.actions["Look"].canceled -= OnLook;
        playerInput.actions["Jump"].performed -= OnJump;
        playerInput.actions["Sprint"].performed -= OnSprintStart;
        playerInput.actions["Sprint"].canceled -= OnSprintStop;
        playerInput.actions["Crouch"].performed -= OnCrouch;
        playerInput.actions["Zoom"].performed -= OnZoomStart;
        playerInput.actions["Zoom"].canceled -= OnZoomStop;

        playerInput.actions.Disable();

        inputEnabled = false;
    }

    private void OnDisable()
    {
        DisableInput();
    }

    private void OnSprintStop(InputAction.CallbackContext context) { sprintHeld = false; }
    private void OnSprintStart(InputAction.CallbackContext context) { sprintHeld = true; }
    private void OnMove(InputAction.CallbackContext ctx) { moveInput = ctx.ReadValue<Vector2>(); }
    private void OnLook(InputAction.CallbackContext ctx) { lookInput = ctx.ReadValue<Vector2>(); }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!enableJump || !isGrounded || isDead.Value) return;
        Jump();
    }

    private void OnZoomStart(InputAction.CallbackContext ctx)
    {
        if (!enableZoom || isSprinting || isDead.Value) return;
        if (holdToZoom) isZoomed = true;
        else isZoomed = !isZoomed;
    }

    private void OnZoomStop(InputAction.CallbackContext ctx) { if (holdToZoom) isZoomed = false; }

    private void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (!this || !isActiveAndEnabled || !IsOwner || !enableCrouch || isDead.Value) return;

        if (holdToCrouch)
        {
            isCrouched = ctx.performed;
            ApplyCrouchState();
            holdToCrouch = false;
        }
        else
        {
            holdToCrouch = true;
            ToggleCrouch();
        }
    }
    #endregion 

    private void ApplyCrouchState()
    {
        if (!this || !isActiveAndEnabled) return;

        if (isCrouched)
        {
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed = baseWalkSpeed * speedReduction;
        }
        else
        {
            transform.localScale = originalScale;
            walkSpeed = baseWalkSpeed;
        }
    }

    private void Awake()
    {
        baseWalkSpeed = walkSpeed;
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;

        if (joint != null) jointOriginalPos = joint.localPosition;
        if (!unlimitedSprint) sprintRemaining = sprintDuration;

        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);
    }

    //  LA METHODE CRUCIALE MODIFIÉE
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        rb = GetComponent<Rigidbody>();

        // 🟢 S'ABONNER À LA MORT (Pour TOUT LE MONDE)
        isDead.OnValueChanged += OnDeathStateChanged;

        // Appliquer l'état initial (au cas où il spawn mort)
        UpdateCapsuleColor(isDead.Value);

        if (IsOwner)
        {
            if (playerInput != null) playerInput.enabled = true;
            if (playerHUD != null) playerHUD.SetActive(true);

            EnableInput();

            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                playerCamera.fieldOfView = fov;
            }

            if (rb != null) rb.isKinematic = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (playerInput != null) playerInput.enabled = false;
            if (playerHUD != null) playerHUD.SetActive(false);
            if (pauseMenu != null) pauseMenu.SetActive(false);

            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            if (rb != null) rb.isKinematic = true;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        // 🔴 Se désabonner proprement pour éviter les fuites de mémoire
        isDead.OnValueChanged -= OnDeathStateChanged;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (crosshair && crosshairObject != null)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
            crosshairObject.gameObject.SetActive(true);
        }
        else if (crosshairObject != null)
        {
            crosshairObject.gameObject.SetActive(false);
        }

        #region Sprint Bar
        sprintBarCG = sprintBarCG ?? GetComponentInChildren<CanvasGroup>();

        if (useSprintBar && sprintBarBG != null && sprintBar != null)
        {
            sprintBarBG.gameObject.SetActive(true);
            sprintBar.gameObject.SetActive(true);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            sprintBarWidth = screenWidth * sprintBarWidthPercent;
            sprintBarHeight = screenHeight * sprintBarHeightPercent;

            sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
            sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 2, sprintBarHeight - 2, 0f);

            if (hideBarWhenFull && sprintBarCG != null)
            {
                sprintBarCG.alpha = 0;
            }
        }
        else
        {
            if (sprintBarBG != null) sprintBarBG.gameObject.SetActive(false);
            if (sprintBar != null) sprintBar.gameObject.SetActive(false);
        }
        #endregion
    }

    private void Update()
    {
        if (isDead.Value) return; // Bloque l'update si mort
        if (!IsOwner) return;

        HandleCamera();
        CheckGround();

        if (enableHeadBob) HeadBob();
    }

    private void HandleCamera()
    {
        if (cameraCanMove && playerCamera != null)
        {
            yaw += lookInput.x * mouseSensitivity;
            if (!invertCamera) pitch -= lookInput.y * mouseSensitivity;
            else pitch += lookInput.y * mouseSensitivity;

            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, yaw, 0);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        if (!enableZoom || playerCamera == null) return;

        var weapon = attackBehaviour?.weaponUsed;
        bool isSniper = weapon != null && weapon.weaponData.weaponFamilyType == WeaponFamilyType.Sniper;
        float targetFov = fov;

        if (isZoomed && weapon != null)
        {
            targetFov = isSniper ? sniperZoomFOV : zoomFOV;
        }
        else
        {
            isZoomed = false;
        }

        bool showSniperUI = isSniper && isZoomed;
        if (sniperCurseur != null) sniperCurseur.SetActive(showSniperUI);
        if (sniperViseur != null) sniperViseur.SetActive(showSniperUI);

        if (sniperVisual != null)
        {
            sniperVisual.transform.localPosition = showSniperUI ? sniperVisualOffset : sniperVisualOriginalPosition;
        }

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, zoomStepTime * Time.deltaTime);
    }

    public void ResetZoom() { isZoomed = false; }

    void FixedUpdate()
    {
        if (isDead.Value)
        {
            if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }
        if (!IsOwner) return;

        #region Movement
        if (playerCanMove)
        {
            Vector3 targetVelocity = new Vector3(moveInput.x, 0, moveInput.y);

            if ((targetVelocity.x != 0f || targetVelocity.z != 0f) && isGrounded)
                isWalking = true;
            else
                isWalking = false;

            bool canSprint = sprintHeld && moveInput.sqrMagnitude > 0.01f;

            if (canSprint)
            {
                isSprinting = true;
                targetVelocity = transform.TransformDirection(targetVelocity) * sprintSpeed;

                Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
                Vector3 velocityChange = (targetVelocity - velocity);
                velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
                velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
                velocityChange.y = 0f;

                if (Mathf.Abs(velocityChange.x) > 0.001f || Mathf.Abs(velocityChange.z) > 0.001f)
                {
                    if (isCrouched) Crouch();
                    if (hideBarWhenFull && !unlimitedSprint && sprintBarCG != null)
                    {
                        sprintBarCG.alpha = Mathf.Clamp01(sprintBarCG.alpha + 5f * Time.deltaTime);
                    }
                }

                if (rb != null) rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
            else
            {
                isSprinting = false;
                sprintHeld = false;
                if (hideBarWhenFull && sprintRemaining == sprintDuration && sprintBarCG != null)
                {
                    sprintBarCG.alpha = Mathf.Clamp01(sprintBarCG.alpha - 3f * Time.deltaTime);
                }

                targetVelocity = transform.TransformDirection(targetVelocity) * walkSpeed;

                Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
                Vector3 velocityChange = (targetVelocity - velocity);
                velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
                velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
                velocityChange.y = 0f;

                if (rb != null) rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
        }
        #endregion
    }

    private void CheckGround()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y - (transform.localScale.y * .5f), transform.position.z);
        Vector3 direction = transform.TransformDirection(Vector3.down);
        float distance = .75f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
        {
            Debug.DrawRay(origin, direction * distance, Color.red);
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void Jump()
    {
        if (isGrounded && rb != null)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            isGrounded = false;
        }
        if (isCrouched && !holdToCrouch) Crouch();
    }

    private void ToggleCrouch() { Crouch(); }

    private void Crouch()
    {
        if (!isCrouched)
        {
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;
            isCrouched = true;
        }
        else
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
            walkSpeed = Mathf.Abs(walkSpeed / speedReduction);
            isCrouched = false;
        }
    }

    private void HeadBob()
    {
        if (isWalking && isGrounded)
        {
            if (isSprinting) timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            else if (isCrouched) timer += Time.deltaTime * (bobSpeed * speedReduction);
            else timer += Time.deltaTime * bobSpeed;

            if (joint != null)
                joint.localPosition = new Vector3(jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x, jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y, jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z);
        }
        else
        {
            timer = 0;
            if (joint != null)
                joint.localPosition = new Vector3(Mathf.Lerp(joint.localPosition.x, jointOriginalPos.x, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.y, jointOriginalPos.y, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.z, jointOriginalPos.z, Time.deltaTime * bobSpeed));
        }
    }

    //  GÉRÉ PAR NETCODE AUTOMATIQUEMENT ICI
    private void OnDeathStateChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"[NETCODE] Changement d'état de vie détecté pour {gameObject.name}. Mort = {newValue}");

        // Tout le monde met à jour la couleur (Blanc si vivant, Rouge si mort)
        UpdateCapsuleColor(newValue);

        if (newValue) 
        {
            DisableInput();

            if (IsOwner)
            {
                if (attackBehaviour != null) attackBehaviour.enabled = false;

                if (playerPalette != null)
                {
                    playerPalette.RemoveWeaponInPalette(WeaponType.Main);
                    playerPalette.RemoveWeaponInPalette(WeaponType.Secondary);
                    playerPalette.RemoveWeaponInPalette(WeaponType.Melee);
                    playerPalette.RemoveWeaponInPalette(WeaponType.Projectile);
                    playerPalette.enabled = false;
                }
            }
        }
        else 
        {
            if (IsOwner)
            {
                EnableInput(); // On redonne les mouvements

                if (attackBehaviour != null) attackBehaviour.enabled = true; // On réactive l'attaque
                if (playerPalette != null) playerPalette.enabled = true;     // On réactive la palette d'armes

                ResetZoom(); // Sécurité pour enlever le zoom du sniper si on est mort en visant
                Debug.Log("[RESPAWN] Armes et mouvements réactivés pour le propriétaire !");
            }
        }
    }

    private void UpdateCapsuleColor(bool dead)
    {
        if (capsuleRenderer != null)
        {
            // On force la couleur directement via le shader au cas où
            capsuleRenderer.material.SetColor("_BaseColor", dead ? Color.red : Color.white); // Pour URP
            capsuleRenderer.material.color = dead ? Color.red : Color.white;                 // Pour Built-in

            Debug.Log($"[COULEUR] Capsule de {gameObject.name} changée en : {(dead ? "ROUGE" : "BLANC")}");
        }
        else
        {
            Debug.LogError($"[ERREUR] Le CapsuleRenderer est manquant sur {gameObject.name} !");
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDieServerRpc()
    {
        // Ce code s'exécutera UNIQUEMENT sur le serveur
        Debug.Log($"[SERVER RPC] Requête de mort reçue sur le serveur pour {gameObject.name}.");
        Die();
    }

    public void Die()
    {
        if (!IsServer) return;

        isDead.Value = true;
        Debug.Log($"[SERVER] Die() exécuté. isDead est maintenant passé à TRUE pour {gameObject.name}.");
    }

    public void RespawnPlayer(Vector3 spawnPosition)
    {
        if (!IsServer) return;

        transform.position = spawnPosition;

        // Le simple fait de passer à false va déclencher le bloc "else" 
        // de OnDeathStateChanged chez le client concerné. Pas besoin de RPC manuel !
        isDead.Value = false;
    }

}