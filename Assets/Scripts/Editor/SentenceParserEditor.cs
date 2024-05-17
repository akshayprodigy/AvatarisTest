using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

public class SentenceParserEditor : EditorWindow
{
    private string inputSentence = "";
    private List<ParsingCode> parseCodes = new List<ParsingCode>();
    private ReorderableList list;
    private string outputResult = "";
    private Vector2 scrollPosition;

    [MenuItem("Window/Sentence Parser")]
    public static void ShowWindow()
    {
        GetWindow<SentenceParserEditor>("Sentence Parser").minSize = new Vector2(600, 400);
    }

    void OnEnable()
    {
        LoadParseCodes();
        InitializeList();
    }

    private void InitializeList()
    {
        list = new ReorderableList(parseCodes, typeof(ParsingCode), true, true, true, true);

        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var code = parseCodes[index];
            rect.y += 2;
            code.code = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width - 60, EditorGUIUtility.singleLineHeight), code.code);
            code.priority = EditorGUI.IntField(new Rect(rect.x + rect.width - 60, rect.y, 60, EditorGUIUtility.singleLineHeight), code.priority);
        };

        list.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Parsing Codes and Priorities");
        };

        list.onAddCallback = (ReorderableList l) =>
        {
            var newCode = ScriptableObjectUtility.CreateAsset<ParsingCode>();
            parseCodes.Add(newCode);
        };

        list.onRemoveCallback = (ReorderableList l) =>
        {
            var codeToRemove = parseCodes[l.index];
            parseCodes.RemoveAt(l.index);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(codeToRemove));
        };
    }

    void OnGUI()
    {
        GUILayout.Label("Input Sentence", EditorStyles.boldLabel);
        inputSentence = EditorGUILayout.TextField(inputSentence);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));
        list.DoLayoutList();
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Parse Sentence"))
        {
            outputResult = FindHighestPriorityMatch();
            Debug.Log(outputResult);
        }

        GUILayout.Label("Output: " + outputResult, EditorStyles.boldLabel);
    }

    private string FindHighestPriorityMatch()
    {
        ParsingCode matchedCode = null;
        int highestPriority = int.MinValue;

        foreach (var code in parseCodes)
        {
            if (IsCodeValid(inputSentence, code.code) && code.priority > highestPriority)
            {
                highestPriority = code.priority;
                matchedCode = code;
            }
        }

        return matchedCode != null ? matchedCode.code : "No match found";
    }


    public string CreateRegexPattern(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input string cannot be empty.", nameof(input));
        }

        // Remove leading and trailing quotation marks if present

        input = input.Trim();
        string trimmedInput = input.Replace("\"", "");
        
        // Split the input based on spaces after removing '&'
        var parts = trimmedInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string pattern = "";

        foreach (var part in parts)
        {
            string cleanPart = part.TrimStart('&');
            string wordPattern = Regex.Replace(cleanPart, @"\[(.*?)\]", m =>
            {
                string content = m.Groups[1].Value.Replace("/", "|"); // Replace / with | inside brackets
                return content.Contains("|") ? $"({content})?" : $"[{content}]?"; // Use parentheses for alternatives
            });
            wordPattern = @"\b" + wordPattern + @"\b";
            if (!string.IsNullOrEmpty(pattern))
            {
                pattern += ".*";
            }
            pattern += wordPattern;
        }
        //Debug.Log("Final regex pattern: " + pattern);

        return pattern;
    }

    private bool IsCodeValid(string sentence, string code)
    {


        string pattern = CreateRegexPattern(code);
        Debug.Log($"Testing - pattern: {pattern} ");
        try
        {
            //string testpattern = @"\blike\b.*\bbanana[s]?\b";
            //string newTestPattern = @".*\blike\b.*\bbanana[s]?\b.*";
            bool isMatch = Regex.IsMatch(sentence, pattern, RegexOptions.IgnoreCase);
            Debug.Log($"Regex.IsMatch: {isMatch}");
            return isMatch;
        }
        catch (ArgumentException ex)
        {
            Debug.LogError("Regex error in pattern: " + pattern + " - " + ex.Message);
            return false;
        }
    }

    private void LoadParseCodes()
    {
        parseCodes = AssetDatabase.FindAssets("t:ParsingCode")
            .Select(guid => AssetDatabase.LoadAssetAtPath<ParsingCode>(AssetDatabase.GUIDToAssetPath(guid)))
            .ToList();
    }
}

public static class ScriptableObjectUtility
{
    public static T CreateAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path))
        {
            path = "Assets";
        }
        else if (System.IO.Path.GetExtension(path) != "")
        {
            path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).Name + ".asset");

        AssetDatabase.CreateAsset(asset, assetPathAndName);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        return asset;
    }
}
