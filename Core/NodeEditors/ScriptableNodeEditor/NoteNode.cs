using UnityEngine;
using System.Collections;
using UnityEditor; 

public class NoteNode : Node
{
	public override string WindowTitle => "Note";

	string text; 

	public override void OnDrawWindow()
	{
		text = EditorGUILayout.TextField(text, GUILayout.Height(50f)); 
	}
}
