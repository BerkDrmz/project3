using UnityEngine;
using UnityEngine.UI;

public class HealthBarController : MonoBehaviour
{
    [Header("UI Referansları")]
    [Tooltip("Üstteki Ana Can Barının dolumunu gösteren Image bileşeni")]
    [SerializeField] private Image hpFillImage;
    
    [Tooltip("Alttaki Flair (Örn: Ekstra Enerji/Overdrive) dolumunu gösteren Image bileşeni")]
    [SerializeField] private Image flairFillImage;

    [Header("Animasyon Ayarları")]
    [Tooltip("Can barının hedefe ulaşma hızı")]
    [SerializeField] private float hpLerpSpeed = 8f;
    
    [Tooltip("Flair barının hedefe ulaşma hızı")]
    [SerializeField] private float flairLerpSpeed = 8f;

    // Hedef doluluk oranları (0.0f ile 1.0f arasında)
    private float _targetHpFill = 1f;
    private float _targetFlairFill = 1f;

    private void Start()
    {
        // Başlangıçta Image bileşenlerinin doğru yapılandırıldığından emin olalım
        ValidateImageSettings(hpFillImage);
        ValidateImageSettings(flairFillImage);

        // Başlangıç doluluk oranlarını mevcut fillAmount değerinden alalım
        if (hpFillImage != null) _targetHpFill = hpFillImage.fillAmount;
        if (flairFillImage != null) _targetFlairFill = flairFillImage.fillAmount;
    }

    private void Update()
    {
        // Ana HP Barını pürüzsüz (smooth) bir şekilde hedefe doğru güncelle
        if (hpFillImage != null && Mathf.Abs(hpFillImage.fillAmount - _targetHpFill) > 0.001f)
        {
            hpFillImage.fillAmount = Mathf.Lerp(hpFillImage.fillAmount, _targetHpFill, Time.deltaTime * hpLerpSpeed);
        }

        // Flair Barını pürüzsüz (smooth) bir şekilde hedefe doğru güncelle
        if (flairFillImage != null && Mathf.Abs(flairFillImage.fillAmount - _targetFlairFill) > 0.001f)
        {
            flairFillImage.fillAmount = Mathf.Lerp(flairFillImage.fillAmount, _targetFlairFill, Time.deltaTime * flairLerpSpeed);
        }
    }

    /// <summary>
    /// Ana HP Barının hedef doluluk oranını günceller.
    /// </summary>
    /// <param name="percentage">0.0f ile 1.0f arasında bir değer</param>
    public void SetHealthFill(float percentage)
    {
        _targetHpFill = Mathf.Clamp01(percentage);
    }

    /// <summary>
    /// Alttaki Flair Barının hedef doluluk oranını günceller.
    /// </summary>
    /// <param name="percentage">0.0f ile 1.0f arasında bir değer</param>
    public void SetFlairFill(float percentage)
    {
        _targetFlairFill = Mathf.Clamp01(percentage);
    }

    /// <summary>
    /// Image bileşenlerinin Filled ve Horizontal yapıda olup olmadığını kontrol eder ve otomatik olarak ayarlar.
    /// </summary>
    private void ValidateImageSettings(Image img)
    {
        if (img != null)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
}
