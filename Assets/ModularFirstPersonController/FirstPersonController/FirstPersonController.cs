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

    #region Camera Movement Variables

    public Camera playerCamera;

    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    // Crosshair
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    [SerializeField] private Image crosshairObject;

    #region Camera Zoom Variables

    public bool enableZoom = true;
    public bool holdToZoom = false;
    public KeyCode zoomKey = KeyCode.Mouse1; // kept for backwards compat if needed
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;

    // Internal Variables
    private bool isZoomed = false;

    #endregion
    #endregion

    #region Movement Variables

    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;

    // Internal Variables
    private bool isWalking = false;

    #region Sprint

    public bool unlimitedSprint = false;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    // Sprint Bar
    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    // Internal Variables
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

    // Internal Variables
    private bool isGrounded = false;

    #endregion

    #region Crouch

    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;

    // Internal Variables
    private bool isCrouched = false;
    private Vector3 originalScale;

    #endregion
    #endregion

    #region Head Bob

    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    // Internal Variables
    private Vector3 jointOriginalPos;
    private float timer = 0;

    #endregion

    [SerializeField] private PlayerInput playerInput;


    // runtime input values
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool sprintHeld;
    private bool crouchPressed;
    private bool zoomPressed;

    private bool inputEnabled = false;
    private float baseWalkSpeed;


    public override void OnNetworkDespawn()
    {
        if (!IsOwner || playerInput == null) return;

        DisableInput();
    }

    private void EnableInput()
    {

        inputEnabled = true;

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

        inputEnabled = false;
    }


    private void OnDisable()
    {
        DisableInput();
    }

    private void OnSprintStop(InputAction.CallbackContext context)
    {
        sprintHeld = false;
    }

    private void OnSprintStart(InputAction.CallbackContext context)
    {
        Debug.Log("Sprint input received.");
        sprintHeld = true;
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!enableJump || !isGrounded) return;
        Jump();
    }

    private void OnZoomStart(InputAction.CallbackContext ctx)
    {
        if (!enableZoom || isSprinting) return;

        if (holdToZoom)
            isZoomed = true;
        else
            isZoomed = !isZoomed;
    }

    private void OnZoomStop(InputAction.CallbackContext ctx)
    {
        if (holdToZoom)
            isZoomed = false;
    }

    private void OnCrouch(InputAction.CallbackContext ctx)
    {
        Debug.Log("Crouch input received. holdToCrouch = " + holdToCrouch);
        if (!this || !isActiveAndEnabled) return;
        if (!IsOwner) return;
        if (!enableCrouch) return;

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




    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn | IsOwner = " + IsOwner);
        if (!IsOwner) return;

        EnableInput();
        if (rb == null) rb = GetComponent<Rigidbody>();

        if (IsOwner)
        {
            if (playerCamera != null)
                playerCamera.enabled = true;

            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }
        else
        {
            if (playerCamera != null)
                playerCamera.enabled = false;

            if (playerCamera != null)
            {
                var audio = playerCamera.GetComponent<AudioListener>();
                if (audio != null) audio.enabled = false;
            }

            // disable local physics for non-owners
            rb.isKinematic = true;
        }
    }

    private void Awake()
    {
        baseWalkSpeed = walkSpeed;

        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        // prefer inspector-assigned crosshair; otherwise try to find
        if (crosshairObject == null)
        {
            // try to find an Image named "Crosshair" in children (if present)
            var found = GetComponentInChildren<Image>();
            if (found != null && found.name.ToLower().Contains("crosshair"))
                crosshairObject = found;
            // else keep null (we handle null later)
        }

        // Safeguard references
        if (playerCamera != null)
            playerCamera.fieldOfView = fov;

        originalScale = transform.localScale;
        if (joint != null) jointOriginalPos = joint.localPosition;

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
        }
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
        if (!IsOwner) return;

        HandleCamera();
        CheckGround();

        if (enableHeadBob)
            HeadBob();
    }


    private void HandleCamera()
    {
        #region Camera

        // Control camera movement
        if (cameraCanMove && playerCamera != null)
        {
            yaw += lookInput.x * mouseSensitivity;

            if (!invertCamera)
                pitch -= lookInput.y * mouseSensitivity;
            else
                pitch += lookInput.y * mouseSensitivity;

            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, yaw, 0);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        #endregion
        #region Camera Zoom

        if (enableZoom && playerCamera != null)
        {
            float targetFov = isZoomed ? zoomFOV : fov;
            playerCamera.fieldOfView = Mathf.Lerp(
                playerCamera.fieldOfView,
                targetFov,
                zoomStepTime * Time.deltaTime
            );

        }

        #endregion

    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        #region Movement

        if (playerCanMove)
        {
            Vector3 targetVelocity = new Vector3(moveInput.x, 0, moveInput.y);

            // Checks if player is walking and isGrounded
            // Will allow head bob (only when grounded)
            if ((targetVelocity.x != 0f || targetVelocity.z != 0f) && isGrounded)
                isWalking = true;
            else
                isWalking = false;

            bool canSprint = sprintHeld && moveInput.sqrMagnitude > 0.01f;

            if (canSprint)
            {
                Debug.Log("Sprinting");
                isSprinting = true;
                // sprint movement
                targetVelocity = transform.TransformDirection(targetVelocity) * sprintSpeed;

                // Apply a force that attempts to reach our target velocity
                Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
                Vector3 velocityChange = (targetVelocity - velocity);
                velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
                velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
                velocityChange.y = 0f;

                // Player is only moving when velocity change != 0, makes sure FOV change only happens during movement
                if (Mathf.Abs(velocityChange.x) > 0.001f || Mathf.Abs(velocityChange.z) > 0.001f)
                {
                    if (isCrouched)
                    {
                        // uncrouch before sprinting if necessary
                        Crouch();
                    }

                    if (hideBarWhenFull && !unlimitedSprint && sprintBarCG != null)
                    {
                        sprintBarCG.alpha = Mathf.Clamp01(sprintBarCG.alpha + 5f * Time.deltaTime);
                    }
                }

                if (rb != null)
                    rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
            else
            {
                // walking movement
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

                if (rb != null)
                    rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }

            //// Adjust camera FOV smoothly for sprint state
            //if (playerCamera != null)
            //{
            //    float targetFov = isSprinting ? sprintFOV : (isZoomed ? zoomFOV : fov);
            //    float step = isSprinting ? sprintFOVStepTime : zoomStepTime;
            //    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, step * Time.deltaTime);
            //}
        }

        #endregion
    }

    // Sets isGrounded based on a raycast sent straight down from the player object
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
        // Adds force to the player rigidbody to jump
        if (isGrounded && rb != null)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            isGrounded = false;
        }

        // When crouched and using toggle system, will uncrouch for a jump
        if (isCrouched && !holdToCrouch)
        {
            Crouch();
        }
    }

    private void ToggleCrouch()
    {
        // toggle crouch state and call Crouch() to apply
        Crouch();
    }

    private void Crouch()
    {
        Debug.Log("Crouch toggled. isCrouched = " + isCrouched);
        // Stands player up to full height
        // Brings walkSpeed back up to original speed
        if (!isCrouched)
        {
            // if we're not flagged crouched, ensure we crouch
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;
            isCrouched = true;
        }
        else
        {
            Debug.Log("Standing up from crouch.");
            // stand up
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
            walkSpeed = Mathf.Abs(walkSpeed / speedReduction); // avoid compounding reductions
            isCrouched = false;
        }
    }

    private void HeadBob()
    {
        if (isWalking && isGrounded)
        {
            // Calculates HeadBob speed during sprint
            if (isSprinting)
            {
                timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            }
            // Calculates HeadBob speed during crouched movement
            else if (isCrouched)
            {
                timer += Time.deltaTime * (bobSpeed * speedReduction);
            }
            // Calculates HeadBob speed during walking
            else
            {
                timer += Time.deltaTime * bobSpeed;
            }
            // Applies HeadBob movement
            if (joint != null)
                joint.localPosition = new Vector3(jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x, jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y, jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z);
        }
        else
        {
            // Resets when player stops moving
            timer = 0;
            if (joint != null)
                joint.localPosition = new Vector3(Mathf.Lerp(joint.localPosition.x, jointOriginalPos.x, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.y, jointOriginalPos.y, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.z, jointOriginalPos.z, Time.deltaTime * bobSpeed));
        }
    }
}
