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

    //Animation
    Animator animator;

    [Header("Camera")]
    public Transform cameraRoot;
    public Transform playerCam;

    public float mouseSensitivity = 50f;
    public float upperLimit = -40f;
    public float bottomLimit = 70f;

    private float xRotation;

    [Header("Arrow")]
    public GameObject HandArrow;

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

        playerCollider = GetComponent<CapsuleCollider>();
        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;

        SetupFootstepAudioSource();
    }

    void Start()
    {
        HandArrow.gameObject.SetActive(false);
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        animator.applyRootMotion = false;
    }

    void HandArrowActive()
    {
        HandArrow.gameObject.SetActive(true);
    }

    private void FixedUpdate()
    {
        Movement();

        StopGroundSliding();

        yRotation += yRotInput;
        rb.MoveRotation(Quaternion.Euler(0f, yRotation, 0f));

        orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void Update()
    {
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
        if (!cameraRoot) return;

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

                animator.SetBool("aim", bowAiming);
            }

            if (isGunEquipped)
            {
                bowAiming = false;

                if (Input.GetMouseButtonDown(1))
                {
                    gunAiming = !gunAiming;
                }

                animator.SetBool("aim", gunAiming);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            bool clickWillGrapple = grapplingGun != null && grapplingGun.CanStartGrapple();

            if (!clickWillGrapple)
            {
                ExitAim();
                animator.SetTrigger("shoot");
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

        rb.AddForce(Vector3.down * Time.deltaTime * 10);

        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x;
        float yMag = mag.y;

        CounterMovement(x, y, mag);

        if (grounded && !jumping && !crouching && !sliding)
        {
            bool noMovementInput = Mathf.Abs(x) < 0.01f && Mathf.Abs(y) < 0.01f;

            if (noMovementInput)
            {
                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                if (horizontalVelocity.magnitude < 2f)
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }
            }
        }

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

        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        if (x > 0 && xMag > currentMaxSpeed) x = 0;
        if (x < 0 && xMag < -currentMaxSpeed) x = 0;
        if (y > 0 && yMag > currentMaxSpeed) y = 0;
        if (y < 0 && yMag < -currentMaxSpeed) y = 0;

        float multiplier = 1f;
        float multiplierV = 1f;

        if (!grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        if (sliding)
        {
            rb.AddForce(orientation.forward * slideForce * Time.deltaTime * externalSpeedMultiplier);
            return;
        }

        if (grounded && crouching)
        {
            multiplierV = 0f;
        }

        rb.AddForce(orientation.transform.forward * y * currentMoveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * currentMoveSpeed * Time.deltaTime * multiplier);
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

    private bool cancellingGrounded;

    private void OnCollisionStay(Collision other)
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
    }

    private void StopGrounded()
    {
        grounded = false;
    }

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
}