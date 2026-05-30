using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class CyberDamageOverlay : MonoBehaviour
{
    [Header("Timing")]
    public float duration = 0.38f;
    public float noiseRefreshRate = 0.03f;
    public float jitterPixels = 18f;

    [Header("Look")]
    public int glitchBarCount = 14;
    public Color cyanGlitch = new Color(0f, 0.95f, 1f, 1f);
    public Color magentaGlitch = new Color(1f, 0.05f, 0.55f, 1f);
    public Color warningColor = new Color(1f, 0.42f, 0.05f, 1f);
    public float criticalWarningDuration = 1.35f;

    private CanvasGroup group;
    private RectTransform root;
    private Image tintImage;
    private Image noiseImage;
    private Image[] glitchBars;
    private TextMeshProUGUI warningText;
    private Texture2D noiseTexture;
    private Color32[] noisePixels;
    private float timer;
    private float noiseTimer;
    private float intensity = 1f;
    private Vector2 warningBasePosition = new Vector2(-46f, 54f);

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        BuildOverlay();
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (noiseTexture != null)
        {
            Destroy(noiseTexture);
        }
    }

    private void EnsureBuilt()
    {
        if (group == null)
        {
            group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.interactable = false;
            group.blocksRaycasts = false;
        }

        if (root == null || warningText == null || noiseImage == null || glitchBars == null)
        {
            BuildOverlay();
        }
    }

    private void Update()
    {
        if (timer <= 0f)
        {
            if (group.alpha > 0f)
            {
                HideImmediate();
            }

            return;
        }

        timer -= Time.unscaledDeltaTime;
        float normalized = Mathf.Clamp01(timer / duration);
        float pulse = Mathf.Sin(Time.unscaledTime * 80f) * 0.5f + 0.5f;
        group.alpha = Mathf.Clamp01((normalized * 0.75f + pulse * 0.25f) * intensity);

        root.anchoredPosition = new Vector2(
            Random.Range(-jitterPixels, jitterPixels) * intensity * normalized,
            Random.Range(-jitterPixels * 0.35f, jitterPixels * 0.35f) * intensity * normalized
        );

        noiseTimer -= Time.unscaledDeltaTime;
        if (noiseTimer <= 0f)
        {
            noiseTimer = noiseRefreshRate;
            RefreshNoise(normalized);
            RefreshGlitchBars(normalized);
        }

        if (warningText != null)
        {
            warningText.alpha = Mathf.Clamp01(0.35f + pulse * 0.65f);
            warningText.rectTransform.anchoredPosition = warningBasePosition + new Vector2(
                Random.Range(-10f, 10f) * normalized,
                Random.Range(-6f, 6f) * normalized
            );
        }
    }

    public void Trigger()
    {
        Trigger(1f);
    }

    public void Trigger(float strength)
    {
        EnsureBuilt();
        ConfigureWarningText("NEURAL FEEDBACK ERROR", 26f, new Vector2(520f, 60f), warningColor);
        intensity = Mathf.Clamp01(strength);
        timer = duration;
        noiseTimer = 0f;
        root.gameObject.SetActive(true);
        group.alpha = intensity;
    }

    public void TriggerCriticalHealth()
    {
        EnsureBuilt();
        ConfigureWarningText("SYSTEM ERROR\nVITALS CRITICAL\nNEURAL LINK UNSTABLE", 30f, new Vector2(650f, 128f), warningColor);
        intensity = 0.9f;
        timer = criticalWarningDuration;
        noiseTimer = 0f;
        root.gameObject.SetActive(true);
        group.alpha = intensity;
    }

    private void ConfigureWarningText(string message, float fontSize, Vector2 size, Color color)
    {
        if (warningText == null) return;

        RectTransform textRt = warningText.rectTransform;
        warningBasePosition = new Vector2(-46f, 54f);
        textRt.anchorMin = new Vector2(1f, 0f);
        textRt.anchorMax = new Vector2(1f, 0f);
        textRt.pivot = new Vector2(1f, 0f);
        textRt.anchoredPosition = warningBasePosition;
        textRt.sizeDelta = size;

        warningText.text = message;
        warningText.fontSize = fontSize;
        warningText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        warningText.alignment = TextAlignmentOptions.BottomRight;
        warningText.color = color;
        warningText.raycastTarget = false;
        warningText.enableWordWrapping = false;
        warningText.outlineColor = Color.black;
        warningText.outlineWidth = 0.18f;
    }

    private void BuildOverlay()
    {
        root = GetOrCreateRect("CyberDamage_GlitchRoot", transform);
        Stretch(root);

        tintImage = GetOrCreateImage("CyberDamage_Tint", root.transform);
        Stretch(tintImage.rectTransform);
        tintImage.sprite = null;
        tintImage.color = new Color(0.02f, 0.0f, 0.025f, 0.28f);
        tintImage.raycastTarget = false;

        noiseTexture = new Texture2D(128, 72, TextureFormat.RGBA32, false);
        noiseTexture.filterMode = FilterMode.Point;
        noiseTexture.wrapMode = TextureWrapMode.Repeat;
        noisePixels = new Color32[noiseTexture.width * noiseTexture.height];
        Sprite noiseSprite = Sprite.Create(
            noiseTexture,
            new Rect(0, 0, noiseTexture.width, noiseTexture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        noiseImage = GetOrCreateImage("CyberDamage_StaticNoise", root.transform);
        Stretch(noiseImage.rectTransform);
        noiseImage.sprite = noiseSprite;
        noiseImage.type = Image.Type.Simple;
        noiseImage.color = new Color(0.7f, 1f, 1f, 0.45f);
        noiseImage.raycastTarget = false;

        glitchBars = new Image[Mathf.Max(1, glitchBarCount)];
        for (int i = 0; i < glitchBars.Length; i++)
        {
            Image bar = GetOrCreateImage("CyberDamage_Bar_" + i.ToString("00"), root.transform);
            RectTransform rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 4f);
            bar.sprite = null;
            bar.raycastTarget = false;
            glitchBars[i] = bar;
        }

        warningText = GetOrCreateText("CyberDamage_Warning", root.transform);
        RectTransform textRt = warningText.rectTransform;
        textRt.anchorMin = new Vector2(1f, 0f);
        textRt.anchorMax = new Vector2(1f, 0f);
        textRt.pivot = new Vector2(1f, 0f);
        textRt.anchoredPosition = new Vector2(-46f, 54f);
        textRt.sizeDelta = new Vector2(520f, 60f);
        warningText.text = "NEURAL FEEDBACK ERROR";
        warningText.fontSize = 26f;
        warningText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        warningText.alignment = TextAlignmentOptions.BottomRight;
        warningText.color = warningColor;
        warningText.raycastTarget = false;
        warningText.enableWordWrapping = false;
        warningText.outlineColor = Color.black;
        warningText.outlineWidth = 0.18f;
    }

    private void RefreshNoise(float normalized)
    {
        byte alphaBase = (byte)Mathf.RoundToInt(Mathf.Lerp(5f, 95f, normalized * intensity));
        for (int i = 0; i < noisePixels.Length; i++)
        {
            bool bright = Random.value > 0.72f;
            byte value = bright ? (byte)Random.Range(155, 255) : (byte)Random.Range(0, 70);
            byte alpha = bright ? alphaBase : (byte)Mathf.RoundToInt(alphaBase * 0.35f);

            if (Random.value > 0.5f)
            {
                noisePixels[i] = new Color32(0, value, 255, alpha);
            }
            else
            {
                noisePixels[i] = new Color32(value, 0, 120, alpha);
            }
        }

        noiseTexture.SetPixels32(noisePixels);
        noiseTexture.Apply(false);
    }

    private void RefreshGlitchBars(float normalized)
    {
        for (int i = 0; i < glitchBars.Length; i++)
        {
            Image bar = glitchBars[i];
            RectTransform rt = bar.rectTransform;
            bool visible = Random.value < Mathf.Lerp(0.25f, 0.9f, normalized);
            bar.enabled = visible;
            if (!visible) continue;

            float height = Random.Range(2f, 16f) * intensity;
            rt.anchoredPosition = new Vector2(Random.Range(-90f, 90f), Random.Range(-470f, 470f));
            rt.sizeDelta = new Vector2(Random.Range(-420f, 240f), height);

            Color c = Random.value > 0.5f ? cyanGlitch : magentaGlitch;
            c.a = Random.Range(0.18f, 0.72f) * normalized * intensity;
            bar.color = c;
        }
    }

    private void HideImmediate()
    {
        timer = 0f;
        group.alpha = 0f;
        if (root != null)
        {
            root.anchoredPosition = Vector2.zero;
            root.gameObject.SetActive(false);
        }
    }

    private static RectTransform GetOrCreateRect(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (existing == null)
        {
            go.transform.SetParent(parent, false);
        }

        return go.GetComponent<RectTransform>();
    }

    private static Image GetOrCreateImage(string name, Transform parent)
    {
        RectTransform rt = GetOrCreateRect(name, parent);
        Image image = rt.GetComponent<Image>();
        if (image == null)
        {
            image = rt.gameObject.AddComponent<Image>();
        }

        return image;
    }

    private static TextMeshProUGUI GetOrCreateText(string name, Transform parent)
    {
        RectTransform rt = GetOrCreateRect(name, parent);
        TextMeshProUGUI text = rt.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        }

        return text;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
