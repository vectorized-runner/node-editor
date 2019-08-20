using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System; 

public class ScriptableObjectInspector : NodeEditor
{
	readonly string scriptableData = "scriptable";
	readonly string noteData = "note";

	[MenuItem("Window/Scriptable Object Inspector")]
	static void ShowEditor()
	{
		editorWindow = GetWindow<ScriptableObjectInspector>();
	}

	public static ScriptableObjectNode FindExistingNode(ScriptableObject inspected)
	{
		// Try and find copy from existing nodes 
		return nodes
			.Where(node => node is ScriptableObjectNode)
			.FirstOrDefault(node => (node as ScriptableObjectNode).GetInspectedObject() == inspected)
			as ScriptableObjectNode;
	}

	public override void GetWindow()
	{
		if(!editorWindow)
		{
			editorWindow = GetWindow<ScriptableObjectInspector>();
		}

		editorWindow.wantsMouseMove = true;
	}

	public override void ShowNodeCreatorMenu()
	{
		GenericMenu menu = new GenericMenu();

		menu.AddItem(new GUIContent("Add Scriptable Node"), false, ContextCallback, scriptableData);
		menu.AddItem(new GUIContent("Add Note"), false, ContextCallback, noteData);

		menu.ShowAsContext(); 
	}

	public override void ContextCallback(object obj)
	{
		base.ContextCallback(obj);

		string callback = obj.ToString(); 

		if(callback.Equals(scriptableData))
		{
			CreateNodeInstance<ScriptableObjectNode>(); 
		}
		else if(callback.Equals(noteData))
		{
			CreateNodeInstance<NoteNode>();
		}
	}
}
