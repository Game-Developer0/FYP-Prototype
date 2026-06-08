using UnityEngine;

public class EcosystemAnimal : MonoBehaviour
{
    [Header("Ecosystem")]
    public EcosystemSpecies species;

    private AnimalSpawnZone spawnZone;
    private bool isRegisteredAsActive = false;
    private bool deathReported = false;

    public bool DeathReported => deathReported;

    public void SpawnedByEcosystem(AnimalSpawnZone zone, EcosystemSpecies newSpecies)
    {
        spawnZone = zone;
        species = newSpecies;
        deathReported = false;

        if (!isRegisteredAsActive)
        {
            EcosystemManager.Instance?.RegisterActiveSpawn(species);
            isRegisteredAsActive = true;
        }

        // Optional reset message for your animal AI/health scripts.
        // If your script has ResetAnimalForSpawn(), it will be called.
        SendMessage("ResetAnimalForSpawn", SendMessageOptions.DontRequireReceiver);
    }

    public void ReportDeathToEcosystem()
    {
        if (deathReported)
            return;

        deathReported = true;

        EcosystemManager.Instance?.RegisterAnimalDeath(species);

        isRegisteredAsActive = false;

        if (spawnZone != null)
        {
            spawnZone.NotifyAnimalDeath(this);
        }
    }

    public void DespawnWithoutDeath()
    {
        if (!deathReported && isRegisteredAsActive)
        {
            EcosystemManager.Instance?.UnregisterActiveSpawn(species);
        }

        isRegisteredAsActive = false;
        deathReported = false;
        spawnZone = null;

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Safety check.
        // If something destroys the animal without telling the ecosystem,
        // at least remove it from active spawned count.
        if (!deathReported && isRegisteredAsActive)
        {
            EcosystemManager.Instance?.UnregisterActiveSpawn(species);
        }
    }
}