using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Weapon Holders")]
    public GameObject bowHolder;
    public GameObject gunHolder;

    [Header("Arrow UI")]
    public GameObject arrowAmmoUI;

    private PlayerMovement playerMovement;
    private Animator animator;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        EquipBow();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipBow();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipGun();
        }
    }

    public void EquipBow()
    {
        bowHolder.SetActive(true);
        gunHolder.SetActive(false);

        // Show the arrow icon and arrow number.
        if (arrowAmmoUI != null)
        {
            arrowAmmoUI.SetActive(true);
        }

        if (playerMovement != null)
        {
            playerMovement.EquipBow();
        }

        if (animator != null)
        {
            animator.SetBool("IsGunEquipped", false);
            animator.SetBool("aim", false);
        }
    }

    public void EquipGun()
    {
        bowHolder.SetActive(false);
        gunHolder.SetActive(true);

        // Hide the arrow icon and arrow number.
        if (arrowAmmoUI != null)
        {
            arrowAmmoUI.SetActive(false);
        }

        if (playerMovement != null)
        {
            playerMovement.EquipGun();
        }

        if (animator != null)
        {
            animator.SetBool("IsGunEquipped", true);
            animator.SetBool("aim", false);
        }
    }
}