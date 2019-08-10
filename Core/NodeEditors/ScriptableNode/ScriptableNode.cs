using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using UnityEditor;

public class ScriptableNode : BaseNode
{
	ScriptableObject scriptable;
	List<ScriptableObject> scriptableFields = new List<ScriptableObject>();
	List<ScriptableNode> childNodes = new List<ScriptableNode>();

	public override string WindowTitle => scriptable != null ? scriptable.name : "Title";

	public void SetScriptable(ScriptableObject so)
	{
		scriptable = so;
	}

	public ScriptableObject GetScriptable()
	{
		return scriptable;
	}

	public override void MakeLink(BaseNode node, Vector2 clickPos)
	{
		ScriptableNode sNode = node as ScriptableNode;

		if(sNode != null)
		{
			ScriptableObject sObject = sNode.GetScriptable();

			// TODO Use List.Add to add this to your list 

			//Type IListRef = typeof(List<>);
			//Type[] IListParam = { typeof(ScriptableObject) };
			//object result = Activator.CreateInstance(IListRef.MakeGenericType(IListParam)); 
		}
	}

	public override void OnBeforeSelfDeleted()
	{
		// Delete child nodes before getting deleted
		DeleteChildNodes();
		ResetFields();
	}

	public override void OnNodeDeleted(BaseNode node)
	{
		if(node is ScriptableNode)
		{
			ScriptableNode scriptableNode = node as ScriptableNode;

			if(childNodes.Contains(scriptableNode))
			{
				BreakNodeLink(scriptableNode);
			}

			// TODO Update methods below here 
		}
	}

	public override void DrawWindow()
	{
		DrawScriptableObj();

		if(scriptable != null)
		{
			DrawFields();
		}
	}

	public override void DrawCurves()
	{
		childNodes.ForEach(node =>
		{
			NodeEditor.DrawTransitionCurve(node.windowRect.MiddleRight(), windowRect.MiddleLeft());
		});
	}

	void BreakNodeLink(ScriptableNode node)
	{
		ScriptableObject so = node.GetScriptable();

		// Remove your link with this node 
		childNodes.Remove(node);
		scriptableFields.Remove(so);

		RemoveScriptableReference(so); 

	}

	void RemoveScriptableReference(ScriptableObject so)
	{
		// TODO Unfinished 
		// Remove scriptable reference from our fields if it exists
		// Use List.Remove method 

		// Find the field corresponding to that scriptableObject 
		Type t = scriptable.GetType();
		FieldInfo[] fields = t.GetFields();

		foreach(var field in fields)
		{
			object value = field.GetValue(scriptable);

			// Single scriptable object field
			if(value is ScriptableObject)
			{
				ScriptableObject fieldSO = field.GetValue(scriptable) as ScriptableObject; 

				if(fieldSO == so)
				{
					// Set value to null on removing of the scriptable object 
					field.SetValue(scriptable, null);

					Debug.Log("Set field value to null, and can return now"); 

					return; 
				}
			}
			else if(value is IEnumerable)
			{
				//(value as IList)

				// TODO Extend for list scriptable objects 
			}
		}
	}

	void LinkNode(ScriptableObject so)
	{
		// yPosition is decided by the count 
		int count = scriptableFields.Count;
		Rect rect = windowRect;
		rect.position -= new Vector2(windowRect.width * 1.2f, -windowRect.height * 1.2f * count);
		// Create scriptable node 
		ScriptableNode scriptableNode = NodeEditor.CreateNode<ScriptableNode>(rect);
		scriptableNode.SetScriptable(so);
		scriptableFields.Add(so);
		childNodes.Add(scriptableNode);
	}

	void DrawScriptableObj()
	{
		EditorGUI.BeginChangeCheck();
		ScriptableObject so = (ScriptableObject)EditorGUILayout.ObjectField("ScriptableObject", scriptable, typeof(ScriptableObject), false);
		if(EditorGUI.EndChangeCheck())
		{
			OnScriptableChanged(so);
		}
	}

	void DrawFields()
	{
		// Get fields of this scriptable object 	
		Type thisType = scriptable.GetType();
		FieldInfo[] fields = thisType.GetFields();

		foreach(var field in fields)
		{
			string fName = field.Name;
			object value = field.GetValue(scriptable);

			EditorGUILayout.LabelField(fName);

			if(value is string)
			{
				DrawStringField(field); 
			}
			else if(value is ScriptableObject)
			{
				DrawScriptableField(field); 
			}
			else if(value is IEnumerable)
			{
				DrawIEnumerableField(field); 
			}
			else
			{
				ScriptableObject so = null;
				EditorGUI.BeginChangeCheck(); 
				so = (ScriptableObject)EditorGUILayout.ObjectField("ScriptableObject", so, typeof(ScriptableObject), false);
				if(EditorGUI.EndChangeCheck())
				{
					field.SetValue(scriptable, so); 
				}
			}
		}
	}

	void OnScriptableChanged(ScriptableObject updated)
	{
		DeleteChildNodes();
		ResetFields();
		SetScriptable(updated);
	}

	void DeleteChildNodes()
	{
		childNodes.ForEach(node => NodeEditor.DeleteNode(node));
	}

	void ResetFields()
	{
		childNodes = new List<ScriptableNode>();
		scriptableFields = new List<ScriptableObject>();
	}

	void DrawIEnumerableField(FieldInfo field)
	{
		//EditorGUILayout.LabelField(field.Name); 

		IEnumerable enumerable = field.GetValue(scriptable) as IEnumerable;
		// Find the type of the elements  
		Type type = enumerable.GetType().GetGenericArguments()[0];
		// If its a scriptable object collection 
		if(typeof(ScriptableObject).IsAssignableFrom(type))
		{
			foreach(var item in enumerable)
			{
				ScriptableObject so = item as ScriptableObject;
				EditorGUI.BeginChangeCheck();
				so = (ScriptableObject)EditorGUILayout.ObjectField(so.name, so, typeof(ScriptableObject), false);
				if(EditorGUI.EndChangeCheck())
				{
					// TODO Do something in here 
					Debug.Log("Single scriptable object field was changed");
				}

				if(!scriptableFields.Contains(so))
				{
					LinkNode(so);
				}
			}
		}
	}

	void DrawScriptableField(FieldInfo field)
	{
		ScriptableObject sObject = field.GetValue(scriptable) as ScriptableObject;

		// Set scriptable object field of the scriptable
		EditorGUI.BeginChangeCheck();
		field.SetValue(scriptable, (ScriptableObject)EditorGUILayout.ObjectField(sObject.name, sObject, typeof(ScriptableObject), false));
		if(EditorGUI.EndChangeCheck())
		{
			Debug.Log("Single scriptable object field was changed");
		}

		// If value does not exist in the field SOs 
		if(!scriptableFields.Contains(sObject))
		{
			LinkNode(sObject);
		}
	}

	void DrawStringField(FieldInfo field)
	{
		// Display the string field 
		//EditorGUILayout.LabelField(field.Name);


		// Set string field of the scriptable
		string val = field.GetValue(scriptable) as string; 
		field.SetValue(scriptable, EditorGUILayout.TextField(val));
	}
}
