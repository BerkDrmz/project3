using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class WaveManager : MonoBehaviour
{
    private const string EnemyTag = "Enemy";

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public GameObject[] wave1Prefabs;
    public GameObject[] wave2Prefabs;
    public GameObject bossPrefab;

    [Header("Wave UI")]
    public TextMeshProUGUI waveText;

    [Header("Arena Neon")]
    public Material neonMaterial;

    [Header("Audio")]
    public AudioSource bgmAudioSource;
    public AudioClip wave1Music;
    public AudioClip wave2Music;
    public AudioClip bossMusic;
    public AudioClip waveClearedSFX;

    [Header("Wave Clear Effect")]
    public GameObject waveClearedEffect;
    [SerializeField] private float waveClearedEffectDuration = 3f;
    [SerializeField] private bool hideWaveClearedEffectAfterDelay = true;

    [Header("Stage Transition Effect")]
    public StageTransitionEffect stageTransitionEffect;
    [SerializeField] private bool useStageTransitionOverlay = true;
    [SerializeField] private string stageShiftStatus = "NEUROCHARGE // ARENA SYNC";

    [Header("Wave Titles")]
    [SerializeField] private string wave1Title = "WAVE 1";
    [SerializeField] private string wave2Title = "WAVE 2";
    [SerializeField] private string bossTitle = "WAVE 3: BOSS FIGHTING";
    [SerializeField] private string waveClearedTitle = "WAVE CLEARED!";

    [Header("Wave Counts")]
    [SerializeField] private int wave1EnemyCount = 3;
    [SerializeField] private int wave2EnemyCount = 5;

    [Header("Timing")]
    [SerializeField] private float enemyCheckInterval = 0.5f;
    [SerializeField] private float waveClearedDelay = 3f;
    [SerializeField] private float waveTitleDuration = 2f;
    [SerializeField] private float musicFadeInDuration = 1.25f;
    [SerializeField] private float neonTransitionDuration = 3f;
    [FormerlySerializedAs("bossRedFlashSpeed")]
    [SerializeField] private float bossBluePulseSpeed = 8f;

    [Header("Neon Colors")]
    [SerializeField] private Color wave1NeonColor = new Color(0f, 0.65f, 1f, 1f);
    [SerializeField] private Color wave2NeonColor = new Color(0f, 0.65f, 1f, 1f);
    [SerializeField] private Color bossNeonColor = new Color(0.08f, 0.38f, 1f, 1f);
    [SerializeField] private float neonEmissionIntensity = 3f;
    [SerializeField] private bool affectArenaEmissionMaterials = true;

    [Header("Runtime Options")]
    [SerializeField] private bool clearExistingEnemiesOnStart = true;
    [SerializeField] private bool tagSpawnedEnemyChildren = false;
    [SerializeField] private Vector3 bossSpawnPosition = Vector3.zero;

    [Header("Checkpoint")]
    [SerializeField] private bool useAutomaticCheckpoints = true;
    [SerializeField] private bool restoreCheckpointOnSceneStart = true;

    [Header("Debug Hotkeys")]
    [SerializeField] private bool allowDebugWaveClearKey = true;
    [SerializeField] private KeyCode clearWaveKey = KeyCode.F8;

    private readonly List<Material> runtimeNeonMaterials = new List<Material>();
    private readonly List<DusmanCan> activeEnemies = new List<DusmanCan>();
    private Coroutine musicFadeCoroutine;
    private Coroutine waveTitleCoroutine;
    private Coroutine waveClearedEffectCoroutine;
    private PlayerController playerController;
    private UIManager uiManager;
    private int currentWave;
    private int activeEnemyCount;
    private bool isTransitioning;
    private bool isWaveRunning;
    private bool allWavesComplete;
    private float nextEnemyCheckTime;

    private void Awake()
    {
        ResolveMissingReferences();
        CacheNeonMaterials();
    }

    private void Start()
    {
        if (waveText != null)
        {
            waveText.gameObject.SetActive(false);
        }

        HideWaveClearedEffect();

        if (clearExistingEnemiesOnStart)
        {
            ClearExistingEnemies();
        }
        else
        {
            RegisterExistingEnemies();
        }

        isTransitioning = false;

        int startingWave = ResolveStartingWaveFromCheckpoint();
        BeginWave(startingWave);
        ShowTemporaryWaveTitle(GetWaveTitle(startingWave));
        nextEnemyCheckTime = Time.time + enemyCheckInterval;
    }

    private void Update()
    {
        if (allowDebugWaveClearKey && Input.GetKeyDown(clearWaveKey))
        {
            DebugClearActiveEnemies();
        }

        if (allWavesComplete || Time.time < nextEnemyCheckTime)
        {
            return;
        }

        nextEnemyCheckTime = Time.time + enemyCheckInterval;
        activeEnemyCount = CountActiveEnemies();

        if (isWaveRunning && !isTransitioning && activeEnemyCount <= 0)
        {
            isWaveRunning = false;
            StartCoroutine(HandleWaveCleared());
        }
    }

    private void BeginWave(int waveNumber)
    {
        currentWave = waveNumber;

        int spawnedCount = 0;

        if (currentWave == 1)
        {
            ApplyNeonColor(wave1NeonColor);
            PlayWaveMusic(wave1Music);
            spawnedCount = SpawnPrefabGroup(wave1Prefabs, wave1EnemyCount);
        }
        else if (currentWave == 2)
        {
            ApplyNeonColor(wave2NeonColor);
            PlayWaveMusic(wave2Music);
            spawnedCount = SpawnPrefabGroup(wave2Prefabs, wave2EnemyCount);
        }
        else if (currentWave == 3)
        {
            ApplyNeonColor(bossNeonColor);
            PlayWaveMusic(bossMusic);
            spawnedCount = SpawnBoss();
        }

        isWaveRunning = spawnedCount > 0;

        if (!isWaveRunning)
        {
            Debug.LogWarning($"[WaveManager] Wave {currentWave} could not start because no enemy prefab is assigned.");
            return;
        }

        SaveCurrentWaveCheckpoint();
    }

    private int SpawnPrefabGroup(GameObject[] prefabs, int targetCount)
    {
        if (prefabs == null || prefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
        {
            return 0;
        }

        int count = Mathf.Max(1, targetCount);
        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            if (prefab == null || spawnPoint == null)
            {
                continue;
            }

            DusmanCan enemyHealth = PrepareEnemy(Instantiate(prefab, spawnPoint.position, spawnPoint.rotation));
            if (enemyHealth != null)
            {
                RegisterEnemy(enemyHealth);
                spawned++;
            }
        }

        return spawned;
    }

    private int SpawnBoss()
    {
        if (bossPrefab == null)
        {
            return 0;
        }

        DusmanCan bossHealth = PrepareEnemy(Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity));
        if (bossHealth == null)
        {
            return 0;
        }

        RegisterEnemy(bossHealth);
        return 1;
    }

    private DusmanCan PrepareEnemy(GameObject enemy)
    {
        if (enemy == null)
        {
            return null;
        }

        SetEnemyTag(enemy);

        DusmanCan enemyHealth = enemy.GetComponent<DusmanCan>();
        if (enemyHealth == null)
        {
            enemyHealth = enemy.GetComponentInChildren<DusmanCan>();
        }

        if (enemyHealth == null)
        {
            enemyHealth = enemy.AddComponent<DusmanCan>();
        }

        return enemyHealth;
    }

    private void SetEnemyTag(GameObject enemy)
    {
        enemy.tag = EnemyTag;

        foreach (Transform child in enemy.GetComponentsInChildren<Transform>(true))
        {
            if (child.gameObject == enemy)
            {
                continue;
            }

            child.gameObject.tag = tagSpawnedEnemyChildren ? EnemyTag : "Untagged";
        }
    }

    private IEnumerator HandleWaveCleared()
    {
        isTransitioning = true;
        StopWaveTitleCoroutine();
        ShowWaveText(waveClearedTitle);
        PlayWaveClearedSfx();
        PlayWaveClearedEffect();
        float transitionDuration = Mathf.Max(0f, neonTransitionDuration);

        if (currentWave == 1)
        {
            PlayStageTransitionOverlay(stageShiftStatus, wave2Title, wave2NeonColor, transitionDuration);
            yield return StartCoroutine(LerpNeonColor(GetCurrentNeonColor(), wave2NeonColor, transitionDuration));
            yield return StartCoroutine(WaitForRemainingWaveClearDelay(transitionDuration));
            HideWaveText();
            ShowWaveText(wave2Title);
            BeginWave(2);
            yield return new WaitForSeconds(waveTitleDuration);
            HideWaveText();
        }
        else if (currentWave == 2)
        {
            PlayStageTransitionOverlay("BOSS SIGNAL LOCKED", bossTitle, bossNeonColor, transitionDuration);
            yield return StartCoroutine(PulseBossBlueTransition(transitionDuration));
            yield return StartCoroutine(WaitForRemainingWaveClearDelay(transitionDuration));
            HideWaveText();
            ShowWaveText(bossTitle);
            ApplyNeonColor(bossNeonColor);
            BeginWave(3);
            yield return new WaitForSeconds(waveTitleDuration);
            HideWaveText();
        }
        else
        {
            PlayStageTransitionOverlay("ARENA STATUS", "ALL WAVES CLEARED", bossNeonColor, waveClearedDelay);
            yield return new WaitForSeconds(waveClearedDelay);
            HideWaveText();
            allWavesComplete = true;
            ApplyNeonColor(bossNeonColor);
            Debug.Log("[WaveManager] All waves cleared.");
        }

        isTransitioning = false;
    }

    private IEnumerator LerpNeonColor(Color fromColor, Color toColor, float duration)
    {
        if (duration <= 0f)
        {
            ApplyNeonColor(toColor);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ApplyNeonColor(Color.Lerp(fromColor, toColor, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        ApplyNeonColor(toColor);
    }

    private IEnumerator PulseBossBlueTransition(float duration)
    {
        if (duration <= 0f)
        {
            ApplyNeonColor(bossNeonColor);
            yield break;
        }

        float elapsed = 0f;
        Color deepBlue = new Color(0f, 0.08f, 0.22f, 1f);
        Color iceBlue = new Color(0.55f, 0.95f, 1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(Time.time * bossBluePulseSpeed, 1f);
            Color liveColor = Color.Lerp(deepBlue, bossNeonColor, pulse);
            ApplyNeonColor(Color.Lerp(liveColor, iceBlue, pulse * 0.35f));
            yield return null;
        }

        ApplyNeonColor(bossNeonColor);
    }

    private void PlayStageTransitionOverlay(string status, string title, Color accentColor, float duration)
    {
        if (!useStageTransitionOverlay || stageTransitionEffect == null)
        {
            return;
        }

        stageTransitionEffect.PlayTransition(status, title, accentColor, duration);
    }

    private IEnumerator WaitForRemainingWaveClearDelay(float elapsedDuration)
    {
        float remainingDelay = waveClearedDelay - elapsedDuration;
        if (remainingDelay > 0f)
        {
            yield return new WaitForSeconds(remainingDelay);
        }
    }

    private int ResolveStartingWaveFromCheckpoint()
    {
        if (!restoreCheckpointOnSceneStart || !WaveCheckpointManager.TryLoadForActiveScene(out WaveCheckpointManager.CheckpointData checkpointData))
        {
            return 1;
        }

        ResolveCheckpointReferences();
        WaveCheckpointManager.ApplyCheckpoint(checkpointData, GetPlayerTransform(), uiManager);
        return Mathf.Clamp(checkpointData.waveNumber, 1, 3);
    }

    private void SaveCurrentWaveCheckpoint()
    {
        if (!useAutomaticCheckpoints)
        {
            return;
        }

        ResolveCheckpointReferences();
        WaveCheckpointManager.SaveWaveCheckpoint(currentWave, GetPlayerTransform(), uiManager);
    }

    private string GetWaveTitle(int waveNumber)
    {
        if (waveNumber == 2)
        {
            return wave2Title;
        }

        if (waveNumber == 3)
        {
            return bossTitle;
        }

        return wave1Title;
    }

    private void ResolveCheckpointReferences()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>(true);
        }

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>(true);
        }
    }

    private Transform GetPlayerTransform()
    {
        return playerController != null ? playerController.transform : null;
    }

    private void ApplyNeonColor(Color color)
    {
        if (runtimeNeonMaterials.Count == 0 && neonMaterial != null)
        {
            runtimeNeonMaterials.Add(neonMaterial);
        }

        for (int i = 0; i < runtimeNeonMaterials.Count; i++)
        {
            Material material = runtimeNeonMaterials[i];
            if (material == null || !material.HasProperty("_EmissionColor"))
            {
                continue;
            }

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * neonEmissionIntensity);
        }
    }

    private Color GetCurrentNeonColor()
    {
        if (neonMaterial != null && neonMaterial.HasProperty("_EmissionColor"))
        {
            Color emissionColor = neonMaterial.GetColor("_EmissionColor");
            if (neonEmissionIntensity > 0f)
            {
                emissionColor.r /= neonEmissionIntensity;
                emissionColor.g /= neonEmissionIntensity;
                emissionColor.b /= neonEmissionIntensity;
            }

            return emissionColor;
        }

        return wave1NeonColor;
    }

    private void PlayWaveMusic(AudioClip clip)
    {
        if (bgmAudioSource == null)
        {
            bgmAudioSource = GetComponent<AudioSource>();
        }

        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(FadeInMusic(clip));
    }

    private IEnumerator FadeInMusic(AudioClip clip)
    {
        bgmAudioSource.Stop();
        bgmAudioSource.clip = clip;
        bgmAudioSource.loop = true;
        MusicPreferences.ApplyToAudioSource(bgmAudioSource);
        bgmAudioSource.volume = 0f;

        if (clip == null)
        {
            yield break;
        }

        bgmAudioSource.Play();

        float elapsed = 0f;
        while (elapsed < musicFadeInDuration)
        {
            elapsed += Time.deltaTime;
            bgmAudioSource.volume = Mathf.Lerp(0f, 1f, Mathf.Clamp01(elapsed / musicFadeInDuration));
            yield return null;
        }

        bgmAudioSource.volume = 1f;
    }

    private void PlayWaveClearedSfx()
    {
        if (bgmAudioSource != null && waveClearedSFX != null)
        {
            bgmAudioSource.PlayOneShot(waveClearedSFX);
        }
    }

    private void PlayWaveClearedEffect()
    {
        if (waveClearedEffect == null)
        {
            return;
        }

        if (waveClearedEffectCoroutine != null)
        {
            StopCoroutine(waveClearedEffectCoroutine);
            waveClearedEffectCoroutine = null;
        }

        waveClearedEffect.SetActive(true);

        ParticleSystem[] particleSystems = waveClearedEffect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }

        Animator[] animators = waveClearedEffect.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (hideWaveClearedEffectAfterDelay)
        {
            waveClearedEffectCoroutine = StartCoroutine(HideWaveClearedEffectAfterDelay());
        }
    }

    private IEnumerator HideWaveClearedEffectAfterDelay()
    {
        yield return new WaitForSeconds(waveClearedEffectDuration);
        HideWaveClearedEffect();
        waveClearedEffectCoroutine = null;
    }

    private void HideWaveClearedEffect()
    {
        if (waveClearedEffect == null)
        {
            return;
        }

        ParticleSystem[] particleSystems = waveClearedEffect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        waveClearedEffect.SetActive(false);
    }

    private void ShowWaveText(string message)
    {
        if (waveText == null)
        {
            return;
        }

        waveText.gameObject.SetActive(true);
        waveText.text = message;
    }

    private void ShowTemporaryWaveTitle(string message)
    {
        StopWaveTitleCoroutine();

        waveTitleCoroutine = StartCoroutine(ShowTemporaryWaveTitleRoutine(message));
    }

    private IEnumerator ShowTemporaryWaveTitleRoutine(string message)
    {
        ShowWaveText(message);
        yield return new WaitForSeconds(waveTitleDuration);
        HideWaveText();
        waveTitleCoroutine = null;
    }

    private void StopWaveTitleCoroutine()
    {
        if (waveTitleCoroutine == null)
        {
            return;
        }

        StopCoroutine(waveTitleCoroutine);
        waveTitleCoroutine = null;
    }

    private void HideWaveText()
    {
        if (waveText != null)
        {
            waveText.gameObject.SetActive(false);
        }
    }

    private void ResolveMissingReferences()
    {
        if (bgmAudioSource == null)
        {
            bgmAudioSource = GetComponent<AudioSource>();
        }

        if (stageTransitionEffect == null)
        {
            stageTransitionEffect = GetComponent<StageTransitionEffect>();
            if (stageTransitionEffect == null)
            {
                stageTransitionEffect = gameObject.AddComponent<StageTransitionEffect>();
            }
        }

        if (waveText == null)
        {
            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.name.Contains("Wave"))
                {
                    waveText = text;
                    break;
                }
            }
        }

        if (waveClearedEffect == null)
        {
            ParticleSystem[] particleSystems = FindObjectsOfType<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem.name.Contains("Laser AOE") || particleSystem.name.Contains("AOE"))
                {
                    waveClearedEffect = particleSystem.gameObject;
                    break;
                }
            }
        }
    }

    private void CacheNeonMaterials()
    {
        runtimeNeonMaterials.Clear();

        if (neonMaterial != null)
        {
            runtimeNeonMaterials.Add(neonMaterial);
        }

        if (!affectArenaEmissionMaterials)
        {
            return;
        }

        Renderer[] renderers = FindObjectsOfType<Renderer>(true);
        foreach (Renderer sceneRenderer in renderers)
        {
            foreach (Material material in sceneRenderer.sharedMaterials)
            {
                if (material == null || !material.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                string materialName = material.name.ToLowerInvariant();
                bool looksLikeArenaNeon =
                    materialName.Contains("neon") ||
                    materialName.Contains("pad") ||
                    materialName.Contains("capture") ||
                    materialName.Contains("pillar") ||
                    materialName.StartsWith("m_f") ||
                    materialName.StartsWith("m_w");

                if (looksLikeArenaNeon && !runtimeNeonMaterials.Contains(material))
                {
                    runtimeNeonMaterials.Add(material);
                }
            }
        }
    }

    private int CountActiveEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            DusmanCan enemy = activeEnemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                activeEnemies.RemoveAt(i);
            }
        }

        return activeEnemies.Count;
    }

    private void RegisterEnemy(DusmanCan enemyHealth)
    {
        if (enemyHealth == null || activeEnemies.Contains(enemyHealth))
        {
            return;
        }

        activeEnemies.Add(enemyHealth);
    }

    private void RegisterExistingEnemies()
    {
        activeEnemies.Clear();

        DusmanCan[] enemies = FindObjectsOfType<DusmanCan>(true);
        foreach (DusmanCan enemyHealth in enemies)
        {
            if (enemyHealth != null && enemyHealth.gameObject.CompareTag(EnemyTag))
            {
                RegisterEnemy(enemyHealth);
            }
        }
    }

    [ContextMenu("Debug Clear Active Enemies")]
    public void DebugClearActiveEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            DusmanCan enemyHealth = activeEnemies[i];
            if (enemyHealth != null)
            {
                enemyHealth.gameObject.SetActive(false);
                Destroy(enemyHealth.gameObject);
            }
        }

        activeEnemies.Clear();
        activeEnemyCount = 0;

        if (isWaveRunning && !isTransitioning)
        {
            isWaveRunning = false;
            StartCoroutine(HandleWaveCleared());
        }
    }

    private void ClearExistingEnemies()
    {
        DusmanCan[] enemies = FindObjectsOfType<DusmanCan>(true);
        foreach (DusmanCan enemyHealth in enemies)
        {
            if (enemyHealth != null && enemyHealth.gameObject.CompareTag(EnemyTag))
            {
                Destroy(enemyHealth.gameObject);
            }
        }

        activeEnemies.Clear();
    }
}
