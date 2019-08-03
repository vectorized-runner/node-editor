using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; 

public abstract class BaseNode : ScriptableObject
{
	public Rect windowRect;
	public bool hasInputs;
	public string windowTitle;

	// Implement on subclasses 
	public abstract void DrawCurves();

	// Draw the window for the base node 
	public virtual void DrawWindow()
	{
		windowTitle = EditorGUILayout.TextField("Title", windowTitle);
	}

	public virtual void SetInput(BaseInputNode inputNode, Vector2 clickPos)
	{

	}

	public virtual void NodeDeleted(BaseNode node)
	{

	}

	public virtual BaseInputNode ClickedOnInput(Vector2 clickPos)
	{
		return null; 
	}

}
