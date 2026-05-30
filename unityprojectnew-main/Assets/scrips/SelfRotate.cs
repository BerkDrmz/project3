using UnityEngine;

public class SelfRotate : MonoBehaviour
{
    public float donmeHizi = 60f;

    void Update()
    {
transform.Rotate(0f, 0f, donmeHizi * Time.deltaTime); // Z ekseninde döner
}
}