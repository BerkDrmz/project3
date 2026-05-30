using UnityEngine;
using UnityEngine.UI;

public class NeuroChargeBar : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("The Image component acting as the filled bar")]
    public Image fillImage;
    
    [Tooltip("Smoothing speed for the fill animation")]
    public float lerpSpeed = 5f;

    private float targetFillAmount = 1f;

    private void Start()
    {
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    private void Update()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, Time.deltaTime * lerpSpeed);
        }
    }

    /// <summary>
    /// Updates the target fill amount of the bar (0.0 to 1.0).
    /// </summary>
    /// <param name="percentage">Value between 0 and 1.</param>
    public void UpdateValue(float percentage)
    {
        targetFillAmount = Mathf.Clamp01(percentage);
    }
}
