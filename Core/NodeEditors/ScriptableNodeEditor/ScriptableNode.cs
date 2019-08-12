using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using UnityEditor;
using System.Linq; 

public class ScriptableNode : BaseNode
{
	ScriptableObject inspectedObject;
	List<ScriptableObject> referencedSOFields = new List<ScriptableObject>();
	List<ScriptableNode> childNodes = new List<ScriptableNode>();
	ScriptableNode parentNode; 
	Rect inspectedRect; 
	Dictionary<ScriptableNode, Rect> fieldByRects = new Dictionary<ScriptableNode, Rect>();
	bool remove; 

	public override string WindowTitle => inspectedObject?.name;

	public void SetInspectedObject(ScriptableObject so)
	{
		inspectedObject = so;
	}

	public Rect GetInspectedRect()
	{
		return inspectedRect; 
	}

	public ScriptableObject GetInspectedObject()
	{
		return inspectedObject;
	}

	public override void OnWindowColorChanged()
	{
		// Child nodes inherit parent color 
		childNodes.ForEach(node => node.windowColor = windowColor);
		// Recursively
		childNodes.ForEach(node => node.OnWindowColorChanged());
	}

	public override void MakeLink(BaseNode node, Vector2 clickPos)
	{
		// todo Implement later 
		// This is making link using transition curve 
		// node: the node to make link with 
		// clickpos: current mouse pos 
	}

	public override void OnBeforeSelfDeleted()
	{
		DeleteChildNodes();
		ResetChildNodeInfo(); 
	}

	public override void OnNodeDeleted(BaseNode node)
	{
		if(node is ScriptableNode)
		{
			ScriptableNode scriptableNode = node as ScriptableNode;

			if(childNodes.Contains(scriptableNode))
			{
				BreakLinkFrom(scriptableNode);
			}
		}
	}

	public override void DrawWindow()
	{
		base.DrawWindow(); 

		DrawInspectedObject();

		if(inspectedObject != null)
		{
			DrawInspectedFields();
		}
	}

	public override void DrawCurves()
	{
		childNodes.ForEach(node =>
		{
			foreach(var kvp in fieldByRects)
			{
				if(kvp.Key == node)
				{
					float startX = node.windowRect.xMax;
					float startY = node.GetInspectedRect().MiddleRight().y;
					Vector2 start = new Vector2(startX, startY);

					float endX = windowRect.xMin;
					float endY = fieldByRects[node].MiddleLeft().y;
					Vector2 end = new Vector2(endX, endY);

					NodeEditor.DrawTransitionCurve(start, end);
				}
			}
		});
	}

	void RemoveNodeInfo(ScriptableNode node)
	{
		// Remove your lode);
		ScriptableObject so = node.GetInspectedObject();

		childNodes.Remove(node); 
		fieldByRects.Remove(node); 
		referencedSOFields.Remove(so);
	}

	void AddNodeInfo(ScriptableNode node)
	{
		AddNodeInfo(node, node.GetInspectedObject()); 
	}

	void AddNodeInfo(ScriptableNode node, ScriptableObject updatedValue)
	{
		referencedSOFields.Add(updatedValue);
		childNodes.Add(node);
	} 

	void BreakLinkFrom(ScriptableNode node)
	{
		ScriptableObject so = node.GetInspectedObject();
		RemoveNodeInfo(node); 
		RemoveInspectedField(so); 
	}     

	void UpdateInspectedField(ScriptableObject oldValue, ScriptableObject newValue)
	{
		Type inspectedType = inspectedObject.GetType();
		FieldInfo[] inspectedFields = inspectedType.GetFields();

		foreach(var field in inspectedFields)
		{
			object value = field.GetValue(inspectedObject);
			Type type = value?.GetType();

			if(typeof(ScriptableObject).IsAssignableFrom(type))
			{
				ScriptableObject scriptable = value as ScriptableObject;

				if(scriptable == oldValue)
				{
					// Set value to null on removing of the scriptable object 
					field.SetValue(inspectedObject, newValue);
					return;
				}
			}
			else if(typeof(IList).IsAssignableFrom(type))
			{
				IList iList = value as IList;
				// Remove old value from the list 
				iList.Remove(oldValue);
				// Add new value to the list
				iList.Add(newValue);
			}
		}
	}

	void RemoveInspectedField(ScriptableObject so)
	{
		// Find the field corresponding to that scriptableObject 
		Type inspectedType = inspectedObject.GetType();
		FieldInfo[] inspectedFields = inspectedType.GetFields();

		foreach(var field in inspectedFields)
		{
			object value = field.GetValue(inspectedObject);
			Type type = value?.GetType();

			if(typeof(ScriptableObject).IsAssignableFrom(type))
			{
				ScriptableObject scriptable = value as ScriptableObject; 

				if(scriptable == so)  
				{
					// Set value to null on removing of the scriptable object 
					field.SetValue(inspectedObject, null);
					return; 
				}
			}
			else if(typeof(IList).IsAssignableFrom(type))
			{
				IList iList = value as IList;
				// Remove from the list 
				iList.Remove(so);
			}
		}
	}

	void MakeLinkWith(ScriptableObject so)
	{
		// yPosition is decided by the count 
		int count = referencedSOFields.Count;
		Rect rect = windowRect;
		rect.position -= new Vector2(windowRect.width * 1.2f, -windowRect.height * 1.2f * count);
		// Create scriptable node 
		ScriptableNode scriptableNode = NodeEditor.CreateNode<ScriptableNode>(rect);
		scriptableNode.SetInspectedObject(so);

		// Node Color is inherited at the beginning 
		scriptableNode.windowColor = windowColor;

		// Update node information
		AddNodeInfo(scriptableNode);
	}

	void DrawInspectedObject()
	{
		EditorGUILayout.LabelField("Inspected Object");

		EditorGUI.BeginDisabledGroup(inspectedObject != null);
		inspectedObject = (ScriptableObject)EditorGUILayout.ObjectField(inspectedObject, typeof(ScriptableObject), false);
		EditorGUI.EndDisabledGroup(); 

		//EditorGUI.BeginChangeCheck();
		//if(EditorGUI.EndChangeCheck())
		//{
		//	OnBeforeInspectedObjectChanged(inspected);
		//}

		if(Event.current.type == EventType.Repaint)
		{
			Rect last = GUILayoutUtility.GetLastRect();
			inspectedRect = ToWindowRect(last); 
		}
	}

	void DrawInspectedFields()
	{
		Type inspectedType = inspectedObject.GetType();
		FieldInfo[] inspectedFields = inspectedType.GetFields();

		foreach(var field in inspectedFields)
		{
			string fName = field.Name;
			object value = field.GetValue(inspectedObject);

			Type type = value?.GetType();

			EditorGUILayout.LabelField(fName);

			if(type == typeof(string))
			{
				DrawStringField(field); 
			}
			else if(typeof(ScriptableObject).IsAssignableFrom(type))
			{
				DrawScriptableField(field); 
			}
			else if(typeof(IList).IsAssignableFrom(type))
			{
				DrawIListField(field);
			}
			else
			{
				DrawUnassignedField(field);
			}
		}
	}

	void OnBeforeInspectedObjectChanged(ScriptableObject updated)
	{
		DeleteChildNodes();
		ResetChildNodeInfo();

		if(parentNode)
		{
			parentNode.OnBeforeInspectedFieldChanged(this, updated);
		}

		// Update inspected object
		SetInspectedObject(updated);
	}

	void OnBeforeInspectedFieldChanged(ScriptableNode node, ScriptableObject updatedValue)
	{
		Debug.Log("on before inspected changed"); 

		RemoveNodeInfo(node);
		AddNodeInfo(node, updatedValue);

		// Update inspected field value
		UpdateInspectedField(node.GetInspectedObject(), updatedValue); 
	}

	void DeleteChildNodes()
	{
		childNodes.ForEach(node => NodeEditor.DeleteNode(node));
	}
	   
	void ResetChildNodeInfo()
	{
		childNodes = new List<ScriptableNode>();
		referencedSOFields = new List<ScriptableObject>();
		fieldByRects = new Dictionary<ScriptableNode, Rect>(); 
	}

	void DrawIListField(FieldInfo field)
	{
		IList iList = field.GetValue(inspectedObject) as IList;
		// Find the type of the elements  
		Type type = iList.GetType().GetGenericArguments()[0];
		// If its a scriptable object collection 
		if(typeof(ScriptableObject).IsAssignableFrom(type))
		{
			foreach(var item in iList)
			{
				ScriptableObject scriptable = item as ScriptableObject;

				EditorGUILayout.BeginHorizontal(); 
				remove = GUILayout.Toggle(remove, "Remove", "Button");

				if(remove)
				{
					BreakLinkFrom(childNodes.FirstOrDefault(n => n.GetInspectedObject() == scriptable));
					remove = false; 
				}
				else
				{
					//EditorGUI.BeginChangeCheck();
					EditorGUI.BeginDisabledGroup(scriptable != null); 
					scriptable = (ScriptableObject)EditorGUILayout.ObjectField(scriptable, typeof(ScriptableObject), false);
					EditorGUI.EndDisabledGroup(); 
					//if(EditorGUI.EndChangeCheck())
					//{
					//	// TODO Implement
					//}
				}

				EditorGUILayout.EndHorizontal(); 

				UpdateFieldRect(scriptable); 

				if(!referencedSOFields.Contains(scriptable))
				{
					MakeLinkWith(scriptable);
				}
			}

			// Draw adding field
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Add", GUILayout.Width(25f)); 
			ScriptableObject so = null;
			EditorGUI.BeginChangeCheck(); 
			so = (ScriptableObject)EditorGUILayout.ObjectField(so, typeof(ScriptableObject), false);
			if(EditorGUI.EndChangeCheck())
			{
				if(so != null)
				{
					iList.Add(so);
				}
			}
			EditorGUILayout.EndHorizontal(); 
		}
	}

	void DrawScriptableField(FieldInfo field)
	{
		ScriptableObject scriptable = field.GetValue(inspectedObject) as ScriptableObject;

		EditorGUILayout.BeginHorizontal();
		remove = GUILayout.Toggle(remove, "Remove", "Button");

		if(remove)
		{
			BreakLinkFrom(childNodes.FirstOrDefault(n => n.GetInspectedObject() == scriptable));
			remove = false; 
		}
		else
		{
			//EditorGUI.BeginChangeCheck();
			EditorGUI.BeginDisabledGroup(scriptable != null); 
			field.SetValue(inspectedObject, (ScriptableObject)EditorGUILayout.ObjectField(scriptable, typeof(ScriptableObject), false));
			EditorGUI.EndDisabledGroup(); 
			//if(EditorGUI.EndChangeCheck())
			//{
			//	// TODO Implement
			//}
		}

		EditorGUILayout.EndHorizontal(); 

		UpdateFieldRect(scriptable); 

		// If value does not exist in the field SOs 
		if(!referencedSOFields.Contains(scriptable))
		{
			MakeLinkWith(scriptable);
		}		
	}

	void UpdateFieldRect(ScriptableObject so)
	{
		if(Event.current.type == EventType.Repaint)
		{
			Rect lastRect = GUILayoutUtility.GetLastRect();
			ScriptableNode node = childNodes.FirstOrDefault(n => n.GetInspectedObject() == so);

			if(!fieldByRects.ContainsKey(node))
			{
				fieldByRects.Add(node, ToWindowRect(lastRect));
			}
			else
			{
				fieldByRects[node] = ToWindowRect(lastRect);
			}
		}
	}

	void DrawStringField(FieldInfo field)
	{
		// Set string field of the scriptable
		string val = field.GetValue(inspectedObject) as string; 
		field.SetValue(inspectedObject, EditorGUILayout.TextField(val));
	}

	void DrawUnassignedField(FieldInfo field)
	{
		// null field is considered scriptable object
		// (not sure how to get type of null)
		ScriptableObject scriptable = null;
		EditorGUI.BeginChangeCheck();
		scriptable = (ScriptableObject)EditorGUILayout.ObjectField(scriptable, typeof(ScriptableObject), false);
		if(EditorGUI.EndChangeCheck())
		{
			field.SetValue(inspectedObject, scriptable);
		}
	}
}
