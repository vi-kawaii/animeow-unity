using UnityEngine;

public class DialogueZone : MonoBehaviour
{
    [Header("Файл диалога (.txt)")]
    [SerializeField] private TextAsset dialogueTextFile;

    private bool isPlayerInside = false;

    private void OnTriggerEnter(Collider other)
    {
        // Лог срабатывает на ЛЮБОЙ объект, зашедший в сферу
        Debug.Log($"[DialogueZone] Что-то вошло в триггер: {other.name} с тегом: {other.tag}");

        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            Debug.Log("[DialogueZone] Игрок с тегом 'Player' распознан! Пытаемся показать кнопку...");

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowInteractionButton(this);
            }
            else
            {
                Debug.LogError("[DialogueZone] Критическая ошибка: DialogueManager.Instance равен null! Скрипт менеджера отсутствует на сцене.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            Debug.Log("[DialogueZone] Игрок вышел из зоны.");

            if (DialogueManager.Instance != null)
            {
                // ИСПРАВЛЕНО: передаем себя (this), чтобы менеджер знал, какую зону закрывать
                DialogueManager.Instance.HideInteractionButton(this);
            }
        }
    }

    public void TriggerDialogue()
    {
        if (isPlayerInside && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueTextFile);

            // ИСПРАВЛЕНО: добавляем (this), чтобы убрать ошибку CS7036
            DialogueManager.Instance.HideInteractionButton(this);
        }
    }
}
