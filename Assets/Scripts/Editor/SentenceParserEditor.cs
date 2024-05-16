//using UnityEngine;
//using UnityEditor;
//using System.Collections.Generic;
//using System.Text.RegularExpressions;
//using System.Linq;
//using System;

//public class SentenceParserEditor : EditorWindow
//{
//    private string inputSentence = "";
//    private List<ParseCode> parseCodes = new List<ParseCode>();
//    private string outputResult = "";

//    [MenuItem("Window/Sentence Parser")]
//    public static void ShowWindow()
//    {
//        GetWindow<SentenceParserEditor>("Sentence Parser");
//    }

//    void OnGUI()
//    {
//        GUILayout.Label("Input Sentence", EditorStyles.boldLabel);
//        inputSentence = EditorGUILayout.TextField(inputSentence);

//        if (GUILayout.Button("Add New Parsing Code"))
//        {
//            parseCodes.Add(new ParseCode());
//        }

//        int indexToRemove = -1;
//        for (int i = 0; i < parseCodes.Count; i++)
//        {
//            GUILayout.BeginHorizontal();
//            parseCodes[i].Code = EditorGUILayout.TextField("Code", parseCodes[i].Code);
//            parseCodes[i].Priority = EditorGUILayout.IntField("Priority", parseCodes[i].Priority);
//            if (GUILayout.Button("Remove"))
//            {
//                indexToRemove = i;
//            }
//            GUILayout.EndHorizontal();
//        }

//        if (indexToRemove != -1)
//        {
//            parseCodes.RemoveAt(indexToRemove);
//        }

//        if (GUILayout.Button("Parse Sentence"))
//        {
//            outputResult = ParseSentence();
//            Debug.Log(outputResult);
//        }

//        GUILayout.Label("Output: " + outputResult, EditorStyles.boldLabel);
//    }

//    private string ParseSentence()
//    {
//        ParseCode highestPriorityCode = null;

//        foreach (var code in parseCodes)
//        {
//            if (IsCodeValid(inputSentence, code.Code))
//            {
//                if (highestPriorityCode == null || code.Priority > highestPriorityCode.Priority)
//                {
//                    highestPriorityCode = code;
//                }
//            }
//        }

//        return highestPriorityCode != null ? highestPriorityCode.Code : "No match found";
//    }

//    private bool IsCodeValid(string sentence, string code)
//    {
//        string pattern = "^" + Regex.Escape(code)
//            .Replace("\\&", ".*\\b") // Word boundary and 'must include'
//            .Replace("[s]", "s?")    // Optional 's'
//            .Replace("[y/ies]", "(y|ies)") // Either 'y' or 'ies'
//            .Replace("\\|", "|")     // OR
//            .Replace("!\\(", "(?!")  // Negative lookahead for group
//            + ".*$";

//        return Regex.IsMatch(sentence, pattern, RegexOptions.IgnoreCase);
//    }

//    private class ParseCode
//    {
//        public string Code;
//        public int Priority;
//    }
//}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using System.Text.RegularExpressions;
using System;
using System.Linq;

public class SentenceParserEditor : EditorWindow
{
    private string inputSentence = "";
    private List<ParseCode> parseCodes = new List<ParseCode>();
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
        list = new ReorderableList(parseCodes, typeof(ParseCode), true, true, true, true);

        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            ParseCode code = (ParseCode)list.list[index];
            rect.y += 2;
            code.Code = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width - 60, EditorGUIUtility.singleLineHeight), code.Code);
            code.Priority = EditorGUI.IntField(new Rect(rect.x + rect.width - 60, rect.y, 60, EditorGUIUtility.singleLineHeight), code.Priority);
        };

        list.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Parsing Codes and Priorities");
        };
    }

    void OnGUI()
    {
        GUILayout.Label("Input Sentence", EditorStyles.boldLabel);
        inputSentence = EditorGUILayout.TextField(inputSentence);

        // Scrollable list
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));
        list.DoLayoutList();
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Parse Sentence"))
        {
            outputResult = ParseSentence();
            Debug.Log(outputResult);
        }

        GUILayout.Label("Output: " + outputResult, EditorStyles.boldLabel);
    }

    private string ParseSentence()
    {
        ParseCode highestPriorityCode = null;

        foreach (var code in parseCodes)
        {
            if (IsCodeValid(inputSentence, code.Code))
            {
                if (highestPriorityCode == null || code.Priority > highestPriorityCode.Priority)
                {
                    highestPriorityCode = code;
                }
            }
        }

        return highestPriorityCode != null ? highestPriorityCode.Code : "No match found";
    }

    //private bool IsCodeValid(string sentence, string code)
    //{
    //    string pattern = "^" + Regex.Escape(code)
    //        .Replace("\\&", ".*\\b") // Word boundary and 'must include'
    //        .Replace("[s]", "s?")    // Optional 's'
    //        .Replace("[y/ies]", "(y|ies)") // Either 'y' or 'ies'
    //        .Replace("\\|", "|")     // OR
    //        .Replace("!\\(", "(?!")  // Negative lookahead for group
    //        + ".*$";

    //    return Regex.IsMatch(sentence, pattern, RegexOptions.IgnoreCase);
    //}

    private bool IsCodeValid(string sentence, string code)
    {
        // Start by converting the custom code format to regex
        string pattern = Regex.Escape(code)
            .Replace("\\&", ".*\\b")  // 'must include' with word boundary
            .Replace("[s]", "s?")     // Optional 's'
            .Replace("[y/ies]", "(y|ies)")  // Either 'y' or 'ies'
            .Replace("\\|", "|")      // OR
            .Replace("!\\(", "(?!");  // Negative lookahead start

        // Ensure all opened groups are properly closed
        int openParens = pattern.Count(f => f == '(');
        int closeParens = pattern.Count(f => f == ')');
        while (openParens > closeParens)
        {
            pattern += ")";
            closeParens++;
        }

        // Complete the pattern to match the entire line
        pattern = "^" + pattern + ".*$";

        // Try matching the pattern
        try
        {
            return Regex.IsMatch(sentence, pattern, RegexOptions.IgnoreCase);
        }
        catch (ArgumentException ex)
        {
            Debug.LogError("Regex error in pattern: " + pattern + " - " + ex.Message);
            return false;
        }
    }


    private class ParseCode
    {
        public string Code;
        public int Priority;
    }
}
