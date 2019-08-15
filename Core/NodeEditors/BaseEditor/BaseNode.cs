using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; 

public abstract class BaseNode : ScriptableObject
{
	public Rect windowRect;
	public Color windowColor = Color.white;
	public int snapValue = 10;
	public bool lockPosition;
	public bool allowResize; 
	public bool snap;
	public bool hidden;
	public bool groupDrag;

	public virtual string WindowTitle { get; private set; } 

	public virtual void DrawCurves() { }

	public virtual void OnBeforeSelfDeleted() { }

	public virtual void OnWindowColorChanged() { }

	public virtual void GroupDrag(Vector2 delta) { }

	public virtual void MakeLink(BaseNode node, Vector2 clickPos) { }

	public virtual void OnNodeDeleted(BaseNode node) { }

	public virtual BaseNode GetNodeOnPosition(Vector2 clickPos)
	{
		return null;
	}

	public virtual void DrawWindow()
	{
		EditorGUILayout.BeginHorizontal();

		EditorGUI.BeginChangeCheck(); 
		windowColor = EditorGUILayout.ColorField(windowColor, GUILayout.Width(50f), GUILayout.Height(15f));
		if(EditorGUI.EndChangeCheck())
		{
			OnWindowColorChanged(); 
		}

		lockPosition = GUILayout.Toggle(lockPosition, "Lock", "Button");
		allowResize = GUILayout.Toggle(allowResize, "Resize", "Button"); 
		snap = GUILayout.Toggle(snap, "Snap", "Button");
		groupDrag = GUILayout.Toggle(groupDrag, "GroupDrag", "Button"); 

		if(snap)
		{
			EditorGUILayout.LabelField("Snap Value", GUILayout.Width(100f));
			snapValue = EditorGUILayout.IntField(snapValue);
		}

		EditorGUILayout.EndVertical(); 
	}

	public Vector3 AdjustClickPos(Vector3 pos)
	{
		// Adjust click pos with windowRect so that 
		// Rect.Contains returns correct value on the nodes 
		return new Vector3(pos.x - windowRect.x, pos.y - windowRect.y);
	}

	public Rect ToFieldRect(Rect original)
	{
		Rect rect = windowRect;
		rect.x += original.x;
		rect.y += original.y + original.height / 2f;
		rect.width = 1f;
		rect.height = 1f;

		return rect;
	}
}
