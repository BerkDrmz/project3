using System.Collections.Generic;
using UnityEngine;

public class PlayerSkills : MonoBehaviour
{
    [Header("Efektler")]
    public GameObject yildirimPrefab;
    public GameObject vurusEfektiPrefab;

    [Header("Yetenek Ayarlari")]
    public float saldiriMenzili = 15f;
    public float beklemeSuresi = 10f;
    public float yetenekHasari = 50f;

    private float sonKullanimZamani = -100f;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (Time.time >= sonKullanimZamani + beklemeSuresi)
        {
            TopluYildirimCak();
            sonKullanimZamani = Time.time;
            return;
        }

        float kalanSure = (sonKullanimZamani + beklemeSuresi) - Time.time;
        Debug.Log("Zeus gucu sarj oluyor! Kalan saniye: " + kalanSure.ToString("F1"));
    }

    private void TopluYildirimCak()
    {
        Collider[] etraftakiler = Physics.OverlapSphere(transform.position, saldiriMenzili);
        HashSet<DusmanCan> etkilenenDusmanlar = new HashSet<DusmanCan>();

        foreach (Collider obje in etraftakiler)
        {
            DusmanCan dusmaninCani = obje.GetComponentInParent<DusmanCan>();
            if (dusmaninCani == null || !etkilenenDusmanlar.Add(dusmaninCani))
            {
                continue;
            }

            Vector3 dusmanPozisyonu = dusmaninCani.transform.position;

            if (yildirimPrefab != null)
            {
                GameObject yildirim = Instantiate(yildirimPrefab, dusmanPozisyonu, Quaternion.identity);
                Destroy(yildirim, 3f);
            }

            if (vurusEfektiPrefab != null)
            {
                GameObject vurusEfekti = Instantiate(vurusEfektiPrefab, dusmanPozisyonu + Vector3.up, Quaternion.identity);
                Destroy(vurusEfekti, 1.5f);
            }

            dusmaninCani.HasarAl(yetenekHasari);
        }
    }
}
