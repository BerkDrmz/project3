using UnityEngine;
using UnityEngine.SceneManagement;

public static class WaveCheckpointManager
{
    private const string HasCheckpointKey = "NeuroCharge.Checkpoint.Has";
    private const string SceneKey = "NeuroCharge.Checkpoint.Scene";
    private const string WaveKey = "NeuroCharge.Checkpoint.Wave";
    private const string PlayerPosXKey = "NeuroCharge.Checkpoint.PlayerPosX";
    private const string PlayerPosYKey = "NeuroCharge.Checkpoint.PlayerPosY";
    private const string PlayerPosZKey = "NeuroCharge.Checkpoint.PlayerPosZ";
    private const string PlayerRotYKey = "NeuroCharge.Checkpoint.PlayerRotY";
    private const string HealthKey = "NeuroCharge.Checkpoint.Health";
    private const string MaxHealthKey = "NeuroCharge.Checkpoint.MaxHealth";

    public struct CheckpointData
    {
        public int waveNumber;
        public Vector3 playerPosition;
        public Quaternion playerRotation;
        public float health;
        public float maxHealth;
    }

    public static void SaveWaveCheckpoint(int waveNumber, Transform playerTransform, UIManager uiManager)
    {
        if (playerTransform == null)
        {
            playerTransform = ResolvePlayerTransform();
        }

        if (uiManager == null)
        {
            uiManager = UnityEngine.Object.FindObjectOfType<UIManager>(true);
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("[WaveCheckpoint] Player transform bulunamadi, checkpoint kaydedilemedi.");
            return;
        }

        float maxHealth = uiManager != null && uiManager.maxHealth > 0f ? uiManager.maxHealth : 100f;
        float health = uiManager != null ? Mathf.Clamp(uiManager.currentHealth, 1f, maxHealth) : maxHealth;

        PlayerPrefs.SetInt(HasCheckpointKey, 1);
        PlayerPrefs.SetString(SceneKey, SceneManager.GetActiveScene().name);
        PlayerPrefs.SetInt(WaveKey, Mathf.Max(1, waveNumber));
        PlayerPrefs.SetFloat(PlayerPosXKey, playerTransform.position.x);
        PlayerPrefs.SetFloat(PlayerPosYKey, playerTransform.position.y);
        PlayerPrefs.SetFloat(PlayerPosZKey, playerTransform.position.z);
        PlayerPrefs.SetFloat(PlayerRotYKey, playerTransform.eulerAngles.y);
        PlayerPrefs.SetFloat(HealthKey, health);
        PlayerPrefs.SetFloat(MaxHealthKey, maxHealth);
        PlayerPrefs.Save();
    }

    public static bool TryLoadForActiveScene(out CheckpointData checkpointData)
    {
        checkpointData = default;

        if (PlayerPrefs.GetInt(HasCheckpointKey, 0) != 1)
        {
            return false;
        }

        string checkpointScene = PlayerPrefs.GetString(SceneKey, string.Empty);
        if (checkpointScene != SceneManager.GetActiveScene().name)
        {
            return false;
        }

        checkpointData.waveNumber = Mathf.Max(1, PlayerPrefs.GetInt(WaveKey, 1));
        checkpointData.playerPosition = new Vector3(
            PlayerPrefs.GetFloat(PlayerPosXKey, 0f),
            PlayerPrefs.GetFloat(PlayerPosYKey, 0f),
            PlayerPrefs.GetFloat(PlayerPosZKey, 0f));
        checkpointData.playerRotation = Quaternion.Euler(0f, PlayerPrefs.GetFloat(PlayerRotYKey, 0f), 0f);
        checkpointData.health = PlayerPrefs.GetFloat(HealthKey, 100f);
        checkpointData.maxHealth = PlayerPrefs.GetFloat(MaxHealthKey, 100f);
        return true;
    }

    public static void ApplyCheckpoint(CheckpointData checkpointData, Transform playerTransform, UIManager uiManager)
    {
        if (playerTransform == null)
        {
            playerTransform = ResolvePlayerTransform();
        }

        if (playerTransform != null)
        {
            CharacterController characterController = playerTransform.GetComponent<CharacterController>();
            bool wasControllerEnabled = characterController != null && characterController.enabled;

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            playerTransform.SetPositionAndRotation(checkpointData.playerPosition, checkpointData.playerRotation);

            if (characterController != null)
            {
                characterController.enabled = wasControllerEnabled;
            }
        }

        if (uiManager == null)
        {
            uiManager = UnityEngine.Object.FindObjectOfType<UIManager>(true);
        }

        if (uiManager != null)
        {
            if (checkpointData.maxHealth > 0f)
            {
                uiManager.maxHealth = checkpointData.maxHealth;
            }

            float maxHealth = Mathf.Max(1f, uiManager.maxHealth);
            uiManager.currentHealth = Mathf.Clamp(checkpointData.health, 1f, maxHealth);
        }
    }

    public static void ClearCheckpoint()
    {
        PlayerPrefs.DeleteKey(HasCheckpointKey);
        PlayerPrefs.DeleteKey(SceneKey);
        PlayerPrefs.DeleteKey(WaveKey);
        PlayerPrefs.DeleteKey(PlayerPosXKey);
        PlayerPrefs.DeleteKey(PlayerPosYKey);
        PlayerPrefs.DeleteKey(PlayerPosZKey);
        PlayerPrefs.DeleteKey(PlayerRotYKey);
        PlayerPrefs.DeleteKey(HealthKey);
        PlayerPrefs.DeleteKey(MaxHealthKey);
        PlayerPrefs.Save();
    }

    private static Transform ResolvePlayerTransform()
    {
        PlayerController playerController = UnityEngine.Object.FindObjectOfType<PlayerController>(true);
        return playerController != null ? playerController.transform : null;
    }
}
