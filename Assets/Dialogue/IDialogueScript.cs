using System.Collections.Generic;

// Структура одной реплики для передачи в менеджер
public struct DialogueLine
{
    public string Speaker;
    public string Text;
}

// Общий интерфейс для всех автогенерируемых классов
public interface IDialogueScript
{
    string DialogueId { get; }
    IEnumerator<DialogueLine> GetLines();
}
