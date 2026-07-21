#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class DialoguePreProcessor : AssetPostprocessor
{
    // Автоматически срабатывает, когда вы сохраняете или меняете .txt файл диалога
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool targetFolderChanged = false;

        foreach (string assetPath in importedAssets)
        {
            if (assetPath.StartsWith("Assets/Dialogue/Scripts/") && assetPath.EndsWith(".txt"))
            {
                GenerateSingleCSharpClass(assetPath);
                targetFolderChanged = true;
            }
        }

        if (targetFolderChanged)
        {
            AssetDatabase.Refresh();
        }
    }

    private static void GenerateSingleCSharpClass(string assetPath)
    {
        string rawText = File.ReadAllText(assetPath);
        string fileName = Path.GetFileNameWithoutExtension(assetPath);

        string[] lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder csharpLines = new StringBuilder();

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            int colonIndex = line.IndexOf(':');
            if (colonIndex != -1)
            {
                string speaker = line.Substring(0, colonIndex).Trim();
                string text = line.Substring(colonIndex + 1).Trim();

                if (speaker.StartsWith("[") && speaker.EndsWith("]"))
                {
                    speaker = speaker.Substring(1, speaker.Length - 2);
                }

                text = text.Replace("\"", "\\\"");
                speaker = speaker.Replace("\"", "\\\"");

                csharpLines.AppendLine($"        yield return new DialogueLine {{ Speaker = \"{speaker}\", Text = \"{text}\" }};");
            }
            else
            {
                string cleanLine = line.Trim().Replace("\"", "\\\"");
                csharpLines.AppendLine($"        yield return new DialogueLine {{ Speaker = \"\", Text = \"{cleanLine}\" }};");
            }
        }

        string className = "Dialogue_" + fileName.Replace(" ", "_").Replace("-", "_");

        string fullCode = $@"// АВТОГЕНЕРИРУЕМЫЙ КОД. НЕ ПРАВИТЬ РУКАМИ!
using System.Collections.Generic;

public class {className} : IDialogueScript
{{
    public string DialogueId => ""{fileName}"";

    public IEnumerator<DialogueLine> GetLines()
    {{
{csharpLines}
    }}
}}";

        string outputDirectory = Path.Combine(Application.dataPath, "Dialogue/Scripts/GeneratedCode");

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string outputPath = Path.Combine(outputDirectory, className + ".cs");
        File.WriteAllText(outputPath, fullCode);

        Debug.Log($"[DialogueCompiler] Код для {fileName} успешно перегенерирован в локальную папку.");
    }

    // Кнопка в верхнем меню Unity для быстрой сборки всех диалогов с нуля (для развертывания проекта)
    [MenuItem("Tools/Dialogue System/Rebuild All Dialogues")]
    public static void RebuildAll()
    {
        string sourceDir = Path.Combine(Application.dataPath, "Dialogue/Scripts");
        if (!Directory.Exists(sourceDir)) return;

        string[] files = Directory.GetFiles(sourceDir, "*.txt");
        foreach (string file in files)
        {
            string relativePath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
            GenerateSingleCSharpClass(relativePath);
        }

        AssetDatabase.Refresh();
        Debug.Log($"[DialogueCompiler] Пересборка завершена! Обработано файлов: {files.Length}");
    }
}
#endif
