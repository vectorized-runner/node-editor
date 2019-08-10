using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; 

public abstract class BaseNode : ScriptableObject
{
	public Rect windowRect;
	public virtual string WindowTitle { get; private set; } 

	public abstract void DrawCurves();

	public virtual void MakeLink(BaseNode node, Vector2 clickPos) { }

	public virtual void OnNodeDeleted(BaseNode node) { }

	public virtual void OnBeforeSelfDeleted() { }

	public virtual void DrawWindow() {}

	public virtual BaseInputNode GetInputNodeClickedOn(Vector2 clickPos)
	{
		return null; 
	}

	public Vector3 AdjustClickPos(Vector3 pos)
	{
		// Adjust click pos with windowRect so that 
		// Rect.Contains returns correct value on the nodes 
		return new Vector3(pos.x - windowRect.x, pos.y - windowRect.y);
	}

	public Rect GetTransitionEndRect(Rect inputWindow, Rect thisWindow)
	{
		Rect rect = thisWindow;
		rect.x += inputWindow.x;
		rect.y += inputWindow.y + inputWindow.height / 2f;
		rect.width = 1f;
		rect.height = 1f;

		return rect;
	}
}
