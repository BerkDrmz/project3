using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MenuEffects : MonoBehaviour
{
    private TMP_FontAsset font;
    private List<RectTransform> particles = new List<RectTransform>();
    private List<Vector2> velocities = new List<Vector2>();
    private RectTransform scanline;
    private RectTransform self;

    public void Init(TMP_FontAsset f) { font = f; }

    void Start()
    {
        self = GetComponent<RectTransform>();
        if (self == null) self = gameObject.AddComponent<RectTransform>();
        self.anchorMin = Vector2.zero; self.anchorMax = Vector2.one;
        self.offsetMin = self.offsetMax = Vector2.zero;

        for (int i = 0; i < 18; i++) CreateParticle();

        var sl = new GameObject("Scanline");
        sl.transform.SetParent(transform, false);
        var img = sl.AddComponent<Image>();
        img.color = new Color(0f, 0.85f, 1f, 0.06f);
        img.raycastTarget = false;
        scanline = sl.GetComponent<RectTransform>();
        scanline.anchorMin = new Vector2(0f, 0.5f);
        scanline.anchorMax = new Vector2(1f, 0.5f);
        scanline.sizeDelta = new Vector2(0f, 2f);
    }

    void CreateParticle()
    {
        var p = new GameObject("P");
        p.transform.SetParent(transform, false);
        var img = p.AddComponent<Image>();
        img.color = new Color(0f, 0.85f, 1f, Random.Range(0.15f, 0.45f));
        img.raycastTarget = false;
        var rt = p.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        float size = Random.Range(2f, 4f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(
            Random.Range(-960f, 960f),
            Random.Range(-540f, 540f));
        particles.Add(rt);
        velocities.Add(new Vector2(
            Random.Range(-3f, 3f),
            Random.Range(8f, 22f)));
    }

    void Update()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            var rt = particles[i];
            rt.anchoredPosition += velocities[i] * Time.deltaTime;
            if (rt.anchoredPosition.y > 540f)
            {
                rt.anchoredPosition = new Vector2(Random.Range(-960f, 960f), -540f);
            }
        }

        if (scanline != null)
        {
            float y = Mathf.Sin(Time.time * 0.4f) * 480f;
            scanline.anchoredPosition = new Vector2(0f, y);
        }
    }
}