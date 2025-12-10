using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;

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

    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public KeyCode sprintKey = KeyCode.LeftShift; // kept for backwards compat if needed
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
    private float sprintCooldownReset;

    #endregion

    #region Jump

    public bool enableJump = true;
    public KeyCode jumpKey = KeyCode.Space; // kept for backwards compat if needed
    public float jumpPower = 5f;

    // Internal Variables
    private bool isGrounded = false;

    #endregion

    #region Crouch

    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public KeyCode crouchKey = KeyCode.LeftControl; // kept for backwards compat if needed
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

    private PlayerInput playerInput;

    // Player Input System --- Actions
    [SerializeField] private InputActionReference moveAction;       // Vector2 (WASD / joystick)
    [SerializeField] private InputActionReference lookAction;       // Vector2 (Mouse delta / stick)
    [SerializeField] private InputActionReference jumpAction;       // Button
    [SerializeField] private InputActionReference sprintAction;     // Button (hold)
    [SerializeField] private InputActionReference crouchAction;     // Button (hold or toggle)
    [SerializeField] private InputActionReference zoomAction;       // Button (hold or toggle)

    // runtime input values
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool sprintHeld;
    private bool crouchPressed;
    private bool zoomPressed;

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (lookAction != null) lookAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
        if (sprintAction != null) sprintAction.action.Enable();
        if (crouchAction != null) crouchAction.action.Enable();
        if (zoomAction != null) zoomAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (lookAction != null) lookAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
        if (sprintAction != null) sprintAction.action.Disable();
        if (crouchAction != null) crouchAction.action.Disable();
        if (zoomAction != null) zoomAction.action.Disable();
    }

    public override void OnNetworkSpawn()
    {
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
            sprintCooldownReset = sprintCooldown;
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
        if (!IsOwner || !Application.isFocused) return;

        // Collecte des inputs du Player Input System (après IsOwner check)
        if (moveAction != null) moveInput = moveAction.action.ReadValue<Vector2>();
        else moveInput = Vector2.zero;

        if (lookAction != null) lookInput = lookAction.action.ReadValue<Vector2>();
        else lookInput = Vector2.zero;

        if (jumpAction != null) jumpPressed = jumpAction.action.WasPressedThisFrame();
        else jumpPressed = false;

        if (sprintAction != null) sprintHeld = sprintAction.action.IsPressed();
        else sprintHeld = false;

        if (crouchAction != null) crouchPressed = crouchAction.action.WasPressedThisFrame();
        else crouchPressed = false;

        if (zoomAction != null) zoomPressed = zoomAction.action.IsPressed();
        else zoomPressed = false;

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

        #region Camera Zoom

        if (enableZoom && playerCamera != null)
        {
            // Toggle vs Hold zoom handling
            if (!holdToZoom && zoomPressed && !isSprinting)
            {
                isZoomed = !isZoomed;
            }
            else if (holdToZoom && !isSprinting)
            {
                isZoomed = zoomPressed;
            }

            // Lerps camera.fieldOfView to allow for a smooth transition
            if (isZoomed)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, zoomStepTime * Time.deltaTime);
            }
            else if (!isZoomed && !isSprinting)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, zoomStepTime * Time.deltaTime);
            }
        }

        #endregion
        #endregion

        #region Sprint

        if (enableSprint)
        {
            // Determine sprint state (activate if conditions met and player is moving)
            bool canSprint = enableSprint && sprintHeld && sprintRemaining > 0f && !isSprintCooldown && moveInput.sqrMagnitude > 0.01f;

            if (canSprint)
            {
                isSprinting = true;
                isZoomed = false; // block zoom while sprinting
                // FOV change handled in movement code (FixedUpdate via isSprinting)
            }
            else
            {
                isSprinting = false;
            }

            if (isSprinting)
            {
                // Drain sprint remaining while sprinting
                if (!unlimitedSprint)
                {
                    sprintRemaining -= 1 * Time.deltaTime;
                    if (sprintRemaining <= 0f)
                    {
                        isSprinting = false;
                        isSprintCooldown = true;
                    }
                }
            }
            else
            {
                // Regain sprint while not sprinting
                sprintRemaining = Mathf.Clamp(sprintRemaining + 1 * Time.deltaTime, 0, sprintDuration);
            }

            // Handles sprint cooldown 
            if (isSprintCooldown)
            {
                sprintCooldown -= 1 * Time.deltaTime;
                if (sprintCooldown <= 0f)
                {
                    isSprintCooldown = false;
                    sprintCooldown = sprintCooldownReset;
                }
            }

            // Handles sprintBar 
            if (useSprintBar && !unlimitedSprint && sprintBar != null)
            {
                float sprintRemainingPercent = sprintRemaining / sprintDuration;
                sprintBar.transform.localScale = new Vector3(sprintRemainingPercent, 1f, 1f);

                if (hideBarWhenFull && sprintBarCG != null)
                {
                    // fade in/out
                    sprintBarCG.alpha = Mathf.MoveTowards(sprintBarCG.alpha, sprintRemainingPercent >= 1f ? 0f : 1f, 5f * Time.deltaTime);
                }
            }
        }

        #endregion

        #region Jump

        if (enableJump && jumpPressed && isGrounded)
        {
            Jump();
        }

        #endregion

        #region Crouch

        if (enableCrouch)
        {
            if (!holdToCrouch && crouchPressed)
            {
                // toggle crouch on press when not hold-to-crouch
                ToggleCrouch();
            }
            else if (holdToCrouch)
            {
                // for hold-to-crouch, Crouch() will check isCrouched state and toggle scale
                if (crouchPressed)
                {
                    Crouch();
                }
            }
        }

        #endregion

        CheckGround();

        if (enableHeadBob)
        {
            HeadBob();
        }
    }

    void FixedUpdate()
    {
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

            bool canSprint = enableSprint && sprintHeld && sprintRemaining > 0f && !isSprintCooldown && moveInput.sqrMagnitude > 0.01f;

            if (canSprint)
            {
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

            // Adjust camera FOV smoothly for sprint state
            if (playerCamera != null)
            {
                float targetFov = isSprinting ? sprintFOV : (isZoomed ? zoomFOV : fov);
                float step = isSprinting ? sprintFOVStepTime : zoomStepTime;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, step * Time.deltaTime);
            }
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
        isCrouched = !isCrouched;
        Crouch();
    }

    private void Crouch()
    {
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
