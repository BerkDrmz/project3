using UnityEngine;

public class DusmanCan : MonoBehaviour
{
    [Header("Dusman Ayarlari")]
    public float can = 100f;
    [SerializeField] private bool debugLogHits = false;

    private bool isDead;

    public void HasarAl(float hasarMiktari)
    {
        if (isDead)
        {
            return;
        }

        can -= hasarMiktari;

        if (debugLogHits)
        {
            Debug.Log(gameObject.name + " vuruldu! Kalan Can: " + can);
        }

        if (can <= 0f)
        {
            isDead = true;

            if (debugLogHits)
            {
                Debug.Log(gameObject.name + " YOK OLDU!");
            }

            Destroy(gameObject);
        }
    }
}
