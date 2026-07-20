using UnityEngine.Animations.Rigging;
using System;
using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Gun Aim Collider Fix")]
    public float gunAimColliderYOffset = 0.90f;
    public float aimColliderLerpSpeed = 12f;

    [Header("Weapon")]
    public bool isGunEquipped = false;

    [Header("Grappling")]
    public GrapplingGun grapplingGun;

    [Header("Aiming Rigs")]
    public Rig bowAimingRig;
    public Rig gunAimingRig;
    public float rigLerpSpeed = 10f;

    private bool bowAiming;
    private bool gunAiming;

    private bool handArrowHiddenBecauseShot = false;

    //Animation
    Animator animator;

    [Header("Camera")]
    public Transform cameraRoot;
    public Transform playerCam;

    [Header("Death Camera")]
    public bool useDeathCamera = true;
    public float deathCameraDistance = 4f;
    public float deathCameraHeight = 1.6f;
    public float deathCameraLookHeight = 1.0f;
    public float deathCameraMoveSpeed = 5f;
    public float deathCameraRotateSpeed = 8f;
    public bool unlockCursorOnDeath = false;

    private bool isDead = false;

    public float mouseSensitivity = 50f;
    public float upperLimit = -40f;
    public float bottomLimit = 70f;

    private float xRotation;

    [Header("Current Arrow Type")]
    public ArrowShoot.ArrowType currentArrowType = ArrowShoot.ArrowType.Simple;

    [Header("Arrow Shoot Script")]
    public ArrowShoot arrowShoot;

    [Header("Hand Arrows")]
    public GameObject SimpleHandArrow;
    public GameObject FireHandArrow;
    public GameObject IceHandArrow;
    public GameObject AcidHandArrow;
    public GameObject VolcanoHandArrow;

    public Transform orientation;

    private Rigidbody rb;

    private float yRotation;
    private float yRotInput;
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;

    [Header("Movement Speeds")]
    public float walkMoveSpeed = 350f;
    public float sprintMoveSpeed = 5500f;

    public float walkMaxSpeed = 12f;
    public float sprintMaxSpeed = 20f;

    [Header("External Slow Effect")]
    [SerializeField] private float externalSpeedMultiplier = 1f;

    public bool grounded;
    public LayerMask whatIsGround;

    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float slideForce = 400;
    public float slideCounterMovement = 0.2f;

    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 550f;

    [Header("Dialogue Lock")]
    public bool blockJumpDuringDialogue = false;
    private float jumpInputUnlockTime = 0f;

    float x, y;
    bool jumping, sprinting, crouching;

    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    private CapsuleCollider playerCollider;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    [Header("Footstep Sounds")]
    public AudioSource footstepAudioSource;

    [Tooltip("Add your 3 grass footstep sounds here.")]
    public AudioClip[] grassFootstepClips;

    [Tooltip("Add your wood footstep sounds here.")]
    public AudioClip[] woodFootstepClips;

    [Tooltip("Objects with this tag will play wood footstep sound.")]
    public string woodSurfaceTag = "Wood";

    [Tooltip("Soft walking volume.")]
    [Range(0f, 1f)]
    public float walkFootstepVolume = 0.35f;

    [Tooltip("Loud running volume.")]
    [Range(0f, 1f)]
    public float runFootstepVolume = 0.75f;

    [Tooltip("Walking pitch variation.")]
    public Vector2 walkPitchRange = new Vector2(0.95f, 1.05f);

    [Tooltip("Running pitch variation.")]
    public Vector2 runPitchRange = new Vector2(1.05f, 1.15f);

    [Tooltip("Ray distance used to check the ground surface.")]
    public float footstepRayDistance = 1.6f;

    [Tooltip("Small protection so duplicate animation events do not play many sounds at once.")]
    public float minFootstepInterval = 0.08f;

    [Tooltip("0 = 2D sound, 1 = full 3D sound.")]
    [Range(0f, 1f)]
    public float footstepSpatialBlend = 0.4f;

    private float nextAllowedFootstepTime;
    private AudioClip lastGrassFootstepClip;
    private AudioClip lastWoodFootstepClip;

    private bool sliding = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (arrowShoot == null)
        {
            arrowShoot = GetComponentInChildren<ArrowShoot>(true);
        }

        playerCollider = GetComponent<CapsuleCollider>();
        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;

        SetupFootstepAudioSource();
    }

    void Start()
    {
        HideAllHandArrows();

        if (arrowShoot != null)
        {
            arrowShoot.SetArrowType(currentArrowType);
        }

        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        animator.applyRootMotion = false;
    }

    public void HandArrowActive()
    {
        if (!isGunEquipped &&
            bowAiming &&
            !handArrowHiddenBecauseShot &&
            HasArrowsAvailable())
        {
            ShowCurrentHandArrow();
        }
        else
        {
            HideAllHandArrows();
        }
    }

    public void SetArrowType(ArrowShoot.ArrowType newArrowType)
    {
        currentArrowType = newArrowType;

        if (arrowShoot != null)
        {
            arrowShoot.SetArrowType(newArrowType);
        }

        HideAllHandArrows();

        if (!isGunEquipped && bowAiming && !handArrowHiddenBecauseShot)
        {
            ShowCurrentHandArrow();
        }

        Debug.Log("Player arrow changed to: " + currentArrowType);
    }
    private bool HasArrowsAvailable()
    {
        if (arrowShoot == null)
        {
            arrowShoot = GetComponentInChildren<ArrowShoot>(true);
        }

        return arrowShoot != null && arrowShoot.CurrentArrows > 0;
    }

    public void ShowCurrentHandArrow()
    {
        // Never show the hand arrow when ammunition is zero.
        if (!HasArrowsAvailable())
        {
            HideAllHandArrows();
            return;
        }

        GameObject handArrow = GetCurrentHandArrow();

        if (handArrow == null)
        {
            return;
        }

        if (handArrow.activeSelf)
        {
            return;
        }

        HideAllHandArrows();

        handArrow.SetActive(true);
        PlayHandArrowParticles(handArrow);
    }

    public void HideCurrentHandArrow()
    {
        HideAllHandArrows();
    }
    public void HideHandArrowBecauseShot()
    {
        handArrowHiddenBecauseShot = true;
        HideAllHandArrows();
    }

    public void HideAllHandArrows()
    {
        HideOneHandArrow(SimpleHandArrow);
        HideOneHandArrow(FireHandArrow);
        HideOneHandArrow(IceHandArrow);
        HideOneHandArrow(AcidHandArrow);
        HideOneHandArrow(VolcanoHandArrow);
    }

    private void HideOneHandArrow(GameObject handArrow)
    {
        if (handArrow == null) return;

        ParticleSystem[] particles = handArrow.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        handArrow.SetActive(false);
    }

    private void PlayHandArrowParticles(GameObject handArrow)
    {
        ParticleSystem[] particles = handArrow.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private GameObject GetCurrentHandArrow()
    {
        switch (currentArrowType)
        {
            case ArrowShoot.ArrowType.Fire:
                return FireHandArrow;

            case ArrowShoot.ArrowType.Ice:
                return IceHandArrow;

            case ArrowShoot.ArrowType.Acid:
                return AcidHandArrow;

            case ArrowShoot.ArrowType.Volcano:
                return VolcanoHandArrow;

            default:
                return SimpleHandArrow;
        }
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            return;
        }

        CheckGrounded();

        Movement();

        StopGroundSliding();

        yRotation += yRotInput;
        rb.MoveRotation(Quaternion.Euler(0f, yRotation, 0f));

        orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void Update()
    {
        if (isDead)
        {
            x = 0f;
            y = 0f;
            jumping = false;
            sprinting = false;
            crouching = false;
            sliding = false;
            yRotInput = 0f;

            return;
        }

        MyInput();
        Look();
        UpdateAimRig();
        UpdateGunAimCollider();
        Animate();
    }

    private void UpdateAimRig()
    {
        float bowTargetWeight = (!isGunEquipped && bowAiming) ? 1f : 0f;
        float gunTargetWeight = (isGunEquipped && gunAiming) ? 1f : 0f;

        if (bowAimingRig != null)
        {
            bowAimingRig.weight = Mathf.Lerp(
                bowAimingRig.weight,
                bowTargetWeight,
                Time.deltaTime * rigLerpSpeed
            );
        }

        if (gunAimingRig != null)
        {
            gunAimingRig.weight = Mathf.Lerp(
                gunAimingRig.weight,
                gunTargetWeight,
                Time.deltaTime * rigLerpSpeed
            );
        }
    }

    public void EquipBow()
    {
        isGunEquipped = false;

        bowAiming = false;
        gunAiming = false;

        if (animator != null)
        {
            animator.SetBool("IsGunEquipped", false);
            animator.SetBool("aim", false);
        }

        if (gunAimingRig != null)
        {
            gunAimingRig.weight = 0f;
        }
    }

    public void EquipGun()
    {
        isGunEquipped = true;

        bowAiming = false;
        gunAiming = false;

        if (animator != null)
        {
            animator.SetBool("IsGunEquipped", true);
            animator.SetBool("aim", false);
        }

        if (bowAimingRig != null)
        {
            bowAimingRig.weight = 0f;
        }
    }

    private void LateUpdate()
    {
        if (isDead)
        {
            UpdateDeathCamera();
            return;
        }

        if (!cameraRoot) return;
        if (!playerCam) return;

        playerCam.position = cameraRoot.position;
    }

    public void ExitAim()
    {
        bowAiming = false;
        gunAiming = false;

        if (animator != null)
        {
            animator.SetBool("aim", false);
        }

        HideAllHandArrows();

        if (bowAimingRig != null)
        {
            bowAimingRig.weight = 0f;
        }

        if (gunAimingRig != null)
        {
            gunAimingRig.weight = 0f;
        }

        if (playerCollider != null && !crouching && !sliding)
        {
            playerCollider.center = originalColliderCenter;
        }
    }

    public void SetJumpBlocked(bool blocked)
    {
        blockJumpDuringDialogue = blocked;
        jumping = false;

        // This prevents the player from jumping on the same Space press
        // that closes the dialogue.
        if (blocked)
        {
            jumpInputUnlockTime = Time.time + 0.1f;
        }
        else
        {
            jumpInputUnlockTime = Time.time + 0.2f;
        }
    }

    private void MyInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");

        bool jumpAllowed = !blockJumpDuringDialogue && Time.time >= jumpInputUnlockTime;
        jumping = jumpAllowed && Input.GetButton("Jump");

        sprinting = Input.GetKey(KeyCode.LeftShift);

        bool controlHeld = Input.GetKey(KeyCode.LeftControl);

        bool isGrapplingNow = grapplingGun != null && grapplingGun.IsGrappling();

        if (isGrapplingNow)
        {
            ExitAim();
        }
        else
        {
            if (!isGunEquipped)
            {
                bowAiming = Input.GetMouseButton(1);
                gunAiming = false;

                if (!bowAiming)
                {
                    handArrowHiddenBecauseShot = false;
                }

                animator.SetBool("aim", bowAiming);

                if (bowAiming &&!handArrowHiddenBecauseShot &&HasArrowsAvailable())
                {
                    ShowCurrentHandArrow();
                }
                else
                {
                    HideAllHandArrows();
                }
            }

            if (isGunEquipped)
            {
                bowAiming = false;
                HideAllHandArrows();

                if (Input.GetMouseButtonDown(1))
                {
                    gunAiming = !gunAiming;
                }

                animator.SetBool("aim", gunAiming);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            bool clickWillGrapple =
                grapplingGun != null &&
                grapplingGun.CanStartGrapple();

            if (!clickWillGrapple)
            {
                // The gun can still use its shooting animation.
                bool canShootWeapon = isGunEquipped;

                // The bow can only use its shooting animation
                // when at least one arrow is available.
                if (!isGunEquipped)
                {
                    canShootWeapon =
                        arrowShoot != null &&
                        arrowShoot.CurrentArrows > 0;
                }

                if (canShootWeapon)
                {
                    ExitAim();
                    animator.SetTrigger("shoot");
                }
                else
                {
                    Debug.Log("Cannot shoot: no arrows available.");
                }
            }
        }

        if (sprinting && Input.GetKeyDown(KeyCode.LeftControl) && grounded)
        {
            StartSlide();
        }
        else if (!sprinting && Input.GetKeyDown(KeyCode.LeftControl))
        {
            StartCrouch();
        }

        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            StopCrouch();
            StopSlide();
        }

        crouching = controlHeld && !sprinting;
    }
    private void Animate()
    {
        if (!animator) return;

        animator.SetBool("Jump", !grounded);
        animator.SetBool("Crouch", crouching);

        animator.applyRootMotion = crouching;

        if (grounded)
        {
            if (crouching)
            {
                animator.SetFloat("X_Crouch", x, 0.1f, Time.deltaTime);
                animator.SetFloat("Y_Crouch", y, 0.1f, Time.deltaTime);

                animator.SetFloat("X_Velocity", 0f);
                animator.SetFloat("Y_Velocity", 0f);
            }
            else
            {
                float multiplier = (x != 0 || y != 0) ? (sprinting ? 6f : 2f) : 0f;

                multiplier *= externalSpeedMultiplier;

                animator.SetFloat("X_Velocity", x * multiplier, 0.1f, Time.deltaTime);
                animator.SetFloat("Y_Velocity", y * multiplier, 0.1f, Time.deltaTime);

                animator.SetFloat("X_Crouch", 0f);
                animator.SetFloat("Y_Crouch", 0f);
            }
        }
        else
        {
            animator.SetFloat("X_Velocity", 0f);
            animator.SetFloat("Y_Velocity", 0f);
            animator.SetFloat("X_Crouch", 0f);
            animator.SetFloat("Y_Crouch", 0f);
        }
    }

    private void StartSlide()
    {
        sliding = true;

        animator.SetBool("Running", true);

        rb.AddForce(orientation.forward * slideForce);
    }

    private void StopSlide()
    {
        if (!sliding) return;
        sliding = false;

        animator.SetBool("Running", false);
    }

    private void StartCrouch()
    {
        animator.SetBool("Running", false);

        playerCollider.height = originalColliderHeight * 0.5f;
        playerCollider.center = originalColliderCenter * 0.5f;

        Vector3 pos = transform.position;
        pos.y -= (originalColliderHeight - playerCollider.height) / 2f;
        transform.position = pos;
    }

    private void StopCrouch()
    {
        sliding = false;

        playerCollider.height = originalColliderHeight;
        playerCollider.center = originalColliderCenter;

        Vector3 pos = transform.position;
        pos.y += (originalColliderHeight - playerCollider.height) / 2f;
        transform.position = pos;
    }

    private void StopGroundSliding()
    {
        if (!grounded) return;
        if (jumping) return;
        if (crouching) return;
        if (sliding) return;

        bool noInput = Mathf.Abs(x) < 0.01f && Mathf.Abs(y) < 0.01f;

        if (!noInput) return;

        Vector3 horizontalVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        horizontalVelocity = Vector3.Lerp(
            horizontalVelocity,
            Vector3.zero,
            Time.fixedDeltaTime * 12f
        );

        if (horizontalVelocity.magnitude < 0.2f)
        {
            horizontalVelocity = Vector3.zero;
        }

        rb.linearVelocity = new Vector3(
            horizontalVelocity.x,
            rb.linearVelocity.y,
            horizontalVelocity.z
        );
    }

    private void Movement()
    {
        if (crouching)
        {
            return;
        }

        // Helps keep the player attached to uneven ground
        if (grounded && !jumping)
        {
            rb.AddForce(Vector3.down * 25f, ForceMode.Acceleration);
        }

        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x;
        float yMag = mag.y;

        CounterMovement(x, y, mag);

        if (readyToJump && jumping)
        {
            Jump();
        }

        float currentMoveSpeed;
        float currentMaxSpeed;

        if (sprinting && grounded && !crouching)
        {
            currentMoveSpeed = sprintMoveSpeed;
            currentMaxSpeed = sprintMaxSpeed;
        }
        else
        {
            currentMoveSpeed = walkMoveSpeed;
            currentMaxSpeed = walkMaxSpeed;
        }

        currentMoveSpeed *= externalSpeedMultiplier;
        currentMaxSpeed *= externalSpeedMultiplier;

        if (x > 0 && xMag > currentMaxSpeed) x = 0;
        if (x < 0 && xMag < -currentMaxSpeed) x = 0;
        if (y > 0 && yMag > currentMaxSpeed) y = 0;
        if (y < 0 && yMag < -currentMaxSpeed) y = 0;

        float multiplier = grounded ? 1f : 0.5f;

        if (sliding)
        {
            Vector3 slideDirection = Vector3.ProjectOnPlane(orientation.forward, normalVector).normalized;
            rb.AddForce(slideDirection * slideForce * Time.fixedDeltaTime * externalSpeedMultiplier);
            return;
        }

        Vector3 moveDirection =
            orientation.forward * y +
            orientation.right * x;

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // Important fix for uneven ground
        if (grounded)
        {
            moveDirection = Vector3.ProjectOnPlane(moveDirection, normalVector).normalized;
        }

        rb.AddForce(moveDirection * currentMoveSpeed * Time.fixedDeltaTime * multiplier);
    }

    private void Jump()
    {
        if (grounded && readyToJump)
        {
            readyToJump = false;

            Vector3 vel = rb.linearVelocity;

            if (vel.y < 0f)
            {
                vel.y = 0f;
                rb.linearVelocity = vel;
            }

            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotInput = mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, upperLimit, bottomLimit);

        playerCam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        float currentMaxSpeed = sprinting ? sprintMaxSpeed : walkMaxSpeed;
        currentMaxSpeed *= externalSpeedMultiplier;

        if (!grounded || jumping) return;

        if (crouching)
        {
            rb.AddForce(currentMaxSpeed * Time.deltaTime * -rb.linearVelocity.normalized * slideCounterMovement);
            return;
        }

        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(currentMaxSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }

        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(currentMaxSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        if (new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude > currentMaxSpeed)
        {
            float fallSpeed = rb.linearVelocity.y;
            Vector3 n = rb.linearVelocity.normalized * currentMaxSpeed;
            rb.linearVelocity = new Vector3(n.x, fallSpeed, n.z);
        }
    }

    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.linearVelocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    //private bool cancellingGrounded;

    /*private void OnCollisionStay(Collision other)
    {
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;

            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }*/

    private void CheckGrounded()
    {
        if (playerCollider == null)
        {
            grounded = false;
            normalVector = Vector3.up;
            return;
        }

        Vector3 origin = transform.position + playerCollider.center + Vector3.up * 0.1f;

        float sphereRadius = playerCollider.radius * 0.9f;
        float checkDistance = (playerCollider.height * 0.5f) + 0.35f;

        RaycastHit hit;

        if (Physics.SphereCast(
            origin,
            sphereRadius,
            Vector3.down,
            out hit,
            checkDistance,
            whatIsGround,
            QueryTriggerInteraction.Ignore))
        {
            if (IsFloor(hit.normal))
            {
                grounded = true;
                normalVector = hit.normal;
            }
            else
            {
                grounded = false;
                normalVector = Vector3.up;
            }
        }
        else
        {
            grounded = false;
            normalVector = Vector3.up;
        }
    }

    /*private void StopGrounded()
    {
        grounded = false;
    }*/

    public bool IsGunAiming()
    {
        return gunAiming;
    }

    private void UpdateGunAimCollider()
    {
        if (playerCollider == null) return;

        if (crouching || sliding) return;

        Vector3 targetCenter = originalColliderCenter;

        if (isGunEquipped && gunAiming)
        {
            targetCenter = originalColliderCenter + new Vector3(0f, gunAimColliderYOffset, 0f);
        }

        playerCollider.center = Vector3.Lerp(
            playerCollider.center,
            targetCenter,
            Time.deltaTime * aimColliderLerpSpeed
        );
    }

    public void SetExternalSpeedMultiplier(float multiplier)
    {
        externalSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
    }

    private void SetupFootstepAudioSource()
    {
        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
        }

        if (footstepAudioSource == null)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
        }

        footstepAudioSource.playOnAwake = false;
        footstepAudioSource.loop = false;
        footstepAudioSource.spatialBlend = footstepSpatialBlend;
    }

    public void PlayFootstepSound()
    {
        if (Time.time < nextAllowedFootstepTime) return;

        if (!CanPlayFootstepSound()) return;

        if (footstepAudioSource == null)
        {
            SetupFootstepAudioSource();
        }

        AudioClip selectedClip = GetFootstepClip();

        if (selectedClip == null) return;

        bool isRunning = IsRunningForFootstep();

        float volume = isRunning ? runFootstepVolume : walkFootstepVolume;

        Vector2 pitchRange = isRunning ? runPitchRange : walkPitchRange;
        footstepAudioSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);

        footstepAudioSource.PlayOneShot(selectedClip, volume);

        nextAllowedFootstepTime = Time.time + minFootstepInterval;
    }

    private bool CanPlayFootstepSound()
    {
        if (!grounded) return false;
        if (jumping) return false;
        if (crouching) return false;
        if (sliding) return false;

        if (grapplingGun != null && grapplingGun.IsGrappling()) return false;

        bool hasMovementInput = Mathf.Abs(x) > 0.01f || Mathf.Abs(y) > 0.01f;

        Vector3 horizontalVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        bool isActuallyMoving = horizontalVelocity.magnitude > 0.25f;

        return hasMovementInput && isActuallyMoving;
    }

    private bool IsRunningForFootstep()
    {
        bool hasMovementInput = Mathf.Abs(x) > 0.01f || Mathf.Abs(y) > 0.01f;

        return sprinting && grounded && hasMovementInput && !crouching && !sliding;
    }

    private AudioClip GetFootstepClip()
    {
        if (IsStandingOnWood())
        {
            AudioClip woodClip = GetRandomWoodFootstepClip();

            if (woodClip != null)
            {
                return woodClip;
            }
        }

        return GetRandomGrassFootstepClip();
    }

    private AudioClip GetRandomGrassFootstepClip()
    {
        if (grassFootstepClips == null || grassFootstepClips.Length == 0)
        {
            return null;
        }

        if (grassFootstepClips.Length == 1)
        {
            return grassFootstepClips[0];
        }

        AudioClip selectedClip = null;

        for (int i = 0; i < 10; i++)
        {
            selectedClip = grassFootstepClips[UnityEngine.Random.Range(0, grassFootstepClips.Length)];

            if (selectedClip != null && selectedClip != lastGrassFootstepClip)
            {
                break;
            }
        }

        if (selectedClip == null)
        {
            selectedClip = grassFootstepClips[0];
        }

        lastGrassFootstepClip = selectedClip;
        return selectedClip;
    }

    private AudioClip GetRandomWoodFootstepClip()
    {
        if (woodFootstepClips == null || woodFootstepClips.Length == 0)
        {
            return null;
        }

        if (woodFootstepClips.Length == 1)
        {
            return woodFootstepClips[0];
        }

        AudioClip selectedClip = null;

        for (int i = 0; i < 10; i++)
        {
            selectedClip = woodFootstepClips[UnityEngine.Random.Range(0, woodFootstepClips.Length)];

            if (selectedClip != null && selectedClip != lastWoodFootstepClip)
            {
                break;
            }
        }

        if (selectedClip == null)
        {
            selectedClip = woodFootstepClips[0];
        }

        lastWoodFootstepClip = selectedClip;
        return selectedClip;
    }
    private bool IsStandingOnWood()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;

        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, footstepRayDistance, whatIsGround, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.CompareTag(woodSurfaceTag);
        }

        return false;
    }
    public void OnPlayerDeath()
    {
        if (isDead) return;

        isDead = true;

        x = 0f;
        y = 0f;
        jumping = false;
        sprinting = false;
        crouching = false;
        sliding = false;
        bowAiming = false;
        gunAiming = false;
        handArrowHiddenBecauseShot = true;
        yRotInput = 0f;

        HideAllHandArrows();

        if (bowAimingRig != null)
        {
            bowAimingRig.weight = 0f;
        }

        if (gunAimingRig != null)
        {
            gunAimingRig.weight = 0f;
        }

        if (animator != null)
        {
            animator.SetBool("aim", false);
            animator.SetBool("Jump", false);
            animator.SetBool("Crouch", false);
            animator.SetBool("Running", false);

            animator.SetFloat("X_Velocity", 0f);
            animator.SetFloat("Y_Velocity", 0f);
            animator.SetFloat("X_Crouch", 0f);
            animator.SetFloat("Y_Crouch", 0f);

            animator.applyRootMotion = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (playerCollider != null)
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.Stop();
        }

        if (arrowShoot != null)
        {
            arrowShoot.enabled = false;
        }

        if (grapplingGun != null)
        {
            grapplingGun.SendMessage("StopGrapple", SendMessageOptions.DontRequireReceiver);
            grapplingGun.enabled = false;
        }

        if (unlockCursorOnDeath)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void UpdateDeathCamera()
    {
        if (!useDeathCamera) return;
        if (playerCam == null) return;

        Vector3 targetPosition =
            transform.position
            - transform.forward * deathCameraDistance
            + Vector3.up * deathCameraHeight;

        Vector3 lookTarget =
            transform.position
            + Vector3.up * deathCameraLookHeight;

        playerCam.position = Vector3.Lerp(
            playerCam.position,
            targetPosition,
            Time.deltaTime * deathCameraMoveSpeed
        );

        Vector3 lookDirection = lookTarget - playerCam.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            playerCam.rotation = Quaternion.Slerp(
                playerCam.rotation,
                targetRotation,
                Time.deltaTime * deathCameraRotateSpeed
            );
        }
    }
    public void OnPlayerRespawn()
    {
        isDead = false;

        x = 0f;
        y = 0f;
        jumping = false;
        sprinting = false;
        crouching = false;
        sliding = false;
        bowAiming = false;
        gunAiming = false;
        handArrowHiddenBecauseShot = false;
        yRotInput = 0f;

        HideAllHandArrows();

        if (bowAimingRig != null)
        {
            bowAimingRig.weight = 0f;
        }

        if (gunAimingRig != null)
        {
            gunAimingRig.weight = 0f;
        }

        if (arrowShoot == null)
        {
            arrowShoot = GetComponentInChildren<ArrowShoot>(true);
        }

        if (arrowShoot != null)
        {
            arrowShoot.enabled = true;
            arrowShoot.ResetArrowsToStartingAmount();
        }

        if (grapplingGun != null)
        {
            grapplingGun.enabled = true;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (playerCollider != null)
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.Stop();
        }

        if (animator != null)
        {
            animator.SetBool("aim", false);
            animator.SetBool("Jump", false);
            animator.SetBool("Crouch", false);
            animator.SetBool("Running", false);

            animator.SetFloat("X_Velocity", 0f);
            animator.SetFloat("Y_Velocity", 0f);
            animator.SetFloat("X_Crouch", 0f);
            animator.SetFloat("Y_Crouch", 0f);

            animator.applyRootMotion = false;
        }

        if (playerCam != null && cameraRoot != null)
        {
            playerCam.position = cameraRoot.position;
            playerCam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}