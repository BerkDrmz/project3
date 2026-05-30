using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class PauseMenuBuilder : MonoBehaviour
{
    private GameObject pausePanel, settingsPanel, dimOverlay, blurBg;
    private bool isPaused = false;
    private TMP_FontAsset gameFont;
    private Canvas canvas;

    private static readonly Color CYAN_PRIMARY  = new Color(0f, 0.88f, 1f, 1f);
    private static readonly Color CYAN_SOFT     = new Color(0f, 0.88f, 1f, 0.55f);
    private static readonly Color PURPLE_ACCENT = new Color(0.75f, 0.2f, 1f, 1f);
    private static readonly Color TEXT_PRIMARY  = new Color(0.95f, 0.98f, 1f, 1f);
    private static readonly Color DANGER_RED    = new Color(1f, 0.35f, 0.45f, 1f);

    void Start()
    {
        gameFont = LoadFont();

        BuildCanvas();
        BuildEventSystem();

        // Tam ekran karartma + gradient
        dimOverlay = CreateDimOverlay(canvas.gameObject);
        blurBg = CreateBlurBg(canvas.gameObject);

        pausePanel    = CreatePanel(canvas.gameObject, "PausePanel");
        settingsPanel = CreatePanel(canvas.gameObject, "SettingsPanel");

        BuildPausePanel();
        BuildSettingsPanel();

        dimOverlay.SetActive(false);
        blurBg.SetActive(false);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    TMP_FontAsset LoadFont()
    {
        // Ana menüyle aynı font olsun, yoksa default'a düş
        var f = Resources.Load<TMP_FontAsset>("Oswald-Bold SDF");
        if (f != null) return f;
        return TMP_Settings.defaultFontAsset;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf)
                StartCoroutine(SwitchPanels(settingsPanel, pausePanel));
            else if (isPaused) Resume();
            else Pause();
        }
    }

    // ─── KURULUM ──────────────────────────────────────────────

    void BuildCanvas()
    {
        GameObject canvasGO = new GameObject("PauseCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
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

    GameObject CreateDimOverlay(GameObject parent)
    {
        GameObject dim = new GameObject("DimOverlay");
        dim.transform.SetParent(parent.transform, false);
        var img = dim.AddComponent<Image>();
        img.color = new Color(0.005f, 0.01f, 0.04f, 0.85f);
        img.raycastTarget = true;
        var rt = dim.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return dim;
    }

    GameObject CreateBlurBg(GameObject parent)
    {
        // Merkezi vinyet — odak için
        GameObject bg = new GameObject("VignetteBg");
        bg.transform.SetParent(parent.transform, false);

        // Üst gradient
        var top = new GameObject("TopFade");
        top.transform.SetParent(bg.transform, false);
        var ti = top.AddComponent<Image>();
        ti.color = new Color(0f, 0f, 0f, 0.4f);
        ti.raycastTarget = false;
        var trt = top.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.7f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // Alt gradient
        var bot = new GameObject("BotFade");
        bot.transform.SetParent(bg.transform, false);
        var bi = bot.AddComponent<Image>();
        bi.color = new Color(0f, 0f, 0f, 0.4f);
        bi.raycastTarget = false;
        var brt = bot.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 0.3f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;

        var rt = bg.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return bg;
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

    // ─── PAUSE / RESUME ──────────────────────────────────────

    void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        dimOverlay.SetActive(true);
        blurBg.SetActive(true);
        StartCoroutine(FadeImageAlpha(dimOverlay.GetComponent<Image>(), 0f, 0.85f, 0.25f));

        pausePanel.SetActive(true);
        StartCoroutine(SimpleFadeIn(pausePanel, 0.35f, true));
    }

    void Resume()
    {
        isPaused = false;
        StartCoroutine(ResumeRoutine());
    }

    IEnumerator ResumeRoutine()
    {
        StartCoroutine(FadeAndDeactivate(pausePanel, 0.18f, true));
        StartCoroutine(FadeAndDeactivate(settingsPanel, 0.18f, true));
        var dimImg = dimOverlay.GetComponent<Image>();
        yield return FadeImageAlpha(dimImg, dimImg.color.a, 0f, 0.22f, true);
        dimOverlay.SetActive(false);
        blurBg.SetActive(false);
        Time.timeScale = 1f;
    }

    IEnumerator SwitchPanels(GameObject hide, GameObject show)
    {
        yield return FadeAndDeactivate(hide, 0.15f, true);
        show.SetActive(true);
        yield return SimpleFadeIn(show, 0.3f, true);
    }

    // ─── PAUSE PANEL — ORTALANMIŞ ────────────────────────────

    void BuildPausePanel()
    {
        // Üst sub-tag
        var subtag = MakeTextCenter(pausePanel, "// SİSTEM ASKIYA ALINDI", 13,
            new Color(PURPLE_ACCENT.r, PURPLE_ACCENT.g, PURPLE_ACCENT.b, 0.9f),
            0, 245, 600, 24);
        subtag.characterSpacing = 7f;
        subtag.fontStyle = FontStyles.Bold;

        // Üst ayraç (kısa)
        Divider(pausePanel, 0, 215, 80,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.6f));

        // BAŞLIK
        var title = MakeTextCenter(pausePanel, "DURAKLATILDI", 78, Color.white,
            0, 145, 900, 100);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8f;
        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(
            Color.white, Color.white,
            CYAN_PRIMARY, CYAN_PRIMARY);
        ApplyNeonGlow(title, CYAN_PRIMARY, 0.18f, 0.35f);

        // Alt ayraç (kısa)
        Divider(pausePanel, 0, 78, 80,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.6f));

        // Butonlar — ortalanmış
        float by = 10f;
        float gap = 62f;
        CreatePauseButton(pausePanel, "DEVAM ET",  new Vector2(0, by),         () => Resume());
        CreatePauseButton(pausePanel, "AYARLAR",   new Vector2(0, by - gap),   () => StartCoroutine(SwitchPanels(pausePanel, settingsPanel)));
        CreatePauseButton(pausePanel, "ANA MENÜ",  new Vector2(0, by - gap*2), () => { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); });
        CreatePauseButton(pausePanel, "ÇIKIŞ",     new Vector2(0, by - gap*3), QuitGame, DANGER_RED);

        // En alt — küçük ipucu
        var hint = MakeTextCenter(pausePanel, "ESC ile devam et", 11,
            new Color(1f, 1f, 1f, 0.4f),
            0, by - gap * 3 - 80, 400, 20);
        hint.characterSpacing = 4f;
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── SETTINGS PANEL — ORTALANMIŞ ─────────────────────────

    void BuildSettingsPanel()
    {
        var subtag = MakeTextCenter(settingsPanel, "// SİSTEM YAPILANDIRMA", 13,
            new Color(PURPLE_ACCENT.r, PURPLE_ACCENT.g, PURPLE_ACCENT.b, 0.9f),
            0, 245, 600, 24);
        subtag.characterSpacing = 7f;
        subtag.fontStyle = FontStyles.Bold;

        Divider(settingsPanel, 0, 215, 80,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.6f));

        var title = MakeTextCenter(settingsPanel, "AYARLAR", 72, Color.white,
            0, 150, 600, 100);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8f;
        ApplyNeonGlow(title, CYAN_PRIMARY, 0.15f, 0.3f);

        Divider(settingsPanel, 0, 80, 80,
            new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.6f));

        // SES
        var lblSes = MakeTextCenter(settingsPanel, "// SES SEVİYESİ", 14, CYAN_SOFT,
            0, 25, 400, 24);
        lblSes.characterSpacing = 6f;
        lblSes.fontStyle = FontStyles.Bold;
        BuildSlider(settingsPanel, new Vector2(0, -15));

        // MÜZİK
        var lblMuz = MakeTextCenter(settingsPanel, "// MÜZİK", 14, CYAN_SOFT,
            0, -75, 400, 24);
        lblMuz.characterSpacing = 6f;
        lblMuz.fontStyle = FontStyles.Bold;
        BuildToggle(settingsPanel, new Vector2(0, -115));

        // GERİ butonu
        CreatePauseButton(settingsPanel, "← GERİ", new Vector2(0, -200),
            () => StartCoroutine(SwitchPanels(settingsPanel, pausePanel)));
    }

    GameObject BuildSlider(GameObject parent, Vector2 pos)
    {
        GameObject slGO = new GameObject("Slider");
        slGO.transform.SetParent(parent.transform, false);
        Slider sl = slGO.AddComponent<Slider>();
        var rt = slGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(380, 12);

        var bg = new GameObject("BG"); bg.transform.SetParent(slGO.transform, false);
        bg.AddComponent<Image>().color = new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.12f);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        var fa = new GameObject("FA"); fa.transform.SetParent(slGO.transform, false);
        var faRt = fa.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
        faRt.offsetMin = faRt.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        fill.AddComponent<Image>().color = new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.85f);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        sl.fillRect = fillRt;
        sl.value = AudioListener.volume;
        sl.onValueChanged.AddListener(v => AudioListener.volume = v);
        return slGO;
    }

    GameObject BuildToggle(GameObject parent, Vector2 pos)
    {
        var tGO = new GameObject("Toggle"); tGO.transform.SetParent(parent.transform, false);
        var tRt = tGO.AddComponent<RectTransform>();
        tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 0.5f);
        tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.anchoredPosition = pos; tRt.sizeDelta = new Vector2(58, 28);
        tGO.AddComponent<Image>().color = new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.15f);
        var tog = tGO.AddComponent<Toggle>();

        var ckGO = new GameObject("Check"); ckGO.transform.SetParent(tGO.transform, false);
        var ckImg = ckGO.AddComponent<Image>();
        ckImg.color = new Color(CYAN_PRIMARY.r, CYAN_PRIMARY.g, CYAN_PRIMARY.b, 0.95f);
        var ckRt = ckGO.GetComponent<RectTransform>();
        ckRt.anchorMin = new Vector2(0.1f, 0.1f); ckRt.anchorMax = new Vector2(0.9f, 0.9f);
        ckRt.offsetMin = ckRt.offsetMax = Vector2.zero;
        tog.graphic = ckImg;
        tog.isOn = MusicPreferences.IsMusicEnabled;
        tog.onValueChanged.AddListener(enabled => MusicPreferences.SetMusicEnabled(enabled));
        return tGO;
    }

    // ─── BUTON — ORTALANMIŞ ──────────────────────────────────

    GameObject CreatePauseButton(GameObject parent, string label, Vector2 pos,
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
        rt.pivot = new Vector2(0.5f, 0.5f); // ORTA pivot
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(380, 50);

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

        // Üst ve alt çizgiler — buton ortalı olunca yan değil üst-alt iyi durur
        var lineT = new GameObject("LineT");
        lineT.transform.SetParent(go.transform, false);
        var lineTImg = lineT.AddComponent<Image>();
        lineTImg.color = new Color(c.r, c.g, c.b, 0.35f);
        lineTImg.raycastTarget = false;
        var ltRt = lineT.GetComponent<RectTransform>();
        ltRt.anchorMin = new Vector2(0.1f, 1f); ltRt.anchorMax = new Vector2(0.9f, 1f);
        ltRt.sizeDelta = new Vector2(0f, 1f);
        ltRt.anchoredPosition = Vector2.zero;

        var lineB = new GameObject("LineB");
        lineB.transform.SetParent(go.transform, false);
        var lineBImg = lineB.AddComponent<Image>();
        lineBImg.color = new Color(c.r, c.g, c.b, 0.35f);
        lineBImg.raycastTarget = false;
        var lbRt = lineB.GetComponent<RectTransform>();
        lbRt.anchorMin = new Vector2(0.1f, 0f); lbRt.anchorMax = new Vector2(0.9f, 0f);
        lbRt.sizeDelta = new Vector2(0f, 1f);
        lbRt.anchoredPosition = Vector2.zero;

        // Yazı — ortalanmış
        var tg = new GameObject("Txt");
        tg.transform.SetParent(go.transform, false);
        var tmp = tg.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.color = c;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.characterSpacing = 6f;
        if (gameFont != null) tmp.font = gameFont;
        var tRT = tg.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;

        // Hover
        var hov = go.AddComponent<HoverEffectCenter>();
        hov.Setup(tmp, lineTImg, lineBImg, fillImg, glowImg, c, Color.white, rt);

        return go;
    }

    // ─── ANİMASYONLAR ────────────────────────────────────────

    IEnumerator SimpleFadeIn(GameObject panel, float dur, bool unscaled = false)
    {
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        float t = 0f;
        while (t < dur)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            cg.alpha = Mathf.SmoothStep(0f, 1f, t / dur);
            yield return null;
        }
        cg.alpha = 1f;
    }

    IEnumerator FadeAndDeactivate(GameObject go, float dur, bool unscaled = false)
    {
        if (!go.activeSelf) yield break;
        var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        float start = cg.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, 0f, t / dur);
            yield return null;
        }
        cg.alpha = 0f;
        go.SetActive(false);
    }

    IEnumerator FadeImageAlpha(Image img, float from, float to, float dur, bool unscaled = false)
    {
        float t = 0f;
        while (t < dur)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            var c = img.color;
            c.a = Mathf.Lerp(from, to, t / dur);
            img.color = c;
            yield return null;
        }
        var fc = img.color; fc.a = to; img.color = fc;
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

    // ─── METİN VE DIVIDER ────────────────────────────────────

    TextMeshProUGUI MakeTextCenter(GameObject parent, string content, int size, Color color,
        float x, float y, float w, float h)
    {
        var go = new GameObject("T_" + (content.Length > 6 ? content.Substring(0, 6) : content));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        if (gameFont != null) tmp.font = gameFont;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return tmp;
    }

    void Divider(GameObject parent, float x, float y, float width, Color color)
    {
        var d = new GameObject("Div");
        d.transform.SetParent(parent.transform, false);
        var img = d.AddComponent<Image>(); img.color = color;
        img.raycastTarget = false;
        var rt = d.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, 1f);
    }
}

// ════════════════════════════════════════════════════════════
//   ORTALANMIŞ HOVER — pause butonları için
// ════════════════════════════════════════════════════════════

public class HoverEffectCenter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private TextMeshProUGUI textMesh;
    private Image lineTop, lineBot, fillImage, glowImage;
    private RectTransform rectTransform;
    private Color normalColor, hoverColor;
    private Vector3 originalScale;
    private Coroutine current;
    private float hoverState = 0f;

    public void Setup(TextMeshProUGUI text, Image lineT, Image lineB, Image fill, Image glow,
                      Color normal, Color hover, RectTransform rt)
    {
        textMesh = text; lineTop = lineT; lineBot = lineB;
        fillImage = fill; glowImage = glow;
        normalColor = normal; hoverColor = hover;
        rectTransform = rt;
        originalScale = rt.localScale;
    }

    public void OnPointerEnter(PointerEventData _) => Animate(1f);
    public void OnPointerExit(PointerEventData _)  => Animate(0f);
    public void OnPointerDown(PointerEventData _)  => Animate(0.95f);
    public void OnPointerUp(PointerEventData _)    => Animate(1f);

    void Animate(float target)
    {
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(Run(target));
    }

    IEnumerator Run(float target)
    {
        float speed = 12f;
        while (Mathf.Abs(hoverState - target) > 0.01f)
        {
            // unscaled delta — pause sırasında çalışsın
            hoverState = Mathf.Lerp(hoverState, target, Time.unscaledDeltaTime * speed);
            float t = Mathf.Clamp01(hoverState);

            // Hafif büyüme
            if (rectTransform != null)
                rectTransform.localScale = originalScale * Mathf.Lerp(1f, 1.03f, t);

            // Yazı
            if (textMesh != null)
            {
                textMesh.color = Color.Lerp(normalColor, hoverColor, t);
                textMesh.characterSpacing = Mathf.Lerp(6f, 8f, t);
            }

            // Üst-alt çizgiler güçlenir ve genişler
            if (lineTop != null)
            {
                lineTop.color = new Color(normalColor.r, normalColor.g, normalColor.b,
                    Mathf.Lerp(0.35f, 1f, t));
                var lt = lineTop.rectTransform;
                lt.anchorMin = new Vector2(Mathf.Lerp(0.1f, 0f, t), 1f);
                lt.anchorMax = new Vector2(Mathf.Lerp(0.9f, 1f, t), 1f);
            }
            if (lineBot != null)
            {
                lineBot.color = new Color(normalColor.r, normalColor.g, normalColor.b,
                    Mathf.Lerp(0.35f, 1f, t));
                var lb = lineBot.rectTransform;
                lb.anchorMin = new Vector2(Mathf.Lerp(0.1f, 0f, t), 0f);
                lb.anchorMax = new Vector2(Mathf.Lerp(0.9f, 1f, t), 0f);
            }

            // İç dolgu
            if (fillImage != null)
                fillImage.color = new Color(normalColor.r, normalColor.g, normalColor.b,
                    Mathf.Lerp(0f, 0.15f, t));

            // Glow
            if (glowImage != null)
                glowImage.color = new Color(normalColor.r, normalColor.g, normalColor.b,
                    Mathf.Lerp(0f, 0.25f, t));

            yield return null;
        }
        hoverState = target;
    }
}
