using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 8.5f;
    public float rotationSpeed = 15f;

    [Header("Genshin Dash/Sprint System")]
    public float dashForce = 18f;
    public float dashDuration = 0.2f;
    public float dashComboWindow = 0.5f;
    public float globalDashCooldown = 1.5f;

    [Header("Snappy Jump Settings")]
    public float jumpHeight = 1.3f;
    public float gravity = -35f;

    [Header("Jump Momentum (Air Inertia)")]
    public float farJumpWindow = 0.15f;
    public float farJumpMultiplier = 1.6f;
    [Range(0f, 10f)]
    [Tooltip("Как быстро гасится импульс прыжка в воздухе (чем меньше, тем дальше летит куб)")]
    public float airInertiaDamping = 2.5f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private float verticalVelocity;

    // Состояния передвижения
    private bool isSprinting;
    private bool isDashing;
    private float dashTimer;

    // Переменные комбо и кулдауна рывков
    private int dashCount = 0;
    private float dashComboTimer = 0f;
    private float cooldownTimer = 0f;
    private Vector3 dashDirection;

    // Системные переменные для плавного скольжения в воздухе
    private float timeSinceLastDashEnded = 99f;
    private Vector3 currentAirHorizontalVelocity; // Текущая горизонтальная скорость в полете

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && transform.parent != null)
        {
            cameraTransform = transform.parent.GetComponentInChildren<Camera>()?.transform;
        }

        // Зануляем минимальную дистанцию сдвига, чтобы контроллер не жрал микро-перемещения
        controller.minMoveDistance = 0f;
    }

    void Update()
    {
        // Никаких пауэр-стопов и return! Все системы работают параллельно.
        HandleTimers();
        HandleInput();
        HandleMovement();
    }

    private void HandleTimers()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0) dashCount = 0;
        }

        if (dashComboTimer > 0 && cooldownTimer <= 0)
        {
            dashComboTimer -= Time.deltaTime;
            if (dashComboTimer <= 0) dashCount = 0;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
                timeSinceLastDashEnded = 0f;
            }
        }
        else
        {
            timeSinceLastDashEnded += Time.deltaTime;
        }
    }

    private bool CheckIfGrounded()
    {
        float rayLength = (controller.height / 2f) + 0.1f;
        return Physics.Raycast(transform.position, Vector3.down, rayLength);
    }

    void HandleInput()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null && mouse == null) return;

        bool isPlayerGrounded = CheckIfGrounded();

        // Модифицируем: считываем мышь для рывка и спринта ТОЛЬКО если Alt НЕ зажат
        bool isAltPressed = ThirdPersonCamera.IsCursorUnlocked;

        bool isDashPressedThisFrame = (keyboard != null && keyboard.leftShiftKey.wasPressedThisFrame) ||
                                      (!isAltPressed && mouse != null && mouse.rightButton.wasPressedThisFrame);

        bool isSprintHeld = (keyboard != null && keyboard.leftShiftKey.isPressed) ||
                            (!isAltPressed && mouse != null && mouse.rightButton.isPressed);

        // 1. ЛОГИКА РЫВКОВ
        if (isDashPressedThisFrame && cooldownTimer <= 0 && isPlayerGrounded)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCount++;
            dashComboTimer = dashComboWindow;

            if (dashCount >= 2)
            {
                cooldownTimer = globalDashCooldown;
                dashComboTimer = 0f;
            }

            Vector3 inputDir = GetInputDirection();
            if (inputDir.magnitude < 0.1f)
            {
                dashDirection = transform.forward;
            }
            else
            {
                dashDirection = CalculateMoveDirection(inputDir);
            }
        }

        // 2. СПРИНТ
        if (isSprintHeld && !isDashing && isPlayerGrounded)
        {
            isSprinting = true;
        }
        else if (!isSprintHeld)
        {
            isSprinting = false;
        }

        // 3. ПРЫЖОК С РАСЧЕТОМ НАЧАЛЬНОГО ИМПУЛЬСА
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && isPlayerGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            float speedOnGround = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 inputDir = GetInputDirection();
            Vector3 directionOnGround = (inputDir.magnitude >= 0.1f) ? CalculateMoveDirection(inputDir) : transform.forward;

            // Задаем стартовый вектор горизонтальной скорости для полета
            if (isDashing || timeSinceLastDashEnded <= farJumpWindow)
            {
                // Рывочный супер-прыжок
                currentAirHorizontalVelocity = dashDirection * (dashForce * farJumpMultiplier);
                isDashing = false;
            }
            else
            {
                // Обычный прыжок (если прыгнули без WASD, скорость 0)
                currentAirHorizontalVelocity = (inputDir.magnitude >= 0.1f) ? (directionOnGround * speedOnGround) : Vector3.zero;
            }
        }
    }

    void HandleMovement()
    {
        Vector3 inputDirection = GetInputDirection();
        Vector3 moveVelocity = Vector3.zero;

        bool isPlayerGrounded = CheckIfGrounded();

        // Если мы в воздухе
        if (!isPlayerGrounded)
        {
            // Рассчитываем, куда игрок ХОЧЕТ лететь с помощью WASD в воздухе
            Vector3 targetAirVelocity = Vector3.zero;
            if (inputDirection.magnitude >= 0.1f)
            {
                targetAirVelocity = CalculateMoveDirection(inputDirection) * (isSprinting ? sprintSpeed : walkSpeed);
            }

            // ПЛАВНОЕ ЗАТУХАНИЕ ИНЕРЦИИ
            currentAirHorizontalVelocity = Vector3.Lerp(currentAirHorizontalVelocity, targetAirVelocity, airInertiaDamping * Time.deltaTime);
            moveVelocity = currentAirHorizontalVelocity;

            // ЖЕСТКИЙ ЗАПРЕТ ПОВОРОТА В ВОЗДУХЕ:
            // Строчка с изменением transform.rotation здесь просто удалена. 
            // Куб летит по инерции, но физически не может повернуться лицом в другую сторону, пока не коснется земли.
        }
        // Если мы на земле
        else
        {
            if (isDashing)
            {
                moveVelocity = dashDirection * dashForce;
                if (dashDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(dashDirection);
                }
            }
            else
            {
                if (inputDirection.magnitude >= 0.1f)
                {
                    Vector3 moveDir = CalculateMoveDirection(inputDirection);
                    float speed = isSprinting ? sprintSpeed : walkSpeed;
                    moveVelocity = moveDir * speed;

                    // На земле поворот работает штатно и плавно
                    Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }

        // Расчет гравитации
        if (isPlayerGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Финальный расчет перемещения
        Vector3 finalVelocity = moveVelocity + new Vector3(0, verticalVelocity, 0);
        controller.Move(finalVelocity * Time.deltaTime);
    }

    private Vector3 GetInputDirection()
    {
        Vector2 moveInput = Vector2.zero;
        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1f;
        }

        return new Vector3(moveInput.x, 0f, moveInput.y).normalized;
    }

    private Vector3 CalculateMoveDirection(Vector3 inputDir)
    {
        if (cameraTransform == null) return inputDir;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        return (camForward * inputDir.z + camRight * inputDir.x).normalized;
    }
}
