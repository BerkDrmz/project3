using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    [Header("Acilacak Parilti / Efekt Objesi")]
    public GameObject zaferPariltisi;

    private void Start()
    {
        if (zaferPariltisi != null)
        {
            zaferPariltisi.SetActive(false);
        }

        InvokeRepeating(nameof(DusmanlariKontrolEt), 1f, 1f);
    }

    private void DusmanlariKontrolEt()
    {
        DusmanCan[] dusmanlar = FindObjectsOfType<DusmanCan>();
        if (dusmanlar.Length != 0)
        {
            return;
        }

        if (zaferPariltisi != null)
        {
            zaferPariltisi.SetActive(true);
        }

        CancelInvoke(nameof(DusmanlariKontrolEt));
    }
}
