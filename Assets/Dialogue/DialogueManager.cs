using UnityEngine;
using UnityEngine.UIElements;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private PanelRenderer panelRenderer;

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
            panelRenderer.RegisterUIReloadCallback(OnUIReloaded);
        }
    }

    private void OnDisable()
    {
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReloaded);
        }

        if (interactButton != null)
        {
            interactButton.clicked -= OnInteractButtonClicked;
        }
    }

    private void OnUIReloaded(PanelRenderer renderer, VisualElement rootElement)
    {
        if (rootElement == null) return;

        interactButton = rootElement.Q<Button>("InteractBtn");
        dialogueBox = rootElement.Q<VisualElement>("DialogueBox");

        if (interactButton != null)
        {
            // Корректируем pickingMode через код для Unity 6 на случай багов UI Builder
            interactButton.pickingMode = PickingMode.Position;

            interactButton.style.display = DisplayStyle.None;

            interactButton.clicked -= OnInteractButtonClicked;
            interactButton.clicked += OnInteractButtonClicked;
        }

        if (dialogueBox != null)
        {
            dialogueBox.pickingMode = PickingMode.Ignore; // Окно не должно блокировать клики по кнопке
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
            interactButton.style.display = DisplayStyle.Flex;
            Debug.Log($"[DialogueManager] Кнопка показана для зоны: {zone.name}");
        }
    }

    // Изменяем метод: теперь он принимает зону, которая просит её скрыть
    public void HideInteractionButton(DialogueZone zone)
    {
        // КЛЮЧЕВАЯ ЗАЩИТА: Сбрасываем зону только если это ТА ЖЕ САМАЯ зона, что сейчас активна.
        // Это предотвратит баг, если игрок быстро перебежал из одной зоны в другую.
        if (currentActiveZone == zone)
        {
            currentActiveZone = null;

            if (interactButton != null)
            {
                interactButton.style.display = DisplayStyle.None;
                Debug.Log($"[DialogueManager] Активная зона сброшена. Кнопка скрыта.");
            }
        }
    }

    private void OnInteractButtonClicked()
    {
        Debug.Log("[DialogueManager] Клик по кнопке зафиксирован!");

        if (currentActiveZone != null)
        {
            // Проверяем, есть ли вообще файл в зоне, из которой мы пытаемся читать
            // Используем Reflection (отражение), чтобы не менять доступ к переменной в DialogueZone,
            // либо просто смотрим, что ответит метод StartDialogue
            Debug.Log($"[DialogueManager] Текущая активная зона: {currentActiveZone.name}. Отправляем запрос на запуск диалога...");

            currentActiveZone.TriggerDialogue();
        }
        else
        {
            Debug.LogWarning("[DialogueManager] КЛИК БЫЛ, но currentActiveZone равен NULL! Менеджер забыл, в какой зоне стоит игрок.");
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

        Debug.Log($"[DialogueManager] МЕНЕДЖЕР: Диалог успешно запущен из файла: {dialogueFile.name}");

        string fullText = dialogueFile.text;
        string[] lines = fullText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            Debug.Log($"[Линия текста]: {line}");
        }

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
        }
    }
}
