using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MenuBuilder : MonoBehaviour
{
    private GameObject mainPanel, settingsPanel, creditsPanel, howToPlayPanel;
    private GameObject overlayDim;
    private TMP_FontAsset gameFont;
    private Canvas canvas;
    private AudioSource menuMusicSource;

    private const string MENU_MUSIC_RESOURCE_PATH = "Audio/Menu/Vector_Protocol";

    // Tema
    private static readonly Color CYAN_PRIMARY  = new Color(0f, 0.88f, 1f, 1f);
    private static readonly Color CYAN_SOFT     = new Color(0f, 0.88f, 1f, 0.55f);
    private static readonly Color PURPLE_ACCENT = new Color(0.75f, 0.2f, 1f, 1f);
    private static readonly Color TEXT_PRIMARY  = new Color(0.95f, 0.98f, 1f, 1f);
    private static readonly Color DANGER_RED    = new Color(1f, 0.35f, 0.45f, 1f);
    private static readonly Color BG_DEEP       = new Color(0.02f, 0.03f, 0.08f, 1f);

    // Sol kenardan içeri ofset (1920 referansa göre — sol kenardan 100px içeri)
    private const float CONTENT_X = -860f; // anchored pos, pivot sol için

    void Start()
    {
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = BG_DEEP;
        gameFont = LoadFont();

        BuildCanvas();
        BuildEventSystem();
        SetupMenuMusic();

        mainPanel      = CreatePanel(canvas.gameObject, "MainPanel");
        settingsPanel  = CreatePanel(canvas.gameObject, "SettingsPanel");
        creditsPanel   = CreatePanel(canvas.gameObject, "CreditsPanel");
        howToPlayPanel = CreatePanel(canvas.gameObject, "HowToPlayPanel");

        overlayDim = CreateDimOverlay(canvas.gameObject);
        overlayDim.SetActive(false);

        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);

        BuildMainPanel();
        BuildSettingsPanel();
        BuildCreditsPanel();
        BuildHowToPlayPanel();

        GameObject fx = new GameObject("MenuEffects");
        fx.transform.SetParent(canvas.transform, false);
        fx.AddComponent<MenuEffects>().Init(gameFont);

        StartCoroutine(SimpleFadeIn(mainPanel, 0.5f));
    }

    TMP_FontAsset LoadFont()
    {
        var f = Resources.Load<TMP_FontAsset>("Oswald-Bold SDF");
        if (f != null) return f;
        // Fallback: TMP default
        return TMP_Settings.defaultFontAsset;
    }

    void SetupMenuMusic()
    {
        AudioClip menuMusic = Resources.Load<AudioClip>(MENU_MUSIC_RESOURCE_PATH);
        if (menuMusic == null)
        {
            Debug.LogWarning("Menu music clip not found at Resources/" + MENU_MUSIC_RESOURCE_PATH);
            return;
        }

        if (menuMusic.loadState != AudioDataLoadState.Loaded)
        {
            menuMusic.LoadAudioData();
        }

        menuMusicSource = GetComponent<AudioSource>();
        if (menuMusicSource == null)
        {
            menuMusicSource = gameObject.AddComponent<AudioSource>();
        }

        menuMusicSource.playOnAwake = false;
        menuMusicSource.loop = true;
        menuMusicSource.clip = menuMusic;
        menuMusicSource.volume = 0.75f;
        MusicPreferences.ApplyToAudioSource(menuMusicSource);

        if (!menuMusicSource.isPlaying)
        {
            menuMusicSource.Stop();
            menuMusicSource.time = 0f;
            menuMusicSource.Play();
        }
    }

    // ─── KURULUM ──────────────────────────────────────────────

    void BuildCanvas()
    {
        GameObject canvasGO = new GameObject("Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
    }

    void BuildEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    GameObject CreatePanel(GameObject parent, string name)
    {
        GameObject p = new GameObject(name);
        p.transform.SetParent(parent.transform, false);
        p.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var rt = p.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        p.AddComponent<CanvasGroup>();
        return p;
    }

    GameObject CreateDimOverlay(GameObject parent)
    {
        GameObject dim = new GameObject("DimOverlay");
        dim.transform.SetParent(parent.transform, false);
        var img = dim.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        img.raycastTarget = false;
        var rt = dim.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        dim.transform.SetSiblingIndex(1);
        return dim;
    }

    // ─── ARKA PLAN ───────────────────────────────────────────

    void AddBg(GameObject panel)
    {
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(panel.transform, false);
        bg.transform.SetAsFirstSibling();
        Image bgImg = bg.AddComponent<Image>();
        Sprite sp = Resources.Load<Sprite>("menu_background");
        bgImg.sprite = sp;
        bgImg.color = sp != null ? Color.white : BG_DEEP;
        bgImg.preserveAspect = false;
        bgImg.raycastTarget = false;
        var rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Sol gradient
        for (int i = 0; i < 20; i++)
        {
            GameObject s = new GameObject("GradStep_" + i);
            s.transform.SetParent(panel.transform, false);
            s.transform.SetSiblingIndex(i + 1);
            Image si = s.AddComponent<Image>();
            si.raycastTarget = false;

            float r = i / 20f;
            float alpha = Mathf.Lerp(0.92f, 0f, Mathf.Pow(r, 0.5f));
            si.color = new Color(0.005f, 0.01f, 0.04f, alpha);

            var sr = s.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(r * 0.5f, 0f);
            sr.anchorMax = new Vector2((r + 0.05f) * 0.5f, 1f);
            sr.offsetMin = sr.offsetMax = Vector2.zero;
        }

        // Alt vinyet
        GameObject vignette = new GameObject("Vignette");
        vignette.transform.SetParent(panel.transform, false);
        vignette.transform.SetSiblingIndex(21);
        Image vi = vignette.AddComponent<Image>();
        vi.color = new Color(0f, 0f, 0f, 0.3f);
        vi.raycastTarget = false;
        var vr = vignette.GetComponent<RectTransform>();
        vr.anchorMin = new Vector2(0f, 0f);
        vr.anchorMax = new Vector2(1f, 0.18f);
        vr.offsetMin = vr.offsetMax = Vector2.zero;
    }

    // ─── ANA MENÜ ─────────────────────────────────────────────

    void BuildMainPanel()
    {
        AddBg(mainPanel);

        // Üst etiket
        var tag = MakeText(mainPanel, "// ADAPTIVE COMBAT SYSTEM", 14,
            new Color(PURPLE_ACCENT.r, PURPLE_ACCENT.g, PURPLE_ACCENT.b, 0.9f),
            CONTENT_X, 380, 460, 24);
        tag.characterSpacing = 6f;
        tag.fontStyle = FontStyles.Bold;
        AttachSlide(tag.gameObject, 0f, -50f);

        // ANA LOGO — büyük ama taşmayacak şekilde
        var logo = MakeText(mainPanel, "NEUROCHARGE", 88, Color.white,
            CONTENT_X, 295, 800, 130);
        logo.fontStyle = FontStyles.Bold;
        logo.characterSpacing = 2f;
        logo.enableVertexGradient = true;
        logo.colorGradient = new VertexGradient(
            Color.white, Color.white,
            CYAN_PRIMARY, CYAN_PRIMARY);
        ApplyNeonGlow(logo, CYAN_PRIMARY, 0.2f, 0.4f);
        AttachSlide(logo.gameObject, 0.06f, -80f);

        // Subtitle — daha makul karakter aralığı
        var sub = MakeText(mainPanel, "ADAPTIVE ARENA", 18,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.85f),
            CONTENT_X, 230, 500, 28);
        sub.characterSpacing = 12f;
        sub.fontStyle = FontStyles.Bold;
        AttachSlide(sub.gameObject, 0.14f, -50f);

        // Üst ayraç
        var divTop = Divider(mainPanel, CONTENT_X, 195, 480,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.5f));
        AttachSlide(divTop, 0.20f, -50f);

        // Butonlar
        float by = 110f;
        float gap = 64f;

        var b1 = MakeButton(mainPanel, "OYUNA BAŞLA",  new Vector2(CONTENT_X, by),
            () =>
            {
                WaveCheckpointManager.ClearCheckpoint();
                SceneManager.LoadScene("SampleScene");
            });
        var b2 = MakeButton(mainPanel, "AYARLAR",       new Vector2(CONTENT_X, by - gap),
            () => OpenSubPanel(settingsPanel));
        var b3 = MakeButton(mainPanel, "NASIL OYNANIR", new Vector2(CONTENT_X, by - gap * 2),
            () => OpenSubPanel(howToPlayPanel));
        var b4 = MakeButton(mainPanel, "HAZIRLAYANLAR", new Vector2(CONTENT_X, by - gap * 3),
            () => OpenSubPanel(creditsPanel));
        var b5 = MakeButton(mainPanel, "ÇIKIŞ",         new Vector2(CONTENT_X, by - gap * 4),
            QuitGame, DANGER_RED);

        AttachSlide(b1, 0.28f);
        AttachSlide(b2, 0.34f);
        AttachSlide(b3, 0.40f);
        AttachSlide(b4, 0.46f);
        AttachSlide(b5, 0.52f);

        // Alt ayraç
        var divBot = Divider(mainPanel, CONTENT_X, by - gap * 4 - 50f, 480,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.25f));
        AttachSlide(divBot, 0.58f, -50f);

        // Sağ alt versiyon
        var ver = MakeTextRight(mainPanel, "v1.0.0  //  BUILD 2024.11", 11,
            new Color(1f, 1f, 1f, 0.4f),
            -40, -510, 300, 20);
        ver.characterSpacing = 4f;
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── AYARLAR ──────────────────────────────────────────────

    void BuildSettingsPanel()
    {
        AddBg(settingsPanel);

        var subtag = MakeText(settingsPanel, "// SİSTEM YAPILANDIRMA", 14,
            new Color(PURPLE_ACCENT.r, PURPLE_ACCENT.g, PURPLE_ACCENT.b, 0.9f),
            CONTENT_X, 380, 460, 24);
        subtag.characterSpacing = 6f;
        subtag.fontStyle = FontStyles.Bold;

        var title = MakeText(settingsPanel, "AYARLAR", 72, Color.white,
            CONTENT_X, 305, 600, 110);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4f;
        ApplyNeonGlow(title, CYAN_PRIMARY, 0.18f, 0.35f);

        Divider(settingsPanel, CONTENT_X, 240, 480,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.5f));

        // Ses
        var l1 = MakeText(settingsPanel, "// SES SEVİYESİ", 14, CYAN_SOFT,
            CONTENT_X, 175, 400, 24);
        l1.characterSpacing = 6f;
        l1.fontStyle = FontStyles.Bold;
        Slider(settingsPanel, new Vector2(CONTENT_X + 210, 130));

        // Müzik
        var l2 = MakeText(settingsPanel, "// MÜZİK", 14, CYAN_SOFT,
            CONTENT_X, 60, 400, 24);
        l2.characterSpacing = 6f;
        l2.fontStyle = FontStyles.Bold;
        Toggle(settingsPanel, new Vector2(CONTENT_X + 30, 18));

        Divider(settingsPanel, CONTENT_X, -50, 480,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.2f));

        MakeButton(settingsPanel, "← GERİ", new Vector2(CONTENT_X, -130),
            () => CloseSubPanel(settingsPanel));
    }

    // ─── HAZIRLAYANLAR ────────────────────────────────────────

    void BuildCreditsPanel()
    {
        AddBg(creditsPanel);

        var subtag = MakeText(creditsPanel, "// EKİP & TEŞEKKÜRLER", 14,
            new Color(PURPLE_ACCENT.r, PURPLE_ACCENT.g, PURPLE_ACCENT.b, 0.9f),
            CONTENT_X, 380, 460, 24);
        subtag.characterSpacing = 6f;
        subtag.fontStyle = FontStyles.Bold;

        var title = MakeText(creditsPanel, "HAZIRLAYANLAR", 72, Color.white,
            CONTENT_X, 305, 800, 110);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4f;
        ApplyNeonGlow(title, CYAN_PRIMARY, 0.18f, 0.35f);

        Divider(creditsPanel, CONTENT_X, 240, 480,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.5f));

        string[] roles = { "// ", "// ", "// ", "// ", "// " };
        string[] names = { "Arda Akbaba", "Batuhan Kurkut", "Osman Berk Durmaz", "Erdem Akdağ", "Rakhat Turdybek" };

        float sy = 165f, gap = 60f;
        for (int i = 0; i < 5; i++)
        {
            var r = MakeText(creditsPanel, roles[i], 12, CYAN_SOFT,
                CONTENT_X, sy - gap * i, 220, 22);
            r.characterSpacing = 5f;
            r.fontStyle = FontStyles.Bold;

            var n = MakeText(creditsPanel, names[i], 22, TEXT_PRIMARY,
                CONTENT_X + 240, sy - gap * i, 280, 32);
            n.fontStyle = FontStyles.Bold;
        }

        Divider(creditsPanel, CONTENT_X, -85, 480,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.2f));

        MakeButton(creditsPanel, "← GERİ", new Vector2(CONTENT_X, -160),
            () => CloseSubPanel(creditsPanel));
    }

    // ─── NASIL OYNANIR ────────────────────────────────────────

    void BuildHowToPlayPanel()
    {
        AddBg(howToPlayPanel);

        var subtag = MakeText(howToPlayPanel, "// KONTROL ŞEMASI", 14,
            new Color(PURPLE_ACCENT.r, PURPLE_ACCENT.g, PURPLE_ACCENT.b, 0.9f),
            CONTENT_X, 380, 460, 24);
        subtag.characterSpacing = 6f;
        subtag.fontStyle = FontStyles.Bold;

        var title = MakeText(howToPlayPanel, "NASIL OYNANIR", 72, Color.white,
            CONTENT_X, 305, 800, 110);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4f;
        ApplyNeonGlow(title, CYAN_PRIMARY, 0.18f, 0.35f);

        Divider(howToPlayPanel, CONTENT_X, 240, 480,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.5f));

        string[] lbls = { "// HAREKET", "// NİŞAN AL", "// ATEŞ ET", "// DURAKLAT" };
        string[] keys = { "WASD", "FARE", "SOL TIK", "ESC" };
        float sy = 165f, gap = 60f;

        for (int i = 0; i < 4; i++)
        {
            var l = MakeText(howToPlayPanel, lbls[i], 12, CYAN_SOFT,
                CONTENT_X, sy - gap * i, 220, 22);
            l.characterSpacing = 5f;
            l.fontStyle = FontStyles.Bold;

            BuildKeyBox(howToPlayPanel, keys[i], new Vector2(CONTENT_X + 280, sy - gap * i));
        }

        Divider(howToPlayPanel, CONTENT_X, -85, 480,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.2f));

        MakeButton(howToPlayPanel, "← GERİ", new Vector2(CONTENT_X, -160),
            () => CloseSubPanel(howToPlayPanel));
    }

    void BuildKeyBox(GameObject parent, string key, Vector2 pos)
    {
        var box = new GameObject("KeyBox_" + key);
        box.transform.SetParent(parent.transform, false);
        var rt = box.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(160, 44);

        // İç dolgu
        var fill = new GameObject("Fill");
        fill.transform.SetParent(box.transform, false);
        var fimg = fill.AddComponent<Image>();
        fimg.color = new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.08f);
        fimg.raycastTarget = false;
        var frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;

        // Çerçeve
        Color borderCol = new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.5f);
        BorderEdge(box, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -1), borderCol);  // üst
        BorderEdge(box, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), borderCol);   // alt
        BorderEdge(box, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), borderCol);   // sol
        BorderEdge(box, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-1, 0), borderCol);  // sağ

        // Yazı
        var t = new GameObject("Txt");
        t.transform.SetParent(box.transform, false);
        var tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.text = key;
        tmp.fontSize = 22;
        tmp.color = TEXT_PRIMARY;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 3f;
        tmp.raycastTarget = false;
        if (gameFont != null) tmp.font = gameFont;
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    void BorderEdge(GameObject parent, Vector2 aMin, Vector2 aMax, Vector2 sizeDelta, Color c)
    {
        var b = new GameObject("Border");
        b.transform.SetParent(parent.transform, false);
        var img = b.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        var rt = b.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
    }

    // ─── PANEL GEÇİŞLERİ ─────────────────────────────────────

    void OpenSubPanel(GameObject sub)
    {
        var mainCG = mainPanel.GetComponent<CanvasGroup>();
        StartCoroutine(FadeCanvasGroup(mainCG, mainCG.alpha, 0f, 0.25f, false));

        overlayDim.SetActive(true);
        StartCoroutine(FadeImageAlpha(overlayDim.GetComponent<Image>(), 0f, 0.6f, 0.3f));

        sub.SetActive(true);
        StartCoroutine(SimpleFadeIn(sub, 0.4f));
    }

    void CloseSubPanel(GameObject sub)
    {
        StartCoroutine(FadeAndDeactivate(sub, 0.2f));
        StartCoroutine(FadeImageAlpha(overlayDim.GetComponent<Image>(), 0.6f, 0f, 0.3f,
            () => overlayDim.SetActive(false)));

        var mainCG = mainPanel.GetComponent<CanvasGroup>();
        StartCoroutine(FadeCanvasGroup(mainCG, 0f, 1f, 0.35f, true));
    }

    // ─── ANİMASYON YARDIMCILARI ──────────────────────────────

    IEnumerator SimpleFadeIn(GameObject panel, float dur)
    {
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.SmoothStep(0f, 1f, t / dur);
            yield return null;
        }
        cg.alpha = 1f;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur, bool interactable)
    {
        if (cg == null) yield break;
        cg.interactable = interactable;
        cg.blocksRaycasts = interactable;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator FadeAndDeactivate(GameObject go, float dur)
    {
        var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        float start = cg.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, 0f, t / dur);
            yield return null;
        }
        cg.alpha = 0f;
        go.SetActive(false);
    }

    IEnumerator FadeImageAlpha(Image img, float from, float to, float dur, System.Action onDone = null)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            var c = img.color;
            c.a = Mathf.Lerp(from, to, t / dur);
            img.color = c;
            yield return null;
        }
        var fc = img.color; fc.a = to; img.color = fc;
        onDone?.Invoke();
    }

    void AttachSlide(GameObject go, float delay, float fromOffset = 50f)
    {
        if (go.GetComponent<CanvasGroup>() == null)
            go.AddComponent<CanvasGroup>();

        var s = go.AddComponent<SlideInOnEnable>();
        s.delay = delay;
        s.fromOffsetX = -fromOffset;
        s.duration = 0.5f;
    }

    // ─── NEON GLOW ───────────────────────────────────────────

    void ApplyNeonGlow(TextMeshProUGUI tmp, Color glowColor, float dilate, float softness)
    {
        Material mat = new Material(tmp.fontMaterial);
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(glowColor.r, glowColor.g, glowColor.b, 0.9f));
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, softness);
        mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, dilate);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);

        mat.EnableKeyword("OUTLINE_ON");
        mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(glowColor.r, glowColor.g, glowColor.b, 0.5f));
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.14f);

        tmp.fontMaterial = mat;
    }

    // ─── METİN — PIVOT SOL (taşma engellendi) ────────────────

    TextMeshProUGUI MakeText(GameObject parent, string text, int size, Color color,
        float x, float y, float w, float h)
    {
        var go = new GameObject("T_" + (text.Length > 8 ? text.Substring(0, 8) : text));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        if (gameFont != null) tmp.font = gameFont;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f); // SOL pivot — taşma yok
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return tmp;
    }

    TextMeshProUGUI MakeTextRight(GameObject parent, string text, int size, Color color,
        float x, float y, float w, float h)
    {
        var go = new GameObject("TR_" + (text.Length > 8 ? text.Substring(0, 8) : text));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.raycastTarget = false;
        if (gameFont != null) tmp.font = gameFont;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return tmp;
    }

    // ─── BUTON ───────────────────────────────────────────────

    GameObject MakeButton(GameObject parent, string label, Vector2 pos,
        UnityEngine.Events.UnityAction action, Color? col = null)
    {
        Color c = col ?? TEXT_PRIMARY;

        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<CanvasGroup>();

        var hit = go.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.001f);
        hit.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(action);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f); // SOL pivot — pos buton sol kenarı
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(440, 52);

        // Glow
        var glow = new GameObject("Glow");
        glow.transform.SetParent(go.transform, false);
        glow.transform.SetAsFirstSibling();
        var glowImg = glow.AddComponent<Image>();
        glowImg.color = new Color(c.r, c.g, c.b, 0f);
        glowImg.raycastTarget = false;
        var glowRT = glow.GetComponent<RectTransform>();
        glowRT.anchorMin = Vector2.zero; glowRT.anchorMax = Vector2.one;
        glowRT.offsetMin = new Vector2(-12f, -8f);
        glowRT.offsetMax = new Vector2(12f, 8f);

        // İç dolgu
        var fill = new GameObject("Fill");
        fill.transform.SetParent(go.transform, false);
        fill.transform.SetSiblingIndex(1);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(c.r, c.g, c.b, 0f);
        fillImg.raycastTarget = false;
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // Sol vurgu çizgisi
        var lineL = new GameObject("LineL");
        lineL.transform.SetParent(go.transform, false);
        var lineLImg = lineL.AddComponent<Image>();
        lineLImg.color = new Color(c.r, c.g, c.b, 0.85f);
        lineLImg.raycastTarget = false;
        var lineLRT = lineL.GetComponent<RectTransform>();
        lineLRT.anchorMin = new Vector2(0f, 0.15f);
        lineLRT.anchorMax = new Vector2(0f, 0.85f);
        lineLRT.offsetMin = Vector2.zero;
        lineLRT.offsetMax = new Vector2(3f, 0f);

        // Yazı
        var tg = new GameObject("Txt");
        tg.transform.SetParent(go.transform, false);
        var tmp = tg.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.color = c;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.characterSpacing = 4f;
        if (gameFont != null) tmp.font = gameFont;
        var tRT = tg.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(22f, 0f);
        tRT.offsetMax = Vector2.zero;

        // Sağ ok
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(go.transform, false);
        var arrow = arrowGO.AddComponent<TextMeshProUGUI>();
        arrow.text = ">";
        arrow.fontSize = 32;
        arrow.color = new Color(c.r, c.g, c.b, 0f);
        arrow.alignment = TextAlignmentOptions.MidlineRight;
        arrow.raycastTarget = false;
        arrow.fontStyle = FontStyles.Bold;
        if (gameFont != null) arrow.font = gameFont;
        var aRT = arrowGO.GetComponent<RectTransform>();
        aRT.anchorMin = Vector2.zero; aRT.anchorMax = Vector2.one;
        aRT.offsetMin = Vector2.zero;
        aRT.offsetMax = new Vector2(-22f, 0f);

        var hov = go.AddComponent<HoverEffect>();
        hov.Setup(tmp, lineLImg, fillImg, glowImg, arrow, c, Color.white, rt);

        return go;
    }

    // ─── SLIDER & TOGGLE & DIVIDER ───────────────────────────

    void Slider(GameObject parent, Vector2 pos)
    {
        var go = new GameObject("Slider"); go.transform.SetParent(parent.transform, false);
        var sl = go.AddComponent<UnityEngine.UI.Slider>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(420, 10);

        var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
        bg.AddComponent<Image>().color = new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.12f);
        var bgr = bg.GetComponent<RectTransform>();
        bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one;
        bgr.offsetMin = bgr.offsetMax = Vector2.zero;

        var fa = new GameObject("FA"); fa.transform.SetParent(go.transform, false);
        var far = fa.AddComponent<RectTransform>();
        far.anchorMin = Vector2.zero; far.anchorMax = Vector2.one;
        far.offsetMin = far.offsetMax = Vector2.zero;

        var fi = new GameObject("Fill"); fi.transform.SetParent(fa.transform, false);
        fi.AddComponent<Image>().color = new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.85f);
        var fir = fi.GetComponent<RectTransform>();
        fir.anchorMin = Vector2.zero; fir.anchorMax = Vector2.one;
        fir.offsetMin = fir.offsetMax = Vector2.zero;

        sl.fillRect = fir;
        sl.value = AudioListener.volume;
        sl.onValueChanged.AddListener(v => AudioListener.volume = v);
    }

    void Toggle(GameObject parent, Vector2 pos)
    {
        var go = new GameObject("Toggle"); go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(56, 26);
        go.AddComponent<Image>().color = new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.15f);
        var tog = go.AddComponent<UnityEngine.UI.Toggle>();

        var ck = new GameObject("Check"); ck.transform.SetParent(go.transform, false);
        var cki = ck.AddComponent<Image>();
        cki.color = new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.95f);
        var ckr = ck.GetComponent<RectTransform>();
        ckr.anchorMin = new Vector2(0.1f, 0.1f); ckr.anchorMax = new Vector2(0.9f, 0.9f);
        ckr.offsetMin = ckr.offsetMax = Vector2.zero;
        tog.graphic = cki;
        tog.isOn = MusicPreferences.IsMusicEnabled;
        tog.onValueChanged.AddListener(enabled =>
        {
            MusicPreferences.SetMusicEnabled(enabled);
            MusicPreferences.ApplyToAudioSource(menuMusicSource);
        });
    }

    GameObject Divider(GameObject parent, float x, float y, float width, Color color)
    {
        var go = new GameObject("Div"); go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>(); img.color = color; img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, 1f);
        return go;
    }
}

// ════════════════════════════════════════════════════════════
//   HOVER EFFECT
// ════════════════════════════════════════════════════════════

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private TextMeshProUGUI textMesh;
    private Image lineImage, fillImage, glowImage;
    private TextMeshProUGUI arrow;
    private RectTransform rectTransform;
    private Color normalColor, hoverColor;
    private Vector2 originalPos;
    private Coroutine current;
    private float hoverState = 0f;

    public void Setup(TextMeshProUGUI text, Image line, Image fill, Image glow,
                      TextMeshProUGUI arrowTxt, Color normal, Color hover, RectTransform rt)
    {
        textMesh = text; lineImage = line; fillImage = fill;
        glowImage = glow; arrow = arrowTxt;
        normalColor = normal; hoverColor = hover;
        rectTransform = rt;
        originalPos = rt.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData _)
    {
        MenuHoverAudio.Play();
        Animate(1f);
    }
    public void OnPointerExit(PointerEventData _)  => Animate(0f);
    public void OnPointerDown(PointerEventData _)  => Animate(1.15f);
    public void OnPointerUp(PointerEventData _)    => Animate(1f);

    void Animate(float target)
    {
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(Run(target));
    }

    IEnumerator Run(float target)
    {
        float speed = 10f;
        while (Mathf.Abs(hoverState - target) > 0.01f)
        {
            hoverState = Mathf.Lerp(hoverState, target, Time.deltaTime * speed);
            float t = Mathf.Clamp01(hoverState);

            if (rectTransform != null)
                rectTransform.anchoredPosition = originalPos + new Vector2(t * 16f, 0f);

            if (textMesh != null)
                textMesh.color = Color.Lerp(normalColor, hoverColor, t);

            if (lineImage != null)
            {
                lineImage.color = Color.Lerp(
                    new Color(normalColor.r, normalColor.g, normalColor.b, 0.85f),
                    new Color(hoverColor.r, hoverColor.g, hoverColor.b, 1f), t);
                lineImage.rectTransform.offsetMax = new Vector2(Mathf.Lerp(3f, 6f, t), 0f);
            }

            if (fillImage != null)
                fillImage.color = new Color(normalColor.r, normalColor.g, normalColor.b,
                    Mathf.Lerp(0f, 0.12f, t));

            if (glowImage != null)
                glowImage.color = new Color(normalColor.r, normalColor.g, normalColor.b,
                    Mathf.Lerp(0f, 0.2f, t));

            if (arrow != null)
            {
                arrow.color = new Color(normalColor.r, normalColor.g, normalColor.b,
                    Mathf.Lerp(0f, 1f, t));
                arrow.rectTransform.offsetMax = new Vector2(Mathf.Lerp(-22f, -10f, t), 0f);
            }

            yield return null;
        }
        hoverState = target;
    }
}

// ════════════════════════════════════════════════════════════
//   SLIDE-IN
// ════════════════════════════════════════════════════════════

public static class MenuHoverAudio
{
    private const float COOLDOWN = 0.05f;
    private const float HOVER_VOLUME = 0.28f;
    private const string HOVER_AUDIO_RESOURCE_PATH = "Audio/Menu/MenuHoverOpen";

    private static AudioSource source;
    private static AudioClip clip;
    private static float lastPlayTime = -1f;

    public static void Play()
    {
        if (Time.unscaledTime - lastPlayTime < COOLDOWN)
        {
            return;
        }

        EnsureSource();
        if (source == null)
        {
            return;
        }

        if (clip == null)
        {
            clip = LoadHoverClip();
            if (clip == null)
            {
                return;
            }
        }

        lastPlayTime = Time.unscaledTime;
        source.PlayOneShot(clip, HOVER_VOLUME);
    }

    private static AudioClip LoadHoverClip()
    {
        AudioClip loadedClip = Resources.Load<AudioClip>(HOVER_AUDIO_RESOURCE_PATH);
        if (loadedClip == null)
        {
            Debug.LogWarning("Menu hover audio clip not found at Resources/" + HOVER_AUDIO_RESOURCE_PATH);
            return null;
        }

        if (loadedClip.loadState != AudioDataLoadState.Loaded)
        {
            loadedClip.LoadAudioData();
        }

        return loadedClip;
    }

    private static void EnsureSource()
    {
        if (source != null)
        {
            return;
        }

        GameObject go = new GameObject("MenuHoverAudio");
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 0.6f;
    }
}

public class SlideInOnEnable : MonoBehaviour
{
    public float delay = 0f;
    public float fromOffsetX = -50f;
    public float duration = 0.5f;
    public bool useUnscaledTime = false;

    private Vector2 targetPos;
    private RectTransform rt;
    private CanvasGroup cg;

    void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        if (rt == null) return;

        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        targetPos = rt.anchoredPosition;
        rt.anchoredPosition = targetPos + new Vector2(fromOffsetX, 0f);
        cg.alpha = 0f;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (delay > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(delay);
            else yield return new WaitForSeconds(delay);
        }
        float t = 0f;
        Vector2 startPos = rt.anchoredPosition;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float e = 1f - Mathf.Pow(1f - n, 3f);
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, e);
            cg.alpha = e;
            yield return null;
        }
        rt.anchoredPosition = targetPos;
        cg.alpha = 1f;
    }
}
