using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Takip Edilecek Karakter")]
    public Transform target;

    [Header("Kamera Ayarları")]
    public Vector3 offset = new Vector3(0, 2, -5); 
    public float smoothSpeed = 5f; 

    private void LateUpdate()
    {
        if (target == null) return;

        // DİKKAT: Burada offset'i karakterin baktığı yöne (lokal koordinatlara) çevirdik!
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);
        
        // Kamerayı yumuşak bir şekilde yeni pozisyona taşı
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Kamera her zaman karakterin biraz yukarısına (sırtına/başına) baksın
        transform.LookAt(target.position + Vector3.up * 1.5f); 
    }
}