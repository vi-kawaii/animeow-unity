using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 0.4f, 0f);

    [Header("Camera Settings")]
    public float sensitivity = 0.15f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;

    [Header("Zoom Settings")]
    public float minDistance = 1.5f;
    public float maxDistance = 8f;
    public float zoomSensitivity = 0.5f;
    public float zoomSmoothTime = 0.1f;

    [Header("Genshin-like Smoothness (Inertia)")]
    public float rotationSmoothTime = 0.1f;

    [Header("Camera Collision (Obstacles)")]
    public LayerMask collisionLayers = ~0;
    public float cameraRadius = 0.3f;

    // Переменные вращения
    private float targetRotationX;
    private float targetRotationY;
    private float currentRotationX;
    private float currentRotationY;
    private float rotationXVelocity;
    private float rotationYVelocity;

    // Переменные зума
    private float targetDistance;
    private float currentDistance;
    private float zoomVelocity;

    void Start()
    {
        GetComponent<Camera>().tag = "MainCamera";

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target == null && transform.parent != null)
        {
            PlayerController player = transform.parent.GetComponentInChildren<PlayerController>();
            if (player != null) target = player.transform;
        }

        Vector3 angles = transform.eulerAngles;
        targetRotationX = currentRotationX = angles.y;
        targetRotationY = currentRotationY = angles.x;

        targetDistance = currentDistance = (minDistance + maxDistance) / 2f;
    }

    void LateUpdate()
    {
        if (target == null) return;

        var mouse = Mouse.current;
        float mouseX = 0f;
        float mouseY = 0f;
        float scrollInput = 0f;

        if (mouse != null)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            mouseX = mouseDelta.x;
            mouseY = mouseDelta.y;
            scrollInput = mouse.scroll.ReadValue().y;
        }

        // 1. ВРАЩЕНИЕ
        targetRotationX += mouseX * sensitivity;
        targetRotationY -= mouseY * sensitivity;
        targetRotationY = Mathf.Clamp(targetRotationY, minVerticalAngle, maxVerticalAngle);

        currentRotationX = Mathf.SmoothDampAngle(currentRotationX, targetRotationX, ref rotationXVelocity, rotationSmoothTime);
        currentRotationY = Mathf.SmoothDampAngle(currentRotationY, targetRotationY, ref rotationYVelocity, rotationSmoothTime);

        // 2. РАСЧЕТ МАКСИМАЛЬНО ДОСТУПНОЙ ДИСТАНЦИИ (РЕЙКАСТ НАПЕРЕД)
        Quaternion currentRotation = Quaternion.Euler(currentRotationY, currentRotationX, 0);
        Vector3 targetPosition = target.position + targetOffset;
        Vector3 rayDirection = (currentRotation * Vector3.back).normalized;

        // Пускаем луч на абсолютно максимальный зум, чтобы узнать, где стена
        float absoluteMaxDistance = maxDistance;
        RaycastHit hit;
        if (Physics.SphereCast(targetPosition, cameraRadius, rayDirection, out hit, maxDistance, collisionLayers))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target))
            {
                // Стена найдена! Теперь это наш новый жесткий потолок для зума
                absoluteMaxDistance = Mathf.Max(minDistance, hit.distance);
            }
        }

        // 3. УМНЫЙ ЗУМ (Без холостого хода)
        // Если из-за приближения к стене целевой зум оказался "за текстурой", сжимаем его до границы стены
        if (targetDistance > absoluteMaxDistance)
        {
            targetDistance = absoluteMaxDistance;
        }

        // Считываем колесико мыши
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            targetDistance -= scrollInput * zoomSensitivity;
        }

        // Ограничиваем ввод колесика: снизу — минимальным зумом, сверху — текущей стеной (или maxDistance)
        targetDistance = Mathf.Clamp(targetDistance, minDistance, absoluteMaxDistance);

        // 4. ПЛАВНОЕ ПЕРЕМЕЩЕНИЕ КАМЕРЫ к новой скорректированной цели
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);

        // Страховочный зажим на случай, если стена сама резко двинулась на игрока
        if (currentDistance > absoluteMaxDistance)
        {
            currentDistance = absoluteMaxDistance;
            zoomVelocity = 0f;
        }

        // 5. ЖЕЛЕЗОБЕТОННОЕ ПРИМЕНЕНИЕ КООРДИНАТ
        transform.position = targetPosition + (rayDirection * currentDistance);
        transform.rotation = currentRotation;
    }
}
