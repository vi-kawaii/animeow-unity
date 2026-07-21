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

    // Переменная для хранения текущего итератора строк диалога
    private System.Collections.Generic.IEnumerator<DialogueLine> _currentDialogueIterator;

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
            interactButton.pickingMode = PickingMode.Position;
            interactButton.style.display = DisplayStyle.None;

            interactButton.clicked -= OnInteractButtonClicked;
            interactButton.clicked += OnInteractButtonClicked;
        }

        if (dialogueBox != null)
        {
            // Окно теперь ловит клики (pickingMode = Position), чтобы продвигать диалог вперед
            dialogueBox.pickingMode = PickingMode.Position;
            dialogueBox.style.display = DisplayStyle.None;

            // Регистрируем клик по самому окну для продвижения текста
            dialogueBox.UnregisterCallback<ClickEvent>(OnDialogueBoxClicked);
            dialogueBox.RegisterCallback<ClickEvent>(OnDialogueBoxClicked);
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

    public void HideInteractionButton(DialogueZone zone)
    {
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
            Debug.Log($"[DialogueManager] Текущая активная зона: {currentActiveZone.name}. Отправляем запрос на запуск диалога...");
            currentActiveZone.TriggerDialogue();
        }
        else
        {
            Debug.LogWarning("[DialogueManager] КЛИК БЫЛ, но currentActiveZone равен NULL!");
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

        // Превращаем имя файла (например, quest_1) в имя скомпилированного класса (Dialogue_quest_1)
        string className = "Dialogue_" + dialogueFile.name.Replace(" ", "_").Replace("-", "_");

        // Ищем этот класс в сборке игры через Рефлексию (HybridCLR полностью это поддерживает в билдax)
        System.Type type = System.Type.GetType(className);

        if (type == null)
        {
            Debug.LogError($"[DialogueManager] Ошибка! Класс {className} не найден. Убедись, что файл лежит в Assets/Dialogue/Scripts/ и нажми верхнее меню Tools -> Dialogue System -> Rebuild All Dialogues.");
            return;
        }

        // Создаем экземпляр сгенерированного класса
        IDialogueScript dialogueScript = (IDialogueScript)System.Activator.CreateInstance(type);

        Debug.Log($"[DialogueManager] МЕНЕДЖЕР: Диалог успешно запущен из ассета {dialogueFile.name} через класс {className}");

        // Управляем видимостью UI элементов
        if (interactButton != null) interactButton.style.display = DisplayStyle.None;
        if (dialogueBox != null) dialogueBox.style.display = DisplayStyle.Flex;

        // Берем итератор реплик и делаем первую реплику
        _currentDialogueIterator = dialogueScript.GetLines();
        AdvanceDialogue();
    }

    private void OnDialogueBoxClicked(ClickEvent evt)
    {
        if (_currentDialogueIterator != null)
        {
            AdvanceDialogue();
        }
    }

    private void AdvanceDialogue()
    {
        // Проверяем, есть ли следующая реплика в итераторе
        if (_currentDialogueIterator != null && _currentDialogueIterator.MoveNext())
        {
            DialogueLine currentLine = _currentDialogueIterator.Current;

            // ВЫВОД В КОНСОЛЬ (как вы просили, пока без лейблов)
            Debug.Log($"[Линия текста] {currentLine.Speaker}: {currentLine.Text}");
        }
        else
        {
            // Реплики закончились — закрываем окно
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        _currentDialogueIterator = null;

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
        }

        // Если игрок все еще стоит в триггере — возвращаем кнопку "Взаимодействовать"
        if (currentActiveZone != null && interactButton != null)
        {
            interactButton.style.display = DisplayStyle.Flex;
        }

        Debug.Log("[DialogueManager] Диалог завершен.");
    }
}
