using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    private CharacterController karakterKontrolcu;
    private Transform kameraTransform;

    public float hareketHizi = 350f;
    public float donusHizi = 720f;

    public float yercekimi = -15f;
    private Vector3 dususHizi;

    private string mevcutAnimasyon;
    private bool aksiyonVarMi;

    [Header("--- YUMRUK SALDIRISI ---")]
    public float yumrukHasari = 25f;
    public float yumrukMenzili = 120f;
    public float yumrukYaricapi = 70f;
    public float yumrukVurusYuksekligi = 90f;
    public float yumrukVurusGecikmesi = 0.35f;
    public float yumrukAnimasyonSuresi = 2.18f;
    public GameObject yumrukHitEfektiPrefab;
    public Transform sagYumrukEfektNoktasi;
    public Transform solYumrukEfektNoktasi;
    public float yumrukHitEfektiOlcegi = 0.35f;
    public float yumrukHitEfektiSuresi = 0.65f;
    public Vector3 yumrukHitEfektiOffset = Vector3.zero;

    [Header("--- ZEUS GUCU (E YETENEGI) ---")]
    public GameObject yildirimPrefab;
    public GameObject vurusEfektiPrefab;

    public float yetenekHasari = 50f;
    public float saldiriMenzili = 15f;
    public float beklemeSuresi = 10f;
    public float efektGecikmesi = 0.5f;
    public float buyuAnimasyonSuresi = 1.5f;
    private float sonKullanimZamani = -100f;

    private readonly Collider[] yumrukSonuclari = new Collider[32];
    private readonly HashSet<DusmanCan> yumruktaVurulanDusmanlar = new HashSet<DusmanCan>();

    private void Start()
    {
        animator = GetComponent<Animator>();
        karakterKontrolcu = GetComponent<CharacterController>();
        CacheCamera();
        ResolveYumrukEfektNoktalari();
    }

    private void Update()
    {
        SavasKontrolu();

        if (!aksiyonVarMi)
        {
            FizikVeHareketKontrolu();
        }
    }

    private void CacheCamera()
    {
        if (Camera.main != null)
        {
            kameraTransform = Camera.main.transform;
        }
    }

    private void FizikVeHareketKontrolu()
    {
        if (karakterKontrolcu == null)
        {
            return;
        }

        if (kameraTransform == null)
        {
            CacheCamera();
            if (kameraTransform == null)
            {
                return;
            }
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 kameraIleri = kameraTransform.forward;
        Vector3 kameraSag = kameraTransform.right;

        kameraIleri.y = 0f;
        kameraSag.y = 0f;
        kameraIleri.Normalize();
        kameraSag.Normalize();

        Vector3 hareketYonu = (kameraIleri * moveZ + kameraSag * moveX).normalized;
        Vector3 yatayHareket = hareketYonu * hareketHizi;

        bool isMoving = yatayHareket.magnitude > 0.1f;

        if (karakterKontrolcu.isGrounded && dususHizi.y < 0)
        {
            dususHizi.y = -2f;
        }
        else
        {
            dususHizi.y += yercekimi * Time.deltaTime;
        }

        Vector3 sonHareket = yatayHareket;
        sonHareket.y = dususHizi.y;

        karakterKontrolcu.Move(sonHareket * Time.deltaTime);

        if (isMoving)
        {
            Quaternion yeniYon = Quaternion.LookRotation(hareketYonu, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, yeniYon, donusHizi * Time.deltaTime);
            AnimasyonDegistir("Fast Run", 0.1f);
        }
        else
        {
            AnimasyonDegistir("Bouncing Fight Idle", 0.1f);
        }
    }

    private void SavasKontrolu()
    {
        bool eYetenegiKullanildi = Input.GetKeyDown(KeyCode.E);

        if (Input.GetMouseButton(1))
        {
            aksiyonVarMi = true;
            AnimasyonDegistir("Block", 0.05f);

            if (karakterKontrolcu != null)
            {
                Vector3 geriKayma = transform.TransformDirection(Vector3.back) * (hareketHizi * 0.2f);
                geriKayma.y = -5f;
                karakterKontrolcu.Move(geriKayma * Time.deltaTime);
            }
        }
        else if (Input.GetMouseButtonDown(0) && !aksiyonVarMi)
        {
            aksiyonVarMi = true;
            AnimasyonDegistir("Punch Combo", 0.05f);
            Invoke(nameof(AksiyonuBitir), yumrukAnimasyonSuresi);
        }
        else if (eYetenegiKullanildi && !aksiyonVarMi)
        {
            if (Time.time >= sonKullanimZamani + beklemeSuresi)
            {
                aksiyonVarMi = true;
                AnimasyonDegistir("BuyuAnim", 0.1f);

                sonKullanimZamani = Time.time;

                Invoke(nameof(TopluYildirimCak), efektGecikmesi);
                Invoke(nameof(AksiyonuBitir), buyuAnimasyonSuresi);
            }
            else
            {
                float kalanSure = (sonKullanimZamani + beklemeSuresi) - Time.time;
                Debug.Log("Zeus gucu sarj oluyor! Kalan saniye: " + kalanSure.ToString("F1"));
            }
        }
        else if (!Input.GetMouseButton(1) && mevcutAnimasyon != "Punch Combo" && mevcutAnimasyon != "BuyuAnim")
        {
            aksiyonVarMi = false;
        }
    }

    public void OnPunchHit()
    {
        YumrukVurusunuUygula(null);
    }

    public void OnRightPunchHit()
    {
        YumrukVurusunuUygula(sagYumrukEfektNoktasi);
    }

    public void OnLeftPunchHit()
    {
        YumrukVurusunuUygula(solYumrukEfektNoktasi);
    }

    private void YumrukVurusunuUygula(Transform tekYumrukNoktasi)
    {
        if (mevcutAnimasyon != "Punch Combo")
        {
            return;
        }

        if (sagYumrukEfektNoktasi == null || solYumrukEfektNoktasi == null)
        {
            ResolveYumrukEfektNoktalari();
        }

        yumruktaVurulanDusmanlar.Clear();

        if (tekYumrukNoktasi != null)
        {
            YumrukNoktasindanHasarVer(tekYumrukNoktasi);
            return;
        }

        YumrukNoktasindanHasarVer(sagYumrukEfektNoktasi);
        YumrukNoktasindanHasarVer(solYumrukEfektNoktasi);
    }

    private void YumrukNoktasindanHasarVer(Transform yumrukNoktasi)
    {
        if (yumrukNoktasi == null)
        {
            yumrukNoktasi = transform;
        }

        int temasSayisi = Physics.OverlapSphereNonAlloc(
            yumrukNoktasi.position,
            yumrukYaricapi,
            yumrukSonuclari,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < temasSayisi; i++)
        {
            Collider temas = yumrukSonuclari[i];
            if (temas == null)
            {
                continue;
            }

            DusmanCan dusmaninCani = temas.GetComponentInParent<DusmanCan>();
            if (dusmaninCani == null || !dusmaninCani.CompareTag("Enemy") || !yumruktaVurulanDusmanlar.Add(dusmaninCani))
            {
                continue;
            }

            dusmaninCani.HasarAl(yumrukHasari);
            Vector3 efektPozisyonu = temas.ClosestPoint(yumrukNoktasi.position);
            Vector3 vurusYonu = dusmaninCani.transform.position - yumrukNoktasi.position;
            if (vurusYonu.sqrMagnitude < 0.0001f)
            {
                vurusYonu = transform.forward;
            }

            YumrukHitEfektiOlustur(yumrukNoktasi, efektPozisyonu, vurusYonu.normalized);
        }
    }

    private void YumrukHitEfektiOlustur(Transform hedef, Vector3 pozisyon, Vector3 vurusYonu)
    {
        if (hedef == null || yumrukHitEfektiPrefab == null)
        {
            return;
        }

        Quaternion rotasyon = Quaternion.LookRotation(vurusYonu, Vector3.up);
        Vector3 offset = hedef.TransformVector(yumrukHitEfektiOffset);
        GameObject efekt = Instantiate(yumrukHitEfektiPrefab, pozisyon + offset, rotasyon);

        float hedefOlcegi = Mathf.Max(hedef.lossyScale.x, hedef.lossyScale.y, hedef.lossyScale.z);
        efekt.transform.localScale = Vector3.one * Mathf.Max(0.01f, yumrukHitEfektiOlcegi * hedefOlcegi);

        ParticleSystem[] parcalar = efekt.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem parca in parcalar)
        {
            ParticleSystem.MainModule main = parca.main;
            main.loop = false;
            parca.Clear(true);
            parca.Play(true);
        }

        Destroy(efekt, Mathf.Max(0.05f, yumrukHitEfektiSuresi));
    }

    private void ResolveYumrukEfektNoktalari()
    {
        Transform[] tumNoktalar = GetComponentsInChildren<Transform>(true);

        foreach (Transform nokta in tumNoktalar)
        {
            if (sagYumrukEfektNoktasi == null && nokta.name == "mixamorig:RightHand")
            {
                sagYumrukEfektNoktasi = nokta;
            }

            if (solYumrukEfektNoktasi == null && nokta.name == "mixamorig:LeftHand")
            {
                solYumrukEfektNoktasi = nokta;
            }

            if (sagYumrukEfektNoktasi != null && solYumrukEfektNoktasi != null)
            {
                return;
            }
        }
    }

    private void TopluYildirimCak()
    {
        Collider[] etraftakiler = Physics.OverlapSphere(transform.position, saldiriMenzili);
        HashSet<DusmanCan> hasarAlanDusmanlar = new HashSet<DusmanCan>();

        foreach (Collider obje in etraftakiler)
        {
            DusmanCan dusmaninCani = obje.GetComponentInParent<DusmanCan>();
            if (dusmaninCani == null || !hasarAlanDusmanlar.Add(dusmaninCani))
            {
                continue;
            }

            Vector3 dusmanPozisyonu = dusmaninCani.transform.position;

            if (yildirimPrefab != null)
            {
                GameObject go = Instantiate(yildirimPrefab, dusmanPozisyonu + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(go, 3f);
            }

            if (vurusEfektiPrefab != null)
            {
                GameObject vfx = Instantiate(vurusEfektiPrefab, dusmanPozisyonu + Vector3.up, Quaternion.identity);
                Destroy(vfx, 1.5f);
            }

            dusmaninCani.HasarAl(yetenekHasari);
        }
    }

    private void AksiyonuBitir()
    {
        aksiyonVarMi = false;
        mevcutAnimasyon = "";
        AnimasyonDegistir("Bouncing Fight Idle", 0.1f);
    }

    private void AnimasyonDegistir(string yeniAnimasyon, float gecisSuresi)
    {
        if (animator == null || mevcutAnimasyon == yeniAnimasyon)
        {
            return;
        }

        animator.CrossFadeInFixedTime(yeniAnimasyon, gecisSuresi);
        mevcutAnimasyon = yeniAnimasyon;
    }

    public float GetUltimateCharge01()
    {
        if (beklemeSuresi <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01((Time.time - sonKullanimZamani) / beklemeSuresi);
    }

    public float GetUltimateRemainingSeconds()
    {
        return Mathf.Max(0f, (sonKullanimZamani + beklemeSuresi) - Time.time);
    }

    public bool IsUltimateReady()
    {
        return GetUltimateCharge01() >= 1f;
    }
}
