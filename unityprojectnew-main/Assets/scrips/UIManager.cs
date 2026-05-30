using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Panelleri")]
    public GameObject deathMenu;
    public Image bloodOverlay; // Bu senin can�n %25 alt�na inince yan�p s�nen panelin
    public Image hitOverlay;   // YEN�: Bu sadece hasar al�nca anl�k fla� yapacak olan panel
    public CyberDamageOverlay cyberDamageOverlay;
    public LiquidCrystalLeak liquidCrystalLeak;


    [Header("Bar G�rselleri")]
    public Image healthFill;
    public Image ultFill;

    [Header("De�erler")]
    public float currentHealth = 100f;
    public float maxHealth = 100f;
    public float currentUlt = 0f;
    public float maxUlt = 100f;

    [Header("Ulti Bağlantısı")]
    public PlayerController ultimateController;
    public bool useAbilityCooldownForUlt = true;
    public bool allowUltDebugKeys = false;

    public float bloodFlashSpeed = 1f;
    public float ultFlashSpeed = 8f;
    public float hitFlashSpeed = 10f; // Fla��n s�nme h�z�

    [Header("UI Yaz�lar�")]
    public GameObject ultReadyText;

    [Header("Karakter Ayarlar�")]
    public GameObject playerModel;
    public MonoBehaviour movementScript;
    public float deathCameraLift = 5f;

    private bool isDead = false;
    private bool lowHealthWarningShown;

    private RectTransform healthFillRect;
    private float healthFillMaxWidth;


void Start()
    {
        Time.timeScale = 1f;
        if (deathMenu != null) deathMenu.SetActive(false);
        if (bloodOverlay != null) bloodOverlay.gameObject.SetActive(false);
        if (hitOverlay != null) hitOverlay.color = Color.clear;
        ResolveCyberDamageOverlay();
        ResolveLiquidCrystalLeak();
        if (ultReadyText != null) ultReadyText.SetActive(false);

        ResolveUltimateController();
        InitializeHealthBar();
        UpdateUltBar();
    }

void Update()
    {
        if (hitOverlay != null && hitOverlay.color.a > 0)
        {
            hitOverlay.color = Color.Lerp(hitOverlay.color, Color.clear, hitFlashSpeed * Time.deltaTime);
        }

        if (isDead) return;

        if (Input.GetKey(KeyCode.I)) currentHealth += 50f * Time.deltaTime;

        if (Input.GetKey(KeyCode.K))
        {
            currentHealth -= 50f * Time.deltaTime;
            TriggerHitFlash();
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();
        UpdateUltBar();
        HandleHealthEffects();
    }

void TriggerHitFlash()
    {
        if (cyberDamageOverlay == null) ResolveCyberDamageOverlay();
        if (liquidCrystalLeak == null) ResolveLiquidCrystalLeak();

        if (cyberDamageOverlay != null)
        {
            if (currentHealth <= maxHealth * 0.25f)
            {
                cyberDamageOverlay.TriggerCriticalHealth();
            }
            else
            {
                cyberDamageOverlay.Trigger();
            }
        }

        // Sıvı Kristal sızıntı efekti
        if (liquidCrystalLeak != null)
        {
            if (currentHealth <= maxHealth * 0.25f)
            {
                liquidCrystalLeak.TriggerCriticalLeak();
            }
            else
            {
                liquidCrystalLeak.TriggerLeak();
            }
            return;
        }

        if (hitOverlay != null)
        {
            hitOverlay.color = new Color(0f, 0.9f, 1f, 0.28f);
        }
    }

void HandleHealthEffects()
    {
        if (currentHealth <= 0 && !isDead)
        {
            Die();
            return;
        }

        bool isLowHealth = currentHealth <= (maxHealth * 0.25f) && !isDead;
        if (isLowHealth)
        {
            if (bloodOverlay != null) bloodOverlay.gameObject.SetActive(false);

            if (!lowHealthWarningShown)
            {
                if (cyberDamageOverlay == null) ResolveCyberDamageOverlay();
                if (cyberDamageOverlay != null) cyberDamageOverlay.TriggerCriticalHealth();
                lowHealthWarningShown = true;
            }
        }
        else
        {
            lowHealthWarningShown = false;
            if (bloodOverlay != null) bloodOverlay.gameObject.SetActive(false);
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (movementScript != null) movementScript.enabled = false;
        Animator anim = playerModel.GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;
        CharacterController cc = playerModel.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        if (bloodOverlay != null)
        {
            bloodOverlay.gameObject.SetActive(true);
            bloodOverlay.color = new Color(0.2f, 0f, 0f, 0.8f);
        }

        if (Camera.main != null)
        {
            MonoBehaviour[] camScripts = Camera.main.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in camScripts) script.enabled = false;
            StartCoroutine(AnimateDeathCamera());
        }

        Invoke("ShowDeathMenu", 1.2f);
    }

    System.Collections.IEnumerator AnimateDeathCamera()
    {
        Transform camTrans = Camera.main.transform;
        Vector3 startPos = camTrans.position;
        Vector3 endPos = camTrans.position + new Vector3(0, deathCameraLift, 0);
        Quaternion startRot = camTrans.rotation;
        Quaternion endRot = Quaternion.LookRotation(playerModel.transform.position - endPos);

        float elapsed = 0f;
        float duration = 1.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            camTrans.position = Vector3.Lerp(startPos, endPos, percent);
            camTrans.rotation = Quaternion.Lerp(startRot, endRot, percent);
            yield return null;
        }
    }

    void ShowDeathMenu()
    {
        if (deathMenu != null) deathMenu.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); }


    void UpdateHealthBar()
    {
        if (healthFill == null) return;

        float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        healthPercent = Mathf.Clamp01(healthPercent);

        if (healthFillRect != null && healthFillMaxWidth > 0f)
        {
            Vector2 size = healthFillRect.sizeDelta;
            size.x = healthFillMaxWidth * healthPercent;
            healthFillRect.sizeDelta = size;
        }
        else
        {
            healthFill.fillAmount = healthPercent;
        }
    }


void InitializeHealthBar()
    {
        if (healthFill == null) return;

        healthFill.type = Image.Type.Simple;
        healthFill.fillAmount = 1f;
        healthFillRect = healthFill.rectTransform;
        healthFillRect.pivot = new Vector2(0f, healthFillRect.pivot.y);
        healthFillMaxWidth = healthFillRect.sizeDelta.x;
        UpdateHealthBar();
    }


void ResolveUltimateController()
    {
        if (ultimateController != null) return;

        ultimateController = movementScript as PlayerController;
        if (ultimateController == null && playerModel != null)
        {
            ultimateController = playerModel.GetComponentInChildren<PlayerController>();
        }
    }

    void UpdateUltBar()
    {
        if (ultFill == null) return;

        if (useAbilityCooldownForUlt)
        {
            if (ultimateController == null) ResolveUltimateController();
            if (ultimateController != null)
            {
                currentUlt = ultimateController.GetUltimateCharge01() * maxUlt;
            }
        }
        else if (allowUltDebugKeys)
        {
            if (Input.GetKey(KeyCode.O)) currentUlt += 50f * Time.deltaTime;
            if (Input.GetKey(KeyCode.L)) currentUlt -= 50f * Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.E) && currentUlt >= maxUlt) currentUlt = 0f;
        }

        currentUlt = Mathf.Clamp(currentUlt, 0, maxUlt);
        float visualUlt = maxUlt > 0f ? currentUlt / maxUlt : 0f;
        ultFill.fillAmount = Mathf.Clamp01(visualUlt);

        if (currentUlt >= maxUlt)
        {
            if (ultReadyText != null) ultReadyText.SetActive(true);
            float lerpTime = Mathf.PingPong(Time.time * ultFlashSpeed, 1f);
            Color ultNormal = Color.white;
            Color ultGlow = new Color(0.55f, 1f, 0.35f, 1f);
            ultFill.color = Color.Lerp(ultNormal, ultGlow, lerpTime);
        }
        else
        {
            if (ultReadyText != null) ultReadyText.SetActive(false);
            ultFill.color = Color.white;
        }
    }


void ResolveCyberDamageOverlay()
    {
        if (cyberDamageOverlay != null) return;

        if (hitOverlay != null)
        {
            cyberDamageOverlay = hitOverlay.GetComponentInParent<CyberDamageOverlay>();
        }

        if (cyberDamageOverlay == null)
        {
            cyberDamageOverlay = GetComponentInParent<CyberDamageOverlay>();
        }
    }

    void ResolveLiquidCrystalLeak()
    {
        if (liquidCrystalLeak != null) return;

        if (hitOverlay != null)
        {
            liquidCrystalLeak = hitOverlay.GetComponentInParent<LiquidCrystalLeak>();
        }

        if (liquidCrystalLeak == null)
        {
            liquidCrystalLeak = GetComponentInParent<LiquidCrystalLeak>();
        }
    }
}