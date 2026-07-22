using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private PanelRenderer panelRenderer;
    private Button interactButton;
    private VisualElement dialogueBox;

    // Ссылки на текстовые элементы внутри DialogueBox
    private Label speakerNameLabel;
    private Label dialogueTextLabel;

    // Ссылка на активную зону триггера
    private DialogueZone currentActiveZone;

    // Переменная для хранения текущего итератора строк диалога
    private System.Collections.Generic.IEnumerator<DialogueLine> _currentDialogueIterator;

    [Header("Настройки Управления (New Input System)")]
    [SerializeField]
    private InputActionReference interactActionReference;

    [SerializeField]
    private InputActionReference nextLineActionReference;

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

        if (interactActionReference != null)
        {
            interactActionReference.action.performed += OnInteractActionTriggered;
            interactActionReference.action.Enable();
        }

        if (nextLineActionReference != null)
        {
            nextLineActionReference.action.performed += OnNextLineActionTriggered;
            nextLineActionReference.action.Disable();
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

        if (interactActionReference != null)
        {
            interactActionReference.action.performed -= OnInteractActionTriggered;
        }

        if (nextLineActionReference != null)
        {
            nextLineActionReference.action.performed -= OnNextLineActionTriggered;
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
            dialogueBox.pickingMode = PickingMode.Position;
            dialogueBox.style.display = DisplayStyle.None;

            // Находим текстовые поля внутри DialogueBox для вывода имени и реплики
            speakerNameLabel = dialogueBox.Q<Label>("SpeakerNameLabel");
            dialogueTextLabel = dialogueBox.Q<Label>("DialogueTextLabel");

            dialogueBox.UnregisterCallback<ClickEvent>(OnDialogueBoxClicked);
            dialogueBox.RegisterCallback<ClickEvent>(OnDialogueBoxClicked);
        }
    }

    // ==========================================
    // ОБРАБОТЧИКИ НАЖАТИЙ КЛАВИШ (NEW INPUT SYSTEM)
    // ==========================================

    private void OnInteractActionTriggered(InputAction.CallbackContext context)
    {
        if (currentActiveZone != null && interactButton != null && interactButton.style.display == DisplayStyle.Flex)
        {
            if (_currentDialogueIterator == null)
            {
                OnInteractButtonClicked();
            }
        }
    }

    private void OnNextLineActionTriggered(InputAction.CallbackContext context)
    {
        if (_currentDialogueIterator != null && dialogueBox != null && dialogueBox.style.display == DisplayStyle.Flex)
        {
            AdvanceDialogue();
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
            }
        }
    }

    private void OnInteractButtonClicked()
    {
        if (currentActiveZone != null)
        {
            currentActiveZone.TriggerDialogue();
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

        string className = "Dialogue_" + dialogueFile.name.Replace(" ", "_").Replace("-", "_");
        System.Type type = System.Type.GetType(className);

        if (type == null)
        {
            Debug.LogError($"[DialogueManager] Ошибка! Класс {className} не найден.");
            return;
        }

        IDialogueScript dialogueScript = (IDialogueScript)System.Activator.CreateInstance(type);

        if (interactButton != null) interactButton.style.display = DisplayStyle.None;
        if (dialogueBox != null) dialogueBox.style.display = DisplayStyle.Flex;

        if (interactActionReference != null) interactActionReference.action.Disable();
        if (nextLineActionReference != null) nextLineActionReference.action.Enable();

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
        if (_currentDialogueIterator != null && _currentDialogueIterator.MoveNext())
        {
            DialogueLine currentLine = _currentDialogueIterator.Current;

            // ВЫВОД НА ЭКРАН ИГРЫ: Передаем имя и текст в UI Toolkit элементы
            if (speakerNameLabel != null)
            {
                speakerNameLabel.text = currentLine.Speaker;
                // Скрываем плашку имени, если автора у реплики нет (например, системное сообщение)
                speakerNameLabel.style.display = string.IsNullOrEmpty(currentLine.Speaker) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (dialogueTextLabel != null)
            {
                dialogueTextLabel.text = currentLine.Text;
            }

            // Оставляем лог в консоли чисто для подстраховки
            Debug.Log($"[Показ на экране] {currentLine.Speaker}: {currentLine.Text}");
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        _currentDialogueIterator = null;

        if (nextLineActionReference != null) nextLineActionReference.action.Disable();
        if (interactActionReference != null) interactActionReference.action.Enable();

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
        }

        if (currentActiveZone != null && interactButton != null)
        {
            interactButton.style.display = DisplayStyle.Flex;
        }

        Debug.Log("[DialogueManager] Диалог завершен.");
    }
}
