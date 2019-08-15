using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using UnityEditor;
using System.Linq;

public class ScriptableObjectNode : BaseNode
{
	public class FieldInfoRect
	{
		// On Parent Node
		public FieldInfo inspectedField;
		public Rect fieldRect;
		public int fieldId;
		// On Children Node 
		public ScriptableObjectNode linkedNode;
	}

	public override string WindowTitle =>
		inspectedObject ? string.Format("{0} ({1})", inspectedObject.name, inspectedObject.GetType()) : null;

	List<FieldInfoRect> fieldInfoRects = new List<FieldInfoRect>();
	List<ScriptableObjectNode> childNodes = new List<ScriptableObjectNode>();
	ScriptableObject inspectedObject;
	Rect inspectedRect;
	bool remove;
	string assetNameText;
	string classNameText;

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

	public override void GroupDrag(Vector2 delta)
	{
		childNodes.ForEach(c =>
		{
			c.windowRect.position += delta;
			c.GroupDrag(delta); 
		}); 
	}

	public override void OnWindowColorChanged()
	{
		foreach(ScriptableObjectNode node in childNodes)
		{
			// Child nodes inherit parent color 
			node.windowColor = windowColor;
			// Recursively
			node.OnWindowColorChanged();
		}
	}

	public override void OnBeforeSelfDeleted()
	{
		DeleteChildNodes();
		childNodes = new List<ScriptableObjectNode>();
	}

	public override void OnNodeDeleted(BaseNode node)
	{
		if(node is ScriptableObjectNode)
		{
			ScriptableObjectNode scriptableNode = node as ScriptableObjectNode;

			if(childNodes.Contains(scriptableNode))
			{
				for(int i = fieldInfoRects.Count - 1; i >= 0; i--)
				{
					FieldInfoRect fRect = fieldInfoRects[i];

					if(fRect.linkedNode == scriptableNode)
					{
						RemoveFieldInfo(fRect.inspectedField, fRect.fieldId);
					}
				}
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
			DrawVisibilityOptions();
		}
	}

	public override void DrawCurves()
	{
		if(!hidden)
		{
			foreach(FieldInfoRect fRect in fieldInfoRects.Where(f => !f.linkedNode.hidden))
			{
				ScriptableObjectNode node = fRect.linkedNode;

				Rect inspected = node.GetInspectedRect();
				float startX = node.windowRect.xMax;
				float startY = (inspected.yMin + inspected.yMax) / 2f;
				Vector2 start = new Vector2(startX, startY);

				float endX = windowRect.xMin;
				float endY = (fRect.fieldRect.yMin + fRect.fieldRect.yMax) / 2f;
				Vector2 end = new Vector2(endX, endY);

				NodeEditor.DrawTransitionCurve(start, end);
			}
		}
	}

	public void HideChildNodes()
	{
		childNodes.ForEach(node =>
		{
			node.HideChildNodes();
			node.hidden = true;
		});
	}

	public void ExpandChildNodes()
	{
		childNodes.ForEach(node =>
		{
			node.ExpandChildNodes();
			node.hidden = false;
		});
	}

	void DrawVisibilityOptions()
	{
		EditorGUILayout.BeginHorizontal();

		EditorGUI.BeginChangeCheck();
		GUILayout.Toggle(false, "Expand All", "Button");
		if(EditorGUI.EndChangeCheck())
		{
			ExpandChildNodes();
		}

		EditorGUI.BeginChangeCheck();
		GUILayout.Toggle(false, "Hide All", "Button");
		if(EditorGUI.EndChangeCheck())
		{
			HideChildNodes();
		}

		EditorGUILayout.EndHorizontal();
	}

	void RemoveFieldRect(FieldInfo field, int fieldId)
	{
		FieldInfoRect fRect = GetFieldRect(null, field, fieldId, false);
		fieldInfoRects.Remove(fRect);

		// Update fieldId for field rects sharing the same field
		// (for updating lists) 
		foreach(var fr in fieldInfoRects.Where(f => f.inspectedField == field))
		{
			if(fr.fieldId > fieldId)
			{
				fr.fieldId--;
			}
		}

		// Check if any node exists without linked field rect 
		ScriptableObjectNode toRemove = childNodes.FirstOrDefault(n => !fieldInfoRects.Any(f => f.linkedNode == n));

		if(toRemove != null)
		{
			childNodes.Remove(toRemove);
		}
	}

	void RemoveFieldInfo(FieldInfo field, int fieldId)
	{
		RemoveFieldReference(field, fieldId);
		Type type = field.FieldType; 

		// For single elements 
		if(fieldId == -1)
		{
			// Remove From field rects and child nodes if it is scriptable object 
			if(typeof(ScriptableObject).IsAssignableFrom(type))
			{
				RemoveFieldRect(field, fieldId); 
			}
		}
		// For lists 
		else
		{
			if(typeof(IList).IsAssignableFrom(type))
			{
				IList iList = field.GetValue(inspectedObject) as IList;
				Type t = iList.GetType().GetGenericArguments()[0];

				if(typeof(ScriptableObject).IsAssignableFrom(t))
				{
					RemoveFieldRect(field, fieldId); 
				}
			}
		}
	}

	void RemoveFieldReference(FieldInfo field, int fieldId)
	{
		// Means it is not a list
		if(fieldId == -1)
		{
			field.SetValue(inspectedObject, null);
		}
		else
		{
			IList iList = field.GetValue(inspectedObject) as IList;
			iList.RemoveAt(fieldId);
		}
	}

	ScriptableObjectNode CreateNodeInstance(ScriptableObject so)
	{
		// yPosition is decided by the count 
		int fieldCount = childNodes.Count;
		Rect rect = windowRect;
		rect.position -= new Vector2(windowRect.width * 1.2f, 0f);
		// Let rect auto layout 
		rect.width = 1f;
		rect.height = 1f;
		// Create scriptable node 
		ScriptableObjectNode scriptableNode = NodeEditor.CreateNodeInstance<ScriptableObjectNode>(rect);
		scriptableNode.SetInspectedObject(so);
		// Node Color is inherited at the beginning 
		scriptableNode.windowColor = windowColor;
		// Hidden by default 
		scriptableNode.hidden = true; 
		// Add to child nodes 
		childNodes.Add(scriptableNode);

		return scriptableNode;
	}

	void DrawInspectedObject()
	{
		EditorGUILayout.LabelField("Inspected Object");

		bool isNull = inspectedObject == null;
		EditorGUI.BeginDisabledGroup(!isNull);
		inspectedObject = (ScriptableObject)EditorGUILayout.ObjectField(inspectedObject, typeof(ScriptableObject), false);
		EditorGUI.EndDisabledGroup();

		if(Event.current.type == EventType.Repaint)
		{
			Rect last = GUILayoutUtility.GetLastRect();
			inspectedRect = ToFieldRect(last);
		}

		if(isNull)
		{
			assetNameText = EditorGUILayout.TextField("Asset Name", assetNameText);
			classNameText = EditorGUILayout.TextField("Class Name", classNameText);

			EditorGUI.BeginChangeCheck();
			GUILayout.Toggle(false, "Create Scriptable", "Button");
			if(EditorGUI.EndChangeCheck())
			{
				inspectedObject = ScriptableObjectUtility.CreateScriptableAsset(assetNameText, classNameText);
			}
		}
	}

	void DrawInspectedFields()
	{
		Type inspectedType = inspectedObject.GetType();
		FieldInfo[] inspectedFields = inspectedType.GetFields();

		foreach(var field in inspectedFields)
		{
			Type type = field.FieldType;

			if(type == typeof(string))
			{
				DrawString(field);
			}
			else if(type == typeof(int))
			{
				DrawInt(field);
			}
			else if(type == typeof(float))
			{
				DrawFloat(field);
			}
			else if(type == typeof(bool))
			{
				DrawBool(field);
			}
			else if(type == typeof(Vector2))
			{
				DrawVector2(field);
			}
			else if(type == typeof(Vector3))
			{
				DrawVector3(field);
			}
			else if(type == typeof(Color))
			{
				DrawColor(field);
			}
			else if(type == typeof(Enum))
			{
				DrawEnum(field);
			}
			else if(typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				DrawUnityObject(field);
			}
			else if(type.IsArray)
			{
				DrawArray(field);
			}
			else if(typeof(IList).IsAssignableFrom(type))
			{
				DrawList(field);
			}
			else
			{
				EditorGUILayout.LabelField(field.Name);
				EditorGUILayout.LabelField("Drawing" + type + "is not supported.");
			}
		}
	}

	void DeleteChildNodes()
	{
		// To prevent collection modified error 
		for(int i = childNodes.Count - 1; i >= 0; i--)
		{
			NodeEditor.DeleteNode(childNodes.ElementAt(i));
		}
	}

	void DrawVisibilityField(ScriptableObject sObject)
	{
		ScriptableObjectNode inspector = childNodes.FirstOrDefault(n => n.inspectedObject == sObject);

		if(inspector != null)
		{
			if(inspector.hidden)
			{
				EditorGUI.BeginChangeCheck();
				GUILayout.Toggle(false, "Expand", "Button");
				if(EditorGUI.EndChangeCheck())
				{
					inspector.hidden = false;
					inspector.ExpandChildNodes();
				}
			}
			else
			{
				EditorGUI.BeginChangeCheck();
				GUILayout.Toggle(false, "Hide", "Button");
				if(EditorGUI.EndChangeCheck())
				{
					inspector.hidden = true;
					inspector.HideChildNodes();
				}
			}
		}
	}

	void UpdateFieldRect(ScriptableObject sObject, FieldInfo fieldInfo, int fieldId)
	{
		if(Event.current.type == EventType.Repaint)
		{
			Rect lastRect = GUILayoutUtility.GetLastRect();
			Rect fieldRect = ToFieldRect(lastRect);
			// Set field info rect here 
			FieldInfoRect fRect = GetFieldRect(sObject, fieldInfo, fieldId, true);
			fRect.fieldRect = fieldRect;
		}
	}

	FieldInfoRect GetFieldRect(ScriptableObject sObject, FieldInfo fieldInfo, int fieldId, bool createNew)
	{
		FieldInfoRect fRect = fieldInfoRects
			.FirstOrDefault(f => f.inspectedField == fieldInfo && f.fieldId == fieldId);

		if(fRect == null && createNew)
		{
			fRect = new FieldInfoRect();
			fRect.inspectedField = fieldInfo;
			fRect.fieldId = fieldId;

			ScriptableObjectNode node = childNodes.FirstOrDefault(n => n.inspectedObject == sObject);

			if(node == null)
			{
				node = CreateNodeInstance(sObject);
			}

			fRect.linkedNode = node;

			fieldInfoRects.Add(fRect);
		}

		return fRect;
	}

	void DrawString(FieldInfo field)
	{
		EditorGUILayout.LabelField(field.Name);
		string val = (string)field.GetValue(inspectedObject);
		field.SetValue(inspectedObject, EditorGUILayout.TextField(val));
	}

	void DrawInt(FieldInfo field)
	{
		EditorGUILayout.LabelField(field.Name);
		int val = (int)field.GetValue(inspectedObject);
		field.SetValue(inspectedObject, EditorGUILayout.IntField(val));
	}

	void DrawFloat(FieldInfo field)
	{
		EditorGUILayout.LabelField(field.Name);
		float val = (float)field.GetValue(inspectedObject);
		field.SetValue(inspectedObject, EditorGUILayout.FloatField(val));
	}

	void DrawBool(FieldInfo field)
	{
		EditorGUILayout.LabelField(field.Name);
		bool val = (bool)field.GetValue(inspectedObject);
		field.SetValue(inspectedObject, EditorGUILayout.Toggle(val));
	}

	void DrawVector2(FieldInfo field)
	{
		Vector2 val = (Vector2)field.GetValue(inspectedObject);
		field.SetValue(inspectedObject, EditorGUILayout.Vector2Field(field.Name, val));
	}

	void DrawVector3(FieldInfo field)
	{
		Vector3 val = (Vector3)field.GetValue(inspectedObject);
		field.SetValue(inspectedObject, EditorGUILayout.Vector3Field(field.Name, val));
	}

	void DrawEnum(FieldInfo field)
	{
		EditorGUILayout.LabelField(field.Name);
		Enum val = (Enum)field.GetValue(inspectedObject);
		field.SetValue(inspectedObject, EditorGUILayout.EnumPopup(val));
	}

	void DrawColor(FieldInfo field)
	{
		EditorGUILayout.LabelField(field.Name);
		Color val = (Color)field.GetValue(inspectedObject);
		field.SetValue(inspectedObject, EditorGUILayout.ColorField(val));
	}

	void DrawArray(FieldInfo field)
	{
		EditorGUILayout.LabelField(field.Name);
		EditorGUILayout.LabelField("Drawing arrays is not supported.");
	}

	void DrawUnityObject(FieldInfo field)
	{
		EditorGUILayout.LabelField(field.Name);
		Type type = field.FieldType;

		// Draw separate window for scriptable objects 
		if(typeof(ScriptableObject).IsAssignableFrom(type))
		{
			DrawScriptableObject(field);
		}
		else
		{
			UnityEngine.Object obj = (UnityEngine.Object)field.GetValue(inspectedObject);
			field.SetValue(inspectedObject, EditorGUILayout.ObjectField(obj, type, false));
		}
	}

	void DrawScriptableObject(FieldInfo field)
	{
		int fieldId = -1;
		ScriptableObject sObject = field.GetValue(inspectedObject) as ScriptableObject;

		if(sObject == inspectedObject)
		{
			Debug.Log("FieldInfo reference is the same as InspectedObject. Abort.");
			return;
		}

		EditorGUILayout.BeginHorizontal();

		bool isNull = sObject == null;

		if(!isNull)
		{
			remove = GUILayout.Toggle(remove, "Remove", "Button");
		}

		DrawVisibilityField(sObject);

		if(remove)
		{
			RemoveFieldInfo(field, fieldId);
			remove = false;
		}
		else
		{
			Type fieldType = field.FieldType;
			EditorGUI.BeginDisabledGroup(!isNull);

			field.SetValue(inspectedObject, EditorGUILayout.ObjectField(sObject, fieldType, false));

			if(!isNull)
			{
				UpdateFieldRect(sObject, field, fieldId);
			}

			EditorGUI.EndDisabledGroup();
		}

		EditorGUILayout.EndHorizontal();
	}

	void DrawList(FieldInfo field)
	{
		IList iList = field.GetValue(inspectedObject) as IList;
		Type type = iList.GetType().GetGenericArguments()[0];

		EditorGUILayout.LabelField(field.Name);

		for(int i = 0; i < iList.Count; i++)
		{
			int fieldId = i;
			EditorGUILayout.BeginHorizontal();

			remove = GUILayout.Toggle(remove, "Remove", "Button");

			if(remove)
			{
				RemoveFieldInfo(field, fieldId);
				remove = false;
				return; 
			}

			if(type == typeof(string))
			{
				iList[i] = EditorGUILayout.TextField((string)iList[i]);
			}
			else if(type == typeof(int))
			{
				iList[i] = EditorGUILayout.IntField((int)iList[i]);
			}
			else if(type == typeof(float))
			{
				iList[i] = EditorGUILayout.FloatField((float)iList[i]);
			}
			else if(type == typeof(bool))
			{
				iList[i] = EditorGUILayout.Toggle((bool)iList[i]);
			}
			else if(type == typeof(Vector2))
			{
				iList[i] = EditorGUILayout.Vector2Field("", (Vector2)iList[i]);
			}
			else if(type == typeof(Vector3))
			{
				iList[i] = EditorGUILayout.Vector3Field("", (Vector3)iList[i]);
			}
			else if(type == typeof(Color))
			{
				iList[i] = EditorGUILayout.ColorField((Color)iList[i]);
			}
			else if(type == typeof(Enum))
			{
				iList[i] = EditorGUILayout.EnumPopup((Enum)iList[i]);
			}
			else if(type.IsArray)
			{
				EditorGUILayout.LabelField("Drawing list of arrays is not supported.");
			}
			else if(type == typeof(IList))
			{
				EditorGUILayout.LabelField("Drawing list of lists is not supported.");
			}
			else if(typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				if(typeof(ScriptableObject).IsAssignableFrom(type))
				{
					ScriptableObject sObject = iList[i] as ScriptableObject;

					if(sObject == inspectedObject)
					{
						Debug.Log("FieldInfo reference is the same as Inspected Object. Abort.");
						return;
					}

					DrawVisibilityField(sObject);

					bool isNull = sObject == null;

					EditorGUI.BeginDisabledGroup(!isNull);
					iList[i] = (ScriptableObject)EditorGUILayout.ObjectField(sObject, type, false);

					if(!isNull)
					{
						UpdateFieldRect(sObject, field, fieldId);
					}

					EditorGUI.EndDisabledGroup();
				}
				else
				{
					iList[i] = EditorGUILayout.ObjectField((UnityEngine.Object)iList[i], type, false);
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		DrawAdditionField(iList, type);
	}

	void DrawAdditionField(IList iList, Type type)
	{
		EditorGUILayout.BeginHorizontal();

		EditorGUI.BeginChangeCheck();
		GUILayout.Toggle(false, "Add", "Button");

		if(EditorGUI.EndChangeCheck())
		{
			if(type == typeof(string))
			{
				iList.Add(default(string));
			}
			else if(type == typeof(int))
			{
				iList.Add(default(int));
			}
			else if(type == typeof(float))
			{
				iList.Add(default(float));
			}
			else if(type == typeof(bool))
			{
				iList.Add(default(bool));
			}
			else if(type == typeof(Vector2))
			{
				iList.Add(default(Vector2));
			}
			else if(type == typeof(Vector3))
			{
				iList.Add(default(Vector3));
			}
			else if(type == typeof(Color))
			{
				iList.Add(default(Color));
			}
			else if(type == typeof(Enum))
			{
				iList.Add(default);
			}
			else if(typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				iList.Add(default);
			}
		}

		EditorGUILayout.EndHorizontal();
	}
}
