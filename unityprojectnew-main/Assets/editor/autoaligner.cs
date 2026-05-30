using UnityEngine;
using UnityEditor;

public class AutoAligner : MonoBehaviour
{
    // Unity'nin üst menüsüne "Tools" adında yeni bir sekme ekler
    [MenuItem("Tools/Arenayı Otomatik Hizala")]
    public static void AlignSelectedObjects()
    {
        // Sadece seçili objeler üzerinde işlem yap
        foreach (GameObject obj in Selection.gameObjects)
        {
            // Pozisyonları en yakın tam sayıya yuvarla (Örn: -4.3 -> -4.0)
            Vector3 pos = obj.transform.position;
            pos.x = Mathf.Round(pos.x);
            pos.y = Mathf.Round(pos.y);
            pos.z = Mathf.Round(pos.z);
            obj.transform.position = pos;

            // Rotasyonu Y ekseninde en yakın 90'ın katına yuvarla (Örn: 100 -> 90)
            Vector3 euler = obj.transform.eulerAngles;
            euler.y = Mathf.Round(euler.y / 90f) * 90f;
            
            // Duvarların tam dik durması için X ve Z rotasyonlarını sıfırla
            euler.x = 0f;
            euler.z = 0f;
            
            obj.transform.eulerAngles = euler;
        }
        
        Debug.Log("Seçili duvarlar başarıyla hizalandı!");
    }
}