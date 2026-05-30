using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class LiquidCrystalLeak : MonoBehaviour
{
    [Header("Timing")]
    public float leakDuration = 2.8f;

    [Header("Leak Colors")]
    public Color deepPurple = new Color(0.22f, 0.0f, 0.42f, 1f);
    public Color darkNavy = new Color(0.05f, 0.02f, 0.35f, 1f);
    public Color inkBleed = new Color(0.12f, 0.0f, 0.28f, 0.9f);

    [Header("Lightning")]
    public int boltCount = 5;
    public int segmentsPerBolt = 10;
    public int branchSegments = 4;
    public Color boltColorCyan = new Color(0.5f, 0.95f, 1f, 1f);
    public Color boltColorPurple = new Color(0.85f, 0.35f, 1f, 1f);
    public float boltFlashInterval = 0.15f;

    [Header("Center Text")]
    public string[] damageMessages = new string[]
    {
        "DISPLAY FRACTURE",
        "LCD BREACH",
        "PIXEL HEMORRHAGE",
        "SCREEN INTEGRITY LOST",
        "VISUAL CORE DAMAGED"
    };
    public Color textColor = new Color(0.75f, 0.25f, 1f, 1f);
    public float textFontSize = 44f;

    private CanvasGroup cg;
    private RectTransform rootRect;
    private Image[] cornerLeaks = new Image[4];
    private Image[] edgeDrips = new Image[4];
    private Image screenTint;
    private TextMeshProUGUI centerText;
    private TextMeshProUGUI subText;
    private Texture2D leakTex;
    private float timer;
    private float maxTimer;
    private bool built;

    // Lightning bolts
    private RectTransform boltContainer;
    private float[] boltLifetimes;
    private Texture2D boltGlowTex;
    private Texture2D boltCoreTex;
    private Image boltFlashImg;

    // 3 layers per bolt: [bolt][segment] x glow/mid/core
    private Image[][] boltGlowLayer;
    private Image[][] boltMidLayer;
    private Image[][] boltCoreLayer;
    // branch arms
    private Image[][] branchGlowLayer;
    private Image[][] branchCoreLayer;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        Build();
        rootRect.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (leakTex != null) Destroy(leakTex);
        if (boltGlowTex != null) Destroy(boltGlowTex);
        if (boltCoreTex != null) Destroy(boltCoreTex);
    }

    void Update()
    {
        if (timer <= 0f)
        {
            if (cg != null && cg.alpha > 0f)
            {
                cg.alpha = 0f;
                if (rootRect != null) rootRect.gameObject.SetActive(false);
            }
            return;
        }

        timer -= Time.unscaledDeltaTime;
        float progress = 1f - Mathf.Clamp01(timer / maxTimer);

        // --- MASTER ALPHA ---
        float alpha;
        if (progress < 0.1f)
            alpha = Mathf.SmoothStep(0f, 1f, progress / 0.1f);
        else if (progress > 0.65f)
            alpha = Mathf.SmoothStep(1f, 0f, (progress - 0.65f) / 0.35f);
        else
            alpha = 1f;

        cg.alpha = Mathf.Clamp01(alpha);

        // --- SCREEN TINT (dark purple wash) ---
        if (screenTint != null)
        {
            Color tc = deepPurple;
            tc.a = 0.35f * alpha;
            screenTint.color = tc;
        }

        // --- CORNER LEAKS: grow from corners ---
        float grow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress * 2f));
        float cornerSize = Mathf.Lerp(0.15f, 0.6f, grow);

        // TL
        SetAnchors(cornerLeaks[0], 0f, 1f - cornerSize, cornerSize, 1f);
        // TR
        SetAnchors(cornerLeaks[1], 1f - cornerSize, 1f - cornerSize, 1f, 1f);
        // BL
        SetAnchors(cornerLeaks[2], 0f, 0f, cornerSize, cornerSize);
        // BR
        SetAnchors(cornerLeaks[3], 1f - cornerSize, 0f, 1f, cornerSize);

        // Corner alpha
        for (int i = 0; i < 4; i++)
        {
            if (cornerLeaks[i] == null) continue;
            Color c = cornerLeaks[i].color;
            c.a = grow * alpha;
            cornerLeaks[i].color = c;
        }

        // --- EDGE DRIPS ---
        float dripA = Mathf.Clamp01(progress * 1.8f) * 0.7f * alpha;
        for (int i = 0; i < 4; i++)
        {
            if (edgeDrips[i] == null) continue;
            Color c = edgeDrips[i].color;
            c.a = dripA;
            edgeDrips[i].color = c;
        }

        // --- LIGHTNING BOLTS ---
        UpdateBolts(alpha, progress);

        // --- TEXT ---
        if (centerText != null)
        {
            float ta = alpha * Mathf.Clamp01(progress * 5f);
            centerText.alpha = ta;
            // Glitch jitter
            float jitter = ta * 3f;
            centerText.rectTransform.anchoredPosition = new Vector2(
                Random.Range(-jitter, jitter),
                20f + Random.Range(-jitter * 0.5f, jitter * 0.5f)
            );
        }
        if (subText != null)
        {
            subText.alpha = alpha * Mathf.Clamp01((progress - 0.08f) * 4f);
        }
    }

    public void TriggerLeak()
    {
        TriggerLeak(1f);
    }

    public void TriggerLeak(float strength)
    {
        if (!built) Build();
        maxTimer = leakDuration * Mathf.Clamp(strength, 0.6f, 1.5f);
        timer = maxTimer;
        rootRect.gameObject.SetActive(true);
        cg.alpha = 0.01f;

        if (centerText != null && damageMessages.Length > 0)
            centerText.text = damageMessages[Random.Range(0, damageMessages.Length)];
        if (subText != null)
            subText.text = "// LIQUID CRYSTAL BREACH DETECTED";
        if (centerText != null)
            centerText.fontSize = textFontSize;

        ShuffleColors();
    }

    public void TriggerCriticalLeak()
    {
        if (!built) Build();
        maxTimer = leakDuration * 2.2f;
        timer = maxTimer;
        rootRect.gameObject.SetActive(true);
        cg.alpha = 0.01f;

        if (centerText != null)
        {
            centerText.text = "CRITICAL DISPLAY FAILURE";
            centerText.fontSize = textFontSize * 1.2f;
        }
        if (subText != null)
            subText.text = "// SCREEN MATRIX COMPROMISED\n// VISUAL OUTPUT UNSTABLE";

        ShuffleColors();
    }

    // ==================== BUILD ====================

    void Build()
    {
        if (cg == null)
        {
            cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        rootRect = MkRect("LCD_Root", transform);
        Stretch(rootRect);

        // Procedural leak texture
        leakTex = MakeLeakTex(128, 128);
        Sprite spr = Sprite.Create(leakTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));

        // Full screen dark tint
        screenTint = MkImg("LCD_Tint", rootRect);
        Stretch(screenTint.rectTransform);
        screenTint.sprite = null;
        screenTint.color = new Color(0.08f, 0f, 0.15f, 0f);
        screenTint.raycastTarget = false;

        // 4 corner leak images
        cornerLeaks[0] = MkImg("LCD_TL", rootRect);
        cornerLeaks[0].sprite = spr;
        cornerLeaks[0].color = deepPurple;
        cornerLeaks[0].raycastTarget = false;

        cornerLeaks[1] = MkImg("LCD_TR", rootRect);
        cornerLeaks[1].sprite = spr;
        cornerLeaks[1].color = darkNavy;
        cornerLeaks[1].raycastTarget = false;
        cornerLeaks[1].rectTransform.localScale = new Vector3(-1, 1, 1);

        cornerLeaks[2] = MkImg("LCD_BL", rootRect);
        cornerLeaks[2].sprite = spr;
        cornerLeaks[2].color = darkNavy;
        cornerLeaks[2].raycastTarget = false;
        cornerLeaks[2].rectTransform.localScale = new Vector3(1, -1, 1);

        cornerLeaks[3] = MkImg("LCD_BR", rootRect);
        cornerLeaks[3].sprite = spr;
        cornerLeaks[3].color = deepPurple;
        cornerLeaks[3].raycastTarget = false;
        cornerLeaks[3].rectTransform.localScale = new Vector3(-1, -1, 1);

        // 4 edge drip strips
        edgeDrips[0] = MkDrip("LCD_EL", rootRect, 0f, 0.1f, 0.04f, 0.9f, deepPurple);
        edgeDrips[1] = MkDrip("LCD_ER", rootRect, 0.96f, 0.1f, 1f, 0.9f, darkNavy);
        edgeDrips[2] = MkDrip("LCD_ET", rootRect, 0.1f, 0.96f, 0.9f, 1f, inkBleed);
        edgeDrips[3] = MkDrip("LCD_EB", rootRect, 0.1f, 0f, 0.9f, 0.04f, inkBleed);

        // Center text
        centerText = MkTxt("LCD_CText", rootRect);
        RectTransform trt = centerText.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 20f);
        trt.sizeDelta = new Vector2(1000f, 120f);
        centerText.text = "DISPLAY FRACTURE";
        centerText.fontSize = textFontSize;
        centerText.fontStyle = FontStyles.Bold;
        centerText.alignment = TextAlignmentOptions.Center;
        centerText.color = textColor;
        centerText.raycastTarget = false;
        centerText.enableWordWrapping = false;
        centerText.outlineColor = new Color32(50, 0, 100, 230);
        centerText.outlineWidth = 0.28f;

        // Sub text
        subText = MkTxt("LCD_SText", rootRect);
        RectTransform srt = subText.rectTransform;
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = new Vector2(0f, -30f);
        srt.sizeDelta = new Vector2(900f, 70f);
        subText.text = "// LIQUID CRYSTAL BREACH DETECTED";
        subText.fontSize = 22f;
        subText.fontStyle = FontStyles.Italic;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = new Color(0.5f, 0.25f, 0.8f, 0.95f);
        subText.raycastTarget = false;
        subText.enableWordWrapping = true;
        subText.outlineColor = new Color32(20, 0, 50, 200);
        subText.outlineWidth = 0.2f;

        // Lightning bolts container
        BuildBolts();

        built = true;
    }

    // ==================== HELPERS ====================

    void SetAnchors(Image img, float minX, float minY, float maxX, float maxY)
    {
        if (img == null) return;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    // ==================== LIGHTNING ====================

    Texture2D MakeBoltTex(int w, int h, bool isCore)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[w * h];
        float cy = h * 0.5f;
        for (int y = 0; y < h; y++)
        {
            float dist = Mathf.Abs(y - cy) / cy; // 0 center, 1 edge
            float a;
            if (isCore)
                a = Mathf.Clamp01(1f - dist * dist); // sharp bright center
            else
                a = Mathf.Clamp01(1f - dist) * (1f - dist); // soft wide glow

            byte ab = (byte)(a * 255);
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color32(255, 255, 255, ab);
        }
        tex.SetPixels32(px);
        tex.Apply(false);
        return tex;
    }

    void BuildBolts()
    {
        boltContainer = MkRect("LCD_Bolts", rootRect);
        Stretch(boltContainer);

        // Procedural glow textures
        boltGlowTex = MakeBoltTex(4, 64, false);
        boltCoreTex = MakeBoltTex(4, 32, true);
        Sprite glowSpr = Sprite.Create(boltGlowTex, new Rect(0, 0, 4, 64), new Vector2(0f, 0.5f));
        Sprite coreSpr = Sprite.Create(boltCoreTex, new Rect(0, 0, 4, 32), new Vector2(0f, 0.5f));

        // Screen flash overlay
        boltFlashImg = MkImg("LCD_BoltFlash", rootRect);
        Stretch(boltFlashImg.rectTransform);
        boltFlashImg.sprite = null;
        boltFlashImg.color = Color.clear;
        boltFlashImg.raycastTarget = false;

        boltGlowLayer = new Image[boltCount][];
        boltMidLayer = new Image[boltCount][];
        boltCoreLayer = new Image[boltCount][];
        branchGlowLayer = new Image[boltCount][];
        branchCoreLayer = new Image[boltCount][];
        boltLifetimes = new float[boltCount];

        for (int b = 0; b < boltCount; b++)
        {
            boltGlowLayer[b] = new Image[segmentsPerBolt];
            boltMidLayer[b] = new Image[segmentsPerBolt];
            boltCoreLayer[b] = new Image[segmentsPerBolt];
            branchGlowLayer[b] = new Image[branchSegments];
            branchCoreLayer[b] = new Image[branchSegments];

            for (int s = 0; s < segmentsPerBolt; s++)
            {
                // Layer 1: Huge outer glow (very wide, soft)
                boltGlowLayer[b][s] = MkBoltSeg("BG_" + b + "_" + s, boltContainer, glowSpr);
                // Layer 2: Mid body
                boltMidLayer[b][s] = MkBoltSeg("BM_" + b + "_" + s, boltContainer, glowSpr);
                // Layer 3: Bright core
                boltCoreLayer[b][s] = MkBoltSeg("BC_" + b + "_" + s, boltContainer, coreSpr);
            }
            for (int s = 0; s < branchSegments; s++)
            {
                branchGlowLayer[b][s] = MkBoltSeg("BB_" + b + "_" + s, boltContainer, glowSpr);
                branchCoreLayer[b][s] = MkBoltSeg("BBc_" + b + "_" + s, boltContainer, coreSpr);
            }
            boltLifetimes[b] = Random.Range(0f, boltFlashInterval);
        }
    }

    Image MkBoltSeg(string n, RectTransform parent, Sprite spr)
    {
        Image img = MkImg(n, parent);
        img.sprite = spr;
        img.type = Image.Type.Sliced;
        img.color = Color.clear;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        img.gameObject.SetActive(false);
        return img;
    }

    void UpdateBolts(float alpha, float progress)
    {
        if (boltGlowLayer == null || boltContainer == null) return;

        // Fade flash
        if (boltFlashImg != null && boltFlashImg.color.a > 0.01f)
        {
            Color fc = boltFlashImg.color;
            fc.a = Mathf.Lerp(fc.a, 0f, Time.unscaledDeltaTime * 12f);
            if (fc.a < 0.01f) fc.a = 0f;
            boltFlashImg.color = fc;
        }

        for (int b = 0; b < boltCount; b++)
        {
            boltLifetimes[b] -= Time.unscaledDeltaTime;
            if (boltLifetimes[b] > 0f) continue;

            bool show = Random.value < 0.6f * alpha;
            if (show)
            {
                RandomizeBolt(b, alpha);
                boltLifetimes[b] = Random.Range(0.05f, boltFlashInterval);

                // Screen flash
                if (boltFlashImg != null)
                {
                    Color flashC = Random.value > 0.5f ? boltColorCyan : boltColorPurple;
                    flashC.a = Random.Range(0.06f, 0.15f) * alpha;
                    boltFlashImg.color = flashC;
                }
            }
            else
            {
                HideBolt(b);
                boltLifetimes[b] = Random.Range(0.05f, boltFlashInterval * 1.8f);
            }
        }
    }

    void HideBolt(int b)
    {
        for (int s = 0; s < segmentsPerBolt; s++)
        {
            if (boltGlowLayer[b][s] != null) boltGlowLayer[b][s].gameObject.SetActive(false);
            if (boltMidLayer[b][s] != null) boltMidLayer[b][s].gameObject.SetActive(false);
            if (boltCoreLayer[b][s] != null) boltCoreLayer[b][s].gameObject.SetActive(false);
        }
        for (int s = 0; s < branchSegments; s++)
        {
            if (branchGlowLayer[b][s] != null) branchGlowLayer[b][s].gameObject.SetActive(false);
            if (branchCoreLayer[b][s] != null) branchCoreLayer[b][s].gameObject.SetActive(false);
        }
    }

    void RandomizeBolt(int boltIndex, float alpha)
    {
        float cW = boltContainer.rect.width;
        float cH = boltContainer.rect.height;
        if (cW <= 0) cW = 1920f;
        if (cH <= 0) cH = 1080f;

        // Start from screen edge
        float startX, startY;
        int edge = Random.Range(0, 4);
        switch (edge)
        {
            case 0: startX = -cW * 0.48f; startY = Random.Range(-cH * 0.4f, cH * 0.4f); break;
            case 1: startX = cW * 0.48f; startY = Random.Range(-cH * 0.4f, cH * 0.4f); break;
            case 2: startX = Random.Range(-cW * 0.4f, cW * 0.4f); startY = cH * 0.48f; break;
            default: startX = Random.Range(-cW * 0.4f, cW * 0.4f); startY = -cH * 0.48f; break;
        }

        Color boltColor = Random.value > 0.45f ? boltColorCyan : boltColorPurple;
        Color whiteCore = new Color(0.95f, 0.97f, 1f, 1f);
        float baseAngle = Random.Range(0f, 360f);
        // Bias toward center
        float toCenterAngle = Mathf.Atan2(-startY, -startX) * Mathf.Rad2Deg;
        baseAngle = toCenterAngle + Random.Range(-40f, 40f);

        Vector2 pos = new Vector2(startX, startY);
        int branchFrom = Random.Range(2, Mathf.Max(3, segmentsPerBolt - 2));
        Vector2 branchPos = pos;
        float branchAngle = baseAngle;

        for (int s = 0; s < segmentsPerBolt; s++)
        {
            float segLen = Random.Range(60f, 180f);
            float angle = baseAngle + Random.Range(-45f, 45f);
            float rad = angle * Mathf.Deg2Rad;

            float glowW = Random.Range(70f, 120f);  // huge outer glow
            float midW = Random.Range(28f, 50f);    // mid body
            float coreW = Random.Range(8f, 18f);    // bright core

            // Outer glow
            PlaceSeg(boltGlowLayer[boltIndex][s], pos, segLen, glowW, angle, boltColor, 0.4f * alpha);
            // Mid
            PlaceSeg(boltMidLayer[boltIndex][s], pos, segLen, midW, angle, boltColor, 0.8f * alpha);
            // Core (white-ish)
            PlaceSeg(boltCoreLayer[boltIndex][s], pos, segLen, coreW, angle, whiteCore, 1f * alpha);

            if (s == branchFrom) { branchPos = pos; branchAngle = angle + Random.Range(-90f, 90f); }

            pos += new Vector2(Mathf.Cos(rad) * segLen, Mathf.Sin(rad) * segLen);
            baseAngle = angle + Random.Range(-55f, 55f);
        }

        // Branch arm
        Vector2 bPos = branchPos;
        for (int s = 0; s < branchSegments; s++)
        {
            float segLen = Random.Range(35f, 100f);
            float angle = branchAngle + Random.Range(-50f, 50f);
            float rad = angle * Mathf.Deg2Rad;

            PlaceSeg(branchGlowLayer[boltIndex][s], bPos, segLen, Random.Range(40f, 70f), angle, boltColor, 0.35f * alpha);
            PlaceSeg(branchCoreLayer[boltIndex][s], bPos, segLen, Random.Range(6f, 12f), angle, whiteCore, 0.9f * alpha);

            bPos += new Vector2(Mathf.Cos(rad) * segLen, Mathf.Sin(rad) * segLen);
            branchAngle = angle + Random.Range(-60f, 60f);
        }
    }

    void PlaceSeg(Image img, Vector2 pos, float len, float width, float angle, Color col, float a)
    {
        if (img == null) return;
        img.gameObject.SetActive(true);
        RectTransform rt = img.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(len, width);
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        Color c = col;
        c.a = a;
        img.color = c;
    }

    void ShuffleColors()
    {
        float s = Random.Range(-0.05f, 0.05f);
        if (cornerLeaks[0] != null) cornerLeaks[0].color = Hue(deepPurple, s);
        if (cornerLeaks[1] != null) cornerLeaks[1].color = Hue(darkNavy, -s);
        if (cornerLeaks[2] != null) cornerLeaks[2].color = Hue(darkNavy, s * 0.7f);
        if (cornerLeaks[3] != null) cornerLeaks[3].color = Hue(deepPurple, -s * 0.7f);
    }

    Color Hue(Color c, float shift)
    {
        float h, s, v;
        Color.RGBToHSV(c, out h, out s, out v);
        Color r = Color.HSVToRGB(Mathf.Repeat(h + shift, 1f), s, v);
        r.a = c.a;
        return r;
    }

    // ==================== TEXTURE GEN ====================

    Texture2D MakeLeakTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (float)x / w;
                float dy = 1f - (float)y / h;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Multi-octave noise for organic ink edges
                float n1 = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.4f;
                float n2 = Mathf.PerlinNoise(x * 0.2f + 50f, y * 0.2f + 50f) * 0.15f;
                float a = Mathf.Clamp01(1.1f - dist * 1.15f + n1 + n2);
                a = a * a; // Squared for ink spread

                // Deep purple-navy color
                byte r = (byte)(30 + a * 35);
                byte g = (byte)(2 + a * 8);
                byte b = (byte)(60 + a * 80);
                byte ab = (byte)(a * 255);

                px[y * w + x] = new Color32(r, g, b, ab);
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false);
        return tex;
    }

    // ==================== UI FACTORY ====================

    Image MkImg(string n, RectTransform parent)
    {
        var rt = MkRect(n, parent);
        var img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        return img;
    }

    Image MkDrip(string n, RectTransform parent, float x0, float y0, float x1, float y1, Color col)
    {
        var img = MkImg(n, parent);
        img.sprite = null;
        Color c = col; c.a = 0f;
        img.color = c;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        return img;
    }

    TextMeshProUGUI MkTxt(string n, RectTransform parent)
    {
        var rt = MkRect(n, parent);
        var txt = rt.GetComponent<TextMeshProUGUI>();
        if (txt == null) txt = rt.gameObject.AddComponent<TextMeshProUGUI>();
        return txt;
    }

    RectTransform MkRect(string n, Transform parent)
    {
        Transform ex = parent.Find(n);
        GameObject go;
        if (ex != null) go = ex.gameObject;
        else
        {
            go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(parent, false);
        }
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
