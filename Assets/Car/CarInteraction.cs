using UnityEngine;
using UnityEngine.InputSystem;

public class CarControllerWithSystem : MonoBehaviour
{
    [Header("Явные ссылки (Только Экшены)")]
    public InputActionReference moveActionRef;     // Экшен WASD (Vector2)
    public InputActionReference brakeActionRef;    // Экшен Пробела (Button)
    public InputActionReference interactActionRef; // Экшен кнопки Е (Button)

    [Header("Настройки автомобиля")]
    public float motorForce = 1500f;
    public float brakeForce = 3000f;
    public float maxSteerAngle = 30f;

    [Header("Состояние")]
    public bool isPlayerInside = false;

    // Внутренние переменные (машина найдет всё сама при старте)
    private GameObject playerObject;
    private Transform doorTransform;
    private WheelCollider FL, FR, BL, BR;
    private Transform visualFL, visualFR, visualBL, visualBR;
    private ThirdPersonCamera cameraScript;
    private Transform playerCameraTarget;

    private Vector2 movementInput;
    private bool isBraking;
    private bool isPlayerNearDoor = false;

    void Start()
    {
        // 1. Автоматически находим дочерний объект двери внутри машины
        GameObject doorObj = transform.Find("Door")?.gameObject;
        if (doorObj != null)
        {
            doorTransform = doorObj.transform;

            // Динамически вешаем физический мост на Door, чтобы ловить события коллайдера
            CarTriggerBridge bridge = doorObj.GetComponent<CarTriggerBridge>();
            if (bridge == null) bridge = doorObj.AddComponent<CarTriggerBridge>();
            bridge.Initialize(this);
        }
        else
        {
            Debug.LogError("Внутри CarPrefab не найден дочерний объект с именем 'Door'!");
        }

        // 2. Автоматически находим все колеса внутри машины
        FindWheelsAutomatically();
    }

    private void FindWheelsAutomatically()
    {
        Transform wheelsGroup = transform.Find("Wheels");
        if (wheelsGroup != null)
        {
            FL = FindComponentInChild<WheelCollider>(wheelsGroup, "FL");
            FR = FindComponentInChild<WheelCollider>(wheelsGroup, "FR");
            BL = FindComponentInChild<WheelCollider>(wheelsGroup, "BL");
            BR = FindComponentInChild<WheelCollider>(wheelsGroup, "BR");

            visualFL = FindVisualSmart(wheelsGroup, "FL");
            visualFR = FindVisualSmart(wheelsGroup, "FR");
            visualBL = FindVisualSmart(wheelsGroup, "BL");
            visualBR = FindVisualSmart(wheelsGroup, "BR");
        }
    }

    void OnEnable()
    {
        if (moveActionRef != null) moveActionRef.action.Enable();
        if (brakeActionRef != null) brakeActionRef.action.Enable();
        if (interactActionRef != null) interactActionRef.action.Enable();
    }

    void OnDisable()
    {
        if (moveActionRef != null) moveActionRef.action.Disable();
        if (brakeActionRef != null) brakeActionRef.action.Disable();
        if (interactActionRef != null) interactActionRef.action.Disable();
    }

    void Update()
    {
        if (doorTransform == null || interactActionRef == null) return;

        // Посадка и высадка на кнопку E
        if (isPlayerNearDoor && !isPlayerInside && interactActionRef.action.triggered)
        {
            GetInCar();
        }
        else if (isPlayerInside && interactActionRef.action.triggered)
        {
            GetOutCar();
        }

        if (!isPlayerInside)
        {
            movementInput = Vector2.zero;
            isBraking = false;
            return;
        }

        if (moveActionRef != null) movementInput = moveActionRef.action.ReadValue<Vector2>();
        if (brakeActionRef != null) isBraking = brakeActionRef.action.IsPressed();
    }

    void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
        UpdateWheelsVisuals();
    }

    // Эти методы вызываются физическим мостом из объекта Door
    public void OnPlayerEnterTrigger(GameObject player)
    {
        if (isPlayerInside) return;

        playerObject = player;
        isPlayerNearDoor = true;
        Debug.Log("<Color=Green>[CarSystem] Физический триггер: Игрок вошел в зону двери! Нажмите E.</Color>");
    }

    public void OnPlayerExitTrigger()
    {
        if (isPlayerInside) return;

        isPlayerNearDoor = false;
        playerObject = null;
        Debug.Log("<Color=Red>[CarSystem] Физический триггер: Игрок вышел из зоны двери.</Color>");
    }

    private void GetInCar()
    {
        isPlayerInside = true;
        isPlayerNearDoor = false; // Сбрасываем триггер, так как игрок теперь внутри машины

        // Достаем камеру из игрока
        cameraScript = playerObject.GetComponentInChildren<ThirdPersonCamera>(true);

        if (cameraScript != null)
        {
            playerCameraTarget = cameraScript.target;
            cameraScript.target = this.transform;
        }

        // Отключаем PlayerInput самого игрока
        PlayerInput playerInput = playerObject.GetComponentInChildren<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;

        playerObject.transform.SetParent(this.transform);
        playerObject.SetActive(false);
    }

    private void GetOutCar()
    {
        isPlayerInside = false;

        playerObject.transform.SetParent(null);
        playerObject.transform.position = doorTransform.position;
        playerObject.transform.rotation = doorTransform.rotation;
        playerObject.SetActive(true);

        // Включаем инпут игрока обратно
        PlayerInput playerInput = playerObject.GetComponentInChildren<PlayerInput>();
        if (playerInput != null) playerInput.enabled = true;

        // Возвращаем камеру обратно на игрока
        if (cameraScript != null)
        {
            cameraScript.target = playerCameraTarget ?? playerObject.transform;
            cameraScript = null;
        }
    }

    private void HandleMotor()
    {
        if (FL == null || FR == null) return;

        FL.motorTorque = movementInput.y * motorForce;
        FR.motorTorque = movementInput.y * motorForce;

        float currentBrake = isBraking ? brakeForce : 0f;
        FL.brakeTorque = currentBrake;
        FR.brakeTorque = currentBrake;
        if (BL != null) BL.brakeTorque = currentBrake;
        if (BR != null) BR.brakeTorque = currentBrake;
    }

    private void HandleSteering()
    {
        if (FL == null || FR == null) return;

        float currentSteerAngle = movementInput.x * maxSteerAngle;
        FL.steerAngle = currentSteerAngle;
        FR.steerAngle = currentSteerAngle;
    }

    private void UpdateWheelsVisuals()
    {
        if (FL && visualFL) UpdateSingleWheel(FL, visualFL);
        if (FR && visualFR) UpdateSingleWheel(FR, visualFR);
        if (BL && visualBL) UpdateSingleWheel(BL, visualBL);
        if (BR && visualBR) UpdateSingleWheel(BR, visualBR);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos; Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    private T FindComponentInChild<T>(Transform parent, string name) where T : Component
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                T component = child.GetComponent<T>();
                if (component != null) return component;
            }
        }
        return null;
    }

    private Transform FindVisualSmart(Transform parent, string partOfName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Contains(partOfName) && child.GetComponent<WheelCollider>() == null) return child;
        }
        return null;
    }
}

// Вспомогательный класс-мост. Создавать отдельный файл для него НЕ нужно.
// Он висит на дочерней Door и перенаправляет события триггера в корень машины.
public class CarTriggerBridge : MonoBehaviour
{
    private CarControllerWithSystem mainController;

    public void Initialize(CarControllerWithSystem controller)
    {
        mainController = controller;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Реагируем на объект с тегом Player или компонентом CharacterController
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            mainController.OnPlayerEnterTrigger(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            mainController.OnPlayerExitTrigger();
        }
    }
}
