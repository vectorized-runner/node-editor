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
		// On parent node 
		public FieldInfo inspectedField;
		public Rect fieldRect;
		public int fieldId;
		// On children node  
		public ScriptableObjectNode linkedNode;
	}

	public override string WindowTitle =>
		inspectedObject ? string.Format("{0} ({1})", inspectedObject.name, inspectedObject.GetType()) : null;

	List<FieldInfoRect> fieldInfoRects = new List<FieldInfoRect>();
	List<ScriptableObjectNode> childNodes = new List<ScriptableObjectNode>();
	FieldInfo[] inspectedFields;
	ScriptableObject inspectedObject;
	Rect inspectedRect;
	Type inspectedType;
	string assetNameField;
	string classNameField;
	string createField;
	string shortcutField;
	string shortcutClassField;

	public void SetInspectedObject(ScriptableObject sObject)
	{
		inspectedObject = sObject;
		OnInspectedObjectChanged();
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
		childNodes.ForEach(node =>
		{
			node.windowRect.position += delta;
			node.GroupDrag(delta);
		});
	}

	public override void OnWindowColorChanged()
	{
		childNodes.ForEach(node =>
		{
			// Child nodes inherit parent color 
			node.windowColor = windowColor;
			// Recursively
			node.OnWindowColorChanged();
		});
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

			// Find the field info rect with linked node as deleted node 
			// Remove its field info
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

	public override void DragHiddenNodes(Vector2 delta)
	{
		// Move hidden child nodes by your move amount 
		// So that their relative layout won't change 
		foreach(var hidden in childNodes.Where(node => node.hidden))
		{
			hidden.windowRect.position += delta;
			hidden.DragHiddenNodes(delta);
		}
	}

	public override void DrawCurves()
	{
		if(!hidden)
		{
			IEnumerable unhidden = fieldInfoRects.Where(f => !f.linkedNode.hidden);

			foreach(FieldInfoRect fRect in unhidden)
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

	void OnInspectedObjectChanged()
	{
		// Update 
		inspectedType = inspectedObject.GetType();
		inspectedFields = inspectedType.GetFields();
	}

	void DrawVisibilityOptions()
	{
		EditorGUILayout.BeginHorizontal();

		if(Toggle("Expand All"))
		{
			ExpandChildNodes();
		}

		if(Toggle("Hide All"))
		{
			HideChildNodes();
		}

		EditorGUILayout.EndHorizontal();
	}

	void RemoveFieldInfoRect(FieldInfo field, int fieldId)
	{
		FieldInfoRect fRect = GetFieldRect(null, field, fieldId, false);
		fieldInfoRects.Remove(fRect);

		// Update fieldId for field rects sharing the same field
		// (for updating lists) 
		foreach(var frect in fieldInfoRects.Where(fr => fr.inspectedField == field))
		{
			if(frect.fieldId > fieldId)
			{
				frect.fieldId--;
			}
		}

		// Check if any node exists without linked field rect 
		ScriptableObjectNode toRemove = childNodes.FirstOrDefault(node => !fieldInfoRects.Any(fr => fr.linkedNode == node));

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
				RemoveFieldInfoRect(field, fieldId);
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
					RemoveFieldInfoRect(field, fieldId);
				}
			}
		}
	}

	void RemoveFieldReference(FieldInfo field, int fieldId)
	{
		OnBeforeInspectedObjectUpdated();

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

	ScriptableObjectNode CreateNodeInstance(ScriptableObject inspected)
	{
		ScriptableObjectNode last = childNodes.LastOrDefault();

		Rect rect = new Rect();

		if(last)
		{
			rect.position = last.windowRect.MiddleLeft() + new Vector2(0, 300f);
		}
		else
		{
			rect.position = windowRect.MiddleLeft() - new Vector2(300f, 0f);
		}

		// Let rect auto layout 
		rect.width = 1f;
		rect.height = 1f;
		// Create scriptable node 
		ScriptableObjectNode scriptableNode = NodeEditor.CreateNodeInstance<ScriptableObjectNode>(rect);
		scriptableNode.SetInspectedObject(inspected);
		// Node Color is inherited at the beginning 
		scriptableNode.windowColor = windowColor;
		// Add to child nodes 
		childNodes.Add(scriptableNode);

		return scriptableNode;
	}

	void OnBeforeInspectedObjectUpdated()
	{
		// Undo functionality for prefabs 
		Undo.RecordObject(inspectedObject, "InspectedObjectModification");
		PrefabUtility.RecordPrefabInstancePropertyModifications(inspectedObject);
		// VERY IMPORTANT 
		// Makes sure scriptable object changes are saved to disk.
		// Without this the inspected object will reset on re-opening the editor.
		EditorUtility.SetDirty(inspectedObject);
	}

	void DrawInspectedObject()
	{
		EditorGUILayout.LabelField("Inspected Object");

		bool isNull = inspectedObject == null;

		EditorGUI.BeginChangeCheck();
		EditorGUI.BeginDisabledGroup(!isNull);
		ScriptableObject temp = null;
		temp = (ScriptableObject)EditorGUILayout.ObjectField(inspectedObject, typeof(ScriptableObject), false);
		EditorGUI.EndDisabledGroup();
		if(EditorGUI.EndChangeCheck())
		{
			SetInspectedObject(temp);
		}

		if(Event.current.type == EventType.Repaint)
		{
			Rect last = GUILayoutUtility.GetLastRect();
			inspectedRect = ToFieldRect(last);
		}

		if(isNull)
		{
			assetNameField = EditorGUILayout.TextField("Asset Name", assetNameField);
			classNameField = EditorGUILayout.TextField("Class Name", classNameField);

			if(Toggle("Create Scriptable"))
			{
				if(classShortcuts.ContainsKey(classNameField))
				{
					string val = classShortcuts[classNameField];
					SetInspectedObject(ScriptableObjectUtility.CreateScriptableAsset(assetNameField, val));
				}
				else
				{
					SetInspectedObject(ScriptableObjectUtility.CreateScriptableAsset(assetNameField, classNameField));
				}
			}

			DrawShortcuts();
		}
	}

	void DrawShortcuts()
	{
		if(classShortcuts.Count > 0)
		{
			EditorGUILayout.LabelField("Saved Shortcuts");

			foreach(var keyValuePair in classShortcuts)
			{
				EditorGUILayout.BeginHorizontal();

				if(Toggle("Remove"))
				{
					NodeEditor.RemoveShortcut(keyValuePair.Key);
				}

				EditorGUILayout.LabelField(keyValuePair.Key);
				EditorGUILayout.LabelField(keyValuePair.Value);
				EditorGUILayout.EndHorizontal();
			}
		}

		// Draw creating new shortcut
		EditorGUILayout.BeginHorizontal();
		shortcutField = EditorGUILayout.TextField("Shortcut", shortcutField);
		shortcutClassField = EditorGUILayout.TextField("Class Name", shortcutClassField);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();

		if(Toggle("Create Shortcut"))
		{
			NodeEditor.CreateShortcut(shortcutField, shortcutClassField);
		}

		if(Toggle("Reset Shortcuts"))
		{
			NodeEditor.ResetShortcuts();
		}

		EditorGUILayout.EndHorizontal();
	}

	bool Toggle(string text)
	{
		EditorGUI.BeginChangeCheck();
		GUILayout.Toggle(false, text, "Button");

		return EditorGUI.EndChangeCheck();
	}

	void DrawInspectedFields()
	{
		foreach(var field in inspectedFields)
		{
			Type type = field.FieldType;

			// Don't draw hideininspector
			if(Attribute.IsDefined(field, typeof(HideInInspector)))
			{
				continue;
			}

			string label = field.Name.SplitCamelCase().UppercaseFirst();

			if(type == typeof(string))
			{
				DrawString(field, label);
			}
			else if(type == typeof(int))
			{
				DrawInt(field, label);
			}
			else if(type == typeof(float))
			{
				DrawFloat(field, label);
			}
			else if(type == typeof(bool))
			{
				DrawBool(field, label);
			}
			else if(type == typeof(Vector2))
			{
				DrawVector2(field, label);
			}
			else if(type == typeof(Vector3))
			{
				DrawVector3(field, label);
			}
			else if(type == typeof(Color))
			{
				DrawColor(field, label);
			}
			else if(type.IsEnum)
			{
				DrawEnum(field, label);
			}
			else if(typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				DrawUnityObject(field, label);
			}
			else if(type.IsArray)
			{
				DrawArray(field, label);
			}
			else if(typeof(IList).IsAssignableFrom(type))
			{
				DrawList(field, label);
			}
			else
			{
				EditorGUILayout.LabelField(field.Name);
				EditorGUILayout.LabelField("Drawing " + type + " is not supported.");
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

	void DrawExpandField(ScriptableObject sObject)
	{
		ScriptableObjectNode inspector = childNodes.FirstOrDefault(node => node.inspectedObject == sObject);

		if(inspector != null)
		{
			if(inspector.hidden)
			{
				if(Toggle("Expand"))
				{
					inspector.hidden = false;
					// Do not expand child nodes 
				}
			}
			else
			{
				if(Toggle("Hide"))
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
			.FirstOrDefault(fr => fr.inspectedField == fieldInfo && fr.fieldId == fieldId);

		if(fRect == null && createNew)
		{
			fRect = new FieldInfoRect();
			fRect.inspectedField = fieldInfo;
			fRect.fieldId = fieldId;

			ScriptableObjectNode node = childNodes.FirstOrDefault(n => n.inspectedObject == sObject);

			if(node == null)
			{
				// Try to find from existing nodes 
				// (Multi linking) 
				// Note: Causes many errors as objects are linking back to themselves in many occasions
				// Whereas creating new node for each child is safer
				node = ScriptableObjectInspector.FindExistingNode(sObject);

				if(node)
				{
					// Add to child nodes 
					childNodes.Add(node);
				}
				else
				{
					// Create new 
					node = CreateNodeInstance(sObject);
				}
			}

			fRect.linkedNode = node;
			fieldInfoRects.Add(fRect);
		}

		return fRect;
	}


	void DrawString(FieldInfo field, string label)
	{
		EditorGUILayout.LabelField(label);
		string val = (string)field.GetValue(inspectedObject);

		EditorGUI.BeginChangeCheck();
		string temp = EditorGUILayout.TextField(val);
		if(EditorGUI.EndChangeCheck())
		{
			OnBeforeInspectedObjectUpdated();
			field.SetValue(inspectedObject, temp);
		}
	}

	void DrawInt(FieldInfo field, string label)
	{
		EditorGUILayout.LabelField(label);
		int val = (int)field.GetValue(inspectedObject);

		EditorGUI.BeginChangeCheck();
		int temp = EditorGUILayout.IntField(val);
		if(EditorGUI.EndChangeCheck())
		{
			OnBeforeInspectedObjectUpdated();
			field.SetValue(inspectedObject, temp);
		}
	}

	void DrawFloat(FieldInfo field, string label)
	{
		EditorGUILayout.LabelField(label);
		float val = (float)field.GetValue(inspectedObject);

		EditorGUI.BeginChangeCheck();
		float temp = EditorGUILayout.FloatField(val);
		if(EditorGUI.EndChangeCheck())
		{
			OnBeforeInspectedObjectUpdated();
			field.SetValue(inspectedObject, temp);
		}
	}

	void DrawBool(FieldInfo field, string label)
	{
		EditorGUILayout.LabelField(label);
		bool val = (bool)field.GetValue(inspectedObject);

		EditorGUI.BeginChangeCheck();
		bool temp = EditorGUILayout.Toggle(val);
		if(EditorGUI.EndChangeCheck())
		{
			OnBeforeInspectedObjectUpdated();
			field.SetValue(inspectedObject, temp);
		}
	}

	void DrawVector2(FieldInfo field, string label)
	{
		Vector2 val = (Vector2)field.GetValue(inspectedObject);

		EditorGUI.BeginChangeCheck();
		Vector2 temp = EditorGUILayout.Vector2Field(label, val);
		if(EditorGUI.EndChangeCheck())
		{
			OnBeforeInspectedObjectUpdated();
			field.SetValue(inspectedObject, temp);
		}
	}

	void DrawVector3(FieldInfo field, string label)
	{
		Vector3 val = (Vector3)field.GetValue(inspectedObject);

		EditorGUI.BeginChangeCheck();
		Vector3 temp = EditorGUILayout.Vector3Field(label, val);
		if(EditorGUI.EndChangeCheck())
		{
			OnBeforeInspectedObjectUpdated();
			field.SetValue(inspectedObject, temp);
		}
	}

	void DrawEnum(FieldInfo field, string label)
	{
		EditorGUILayout.LabelField(label);
		Enum val = (Enum)field.GetValue(inspectedObject);

		EditorGUI.BeginChangeCheck();
		Enum temp = EditorGUILayout.EnumPopup(val);
		if(EditorGUI.EndChangeCheck())
		{
			OnBeforeInspectedObjectUpdated();
			field.SetValue(inspectedObject, temp);
		}
	}

	void DrawColor(FieldInfo field, string label)
	{
		EditorGUILayout.LabelField(label);
		Color val = (Color)field.GetValue(inspectedObject);

		EditorGUI.BeginChangeCheck();
		Color temp = EditorGUILayout.ColorField(val);
		if(EditorGUI.EndChangeCheck())
		{
			OnBeforeInspectedObjectUpdated();
			field.SetValue(inspectedObject, temp);
		}
	}

	void DrawArray(FieldInfo field, string label)
	{
		EditorGUILayout.LabelField(label);
		EditorGUILayout.LabelField("Drawing arrays is not supported.");
	}

	void DrawUnityObject(FieldInfo field, string label)
	{
		EditorGUILayout.LabelField(label);
		Type type = field.FieldType;

		// Draw separate window for scriptable objects 
		if(typeof(ScriptableObject).IsAssignableFrom(type))
		{
			DrawScriptableObject(field);
		}
		else
		{
			EditorGUI.BeginChangeCheck();
			UnityEngine.Object obj = (UnityEngine.Object)field.GetValue(inspectedObject);
			if(EditorGUI.EndChangeCheck())
			{
				OnBeforeInspectedObjectUpdated();
				field.SetValue(inspectedObject, EditorGUILayout.ObjectField(obj, type, false));
			}
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
			if(Toggle("Remove"))
			{
				RemoveFieldInfo(field, fieldId);
				return;
			}

			UpdateFieldRect(sObject, field, fieldId);
		}
		else
		{
			DrawCreateField(field, field.FieldType.ToString());
		}

		DrawExpandField(sObject);

		Type fieldType = field.FieldType;

		EditorGUI.BeginDisabledGroup(!isNull);
		EditorGUI.BeginChangeCheck();

		ScriptableObject temp = (ScriptableObject)EditorGUILayout.ObjectField(sObject, fieldType, false);
		if(EditorGUI.EndChangeCheck())
		{
			OnBeforeInspectedObjectUpdated();
			field.SetValue(inspectedObject, temp);
		}

		EditorGUI.EndDisabledGroup();
		EditorGUILayout.EndHorizontal();
	}

	void DrawCreateField(IList iList, int index)
	{
		string className = iList.GetType().GetGenericArguments()[0].ToString();

		if(Toggle("Create"))
		{
			OnBeforeInspectedObjectUpdated();
			ScriptableObject asset = ScriptableObjectUtility.CreateScriptableAsset(createField, className);
			iList[index] = asset;
		}

		createField = EditorGUILayout.TextField(createField);
	}

	void DrawCreateField(FieldInfo field, string className)
	{
		if(Toggle("Create"))
		{
			OnBeforeInspectedObjectUpdated();
			ScriptableObject asset = ScriptableObjectUtility.CreateScriptableAsset(createField, className);
			field.SetValue(inspectedObject, asset);
		}

		createField = EditorGUILayout.TextField(createField);
	}

	void DrawList(FieldInfo field, string label)
	{
		EditorGUILayout.LabelField(label);

		// If not IList (caused by uninitialized lists?) 
		if(!(field.GetValue(inspectedObject) is IList iList))
		{
			return;
		}

		Type type = iList.GetType().GetGenericArguments()[0];

		for(int i = 0; i < iList.Count; i++)
		{
			int fieldId = i;
			EditorGUILayout.BeginHorizontal();

			if(Toggle("Remove"))
			{
				RemoveFieldInfo(field, fieldId);
				return;
			}

			// Shift this element to left
			if(Toggle("Up"))
			{
				if(i != 0)
				{
					OnBeforeInspectedObjectUpdated();

					if(typeof(ScriptableObject).IsAssignableFrom(type))
					{
						// Update field info rects
						var fRect1 = fieldInfoRects.First(fr => fr.inspectedField == field && fr.fieldId == i);
						var frect2 = fieldInfoRects.First(fr => fr.inspectedField == field && fr.fieldId == i - 1);
						fRect1.fieldId--;
						frect2.fieldId++;
					}

					// Update the list 
					object t = iList[i - 1];
					iList[i - 1] = iList[i];
					iList[i] = t;
				}
			}

			// Shift this element to right 
			if(Toggle("Down"))
			{
				if(i != iList.Count - 1)
				{
					OnBeforeInspectedObjectUpdated();

					if(typeof(ScriptableObject).IsAssignableFrom(type))
					{
						// Update field info rects
						var fRect1 = fieldInfoRects.First(fr => fr.inspectedField == field && fr.fieldId == i);
						var frect2 = fieldInfoRects.First(fr => fr.inspectedField == field && fr.fieldId == i + 1);
						fRect1.fieldId++;
						frect2.fieldId--;
					}

					// Update the list 
					object t = iList[i + 1];
					iList[i + 1] = iList[i];
					iList[i] = t;
				}
			}

			EditorGUI.BeginChangeCheck();

			object temp = null;

			if(type == typeof(string))
			{
				temp = EditorGUILayout.TextField((string)iList[i]);
			}
			else if(type == typeof(int))
			{
				temp = EditorGUILayout.IntField((int)iList[i]);
			}
			else if(type == typeof(float))
			{
				temp = EditorGUILayout.FloatField((float)iList[i]);
			}
			else if(type == typeof(bool))
			{
				temp = EditorGUILayout.Toggle((bool)iList[i]);
			}
			else if(type == typeof(Vector2))
			{
				temp = EditorGUILayout.Vector2Field("", (Vector2)iList[i]);
			}
			else if(type == typeof(Vector3))
			{
				temp = EditorGUILayout.Vector3Field("", (Vector3)iList[i]);
			}
			else if(type == typeof(Color))
			{
				temp = EditorGUILayout.ColorField((Color)iList[i]);
			}
			else if(type == typeof(Enum))
			{
				temp = EditorGUILayout.EnumPopup((Enum)iList[i]);
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
						Debug.Log("Field Info reference is the same as Inspected Object. Abort.");
						return;
					}

					DrawExpandField(sObject);

					bool isNull = sObject == null;

					if(isNull)
					{
						DrawCreateField(iList, i);
					}

					EditorGUI.BeginDisabledGroup(!isNull);
					temp = EditorGUILayout.ObjectField(sObject, type, false);

					if(!isNull)
					{
						UpdateFieldRect(sObject, field, fieldId);
					}

					EditorGUI.EndDisabledGroup();
				}
				else
				{
					temp = EditorGUILayout.ObjectField((UnityEngine.Object)iList[i], type, false);
				}
			}

			if(EditorGUI.EndChangeCheck())
			{
				OnBeforeInspectedObjectUpdated();
				iList[i] = temp;
			}

			EditorGUILayout.EndHorizontal();
		}

		DrawAdditionField(iList, type);
	}

	void DrawAdditionField(IList iList, Type type)
	{
		EditorGUILayout.BeginHorizontal();

		if(Toggle("Add"))
		{
			OnBeforeInspectedObjectUpdated();

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
