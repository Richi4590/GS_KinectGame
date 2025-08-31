using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class LemmingsMarchManager : MonoBehaviour
{
    [Header("Lemming Settings")]
    public GameObject characterPrefab;
    public List<string> TagsLemmingsHave = new List<string>();
    public List<string> TagsLemmingsShouldReactTo = new List<string>();
    public LayerMask layersLemmingsIgnore;
    public float lemmingMoveSpeed = 2f;

    [Header("Spawn Settings")]
    [Min(0)] public int characterCount = 10;
    public float spawnRadius = 5f;
    public float delayBetweenMarches = 1f;
    public Transform lemmingsSpawnRoot;
    public Transform exitPoint;
    public bool destroyLemmingsIfRespawns = false;

    private CoroutineTracker marchingCoroutine;

    private AudioSource audioSource;

    public bool StartMarching
    {
        get => _startMarching;
        set
        {
            if (_startMarching != value)
            {
                _startMarching = value;
                StartCoroutine(OnViewSettingsChangedCoroutine());
            }
        }
    }

    [SerializeField] List<AudioClip> deathSounds;
    [SerializeField] private List<LemmingsControllerAI> spawnedLemmings = new List<LemmingsControllerAI>();
    [SerializeField] private List<LemmingsControllerAI> idleLemmings = new List<LemmingsControllerAI>();

    [SerializeField] private bool _startMarching = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        foreach (Transform child in lemmingsSpawnRoot.transform)
        {
            Destroy(child.gameObject);
        }

        SpawnLemmings();
        marchingCoroutine = new CoroutineTracker(this, MarchingSequenceLoop());

        marchingCoroutine.Start();
    }
     
    private void Update()
    {
        if (!marchingCoroutine.IsRunning && gameObject.activeInHierarchy)
            marchingCoroutine?.Start();
    }
     
    private void OnValidate()
    {
        if (Application.isPlaying && gameObject.activeInHierarchy)
        {
            StartCoroutine(OnViewSettingsChangedCoroutine());
        }

    }

    private IEnumerator OnViewSettingsChangedCoroutine()
    {
        yield return null;

        if (StartMarching)
        {
            SpawnTheRestOfLemmings();
        }
        else
        {
            DestroyAllLemmings();
            SpawnLemmings();
        }

    }

    public IEnumerator MarchingSequenceLoop()
    {
        yield return null;
            
        Vector3 flatDirection = exitPoint.forward.normalized;
        flatDirection.y = 0; // Flatten it to the XZ plane

        while (gameObject.activeInHierarchy)
        {
            if (_startMarching)
            {
                yield return new WaitForSeconds(delayBetweenMarches);

                if (idleLemmings.Count > 0)
                {
                    if (idleLemmings[0] != null)
                    {
                        while (!idleLemmings[0].LemmingInitialized)
                            yield return null;

                        idleLemmings[0].BeginMarching(exitPoint.transform.position);
                        idleLemmings[0].transform.rotation = Quaternion.LookRotation(flatDirection, Vector3.up);
                        idleLemmings.Remove(idleLemmings[0]);
                    }
                }
            }
            else
                yield return null;
        }
    }

    public void SpawnLemming()
    {
        Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomPos.x, 0, randomPos.y);

        Quaternion randomRot = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        GameObject characterObj = Instantiate(characterPrefab, spawnPos, randomRot, lemmingsSpawnRoot);
        LemmingsControllerAI lemmingController = characterObj.GetComponent<LemmingsControllerAI>();
        lemmingController.InitLemming(this, lemmingMoveSpeed, TagsLemmingsHave, TagsLemmingsShouldReactTo, layersLemmingsIgnore);

        spawnedLemmings.Add(lemmingController);
        idleLemmings.Add(lemmingController);
        lemmingController.OnDestroyEvent += LemmingDestroyed;
        lemmingController.OnRespawnEvent += LemmingRespawned;
    }

    public void RespawnLemming(LemmingsControllerAI respawnedLemming)
    {
        respawnedLemming.ChangeState(LemmingsControllerAI.State.IdleWander);
        Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomPos.x, 0, randomPos.y);
        Quaternion randomRot = Quaternion.Euler(0, Random.Range(0, 360f), 0);

        respawnedLemming.transform.position = spawnPos;
        respawnedLemming.transform.rotation = randomRot;

        respawnedLemming.InitLemming(this, lemmingMoveSpeed, TagsLemmingsHave, TagsLemmingsShouldReactTo, layersLemmingsIgnore);
        idleLemmings.Add(respawnedLemming);
    }

    private void LemmingDestroyed(LemmingsControllerAI destroyedLemming)
    {
        audioSource.PlayOneShot(deathSounds[Random.Range(0, deathSounds.Count)]);

        destroyedLemming.OnDestroyEvent -= LemmingDestroyed;
        destroyedLemming.OnRespawnEvent -= LemmingRespawned;
        spawnedLemmings.Remove(destroyedLemming);

        if (destroyedLemming.currentState == LemmingsControllerAI.State.IdleWander)
            idleLemmings.Remove(destroyedLemming);

        SpawnLemming();
    }

    private void LemmingRespawned(LemmingsControllerAI respawnedLemming)
    {
        audioSource.pitch = Random.Range(0.85f, 1.15f);
        audioSource.PlayOneShot(deathSounds[Random.Range(0, deathSounds.Count)]);
        RespawnLemming(respawnedLemming);
    }

    private void SpawnLemmings()
    {
        for (int i = 0; i < characterCount; i++)
        {
            SpawnLemming();
        }
    }

    private void SpawnTheRestOfLemmings()
    {
        int restToSpawn = characterCount - spawnedLemmings.Count;

        if (restToSpawn > 0)
        {
            for (int i = 0; i < restToSpawn; i++)
            {
                SpawnLemming();
            }
        }
        else
        {
            restToSpawn = -restToSpawn;

            for (int i = 0; i < restToSpawn; i++)
            {
                DestroyIdleLemming();
            }
        }
        

    }

    private void DestroyLemming(LemmingsControllerAI lemmingToBeDestroyed)
    {
        ClearLemmingFromLists(lemmingToBeDestroyed);
        lemmingToBeDestroyed.DestroyLemming();
    }

    private void DestroyIdleLemming()
    {
        if (idleLemmings.Count > 0)
        {
            LemmingsControllerAI idleLemmingToBeDestroyed = idleLemmings[idleLemmings.Count - 1];
            ClearLemmingFromLists(idleLemmingToBeDestroyed);
            idleLemmingToBeDestroyed.DestroyLemmingNoNotify();
        }
        else //delete a marching lemming
        {
            if (spawnedLemmings.Count > 0)
            {
                LemmingsControllerAI spawnedLemmingToBeDestroyed = spawnedLemmings[spawnedLemmings.Count - 1];
                spawnedLemmings.Remove(spawnedLemmingToBeDestroyed);
                spawnedLemmingToBeDestroyed.DestroyLemmingNoNotify();
            }
        }
    }

    private void DestroyMarchingLemming()
    {
        if (spawnedLemmings.Count > 0)
        { 
            LemmingsControllerAI spawnedLemmingToBeDestroyed = spawnedLemmings[spawnedLemmings.Count - 1];
            spawnedLemmings.Remove(spawnedLemmingToBeDestroyed);
            spawnedLemmingToBeDestroyed.DestroyLemmingNoNotify();
        }
    }

    private void DestroyAllLemmings()
    {
        ClearLemmingsLists();

        foreach (LemmingsControllerAI character in spawnedLemmings)
        {
            character.DestroyLemmingNoNotify();
        }
    }

    private void ClearLemmingsLists()
    {
        spawnedLemmings.Clear();
        idleLemmings.Clear();
    }

    private void ClearLemmingFromLists(LemmingsControllerAI lemmingToBeRemoved)
    {
        idleLemmings.Remove(lemmingToBeRemoved);
        spawnedLemmings.Remove(lemmingToBeRemoved);
    }

    void OnDrawGizmos()
    {
        // Draw spawn circle
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Draw exit direction arrow
        Gizmos.color = Color.red;

        Vector3 dir = exitPoint.forward.normalized;
        dir.y = 0; // Flatten it to the XZ plane

        if (dir.sqrMagnitude > 0.001f)
        {
            Vector3 start = exitPoint.position;
            Vector3 end = start + dir * (spawnRadius + 2f);

            Gizmos.DrawLine(start, end);

            // Draw arrowhead
            Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 210, 0) * Vector3.forward;

            float arrowHeadLength = 0.5f;
            Gizmos.DrawLine(end, end + right * arrowHeadLength);
            Gizmos.DrawLine(end, end + left * arrowHeadLength);
        }
    }

    private void OnDestroy()
    {
        marchingCoroutine.StopAndCleanup();
        marchingCoroutine = null;

    }
}
