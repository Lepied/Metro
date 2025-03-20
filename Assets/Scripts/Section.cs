using UnityEngine;

public class Section : MonoBehaviour
{
    private BoxCollider2D sectionBounds;
    
    private void Awake()
    {
        sectionBounds = GetComponent<BoxCollider2D>();
        sectionBounds.isTrigger = true;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            //플레이어가 여기 오면 알림
            FindObjectOfType<CameraController>().SetCurrentSection(sectionBounds);
        }
    }
}