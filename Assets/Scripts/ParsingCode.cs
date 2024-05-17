using UnityEngine;

[CreateAssetMenu(fileName = "ParsingCode", menuName = "Sentence Parser/Parsing Code", order = 1)]
public class ParsingCode : ScriptableObject
{
    public string code;
    public int priority;
}