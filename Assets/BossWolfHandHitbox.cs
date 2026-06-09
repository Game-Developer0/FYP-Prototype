using UnityEngine;

public class BossWolfHandHitbox : MonoBehaviour
{
    public BossWolfHandDamage bossWolfHandDamage;

    private void Start()
    {
        if (bossWolfHandDamage == null)
        {
            bossWolfHandDamage = GetComponentInParent<BossWolfHandDamage>();
        }

        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (bossWolfHandDamage != null)
        {
            bossWolfHandDamage.TryDamagePlayer(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (bossWolfHandDamage != null)
        {
            bossWolfHandDamage.TryDamagePlayer(other);
        }
    }
}