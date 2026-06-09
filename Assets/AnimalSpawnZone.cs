using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalSpawnZone : MonoBehaviour
{
    [Serializable]
    public class SpawnRule
    {
        public string ruleName;

        [Header("Animal")]
        public EcosystemSpecies species;
        public GameObject animalPrefab;

        [Header("Spawn Points")]
        public List<Transform> spawnPoints = new List<Transform>();

        [Header("Limit In This Zone")]
        public int desiredActiveInThisZone = 3;

        [Header("Group Spawn")]
        public int minGroupSize = 1;
        public int maxGroupSize = 1;

        [Range(0f, 1f)]
        public float spawnChance = 1f;
    }

    [Header("References")]
    public Transform player;
    public Transform townCenter;

    [Header("Zone Distance")]
    public float spawnDistance = 80f;
    public float despawnDistance = 130f;
    public float minimumDistanceFromPlayer = 30f;

    [Header("Town Safe Area")]
    public bool preventSpawnInsideTownSafeZone = true;
    public float townSafeRadius = 60f;

    [Header("Spawn Settings")]
    public float checkInterval = 2f;
    public float respawnDelayAfterDeath = 25f;
    public int maxSpawnAttemptsPerRule = 20;

    [Header("Disable / Enable Instead Of Destroy")]
    public bool keepAnimalsDisabledWhenZoneInactive = true;
    public bool moveAnimalsBackToHomeWhenEnabledAgain = true;

    [Header("Soft Despawn")]
    public bool useSoftDespawn = true;
    public float despawnWhenAnimalNearHomeDistance = 4f;
    public float forceDespawnIfVeryFarFromPlayer = 250f;
    public float returnHomeRepathInterval = 1f;

    [Header("Ground Placement")]
    public bool snapSpawnToGround = true;
    public LayerMask groundLayer = ~0;
    public float groundRayStartHeight = 50f;
    public float groundRayDistance = 120f;
    public float spawnHeightOffset = 0.05f;
    public bool warpAgentToSpawnPosition = true;

    [Header("NavMesh")]
    public bool useNavMeshSampling = true;
    public float navMeshSampleRadius = 8f;

    [Header("Rules")]
    public List<SpawnRule> spawnRules = new List<SpawnRule>();

    [Header("Debug")]
    public bool showDebugMessages = true;

    private readonly List<EcosystemAnimal> activeAnimals = new List<EcosystemAnimal>();
    private readonly Dictionary<EcosystemAnimal, Vector3> animalHomePositions = new Dictionary<EcosystemAnimal, Vector3>();
    private readonly HashSet<EcosystemAnimal> animalsReturningHome = new HashSet<EcosystemAnimal>();
    private readonly Dictionary<EcosystemSpecies, float> nextAllowedSpawnTime = new Dictionary<EcosystemSpecies, float>();
    private readonly Dictionary<EcosystemAnimal, float> nextReturnPathTime = new Dictionary<EcosystemAnimal, float>();

    private bool zoneActive = false;
    private bool zoneSoftDespawning = false;
    private float nextCheckTime = 0f;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogWarning($"[{name}] Player reference is missing. Assign Player manually or tag your player as Player.");
            }
        }
    }

    private void Update()
    {
        if (Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + checkInterval;

        if (player == null)
            return;

        UpdateZoneState();
    }

    private void UpdateZoneState()
    {
        float distanceFromPlayer = Vector3.Distance(player.position, transform.position);

        if (zoneSoftDespawning)
        {
            if (distanceFromPlayer <= spawnDistance)
            {
                CancelSoftDespawn();
                EnableStoredAnimals();

                zoneActive = true;

                if (showDebugMessages)
                {
                    Debug.Log($"[{name}] Player returned. Same animals enabled again.");
                }

                TrySpawnAnimals();
                return;
            }

            ProcessSoftDespawn();
            return;
        }

        if (!zoneActive && distanceFromPlayer <= spawnDistance)
        {
            zoneActive = true;

            EnableStoredAnimals();

            if (showDebugMessages)
            {
                Debug.Log($"[{name}] Spawn zone activated.");
            }

            TrySpawnAnimals();
            return;
        }

        if (zoneActive && distanceFromPlayer >= despawnDistance)
        {
            zoneActive = false;

            if (keepAnimalsDisabledWhenZoneInactive)
            {
                if (useSoftDespawn)
                {
                    StartSoftDespawn();

                    if (showDebugMessages)
                    {
                        Debug.Log($"[{name}] Zone deactivated. Animals are returning home, then they will be disabled.");
                    }
                }
                else
                {
                    DisableAllAnimalsButKeepThem();

                    if (showDebugMessages)
                    {
                        Debug.Log($"[{name}] Zone deactivated. Animals disabled but not destroyed.");
                    }
                }
            }
            else
            {
                DespawnAllAnimalsImmediately();

                if (showDebugMessages)
                {
                    Debug.Log($"[{name}] Zone deactivated. Animals destroyed/despawned.");
                }
            }

            return;
        }

        if (zoneActive)
        {
            TrySpawnAnimals();
        }
    }

    private void TrySpawnAnimals()
    {
        if (!zoneActive || zoneSoftDespawning)
            return;

        CleanActiveAnimalList();

        if (EcosystemManager.Instance == null)
        {
            Debug.LogWarning($"[{name}] No EcosystemManager found in scene.");
            return;
        }

        foreach (SpawnRule rule in spawnRules)
        {
            if (rule == null)
                continue;

            if (rule.animalPrefab == null)
                continue;

            if (Time.time < GetNextAllowedSpawnTime(rule.species))
                continue;

            int currentInZone = CountLivingAnimalsOfSpecies(rule.species);
            int missingAmount = rule.desiredActiveInThisZone - currentInZone;

            if (missingAmount <= 0)
                continue;

            int attempts = 0;

            while (missingAmount > 0 && attempts < maxSpawnAttemptsPerRule)
            {
                attempts++;

                if (!EcosystemManager.Instance.CanSpawn(rule.species))
                    break;

                int groupSize = UnityEngine.Random.Range(rule.minGroupSize, rule.maxGroupSize + 1);
                groupSize = Mathf.Clamp(groupSize, 1, missingAmount);

                bool spawnedAny = false;

                for (int i = 0; i < groupSize; i++)
                {
                    if (missingAmount <= 0)
                        break;

                    bool spawned = TrySpawnOneAnimal(rule);

                    if (spawned)
                    {
                        spawnedAny = true;
                        missingAmount--;
                    }
                }

                if (!spawnedAny)
                    break;
            }
        }
    }

    private bool TrySpawnOneAnimal(SpawnRule rule)
    {
        if (UnityEngine.Random.value > rule.spawnChance)
            return false;

        if (!TryGetSpawnPosition(rule, out Vector3 spawnPosition, out Quaternion spawnRotation))
            return false;

        GameObject animalObject = Instantiate(rule.animalPrefab, spawnPosition, spawnRotation);

        NavMeshAgent spawnedAgent = animalObject.GetComponent<NavMeshAgent>();

        if (spawnedAgent != null)
        {
            spawnedAgent.baseOffset = 0f;

            if (warpAgentToSpawnPosition && spawnedAgent.isOnNavMesh)
            {
                spawnedAgent.Warp(spawnPosition);
            }
        }

        EcosystemAnimal ecosystemAnimal = animalObject.GetComponent<EcosystemAnimal>();

        if (ecosystemAnimal == null)
        {
            ecosystemAnimal = animalObject.AddComponent<EcosystemAnimal>();
        }

        ecosystemAnimal.SpawnedByEcosystem(this, rule.species);

        activeAnimals.Add(ecosystemAnimal);

        // Very important: remember the animal's home/spawn position.
        animalHomePositions[ecosystemAnimal] = spawnPosition;

        if (showDebugMessages)
        {
            Debug.Log($"[{name}] Spawned {rule.species} at {spawnPosition}");
        }

        return true;
    }

    private bool TryGetSpawnPosition(SpawnRule rule, out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = Vector3.zero;
        spawnRotation = Quaternion.identity;

        if (rule.spawnPoints == null || rule.spawnPoints.Count == 0)
            return false;

        for (int attempt = 0; attempt < maxSpawnAttemptsPerRule; attempt++)
        {
            Transform point = rule.spawnPoints[UnityEngine.Random.Range(0, rule.spawnPoints.Count)];

            if (point == null)
                continue;

            Vector3 rawPosition = point.position;

            if (player != null)
            {
                float distanceFromPlayer = Vector3.Distance(player.position, rawPosition);

                if (distanceFromPlayer < minimumDistanceFromPlayer)
                    continue;
            }

            if (preventSpawnInsideTownSafeZone && townCenter != null)
            {
                float distanceFromTown = Vector3.Distance(townCenter.position, rawPosition);

                if (distanceFromTown < townSafeRadius)
                    continue;
            }

            Vector3 finalPosition = rawPosition;

            if (useNavMeshSampling)
            {
                bool foundNavMeshPosition = NavMesh.SamplePosition(
                    rawPosition,
                    out NavMeshHit navMeshHit,
                    navMeshSampleRadius,
                    NavMesh.AllAreas
                );

                if (!foundNavMeshPosition)
                    continue;

                finalPosition = navMeshHit.position;
            }

            if (snapSpawnToGround)
            {
                Vector3 rayStart = finalPosition + Vector3.up * groundRayStartHeight;

                bool hitGround = Physics.Raycast(
                    rayStart,
                    Vector3.down,
                    out RaycastHit groundHit,
                    groundRayDistance,
                    groundLayer,
                    QueryTriggerInteraction.Ignore
                );

                if (hitGround)
                {
                    finalPosition = groundHit.point + Vector3.up * spawnHeightOffset;
                }
            }

            spawnPosition = finalPosition;
            spawnRotation = point.rotation;
            return true;
        }

        return false;
    }

    private void StartSoftDespawn()
    {
        CleanActiveAnimalList();

        zoneSoftDespawning = true;
        animalsReturningHome.Clear();
        nextReturnPathTime.Clear();

        foreach (EcosystemAnimal animal in activeAnimals)
        {
            if (animal == null)
                continue;

            if (animal.DeathReported)
                continue;

            if (!animal.gameObject.activeSelf)
                continue;

            Vector3 homePosition = GetAnimalHomePosition(animal);

            animalsReturningHome.Add(animal);
            nextReturnPathTime[animal] = 0f;

            animal.gameObject.SendMessage(
                "BeginReturnHomeForDespawning",
                homePosition,
                SendMessageOptions.DontRequireReceiver
            );

            SendAnimalBackHome(animal, homePosition);
        }

        if (AreAllLivingAnimalsDisabled())
        {
            FinishSoftDespawn();
        }
    }

    private void ProcessSoftDespawn()
    {
        CleanActiveAnimalList();

        if (AreAllLivingAnimalsDisabled())
        {
            FinishSoftDespawn();
            return;
        }

        List<EcosystemAnimal> animalsToDisable = new List<EcosystemAnimal>();

        foreach (EcosystemAnimal animal in activeAnimals)
        {
            if (animal == null)
                continue;

            if (animal.DeathReported)
                continue;

            if (!animal.gameObject.activeSelf)
                continue;

            Vector3 homePosition = GetAnimalHomePosition(animal);

            float distanceToHome = Vector3.Distance(animal.transform.position, homePosition);
            float distanceToPlayer = Vector3.Distance(animal.transform.position, player.position);

            if (distanceToHome <= despawnWhenAnimalNearHomeDistance)
            {
                animalsToDisable.Add(animal);
                continue;
            }

            if (distanceToPlayer >= forceDespawnIfVeryFarFromPlayer)
            {
                animalsToDisable.Add(animal);
                continue;
            }

            if (Time.time >= GetNextReturnPathTime(animal))
            {
                SendAnimalBackHome(animal, homePosition);
                nextReturnPathTime[animal] = Time.time + returnHomeRepathInterval;
            }
        }

        foreach (EcosystemAnimal animal in animalsToDisable)
        {
            DisableAnimalButKeepInZone(animal);
        }

        if (AreAllLivingAnimalsDisabled())
        {
            FinishSoftDespawn();
        }
    }

    private void SendAnimalBackHome(EcosystemAnimal animal, Vector3 homePosition)
    {
        if (animal == null)
            return;

        if (!animal.gameObject.activeInHierarchy)
            return;

        NavMeshAgent agent = animal.GetComponent<NavMeshAgent>();

        if (agent == null)
            return;

        if (!agent.enabled)
            return;

        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.stoppingDistance = 0.5f;
        agent.SetDestination(homePosition);
    }

    private void DisableAllAnimalsButKeepThem()
    {
        CleanActiveAnimalList();

        List<EcosystemAnimal> animalsToDisable = new List<EcosystemAnimal>(activeAnimals);

        foreach (EcosystemAnimal animal in animalsToDisable)
        {
            DisableAnimalButKeepInZone(animal);
        }

        FinishSoftDespawn();
    }

    private void DisableAnimalButKeepInZone(EcosystemAnimal animal)
    {
        if (animal == null)
            return;

        if (animal.DeathReported)
            return;

        animalsReturningHome.Remove(animal);
        nextReturnPathTime.Remove(animal);

        if (animal.gameObject.activeSelf)
        {
            NavMeshAgent agent = animal.GetComponent<NavMeshAgent>();

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }

            animal.gameObject.SendMessage(
                "CancelReturnHomeForDespawning",
                SendMessageOptions.DontRequireReceiver
            );

            // This is the main fix.
            // We do NOT destroy the animal.
            // We do NOT remove it from activeAnimals.
            // We only disable it.
            animal.gameObject.SetActive(false);

            if (showDebugMessages)
            {
                Debug.Log($"[{name}] Disabled {animal.species} but kept it saved in this zone.");
            }
        }
    }

    private void EnableStoredAnimals()
    {
        CleanActiveAnimalList();

        zoneSoftDespawning = false;
        animalsReturningHome.Clear();
        nextReturnPathTime.Clear();

        foreach (EcosystemAnimal animal in activeAnimals)
        {
            if (animal == null)
                continue;

            if (animal.DeathReported)
                continue;

            if (!animal.gameObject.activeSelf)
            {
                animal.gameObject.SetActive(true);
            }

            if (moveAnimalsBackToHomeWhenEnabledAgain)
            {
                MoveAnimalToHome(animal);
            }

            NavMeshAgent agent = animal.GetComponent<NavMeshAgent>();

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            animal.gameObject.SendMessage(
                "CancelReturnHomeForDespawning",
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private void MoveAnimalToHome(EcosystemAnimal animal)
    {
        if (animal == null)
            return;

        Vector3 homePosition = GetAnimalHomePosition(animal);

        if (useNavMeshSampling)
        {
            bool foundNavMeshPosition = NavMesh.SamplePosition(
                homePosition,
                out NavMeshHit navMeshHit,
                navMeshSampleRadius,
                NavMesh.AllAreas
            );

            if (foundNavMeshPosition)
            {
                homePosition = navMeshHit.position;
            }
        }

        NavMeshAgent agent = animal.GetComponent<NavMeshAgent>();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(homePosition);
            agent.ResetPath();
            agent.isStopped = false;
        }
        else
        {
            animal.transform.position = homePosition;
        }
    }

    private bool AreAllLivingAnimalsDisabled()
    {
        foreach (EcosystemAnimal animal in activeAnimals)
        {
            if (animal == null)
                continue;

            if (animal.DeathReported)
                continue;

            if (animal.gameObject.activeSelf)
                return false;
        }

        return true;
    }

    private void FinishSoftDespawn()
    {
        zoneSoftDespawning = false;
        animalsReturningHome.Clear();
        nextReturnPathTime.Clear();

        if (showDebugMessages)
        {
            Debug.Log($"[{name}] Zone animals are now disabled/saved.");
        }
    }

    private void CancelSoftDespawn()
    {
        zoneSoftDespawning = false;

        foreach (EcosystemAnimal animal in animalsReturningHome)
        {
            if (animal == null)
                continue;

            if (!animal.gameObject.activeSelf)
                continue;

            animal.gameObject.SendMessage(
                "CancelReturnHomeForDespawning",
                SendMessageOptions.DontRequireReceiver
            );
        }

        animalsReturningHome.Clear();
        nextReturnPathTime.Clear();
    }

    public void NotifyAnimalDeath(EcosystemAnimal animal)
    {
        if (animal == null)
            return;

        activeAnimals.Remove(animal);
        animalHomePositions.Remove(animal);
        animalsReturningHome.Remove(animal);
        nextReturnPathTime.Remove(animal);

        SetNextAllowedSpawnTime(animal.species, Time.time + respawnDelayAfterDeath);
    }

    private void DespawnAllAnimalsImmediately()
    {
        CleanActiveAnimalList();

        List<EcosystemAnimal> animalsToDespawn = new List<EcosystemAnimal>(activeAnimals);

        foreach (EcosystemAnimal animal in animalsToDespawn)
        {
            if (animal != null && !animal.DeathReported)
            {
                animal.DespawnWithoutDeath();
            }
        }

        activeAnimals.Clear();
        animalHomePositions.Clear();
        animalsReturningHome.Clear();
        nextReturnPathTime.Clear();
    }

    private void CleanActiveAnimalList()
    {
        activeAnimals.RemoveAll(animal => animal == null);

        List<EcosystemAnimal> nullKeys = new List<EcosystemAnimal>();

        foreach (EcosystemAnimal animal in animalHomePositions.Keys)
        {
            if (animal == null)
            {
                nullKeys.Add(animal);
            }
        }

        foreach (EcosystemAnimal animal in nullKeys)
        {
            animalHomePositions.Remove(animal);
            animalsReturningHome.Remove(animal);
            nextReturnPathTime.Remove(animal);
        }
    }

    private Vector3 GetAnimalHomePosition(EcosystemAnimal animal)
    {
        if (animal != null && animalHomePositions.TryGetValue(animal, out Vector3 homePosition))
        {
            return homePosition;
        }

        if (animal != null)
        {
            return animal.transform.position;
        }

        return transform.position;
    }

    private int CountLivingAnimalsOfSpecies(EcosystemSpecies species)
    {
        int count = 0;

        foreach (EcosystemAnimal animal in activeAnimals)
        {
            if (animal == null)
                continue;

            if (animal.DeathReported)
                continue;

            if (animal.species == species)
            {
                count++;
            }
        }

        return count;
    }

    private float GetNextAllowedSpawnTime(EcosystemSpecies species)
    {
        if (nextAllowedSpawnTime.TryGetValue(species, out float time))
        {
            return time;
        }

        return 0f;
    }

    private void SetNextAllowedSpawnTime(EcosystemSpecies species, float time)
    {
        nextAllowedSpawnTime[species] = time;
    }

    private float GetNextReturnPathTime(EcosystemAnimal animal)
    {
        if (animal != null && nextReturnPathTime.TryGetValue(animal, out float time))
        {
            return time;
        }

        return 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, despawnDistance);

        if (townCenter != null && preventSpawnInsideTownSafeZone)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(townCenter.position, townSafeRadius);
        }
    }
}