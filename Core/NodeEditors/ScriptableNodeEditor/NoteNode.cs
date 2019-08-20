using UnityEngine;
using System.Collections;
using UnityEditor; 

public class NoteNode : BaseNode
{
	public override string WindowTitle => "Note";

	string text; 

	public override void DrawWindow()
	{
		text = EditorGUILayout.TextField(text, GUILayout.Height(50f)); 
	}
}
