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


    public string ConvertToRegex(string input)
    {
        Stack<string> operatorStack = new Stack<string>();
        Stack<string> valueStack = new Stack<string>();

        input = input.Trim();

        if (!input.StartsWith("("))
        {
            // Directly process the input if it does not start with '('
            return ProcessExpression(input);
        }

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '(')
            {
                operatorStack.Push(c.ToString());
            }
            else if (c == ')')
            {

                // need to change this logic 
                string value = "";

                if(operatorStack.Count > 0 && operatorStack.Peek() == "(")
                {
                    operatorStack.Pop();
                    value = valueStack.Pop();
                    //string val1 = ProcessExpression(value.Trim());

                    if(operatorStack.Count>0 && operatorStack.Peek() == "!")
                    {
                        operatorStack.Pop();
                        string data = $"(?!{value})";
                        valueStack.Push(data);
                    }
                    else
                    {
                        string data = $"(?={value})";//.*
                        valueStack.Push(data);
                    }

                }
            }
            else if (c == '|' || c == '!' || (c == '&' && (i + 1 < input.Length && !Char.IsLetterOrDigit(input[i + 1]))))
            {
                operatorStack.Push(c.ToString());
            }
            else
            {
                if (input[i] == ' ')
                    continue;
                string value = "";
                while (i < input.Length && input[i] != '(' && input[i] != ')')//&& input[i] != '|' && input[i] != '&'
                {
                    value += input[i];
                    i++;
                }
                i--;
                string data = ProcessExpression(value.Trim());

                valueStack.Push(data);
            }
        }
        // Process remaining operators and values
        while (operatorStack.Count > 0)
        {
            string op = operatorStack.Pop();
            

            if (op == "|")
            {
                string val1 = valueStack.Pop();
                string val2 = valueStack.Pop();
                valueStack.Push($"{val2}{val1}");
            }
            else if (op == "&")
            {
                string val1 = valueStack.Pop();
                string val2 = valueStack.Pop();
                valueStack.Push($"{val2}.*{val1}");
            }else if (op == "!")
            {
                string val1 = valueStack.Pop();
                string value = $"(?!{val1}).*";
                valueStack.Push(value);
            }
        }
        string finalValue = $"^{valueStack.Pop()}.+$";
        return finalValue;
    }


    private string ProcessExpression(string expression)
    {
        string cleanedInput = expression.Replace("\"", "");

        var parts = cleanedInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string pattern = "";

        foreach (var part in parts)
        {
            string cleanPart = part.TrimStart('&');
            string wordPattern = Regex.Replace(cleanPart, @"\[(.*?)\]", m =>
            {
                string content = m.Groups[1].Value.Replace("/", "|");
                return content.Contains("|") ? $"({content})?" : $"[{content}]?";
            });
            wordPattern = @"\b" + wordPattern + @"\b";
            if (!string.IsNullOrEmpty(pattern))
            {
                pattern += ".*";
            }
            pattern += wordPattern;
        }

        string finaPattern = $".*{pattern}.*";

        return finaPattern;
    }

    private bool IsCodeValid(string sentence, string code)
    {


        string pattern = ConvertToRegex(code);
        Debug.Log($"Testing - pattern: {pattern} ");
        try
        {
            //string testpattern = @"\blike\b.*\bbanana[s]?\b";  \blike\b.*\bbanana[s]?\b 
            //string newTestPattern = @".*\blike\b.*\bbanana[s]?\b.*";
            //string testpattern = @"^(?=.*\bprefer\b.*\bstrawberr(y|ies)\b)(?!.*\blike\b.*\bbanana(s)?\b).+$";//  ^(?=.*\bprefer\b.*\bstrawberr(y|ies)?\b.*)|(?!.*\blike\b.*\bbanana[s]?\b.*).+$   
            //string text = @"\bprefer (strawberry|strawberries)\b(?!.*\blike bananas?\b)";
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
