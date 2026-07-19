using UnityEngine;
using UnityEngine.UIElements; // Важно для UI Toolkit

public class DialogueManager : MonoBehaviour
{
    // Глобальная статическая ссылка для всех зон и нод
    public static DialogueManager Instance { get; private set; }

    private PanelRenderer panelRenderer;

    // Ссылки на элементы UI
    private Button interactButton;
    private VisualElement dialogueBox;

    // Ссылка на активную зону триггера
    private DialogueZone currentActiveZone;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ищем PanelRenderer на дочерних объектах
        panelRenderer = GetComponentInChildren<PanelRenderer>();

        if (panelRenderer == null)
        {
            Debug.LogError($"[DialogueManager] Ошибка! PanelRenderer не найден в дочерних объектах {gameObject.name}.");
        }
    }

    private void OnEnable()
    {
        if (panelRenderer != null)
        {
            // НОВЫЙ СТАНДАРТ UNITY 6: Подписываемся на событие загрузки/перезагрузки UI
            panelRenderer.RegisterUIReloadCallback(OnUIReloaded);
        }
    }

    private void OnDisable()
    {
        if (panelRenderer != null)
        {
            // Обязательно отписываемся, чтобы не было утечек памяти
            panelRenderer.UnregisterUIReloadCallback(OnUIReloaded);
        }

        // Отписываемся от клика кнопки, если она существовала
        if (interactButton != null)
        {
            interactButton.clicked -= OnInteractButtonClicked;
        }
    }

    // Этот метод Unity 6 вызывает автоматически, когда панель отрисовалась и готова к работе
    private void OnUIReloaded(PanelRenderer renderer, VisualElement rootElement)
    {
        if (rootElement == null) return;

        // Ищем элементы внутри предоставленного Unity корня (rootElement)
        interactButton = rootElement.Q<Button>("InteractBtn");
        dialogueBox = rootElement.Q<VisualElement>("DialogueBox");

        // Настраиваем начальное состояние кнопки взаимодействия
        if (interactButton != null)
        {
            interactButton.style.display = DisplayStyle.None;

            // Сначала отписываемся (на случай перезагрузки UI), потом подписываемся заново
            interactButton.clicked -= OnInteractButtonClicked;
            interactButton.clicked += OnInteractButtonClicked;
        }

        // Настраиваем начальное состояние окна диалога
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
        }
    }

    // ==========================================
    // ЛОГИКА ВЗАИМОДЕЙСТВИЯ (ИНТЕРФЕЙС)
    // ==========================================

    public void ShowInteractionButton(DialogueZone zone)
    {
        currentActiveZone = zone;
        if (interactButton != null)
        {
            interactButton.style.display = DisplayStyle.Flex; // Показываем кнопку взаимодействия
        }
    }

    public void HideInteractionButton()
    {
        currentActiveZone = null;
        if (interactButton != null)
        {
            interactButton.style.display = DisplayStyle.None; // Скрываем кнопку взаимодействия
        }
    }

    private void OnInteractButtonClicked()
    {
        if (currentActiveZone != null)
        {
            currentActiveZone.TriggerDialogue(); // Запуск диалога через зону
        }
    }

    // ==========================================
    // ЛОГИКА ДИАЛОГА (МЕНЕДЖЕР)
    // ==========================================

    public void StartDialogue(TextAsset dialogueFile)
    {
        if (dialogueFile == null)
        {
            Debug.LogError("[DialogueManager] Ошибка: Передан пустой файл диалога!");
            return;
        }

        Debug.Log($"[DialogueManager] Диалог успешно запущен из файла: {dialogueFile.name}");

        string fullText = dialogueFile.text;
        string[] lines = fullText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            Debug.Log($"[Линия текста]: {line}");
        }

        // Показываем само окно диалога на экране
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
        }
    }
}
