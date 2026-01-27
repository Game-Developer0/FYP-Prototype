
using System;
using UnityEngine;
using System.Collections;


public class PlayerMovement : MonoBehaviour
{
    //Animation
    Animator animator;

    [Header("Camera")]
    public Transform cameraRoot;   // Empty GameObject following head/chest bone
    public Transform playerCam;

    public float mouseSensitivity = 50f;
    public float upperLimit = -40f;
    public float bottomLimit = 70f;

    private float xRotation;

    //Assingables
    [Header("Arrow")]
    public GameObject HandArrow;

    public Transform orientation;

    //Other
    private Rigidbody rb;

    //Rotation and look
    private float yRotation;
    private float yRotInput;
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;

    //Movement
    [Header("Movement Speeds")]
    public float walkMoveSpeed = 350f;
    public float sprintMoveSpeed = 5500f;

    public float walkMaxSpeed = 12f;
    public float sprintMaxSpeed = 20f;

    public bool grounded;
    public LayerMask whatIsGround;

    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    //Crouch & Slide
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float slideForce = 400;
    public float slideCounterMovement = 0.2f;

    //Jumping
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 550f;

    //Input
    float x, y;
    bool jumping, sprinting, crouching;

    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    //CapsuleCollider
    private CapsuleCollider playerCollider;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        playerCollider = GetComponent<CapsuleCollider>();
        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;
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

        // Apply horizontal rotation to Rigidbody in FixedUpdate
        yRotation += yRotInput;
        rb.MoveRotation(Quaternion.Euler(0f, yRotation, 0f));

        // Orientation follows player rotation
        orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void Update()
    {
        MyInput();
        Look();
        Animate();

    }
    private void LateUpdate()
    {
        if (!cameraRoot) return;

        // Camera follows animated root (head / spine)
        playerCam.position = cameraRoot.position;
    }
    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    private void MyInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        sprinting = Input.GetKey(KeyCode.LeftShift);

        bool controlHeld = Input.GetKey(KeyCode.LeftControl);
        //shoot
        if (Input.GetButton("Fire1"))
        {
            animator.SetBool("aim", true);
        }

        if (Input.GetButtonUp("Fire1"))
        {
            animator.SetBool("shoot", true);
            animator.SetBool("aim", false);
        }
        else
        {
            animator.SetBool("shoot", false);
        }



        // Decide crouch vs slide
        if (sprinting && Input.GetKeyDown(KeyCode.LeftControl) && grounded)
        {
            StartSlide();
        }
        else if (!sprinting && Input.GetKeyDown(KeyCode.LeftControl))
        {
            StartCrouch();
        }

        // Stop crouch/slide when releasing control
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            StopCrouch();
            StopSlide();
        }

        // Update crouching bool for Animator
        crouching = controlHeld && !sprinting; // Only crouching when not sliding
    }


    private void Animate()
    {
        if (!animator) return;

        // Jump animation
        animator.SetBool("Jump", !grounded);

        // Set crouch bool
        animator.SetBool("Crouch", crouching);

        // Enable root motion only when crouching
        animator.applyRootMotion = crouching;

        if (grounded)
        {
            if (crouching)
            {
                animator.SetFloat("X_Crouch", x, 0.1f, Time.deltaTime);
                animator.SetFloat("Y_Crouch", y, 0.1f, Time.deltaTime);

                // Disable normal blend movement while crouching
                animator.SetFloat("X_Velocity", 0f);
                animator.SetFloat("Y_Velocity", 0f);
            }
            else
            {
                float multiplier = (x != 0 || y != 0) ? (sprinting ? 6f : 2f) : 0f;
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



    private bool sliding = false;

    private void StartSlide()
    {
        sliding = true;

        // Set Running parameter for slide animation
        animator.SetBool("Running", true);

        // Shrink collider if needed
        //playerCollider.height = originalColliderHeight * 0.5f;
        //playerCollider.center = originalColliderCenter * 0.5f;

        // Apply forward force
        rb.AddForce(orientation.forward * slideForce);
    }

    private void StopSlide()
    {
        if (!sliding) return;
        sliding = false;

        animator.SetBool("Running", false);

        // Restore collider
       // playerCollider.height = originalColliderHeight;
       // playerCollider.center = originalColliderCenter;
    }

    private void StartCrouch()
    {
        sliding = true;
        animator.SetBool("Running", true); // Set running parameter for slide animation
        // Shrink collider only
        playerCollider.height = originalColliderHeight * 0.5f;
        playerCollider.center = originalColliderCenter * 0.5f;

        // Optional: push player down so feet stay on ground
        Vector3 pos = transform.position;
        pos.y -= (originalColliderHeight - playerCollider.height) / 2f;
        transform.position = pos;

        if (rb.linearVelocity.magnitude > 0.5f && grounded)
        {
            rb.AddForce(orientation.forward * slideForce);
        }
    }

    private void StopCrouch()
    {
        // Restore collider
        playerCollider.height = originalColliderHeight;
        playerCollider.center = originalColliderCenter;

        // Optional: move player up to match collider height
        Vector3 pos = transform.position;
        pos.y += (originalColliderHeight - playerCollider.height) / 2f;
        transform.position = pos;
    }

    private void Movement()
    {
        if (crouching)
        {
            // Let Root Motion handle movement
            return;
        }
        //Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10);

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        //If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();

        //Set max speed
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


        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > currentMaxSpeed) x = 0;
        if (x < 0 && xMag < -currentMaxSpeed) x = 0;
        if (y > 0 && yMag > currentMaxSpeed) y = 0;
        if (y < 0 && yMag < -currentMaxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }
        if (sliding)
        {
            rb.AddForce(orientation.forward * slideForce * Time.deltaTime);
            return; // Skip normal movement while sliding
        }
        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * currentMoveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * currentMoveSpeed * Time.deltaTime * multiplier);

    }

    private void Jump()
    {
        if (grounded && readyToJump)
        {
            readyToJump = false;

            //Add jump forces
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            //If jumping while falling, reset y velocity.
            Vector3 vel = rb.linearVelocity;
            if (rb.linearVelocity.y < 0.5f)
                rb.linearVelocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.linearVelocity.y > 0)
                rb.linearVelocity = new Vector3(vel.x, vel.y / 2, vel.z);

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

        // Horizontal rotation (player body)
        yRotInput = mouseX;

        // Vertical rotation (camera only)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, upperLimit, bottomLimit);

        playerCam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }



    private void CounterMovement(float x, float y, Vector2 mag)
    {
        float currentMaxSpeed = sprinting ? sprintMaxSpeed : walkMaxSpeed;
        if (!grounded || jumping) return;

        //Slow down sliding
        if (crouching)
        {
            rb.AddForce(currentMaxSpeed * Time.deltaTime * -rb.linearVelocity.normalized * slideCounterMovement);
            return;
        }

        //Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(currentMaxSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(currentMaxSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.


        if (new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude > currentMaxSpeed)
        {
            float fallspeed = rb.linearVelocity.y;
            Vector3 n = rb.linearVelocity.normalized * currentMaxSpeed;
            rb.linearVelocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
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

}
