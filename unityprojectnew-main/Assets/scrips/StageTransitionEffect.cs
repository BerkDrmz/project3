using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageTransitionEffect : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float minDuration = 1.45f;

    [Header("Neon Blue Style")]
    [SerializeField] private Color primaryBlue = new Color(0f, 0.72f, 1f, 1f);
    [SerializeField] private Color iceBlue = new Color(0.58f, 0.95f, 1f, 1f);
    [SerializeField] private Color deepPanelBlue = new Color(0.015f, 0.05f, 0.13f, 1f);
    [SerializeField] private int scanLineCount = 20;
    [SerializeField] private int dataTickCount = 18;
    [SerializeField] private string protocolLabel = "NEUROCHARGE // ARENA SYNC";

    private CanvasGroup canvasGroup;
    private RectTransform root;
    private RectTransform leftGate;
    private RectTransform rightGate;
    private RectTransform topRail;
    private RectTransform bottomRail;
    private RectTransform centerPanel;
    private RectTransform progressFill;
    private RectTransform scanline;
    private RectTransform sweepHighlight;
    private RectTransform[] frameLines;
    private RectTransform[] scanLines;
    private RectTransform[] dataTicks;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI stageText;
    private TextMeshProUGUI subText;
    private Image backdrop;
    private Image centerPanelImage;
    private Coroutine transitionCoroutine;
    private bool isBuilt;

    public void PlayTransition(string status, string stageName, Color accentColor, float duration)
    {
        BuildIfNeeded();

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionRoutine(status, stageName, GetNeonBlueAccent(accentColor), Mathf.Max(minDuration, duration)));
    }

    private IEnumerator TransitionRoutine(string status, string stageName, Color accentColor, float duration)
    {
        root.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        statusText.text = string.IsNullOrWhiteSpace(status) ? protocolLabel : status;
        stageText.text = stageName;
        subText.text = "GRID ONLINE  //  TARGET ROUTE RECALCULATED  //  NEXT WAVE ARMED";
        stageText.maxVisibleCharacters = 0;
        stageText.ForceMeshUpdate();

        SetAccentColor(accentColor);
        int visibleCharacters = stageText.textInfo.characterCount;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float n = Mathf.Clamp01(elapsed / duration);
            float fadeIn = Smooth(Mathf.Clamp01(n / 0.16f));
            float fadeOut = Smooth(Mathf.Clamp01((1f - n) / 0.18f));
            float visibility = Mathf.Min(fadeIn, fadeOut);
            float open = Smooth(Mathf.Clamp01(n / 0.28f));
            float exit = Smooth(Mathf.Clamp01((n - 0.78f) / 0.22f));
            float stable = 1f - exit;
            float pulse = 0.55f + Mathf.Sin(Time.time * 11f) * 0.22f;

            canvasGroup.alpha = visibility;
            backdrop.color = new Color(0.005f, 0.014f, 0.035f, 0.78f * visibility);
            centerPanel.localScale = new Vector3(Mathf.Lerp(0.92f, 1f, open) + exit * 0.035f, Mathf.Lerp(0.82f, 1f, open), 1f);
            centerPanelImage.color = new Color(deepPanelBlue.r, deepPanelBlue.g, deepPanelBlue.b, 0.72f * visibility * stable);

            leftGate.anchoredPosition = new Vector2(Mathf.Lerp(-620f, -56f, open) - exit * 760f, 0f);
            rightGate.anchoredPosition = new Vector2(Mathf.Lerp(620f, 56f, open) + exit * 760f, 0f);
            topRail.anchoredPosition = new Vector2(Mathf.Lerp(-1920f, 0f, open) + exit * 1920f, 0f);
            bottomRail.anchoredPosition = new Vector2(Mathf.Lerp(1920f, 0f, open) - exit * 1920f, 0f);

            progressFill.sizeDelta = new Vector2(980f * Smooth(Mathf.Clamp01((n - 0.18f) / 0.58f)), 6f);
            scanline.anchoredPosition = new Vector2(0f, Mathf.Lerp(420f, -420f, Mathf.Repeat(n * 1.55f, 1f)));
            sweepHighlight.anchoredPosition = new Vector2(Mathf.Lerp(-720f, 720f, Mathf.Repeat(n * 1.28f, 1f)), 0f);

            int targetVisibleCharacters = Mathf.RoundToInt(visibleCharacters * Smooth(Mathf.Clamp01((n - 0.12f) / 0.26f)));
            stageText.maxVisibleCharacters = targetVisibleCharacters;
            stageText.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(10f, 24f, open));
            statusText.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(76f, 108f, open));
            subText.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-74f, -102f, open));

            SetGraphicAlpha(stageText, visibility);
            SetGraphicAlpha(statusText, visibility * Mathf.Lerp(0.72f, 1f, pulse));
            SetGraphicAlpha(subText, visibility * 0.64f);
            AnimateFrame(accentColor, visibility, pulse, n);
            AnimateScanLines(accentColor, visibility, n);
            AnimateDataTicks(accentColor, visibility, n);

            yield return null;
        }

        stageText.maxVisibleCharacters = int.MaxValue;
        canvasGroup.alpha = 0f;
        root.gameObject.SetActive(false);
        transitionCoroutine = null;
    }

    private void BuildIfNeeded()
    {
        if (isBuilt)
        {
            return;
        }

        GameObject canvasGo = new GameObject("StageTransitionCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 92;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasGo.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        root = canvasGo.GetComponent<RectTransform>();
        Stretch(root);

        backdrop = CreateImage("Backdrop", root, Color.clear);
        Stretch(backdrop.rectTransform);

        BuildScanLines();
        BuildGatePanels();
        BuildCenterPanel();
        BuildFrame();
        BuildText();
        BuildDataTicks();

        root.gameObject.SetActive(false);
        isBuilt = true;
    }

    private void BuildScanLines()
    {
        scanLines = new RectTransform[Mathf.Max(1, scanLineCount)];

        for (int i = 0; i < scanLines.Length; i++)
        {
            RectTransform line = CreateImage($"BlueSignalLine_{i}", root, new Color(primaryBlue.r, primaryBlue.g, primaryBlue.b, 0.08f)).rectTransform;
            line.anchorMin = new Vector2(0f, 0.5f);
            line.anchorMax = new Vector2(1f, 0.5f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.sizeDelta = new Vector2(0f, 1f);
            line.anchoredPosition = new Vector2(0f, Mathf.Lerp(-470f, 470f, i / Mathf.Max(1f, scanLines.Length - 1f)));
            scanLines[i] = line;
        }
    }

    private void BuildGatePanels()
    {
        leftGate = CreateImage("LeftHologate", root, new Color(primaryBlue.r, primaryBlue.g, primaryBlue.b, 0.14f)).rectTransform;
        leftGate.anchorMin = new Vector2(0f, 0f);
        leftGate.anchorMax = new Vector2(0f, 1f);
        leftGate.pivot = new Vector2(0f, 0.5f);
        leftGate.sizeDelta = new Vector2(520f, 0f);
        leftGate.anchoredPosition = new Vector2(-620f, 0f);

        rightGate = CreateImage("RightHologate", root, new Color(primaryBlue.r, primaryBlue.g, primaryBlue.b, 0.14f)).rectTransform;
        rightGate.anchorMin = new Vector2(1f, 0f);
        rightGate.anchorMax = new Vector2(1f, 1f);
        rightGate.pivot = new Vector2(1f, 0.5f);
        rightGate.sizeDelta = new Vector2(520f, 0f);
        rightGate.anchoredPosition = new Vector2(620f, 0f);

        topRail = CreateImage("TopLightRail", root, Color.white).rectTransform;
        topRail.anchorMin = new Vector2(0f, 1f);
        topRail.anchorMax = new Vector2(1f, 1f);
        topRail.pivot = new Vector2(0.5f, 1f);
        topRail.sizeDelta = new Vector2(0f, 72f);
        topRail.anchoredPosition = new Vector2(-1920f, 0f);

        bottomRail = CreateImage("BottomLightRail", root, Color.white).rectTransform;
        bottomRail.anchorMin = new Vector2(0f, 0f);
        bottomRail.anchorMax = new Vector2(1f, 0f);
        bottomRail.pivot = new Vector2(0.5f, 0f);
        bottomRail.sizeDelta = new Vector2(0f, 72f);
        bottomRail.anchoredPosition = new Vector2(1920f, 0f);
    }

    private void BuildCenterPanel()
    {
        centerPanelImage = CreateImage("StageDataPanel", root, Color.clear);
        centerPanel = centerPanelImage.rectTransform;
        centerPanel.anchorMin = centerPanel.anchorMax = new Vector2(0.5f, 0.5f);
        centerPanel.pivot = new Vector2(0.5f, 0.5f);
        centerPanel.sizeDelta = new Vector2(1180f, 250f);
        centerPanel.anchoredPosition = Vector2.zero;

        RectTransform progressBack = CreateImage("ProgressRail", centerPanel, new Color(primaryBlue.r, primaryBlue.g, primaryBlue.b, 0.18f)).rectTransform;
        progressBack.anchorMin = progressBack.anchorMax = new Vector2(0.5f, 0f);
        progressBack.pivot = new Vector2(0.5f, 0.5f);
        progressBack.sizeDelta = new Vector2(980f, 2f);
        progressBack.anchoredPosition = new Vector2(0f, 35f);

        progressFill = CreateImage("ProgressFill", centerPanel, primaryBlue).rectTransform;
        progressFill.anchorMin = progressFill.anchorMax = new Vector2(0.5f, 0f);
        progressFill.pivot = new Vector2(0f, 0.5f);
        progressFill.sizeDelta = new Vector2(0f, 6f);
        progressFill.anchoredPosition = new Vector2(-490f, 35f);

        scanline = CreateImage("ActiveScanline", centerPanel, new Color(primaryBlue.r, primaryBlue.g, primaryBlue.b, 0.28f)).rectTransform;
        scanline.anchorMin = new Vector2(0f, 0.5f);
        scanline.anchorMax = new Vector2(1f, 0.5f);
        scanline.pivot = new Vector2(0.5f, 0.5f);
        scanline.sizeDelta = new Vector2(0f, 3f);

        sweepHighlight = CreateImage("SweepHighlight", centerPanel, new Color(iceBlue.r, iceBlue.g, iceBlue.b, 0.22f)).rectTransform;
        sweepHighlight.anchorMin = sweepHighlight.anchorMax = new Vector2(0.5f, 0.5f);
        sweepHighlight.pivot = new Vector2(0.5f, 0.5f);
        sweepHighlight.sizeDelta = new Vector2(54f, 250f);
        sweepHighlight.localEulerAngles = new Vector3(0f, 0f, -12f);
    }

    private void BuildFrame()
    {
        frameLines = new RectTransform[12];
        frameLines[0] = CreateFrameLine("FrameTop", new Vector2(0f, 1f), new Vector2(0f, 128f), new Vector2(960f, 3f));
        frameLines[1] = CreateFrameLine("FrameBottom", new Vector2(0f, 0f), new Vector2(0f, -128f), new Vector2(960f, 3f));
        frameLines[2] = CreateFrameLine("FrameLeft", new Vector2(0f, 0.5f), new Vector2(-590f, 0f), new Vector2(3f, 156f));
        frameLines[3] = CreateFrameLine("FrameRight", new Vector2(1f, 0.5f), new Vector2(590f, 0f), new Vector2(3f, 156f));
        frameLines[4] = CreateFrameLine("CornerTL_H", new Vector2(0f, 1f), new Vector2(-480f, 96f), new Vector2(180f, 4f));
        frameLines[5] = CreateFrameLine("CornerTL_V", new Vector2(0f, 1f), new Vector2(-568f, 66f), new Vector2(4f, 64f));
        frameLines[6] = CreateFrameLine("CornerTR_H", new Vector2(1f, 1f), new Vector2(480f, 96f), new Vector2(180f, 4f));
        frameLines[7] = CreateFrameLine("CornerTR_V", new Vector2(1f, 1f), new Vector2(568f, 66f), new Vector2(4f, 64f));
        frameLines[8] = CreateFrameLine("CornerBL_H", new Vector2(0f, 0f), new Vector2(-480f, -96f), new Vector2(180f, 4f));
        frameLines[9] = CreateFrameLine("CornerBL_V", new Vector2(0f, 0f), new Vector2(-568f, -66f), new Vector2(4f, 64f));
        frameLines[10] = CreateFrameLine("CornerBR_H", new Vector2(1f, 0f), new Vector2(480f, -96f), new Vector2(180f, 4f));
        frameLines[11] = CreateFrameLine("CornerBR_V", new Vector2(1f, 0f), new Vector2(568f, -66f), new Vector2(4f, 64f));
    }

    private void BuildText()
    {
        statusText = CreateText("StatusText", centerPanel, 22f, 900f, 46f);
        statusText.text = protocolLabel;
        statusText.characterSpacing = 10f;
        statusText.rectTransform.anchoredPosition = new Vector2(0f, 108f);

        stageText = CreateText("StageText", centerPanel, 80f, 1060f, 118f);
        stageText.fontStyle = FontStyles.Bold;
        stageText.enableAutoSizing = true;
        stageText.fontSizeMin = 42f;
        stageText.fontSizeMax = 84f;
        stageText.characterSpacing = 2f;
        stageText.outlineWidth = 0.18f;
        stageText.rectTransform.anchoredPosition = new Vector2(0f, 24f);

        subText = CreateText("SubText", centerPanel, 18f, 980f, 34f);
        subText.characterSpacing = 5f;
        subText.rectTransform.anchoredPosition = new Vector2(0f, -102f);
    }

    private void BuildDataTicks()
    {
        dataTicks = new RectTransform[Mathf.Max(1, dataTickCount)];
        for (int i = 0; i < dataTicks.Length; i++)
        {
            RectTransform tick = CreateImage($"DataTick_{i}", root, Color.white).rectTransform;
            bool left = i % 2 == 0;
            tick.anchorMin = tick.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
            tick.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            tick.sizeDelta = new Vector2(Mathf.Lerp(48f, 180f, (i % 5) / 4f), 3f);
            tick.anchoredPosition = new Vector2(left ? 44f : -44f, Mathf.Lerp(-360f, 360f, i / Mathf.Max(1f, dataTicks.Length - 1f)));
            dataTicks[i] = tick;
        }
    }

    private RectTransform CreateFrameLine(string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform line = CreateImage(name, centerPanel, primaryBlue).rectTransform;
        line.anchorMin = line.anchorMax = anchor;
        line.pivot = new Vector2(0.5f, 0.5f);
        line.anchoredPosition = anchoredPosition;
        line.sizeDelta = size;
        return line;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, float fontSize, float width, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = Resources.Load<TMP_FontAsset>("Oswald-Bold SDF") ?? TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.color = Color.white;

        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);

        return text;
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void SetAccentColor(Color accentColor)
    {
        Color frameColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.78f);
        Color dimColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.16f);
        Color railColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.32f);
        Color gateColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.13f);

        SetImageColor(leftGate, gateColor);
        SetImageColor(rightGate, gateColor);
        SetImageColor(topRail, railColor);
        SetImageColor(bottomRail, railColor);
        SetImageColor(progressFill, frameColor);
        SetImageColor(scanline, new Color(accentColor.r, accentColor.g, accentColor.b, 0.26f));
        SetImageColor(sweepHighlight, new Color(iceBlue.r, iceBlue.g, iceBlue.b, 0.22f));

        statusText.color = new Color(accentColor.r, accentColor.g, accentColor.b, 1f);
        stageText.color = Color.white;
        stageText.outlineColor = accentColor;
        subText.color = new Color(iceBlue.r, iceBlue.g, iceBlue.b, 1f);
        stageText.colorGradient = new VertexGradient(Color.white, Color.white, iceBlue, accentColor);
        stageText.enableVertexGradient = true;

        for (int i = 0; i < scanLines.Length; i++)
        {
            SetImageColor(scanLines[i], dimColor);
        }

        for (int i = 0; i < frameLines.Length; i++)
        {
            SetImageColor(frameLines[i], frameColor);
        }

        for (int i = 0; i < dataTicks.Length; i++)
        {
            SetImageColor(dataTicks[i], dimColor);
        }
    }

    private void AnimateFrame(Color accentColor, float visibility, float pulse, float normalizedTime)
    {
        for (int i = 0; i < frameLines.Length; i++)
        {
            float offsetPulse = 0.5f + Mathf.Sin((normalizedTime * 18f) + i * 0.75f) * 0.5f;
            SetImageColor(frameLines[i], new Color(accentColor.r, accentColor.g, accentColor.b, visibility * Mathf.Lerp(0.34f, 0.86f, offsetPulse * pulse)));
        }
    }

    private void AnimateScanLines(Color accentColor, float visibility, float normalizedTime)
    {
        for (int i = 0; i < scanLines.Length; i++)
        {
            float wave = 0.5f + Mathf.Sin((normalizedTime * 20f) + i * 0.42f) * 0.5f;
            float y = Mathf.Lerp(-470f, 470f, i / Mathf.Max(1f, scanLines.Length - 1f));
            scanLines[i].anchoredPosition = new Vector2(0f, y + Mathf.Sin((normalizedTime * 10f) + i) * 5f);
            SetImageColor(scanLines[i], new Color(accentColor.r, accentColor.g, accentColor.b, visibility * Mathf.Lerp(0.025f, 0.12f, wave)));
        }
    }

    private void AnimateDataTicks(Color accentColor, float visibility, float normalizedTime)
    {
        for (int i = 0; i < dataTicks.Length; i++)
        {
            bool left = i % 2 == 0;
            float flicker = 0.5f + Mathf.Sin((normalizedTime * 32f) + i * 1.7f) * 0.5f;
            float x = (left ? 44f : -44f) + (left ? 1f : -1f) * Mathf.Lerp(0f, 34f, flicker);
            dataTicks[i].anchoredPosition = new Vector2(x, dataTicks[i].anchoredPosition.y);
            SetImageColor(dataTicks[i], new Color(accentColor.r, accentColor.g, accentColor.b, visibility * Mathf.Lerp(0.08f, 0.36f, flicker)));
        }
    }

    private void SetImageColor(RectTransform rectTransform, Color color)
    {
        Image image = rectTransform.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }
    }

    private void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private Color GetNeonBlueAccent(Color requestedAccent)
    {
        Color softened = Color.Lerp(primaryBlue, requestedAccent, 0.28f);
        softened.r = Mathf.Min(softened.r, 0.22f);
        softened.g = Mathf.Max(softened.g, 0.55f);
        softened.b = Mathf.Max(softened.b, 0.95f);
        softened.a = 1f;
        return softened;
    }

    private float Smooth(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
