using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

public class ScriptableObjectInspector : NodeEditor
{
	enum WindowMode { edit, visualization }

	static Dictionary<Type, Color> typeByColor = new Dictionary<Type, Color>();

	WindowMode windowMode = WindowMode.edit;

	const string ScriptableData = "scriptable";
	const string NoteData = "note";

	[MenuItem("Window/Scriptable Object Inspector")]
	static void ShowEditor()
	{
		editorWindow = GetWindow<ScriptableObjectInspector>();
	}

	public static void OnTypeInspected(Type type)
	{
		if(!typeByColor.ContainsKey(type))
		{
			typeByColor.Add(type, Color.white);
		}
	}

	public static ScriptableObjectNode FindExistingNode(ScriptableObject inspected)
	{
		return nodes
			.FirstOrDefault(node => (node as ScriptableObjectNode)?.GetInspectedObject() == inspected)
			as ScriptableObjectNode;
	}

	public override void DrawSettings()
	{
		base.DrawSettings();

		DrawWindowMode();
		DrawClassByColor();
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

		menu.AddItem(new GUIContent("Add Scriptable Node"), false, ContextCallback, ScriptableData);
		menu.AddItem(new GUIContent("Add Note"), false, ContextCallback, NoteData);

		menu.ShowAsContext();
	}

	public override void ContextCallback(object obj)
	{
		base.ContextCallback(obj);

		string callback = obj.ToString();

		if(callback.Equals(ScriptableData))
		{
			CreateNodeInstance<ScriptableObjectNode>();
		}
		else if(callback.Equals(NoteData))
		{
			CreateNodeInstance<NoteNode>();
		}
	}

	public override void DrawCurves(Node node)
	{
		if(windowMode == WindowMode.edit)
		{
			base.DrawCurves(node);
		}
		else
		{
			if(node is ScriptableObjectNode sNode)
			{
				sNode.OnVisualizeCurves(); 
			}
		} 
	}

	public override void WindowFunction(int id)
	{
		if(windowMode == WindowMode.edit)
		{
			// Base class draws edit window 
			base.WindowFunction(id);
		}
		else
		{
			if(nodes.ElementAt(id) is ScriptableObjectNode sNode)
			{
				sNode.OnVisualizeWindow();
				DragWindow(sNode);
			}
		}
	}

	void DrawWindowMode()
	{
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Window Mode", EditorStyles.boldLabel);

		EditorGUI.BeginChangeCheck(); 
		windowMode = (WindowMode)EditorGUILayout.EnumPopup(windowMode);
		if(EditorGUI.EndChangeCheck())
		{
			if(windowMode == WindowMode.edit)
			{
				nodes.ForEach(node => node.allowResizeHorizontal = false); 
			}
			else if(windowMode == WindowMode.visualization)
			{
				nodes.ForEach(node => node.allowResizeHorizontal = true); 
			}

			AutoLayoutNodes(); 
		}
	}

	void AutoLayoutNodes()
	{
		nodes.ForEach(node =>
		{
			node.windowRect.width = 1f;
			node.windowRect.height = 1f; 
		});
	}

	void DrawClassByColor()
	{
		if(typeByColor.Count > 0)
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Class by Color", EditorStyles.boldLabel);

			// Draw types by color 
			foreach(var key in typeByColor.Keys.ToList())
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(key.ToString(), GUILayout.Width(100f));
				typeByColor[key] = EditorGUILayout.ColorField(typeByColor[key]);
				EditorGUILayout.EndHorizontal();
			}

			if(GUILayout.Toggle(false, "Apply", "Button"))
			{
				// On Apply Color by Class
				nodes.ForEach(node =>
				{
					if(node is ScriptableObjectNode sNode)
					{
						sNode.windowColor = typeByColor.FirstOrDefault(kvp => kvp.Key == sNode.GetInspectedObject().GetType()).Value;
					}
				});
			}
		}
	}
}

