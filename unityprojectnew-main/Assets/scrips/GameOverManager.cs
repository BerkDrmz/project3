using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class GameOverManager : MonoBehaviour
{
    [Header("UI Elements")]
    public CanvasGroup gameOverCanvasGroup;
    public TMP_Text mainTitleText; // SYSTEM FAILURE
    public TMP_Text subTitleText1; // NEURAL LINK SEVERED
    public TMP_Text subTitleText2; // CRITICAL CORE DEPLETION

    [Header("Buttons")]
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Menu Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Glitch / Flicker Settings")]
    public float minFlickerInterval = 0.05f;
    public float maxFlickerInterval = 0.15f;
    public float glitchPauseDuration = 2.5f;

    [Header("Neon Button Settings")]
    public Color normalBorderColor = new Color(1f, 0f, 0f, 0.4f); // Yarı saydam siberpunk kırmızı
    public Color glowBorderColor = new Color(1f, 0.1f, 0.1f, 1f); // Parlak kırmızı neon glow

    private void Start()
    {
        // Başlangıçta metinler için Flicker / Glitch Coroutine'lerini başlat
        if (mainTitleText != null) StartCoroutine(TextGlitchEffect(mainTitleText));
        if (subTitleText1 != null) StartCoroutine(TextGlitchEffect(subTitleText1));
        if (subTitleText2 != null) StartCoroutine(TextGlitchEffect(subTitleText2));

        // Butonların dinamik Neon Glow (Pointer Enter/Exit) olaylarını kur
        SetupNeonButton(restartButton, OnRestartClicked);
        SetupNeonButton(mainMenuButton, OnMainMenuClicked);

        // Ekranın siberpunk atmosferine uygun şekilde yavaşça belirmesi için
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            StartCoroutine(FadeInCanvas(gameOverCanvasGroup, 1.2f));
        }
    }

    private IEnumerator FadeInCanvas(CanvasGroup cg, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    #region Coroutine - Glitch & Flicker Effect
    private IEnumerator TextGlitchEffect(TMP_Text targetText)
    {
        Color originalColor = targetText.color;
        Color dimmedColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.15f);

        while (true)
        {
            // Rastgele aralıklarla stabil kalma süresi
            yield return new WaitForSecondsRealtime(Random.Range(1f, glitchPauseDuration));

            // Hızlı Glitch (Flicker) anı
            int flickerCount = Random.Range(2, 5);
            for (int i = 0; i < flickerCount; i++)
            {
                // Işığı/Glow'u kıs
                targetText.color = dimmedColor;
                yield return new WaitForSecondsRealtime(Random.Range(minFlickerInterval, maxFlickerInterval));

                // Işığı/Glow'u geri aç
                targetText.color = originalColor;
                yield return new WaitForSecondsRealtime(Random.Range(minFlickerInterval, maxFlickerInterval));
            }
        }
    }
    #endregion

    #region Button Neon Setup & Actions
    private void SetupNeonButton(Button button, UnityEngine.Events.UnityAction clickAction)
    {
        if (button == null) return;

        // Butona tıklama işlevini ekle
        button.onClick.AddListener(clickAction);

        // Kenarlık görselini al (Outlined Image)
        Image borderImage = button.GetComponent<Image>();
        if (borderImage != null) borderImage.color = normalBorderColor;

        // Mouse (Pointer) etkileşimleri için EventTrigger bileşeni
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

        // Fare Üzerine Geldiğinde (Hover / Glow)
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => {
            if (borderImage != null) StartCoroutine(ColorShiftBorder(borderImage, glowBorderColor, 0.1f));
            
            // Buton içindeki metne de parlama efekti ver
            TMP_Text btnText = button.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.color = Color.white; // Yazıyı daha belirgin yap
        });
        trigger.triggers.Add(entryEnter);

        // Fare Üzerinden Çıktığında (Normal State)
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => {
            if (borderImage != null) StartCoroutine(ColorShiftBorder(borderImage, normalBorderColor, 0.25f));
            
            TMP_Text btnText = button.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.color = new Color(1f, 0.8f, 0.8f); // Hafif kızıl/soluk tona dön
        });
        trigger.triggers.Add(entryExit);
    }

    private IEnumerator ColorShiftBorder(Image targetImage, Color targetColor, float duration)
    {
        Color startColor = targetImage.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            targetImage.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            yield return null;
        }
        targetImage.color = targetColor;
    }

    public void OnRestartClicked()
    {
        Debug.Log("[System] Neural Restart Initiated...");
        Time.timeScale = 1f; // Zaman akışını normale döndür
        // Mevcut aktif sahneyi yeniden yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnMainMenuClicked()
    {
        Debug.Log("[System] Link Severed. Returning to Main Menu...");
        Time.timeScale = 1f; // Zaman akışını normale döndür
        // Ana Menü sahnesini yükle
        SceneManager.LoadScene(string.IsNullOrEmpty(mainMenuSceneName) ? "MainMenu" : mainMenuSceneName);
    }
    #endregion
}
