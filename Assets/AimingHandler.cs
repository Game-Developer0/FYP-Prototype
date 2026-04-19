using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AimingHandler : MonoBehaviour
{
    //[SerializeField] private CameraController cameraController;
    //[SerializeField] private ReachingOut reachingOut;

    private Animator animator;

    [Header("Bow")]
    [SerializeField] private SkinnedMeshRenderer bowSkinnedMeshRenderer;

    [Header("Rig & Constraints")]
    [SerializeField] private Rig aimingRig;
    [SerializeField] private Transform rightHand;

    [Header("Physics")]
    [SerializeField] private LayerMask aimingMask;
    [SerializeField] private GameObject aimTarget;

    private RaycastHit hitInfo;
    private Ray aimingRay;

    private bool isBowVisible;
    public bool isAiming;

    void Start()
    {
        animator = GetComponent<Animator>();

        isBowVisible = false;
    }

    void Update()
    {
        // Draw bow.
        if (Input.GetButtonDown("Jump") && !isBowVisible)
        {
            StartCoroutine(BowVisibility("Standing Equip Bow", 0.15f, true));
        }

        // Hide Bow.
        if (Input.GetKeyDown(KeyCode.H))
        {
            StartCoroutine(BowVisibility("Standing Disarm Bow", 0.3f, false));
        }

        // Enter aim-mode.
        if (Input.GetMouseButton(1))
        {
            isAiming = true;
        }
        else // Exit aim-mode.
        {
            isAiming = false;
        }
        animator.SetBool("Aim", isAiming);
    }
    /*private void LateUpdate()
    {
        if (reachingOut.EnteredReachingOutArea) return;

        if (isAiming)
        {
            cameraController.AimingBehaviour();
        }
        else
        {
            cameraController.NormalBehaviour();
        }
    }*/
    private IEnumerator BowVisibility(string animName, float visibilityTime, bool isVisible)
    {
        animator.CrossFade(animName, 0.2f);

        // Wait until the animator transitions to the new state.
        AnimatorStateInfo nextState;
        do
        {
            yield return null;
            nextState = animator.GetCurrentAnimatorStateInfo(1);
        } while (!nextState.IsName(animName));

        float time = 0;

        // Continue while loop until the specified time has passed.
        while (time <= nextState.length)
        {
            time += Time.deltaTime;

            if (time > visibilityTime)
            {
                bowSkinnedMeshRenderer.enabled = isVisible;
                isBowVisible = isVisible;
                yield break;
            }

            yield return null;
        }
    }
}
