using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EcosystemSpecies
{
    Chomper,
    Stegasaurus,
    Wolf,
    Bear,
    BossWolf
}

public class EcosystemManager : MonoBehaviour
{
    public static EcosystemManager Instance { get; private set; }

    [Serializable]
    public class SpeciesInfo
    {
        public EcosystemSpecies species;

        [Header("Population")]
        public int startingPopulation = 10;
        public int maxPopulation = 25;

        [Header("Status")]
        public bool showStatusWarnings = true;
        public int atRiskAt = 5;
        public int endangeredAt = 2;

        [Header("Recovery")]
        public bool canRecoverOverTime = false;

        [HideInInspector] public int currentPopulation;
        [HideInInspector] public int activeSpawnedCount;
    }

    [Header("Species Population")]
    public List<SpeciesInfo> speciesList = new List<SpeciesInfo>();

    [Header("Background Ecosystem Simulation")]
    public bool runBackgroundSimulation = true;
    public float backgroundTickSeconds = 60f;

    [Header("On Screen Warnings")]
    public bool showScreenWarnings = true;
    public bool warnOnlyWhenStatusChanges = true;

    private Dictionary<EcosystemSpecies, string> lastKnownStatus = new Dictionary<EcosystemSpecies, string>();

    [Header("Debug")]
    public bool showDebugMessages = true;

    private Coroutine backgroundRoutine;

    private void Reset()
    {
        CreateDefaultSpeciesList();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (speciesList == null || speciesList.Count == 0)
        {
            CreateDefaultSpeciesList();
        }

        InitializePopulations();
    }

    private void Start()
    {
        if (runBackgroundSimulation)
        {
            backgroundRoutine = StartCoroutine(BackgroundSimulationLoop());
        }
    }

    private void CreateDefaultSpeciesList()
    {
        speciesList = new List<SpeciesInfo>
        {
            new SpeciesInfo
            {
                species = EcosystemSpecies.Chomper,
                startingPopulation = 25,
                maxPopulation = 35,
                atRiskAt = 10,
                endangeredAt = 5,
                canRecoverOverTime = true
            },

            new SpeciesInfo
            {
                species = EcosystemSpecies.Stegasaurus,
                startingPopulation = 10,
                maxPopulation = 12,
                atRiskAt = 6,
                endangeredAt = 3,
                canRecoverOverTime = true
            },

            new SpeciesInfo
            {
                species = EcosystemSpecies.Wolf,
                startingPopulation = 8,
                maxPopulation = 10,
                atRiskAt = 2,
                endangeredAt = 1,
                canRecoverOverTime = false
            },

            new SpeciesInfo
            {
                species = EcosystemSpecies.Bear,
                startingPopulation = 3,
                maxPopulation = 4,
                atRiskAt = 1,
                endangeredAt = 1,
                canRecoverOverTime = false
            },

            new SpeciesInfo
            {
                species = EcosystemSpecies.BossWolf,
                startingPopulation = 1,
                maxPopulation = 1,
                atRiskAt = 0,
                endangeredAt = 0,
                canRecoverOverTime = false
            }
        };
    }

    private void InitializePopulations()
    {
        lastKnownStatus.Clear();

        foreach (SpeciesInfo info in speciesList)
        {
            info.currentPopulation = Mathf.Clamp(info.startingPopulation, 0, info.maxPopulation);
            info.activeSpawnedCount = 0;

            lastKnownStatus[info.species] = GetPopulationStatus(info.species);
        }
    }

    private void CheckSpeciesStatusWarning(EcosystemSpecies species)
    {
        if (!showScreenWarnings)
            return;

        SpeciesInfo info = GetSpeciesInfo(species);

        if (info == null)
            return;

        string currentStatus = GetPopulationStatus(species);

        if (warnOnlyWhenStatusChanges)
        {
            if (lastKnownStatus.TryGetValue(species, out string previousStatus))
            {
                if (previousStatus == currentStatus)
                    return;
            }
        }

        lastKnownStatus[species] = currentStatus;

        if (currentStatus == "Safe")
        {
            EcosystemWarningUI.Instance?.ShowWarning($"{species} population is now Safe.", 3f);
            return;
        }

        if (currentStatus == "At Risk")
        {
            EcosystemWarningUI.Instance?.ShowWarning($"Warning! {species} population is At Risk.", 4f);
            return;
        }

        if (currentStatus == "Endangered")
        {
            EcosystemWarningUI.Instance?.ShowWarning($"Critical Warning! {species} is Endangered. Protect them!", 5f);
            return;
        }

        if (currentStatus == "Extinct")
        {
            EcosystemWarningUI.Instance?.ShowWarning($"Ecosystem Failure! {species} is Extinct.", 6f);
            return;
        }
    }
    private IEnumerator BackgroundSimulationLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(backgroundTickSeconds);
            RunBackgroundSimulationTick();
        }
    }

    private void RunBackgroundSimulationTick()
    {
        int wolfPopulation = GetPopulation(EcosystemSpecies.Wolf);
        int bearPopulation = GetPopulation(EcosystemSpecies.Bear);
        int bossWolfPopulation = GetPopulation(EcosystemSpecies.BossWolf);

        // Wolves hunt small creatures.
        if (wolfPopulation > 0)
        {
            TryReduceHiddenPopulation(EcosystemSpecies.Chomper, 1);

            if (wolfPopulation >= 5 && UnityEngine.Random.value < 0.35f)
            {
                TryReduceHiddenPopulation(EcosystemSpecies.Stegasaurus, 1);
            }
        }

        // Bears are rare but dangerous.
        if (bearPopulation > 0)
        {
            if (UnityEngine.Random.value < 0.5f)
            {
                TryReduceHiddenPopulation(EcosystemSpecies.Stegasaurus, 1);
            }

            if (UnityEngine.Random.value < 0.35f)
            {
                TryReduceHiddenPopulation(EcosystemSpecies.Chomper, 1);
            }
        }

        // Boss wolf creates extra danger while alive.
        if (bossWolfPopulation > 0)
        {
            if (UnityEngine.Random.value < 0.6f)
            {
                TryReduceHiddenPopulation(EcosystemSpecies.Stegasaurus, 1);
            }
        }

        // If predators are controlled, peaceful animals recover slowly.
        if (wolfPopulation <= 3 && bearPopulation <= 1 && bossWolfPopulation == 0)
        {
            TryIncreasePopulation(EcosystemSpecies.Chomper, 1);

            if (UnityEngine.Random.value < 0.5f)
            {
                TryIncreasePopulation(EcosystemSpecies.Stegasaurus, 1);
            }
        }
    }

    public bool CanSpawn(EcosystemSpecies species)
    {
        SpeciesInfo info = GetSpeciesInfo(species);

        if (info == null)
            return false;

        if (info.currentPopulation <= 0)
            return false;

        return info.activeSpawnedCount < info.currentPopulation;
    }

    public void RegisterActiveSpawn(EcosystemSpecies species)
    {
        SpeciesInfo info = GetSpeciesInfo(species);

        if (info == null)
            return;

        info.activeSpawnedCount = Mathf.Min(info.activeSpawnedCount + 1, info.currentPopulation);

        if (showDebugMessages)
        {
            Debug.Log($"[Ecosystem] Spawned {species}. Active: {info.activeSpawnedCount}, Population: {info.currentPopulation}");
        }
    }

    public void UnregisterActiveSpawn(EcosystemSpecies species)
    {
        SpeciesInfo info = GetSpeciesInfo(species);

        if (info == null)
            return;

        info.activeSpawnedCount = Mathf.Max(0, info.activeSpawnedCount - 1);

        if (showDebugMessages)
        {
            Debug.Log($"[Ecosystem] Despawned {species}. Active: {info.activeSpawnedCount}, Population: {info.currentPopulation}");
        }
    }

    public void RegisterAnimalDeath(EcosystemSpecies species)
    {
        SpeciesInfo info = GetSpeciesInfo(species);

        if (info == null)
            return;

        info.activeSpawnedCount = Mathf.Max(0, info.activeSpawnedCount - 1);
        info.currentPopulation = Mathf.Max(0, info.currentPopulation - 1);

        string status = GetPopulationStatus(species);

        Debug.Log($"[Ecosystem] {species} died. Population: {info.currentPopulation}. Status: {status}");

        CheckSpeciesStatusWarning(species);

        if (species == EcosystemSpecies.BossWolf && info.currentPopulation <= 0)
        {
            EcosystemWarningUI.Instance?.ShowWarning("Boss Wolf defeated! Ecosystem danger reduced.", 5f);
        }

        if (info.showStatusWarnings)
        {
            if (status == "Endangered")
            {
                Debug.LogWarning($"[Ecosystem Warning] {species} is ENDANGERED!");
            }
            else if (status == "Extinct")
            {
                Debug.LogError($"[Ecosystem Warning] {species} is EXTINCT!");
            }
        }
    }

    private void TryReduceHiddenPopulation(EcosystemSpecies species, int amount)
    {
        SpeciesInfo info = GetSpeciesInfo(species);

        if (info == null)
            return;

        int hiddenPopulation = info.currentPopulation - info.activeSpawnedCount;

        if (hiddenPopulation <= 0)
            return;

        int actualReduceAmount = Mathf.Min(amount, hiddenPopulation);
        info.currentPopulation = Mathf.Max(0, info.currentPopulation - actualReduceAmount);

        if (showDebugMessages)
        {
            Debug.Log($"[Ecosystem] Background danger reduced {species} by {actualReduceAmount}. Population: {info.currentPopulation}");
        }

        CheckSpeciesStatusWarning(species);
    }

    private void TryIncreasePopulation(EcosystemSpecies species, int amount)
    {
        SpeciesInfo info = GetSpeciesInfo(species);

        if (info == null)
            return;

        if (!info.canRecoverOverTime)
            return;

        if (info.currentPopulation <= 0)
            return;

        int oldPopulation = info.currentPopulation;
        info.currentPopulation = Mathf.Min(info.currentPopulation + amount, info.maxPopulation);

        if (showDebugMessages && info.currentPopulation != oldPopulation)
        {
            Debug.Log($"[Ecosystem] {species} recovered by {amount}. Population: {info.currentPopulation}");
        }

        if (info.currentPopulation != oldPopulation)
        {
            CheckSpeciesStatusWarning(species);
        }
    }

    public int GetPopulation(EcosystemSpecies species)
    {
        SpeciesInfo info = GetSpeciesInfo(species);

        if (info == null)
            return 0;

        return info.currentPopulation;
    }

    public int GetActiveSpawnedCount(EcosystemSpecies species)
    {
        SpeciesInfo info = GetSpeciesInfo(species);

        if (info == null)
            return 0;

        return info.activeSpawnedCount;
    }

    public string GetPopulationStatus(EcosystemSpecies species)
    {
        SpeciesInfo info = GetSpeciesInfo(species);

        if (info == null)
            return "Unknown";

        if (info.currentPopulation <= 0)
            return "Extinct";

        if (info.currentPopulation <= info.endangeredAt)
            return "Endangered";

        if (info.currentPopulation <= info.atRiskAt)
            return "At Risk";

        return "Safe";
    }

    private SpeciesInfo GetSpeciesInfo(EcosystemSpecies species)
    {
        for (int i = 0; i < speciesList.Count; i++)
        {
            if (speciesList[i].species == species)
            {
                return speciesList[i];
            }
        }

        return null;
    }
}