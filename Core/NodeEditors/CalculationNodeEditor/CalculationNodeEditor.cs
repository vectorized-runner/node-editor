using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CalculationNodeEditor : NodeEditor
{
	readonly string inputNodeData = "inputNode";
	readonly string outputNodeData = "outputNode";
	readonly string calculationNodeData = "calculationNode";
	readonly string comparisonNodeData = "comparisonNode";

	// Settings 
	Color inputNodeColor = Color.red;
	Color operationNodeColor = Color.yellow;
	Color outputNodeColor = Color.blue;

	[MenuItem("Window/Calc Node Editor")]
	static void ShowEditor()
	{
		// Window popup function on the editor	
		editorWindow = GetWindow<CalculationNodeEditor>();
	}

	public override void GetWindow()
	{
		if(!editorWindow)
		{
			editorWindow = GetWindow<CalculationNodeEditor>(); 
		}
	}

	public override void ShowNodeCreatorMenu()
	{
		// Show Creator options on the menu 
		GenericMenu menu = new GenericMenu();

		menu.AddItem(new GUIContent("Add Input Node"), false, ContextCallback, inputNodeData);
		menu.AddItem(new GUIContent("Add Output Node"), false, ContextCallback, outputNodeData);
		menu.AddItem(new GUIContent("Add Calculation Node"), false, ContextCallback, calculationNodeData);
		menu.AddItem(new GUIContent("Add Comparison Node"), false, ContextCallback, comparisonNodeData);

		menu.ShowAsContext();
	}

	public override void DrawSettings()
	{
		base.DrawSettings();

		EditorGUILayout.Space(); 

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Operation Tint", EditorStyles.boldLabel, GUILayout.Width(100f), GUILayout.Height(20f));
		operationNodeColor = EditorGUILayout.ColorField(operationNodeColor, GUILayout.Width(50f), GUILayout.Height(20f));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Input Tint", EditorStyles.boldLabel, GUILayout.Width(100f), GUILayout.Height(20f));
		inputNodeColor = EditorGUILayout.ColorField(inputNodeColor, GUILayout.Width(50f), GUILayout.Height(20f));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Output Tint", EditorStyles.boldLabel, GUILayout.Width(100f), GUILayout.Height(20f));
		outputNodeColor = EditorGUILayout.ColorField(outputNodeColor, GUILayout.Width(50f), GUILayout.Height(20f));
		EditorGUILayout.EndHorizontal();
	}

	public override void PaintNode(BaseNode node)
	{
		// Set the color of the window 
		if(node is OperationNode)
		{
			GUI.color = operationNodeColor;
		}
		else if(node is InputNode)
		{
			GUI.color = inputNodeColor;
		}
		else if(node is OutputNode)
		{
			GUI.color = outputNodeColor;
		}
	}

	public override void ContextCallback(object obj)
	{
		base.ContextCallback(obj);

		string callback = obj.ToString();

		if(callback.Equals(inputNodeData))
		{
			CreateNode<InputNode>();
		}
		else if(callback.Equals(outputNodeData))
		{
			CreateNode<OutputNode>();
		}
		else if(callback.Equals(calculationNodeData))
		{
			CreateNode<CalculationNode>();
		}
		else if(callback.Equals(comparisonNodeData))
		{
			CreateNode<ComparisonNode>();
		}
	}
}
