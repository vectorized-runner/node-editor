using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System; 

public class ScriptableObjectInspector : NodeEditor
{
	readonly string scriptableData = "scriptable";

	[MenuItem("Window/Scriptable Object Inspector")]
	static void ShowEditor()
	{
		editorWindow = GetWindow<ScriptableObjectInspector>(); 
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
	}
}
