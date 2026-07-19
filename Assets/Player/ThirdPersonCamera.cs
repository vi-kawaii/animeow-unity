using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    // Статический флаг, доступный для PlayerController
    public static bool IsCursorUnlocked { get; private set; } = false;

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

    private float targetRotationX;
    private float targetRotationY;
    private float currentRotationX;
    private float currentRotationY;
    private float rotationXVelocity;
    private float rotationYVelocity;

    private float targetDistance;
    private float currentDistance;
    private float zoomVelocity;

    void Start()
    {
        GetComponent<Camera>().tag = "MainCamera";

        // Стартовое состояние курсора
        LockCursor();

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

        // Включаем/выключаем курсор по Alt
        HandleCursorToggle();

        var mouse = Mouse.current;
        float mouseX = 0f;
        float mouseY = 0f;
        float scrollInput = 0f;

        if (mouse != null)
        {
            // Читаем дельту мыши ТОЛЬКО если Alt НЕ зажат
            if (!IsCursorUnlocked)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                mouseX = mouseDelta.x;
                mouseY = mouseDelta.y;
            }
            scrollInput = mouse.scroll.ReadValue().y;
        }

        // 1. ВРАЩЕНИЕ (углы изменятся только если Alt не зажат, иначе камера плавно держит старый ракурс)
        targetRotationX += mouseX * sensitivity;
        targetRotationY -= mouseY * sensitivity;
        targetRotationY = Mathf.Clamp(targetRotationY, minVerticalAngle, maxVerticalAngle);

        currentRotationX = Mathf.SmoothDampAngle(currentRotationX, targetRotationX, ref rotationXVelocity, rotationSmoothTime);
        currentRotationY = Mathf.SmoothDampAngle(currentRotationY, targetRotationY, ref rotationYVelocity, rotationSmoothTime);

        Quaternion currentRotation = Quaternion.Euler(currentRotationY, currentRotationX, 0);

        // 2. РАСЧЕТ И ПРИМЕНЕНИЕ КООРДИНАТ И КОЛЛИЗИЙ (камера продолжает лететь за игроком!)
        ApplyCameraTransform(currentRotation, scrollInput);
    }

    private void HandleCursorToggle()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Проверяем удержание клавиши Alt (Left Alt)
        if (keyboard.leftAltKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }
        else if (keyboard.leftAltKey.wasReleasedThisFrame)
        {
            LockCursor();
        }
    }

    private void UnlockCursor()
    {
        IsCursorUnlocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ПРИНУДИТЕЛЬНЫЙ СБРОС ФОКУСА ДЛЯ UI TOOLKIT В UNITY 6
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void LockCursor()
    {
        IsCursorUnlocked = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Перепишем ApplyCameraTransform, чтобы колесико мыши работало всегда
    private void ApplyCameraTransform(Quaternion rotation, float scrollInput)
    {
        Vector3 targetPosition = target.position + targetOffset;
        Vector3 rayDirection = (rotation * Vector3.back).normalized;

        float absoluteMaxDistance = maxDistance;
        RaycastHit hit;
        if (Physics.SphereCast(targetPosition, cameraRadius, rayDirection, out hit, maxDistance, collisionLayers))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target))
            {
                absoluteMaxDistance = Mathf.Max(minDistance, hit.distance);
            }
        }

        if (targetDistance > absoluteMaxDistance)
        {
            targetDistance = absoluteMaxDistance;
        }

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            targetDistance -= scrollInput * zoomSensitivity;
        }

        targetDistance = Mathf.Clamp(targetDistance, minDistance, absoluteMaxDistance);
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);

        if (currentDistance > absoluteMaxDistance)
        {
            currentDistance = absoluteMaxDistance;
            zoomVelocity = 0f;
        }

        transform.position = targetPosition + (rayDirection * currentDistance);
        transform.rotation = rotation;
    }
}
