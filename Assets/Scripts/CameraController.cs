using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public float smoothTime = 0.15f; //카메라가 부드럽게 이동하는 시간
    private Vector3 velocity = Vector3.zero;

    private BoxCollider2D currentSectionBounds; // 현재섹션경계
    private Camera mainCamera;
    private float cameraHalfHeight;
    private float cameraHalfWidth;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        CalculateCameraDimensions();
    }
    private void CalculateCameraDimensions()
    {
        cameraHalfHeight = mainCamera.orthographicSize;
        cameraHalfWidth = cameraHalfHeight * mainCamera.aspect;
    }

    private void Update()
    {
        if (currentSectionBounds != null)
        {   // 현재 섹션 내에서 플레이어 따라가기 but 섹션 경계안넘게
            Vector3 targetPosition = new Vector3(player.position.x, player.position.y, transform.position.z);
            float minX = currentSectionBounds.bounds.min.x + cameraHalfWidth;
            float maxX = currentSectionBounds.bounds.max.x - cameraHalfWidth;
            float minY = currentSectionBounds.bounds.min.y + cameraHalfHeight;
            float maxY = currentSectionBounds.bounds.max.y - cameraHalfHeight;

            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }

    public void SetCurrentSection(BoxCollider2D sectionBounds)
    {
        currentSectionBounds = sectionBounds;

        //섹션바뀌고 카메라 위치
        StartCoroutine(AdjustCameraPosition());
    }

    private IEnumerator AdjustCameraPosition()
    {
        //플레이어 위치 따라서 이쁘게
        Vector3 targetPosition = new Vector3(player.position.x, player.position.y, transform.position.z);

        float minX = currentSectionBounds.bounds.min.x + cameraHalfWidth;
        float maxX = currentSectionBounds.bounds.max.x - cameraHalfWidth;
        float minY = currentSectionBounds.bounds.min.y + cameraHalfHeight;
        float maxY = currentSectionBounds.bounds.max.y - cameraHalfHeight;

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        float elapsedTime = 0f;
        Vector3 initialPosition = transform.position;

        while (elapsedTime < smoothTime)
        {
            transform.position = Vector3.Lerp(initialPosition, targetPosition, (elapsedTime / smoothTime));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
    }

}
