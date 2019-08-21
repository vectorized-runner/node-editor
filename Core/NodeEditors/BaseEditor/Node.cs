using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; 

public abstract class Node : ScriptableObject
{
	public Rect windowRect;
	public Color windowColor = Color.white;
	public Dictionary<string, string> classShortcuts = new Dictionary<string, string>();
	public int snapValue = 10;
	public bool allowResizeHorizontal = true; 
	public bool lockPosition;
	public bool snap;
	public bool isHidden;
	public bool groupDrag;
	public bool allowResizeVertical;

	public virtual string WindowTitle { get; private set; } 

	public virtual void OnDrawCurves() { }

	public virtual void OnBeforeSelfDeleted() { }

	public virtual void DragHiddenNodes(Vector2 delta) { }

	public virtual void GroupDrag(Vector2 delta) { }

	public virtual void MakeLink(Node node, Vector2 mousePos) { }

	public virtual void OnNodeDeleted(Node node) { }

	public virtual Node GetNodeOnPosition(Vector2 mousePos)
	{
		return null;
	}

	public virtual void SetShortcuts(Dictionary<string, string> shortcuts)
	{
		classShortcuts = shortcuts;
	}

	public virtual void OnDrawWindow()
	{
		EditorGUILayout.BeginHorizontal();

		DrawWindowColor();
		DrawLockPosition();
		DrawResize();
		DrawGroupDrag(); 
		DrawSnap();

		EditorGUILayout.EndVertical(); 
	}

	public Rect ToLocalWindowRect(Rect original)
	{
		// Convert GUI position rect to 
		// Rect that is local to this window 
		Rect rect = windowRect;
		rect.x += original.x;
		rect.y += original.y + original.height / 2f;
		// Crucial for the rect to show exact position
		// when drawing curves
		rect.width = 1f;
		rect.height = 1f;

		return rect;
	}

	void DrawGroupDrag()
	{
		groupDrag = GUILayout.Toggle(groupDrag, "GroupDrag", "Button");
	}

	void DrawSnap()
	{
		snap = GUILayout.Toggle(snap, "Snap", "Button");
		if(snap)
		{
			EditorGUILayout.LabelField("Snap Value", GUILayout.Width(100f));
			snapValue = EditorGUILayout.IntField(snapValue);
		}
	}

	void DrawResize()
	{
		// Allow resize horizontal by default 
		// Don't allow resize vertical 

		//allowResizeHorizontal = GUILayout.Toggle(allowResizeHorizontal, "Resize", "Button");
	}

	void DrawWindowColor()
	{
		windowColor = EditorGUILayout.ColorField(windowColor, GUILayout.Width(50f), GUILayout.Height(15f));
	}

	void DrawLockPosition()
	{
		lockPosition = GUILayout.Toggle(lockPosition, "Lock", "Button");
	}
}
