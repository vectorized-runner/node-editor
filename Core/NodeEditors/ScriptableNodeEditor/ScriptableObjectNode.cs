using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using UnityEditor;
using System.Linq;

public class ScriptableObjectNode : Node
{
	public class FieldInfoContainer
	{
		// This class helps us relate 
		// Field infos with their related rect on main node
		// and the other node they are linked with

		// On parent node 
		public FieldInfo inspectedField;
		public Rect fieldRect;
		// Keep track of lists
		// Index of the element for lists
		// -1 for non-lists
		public int fieldId;
		// On children node  
		public ScriptableObjectNode linkedNode;
	}

	public override string WindowTitle =>
		inspectedObject ? string.Format("{0} ({1})", inspectedObject.name, inspectedObject.GetType()) : null;

	List<FieldInfoContainer> fieldInfoContainers = new List<FieldInfoContainer>();
	List<ScriptableObjectNode> childNodes = new List<ScriptableObjectNode>();
	FieldInfo[] inspectedObjectFields;
	ScriptableObject inspectedObject;
	Rect inspectedObjectRect;
	Type inspectedType;
	string assetNameField;
	string classNameField;
	string createField;
	string shortcutField;
	string shortcutClassField;

	const int NonListFieldId = -1;

	public void SetInspectedObject(ScriptableObject sObject)
	{
		inspectedObject = sObject;
		OnInspectedObjectChanged();
	}

	public Rect GetInspectedRect()
	{
		return inspectedObjectRect;
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

	public override void OnBeforeSelfDeleted()
	{
		DeleteChildNodes();
	}

	public override void OnNodeDeleted(Node node)
	{
		if(node is ScriptableObjectNode scriptableNode)
		{
			// Find the field info rect with linked node as deleted node 
			// Remove its field info
			if(childNodes.Contains(scriptableNode))
			{
				for(int i = fieldInfoContainers.Count - 1; i >= 0; i--)
				{
					FieldInfoContainer fRect = fieldInfoContainers[i];

					if(fRect.linkedNode == scriptableNode)
					{
						RemoveFieldInfo(fRect.inspectedField, fRect.fieldId);
					}
				}
			}
		}
	}

	public override void OnDrawWindow()
	{
		base.OnDrawWindow();

		DrawInspectedObject();

		if(inspectedObject != null)
		{
			DrawInspectedFields();
			DrawExpandHideAll();
		}
	}

	public override void DragHiddenNodes(Vector2 delta)
	{
		// Move hidden child nodes by your move amount 
		// So that their relative layout won't change 
		childNodes.ForEach(node =>
		{
			if(node.isHidden)
			{
				node.windowRect.position += delta;
				node.DragHiddenNodes(delta);
			}
		});
	}

	public override void OnDrawCurves()
	{
		if(!isHidden)
		{
			fieldInfoContainers.ForEach(container =>
			{
				if(!container.linkedNode.isHidden)
				{
					ScriptableObjectNode node = container.linkedNode;

					Rect inspected = node.GetInspectedRect();
					float startX = node.windowRect.xMax;
					float startY = (inspected.yMin + inspected.yMax) / 2f;
					Vector2 start = new Vector2(startX, startY);

					float endX = windowRect.xMin;
					float endY = (container.fieldRect.yMin + container.fieldRect.yMax) / 2f;
					Vector2 end = new Vector2(endX, endY);

					Color blend = (node.windowColor + windowColor) / 2f;

					NodeEditor.DrawCurve(start, end, blend);
				}
			});
		}
	}

	public void OnVisualizeCurves()
	{
		// Allow resize on visualize 
		allowResizeHorizontal = true; 

		if(!isHidden)
		{
			fieldInfoContainers.ForEach(container =>
			{
				ScriptableObjectNode node = container.linkedNode;

				if(!node.isHidden)
				{
					Rect other = node.windowRect;
					Vector2 start = other.CenterRight();
					Vector2 end = windowRect.CenterLeft();

					Color blend = (node.windowColor + windowColor) / 2f;

					NodeEditor.DrawCurve(start, end, blend);
				}
			});
		}	
	}

	public void OnVisualizeWindow()
	{
		if(inspectedObject != null)
		{
			// Draw first string for visualizing
			foreach(FieldInfo field in inspectedObjectFields)
			{
				if(field.FieldType == typeof(string))
				{
					string label = string.Format("{0}: {1}", field.Name.ToInspector(), field.GetValue(inspectedObject) as string);
					EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
					return;
				}
			}

			// If no string was found 
			EditorGUILayout.LabelField("Nothing to show");
		}
		else
		{
			EditorGUILayout.LabelField("Inspected Object not selected.");
		}
	}

	public void HideChildNodes()
	{
		childNodes.ForEach(node =>
		{
			node.HideChildNodes();
			node.isHidden = true;
		});
	}

	public void ExpandChildNodes()
	{
		childNodes.ForEach(node =>
		{
			node.ExpandChildNodes();
			node.isHidden = false;
		});
	}

	void OnInspectedObjectChanged()
	{
		// Update fields
		inspectedType = inspectedObject.GetType();
		inspectedObjectFields = inspectedType.GetFields();
		// Notify scriptable object inspector 
		ScriptableObjectInspector.OnTypeInspected(inspectedType);
	}

	void DrawExpandHideAll()
	{
		EditorGUILayout.Space();
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

	void RemoveFieldInfoContainer(FieldInfo field, int fieldId)
	{
		FieldInfoContainer container = GetFieldContainer(null, field, fieldId, false);
		fieldInfoContainers.Remove(container);

		// Update fieldId for field rects sharing the same field
		// (for updating lists) 
		foreach(var listElement in fieldInfoContainers.Where(cont => cont.inspectedField == field))
		{
			if(listElement.fieldId > fieldId)
			{
				listElement.fieldId--;
			}
		}

		// Check if any node exists without linked field rect 
		ScriptableObjectNode nodeWithoutLink = childNodes.FirstOrDefault(node => !fieldInfoContainers.Any(fr => fr.linkedNode == node));

		if(nodeWithoutLink != null)
		{
			childNodes.Remove(nodeWithoutLink);
		}
	}

	void RemoveFieldInfo(FieldInfo field, int fieldId)
	{
		RemoveFieldReference(field, fieldId);
		Type type = field.FieldType;

		// For single elements 
		if(fieldId == NonListFieldId)
		{
			// Remove From field rects and child nodes if it is scriptable object 
			if(typeof(ScriptableObject).IsAssignableFrom(type))
			{
				RemoveFieldInfoContainer(field, fieldId);
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
					RemoveFieldInfoContainer(field, fieldId);
				}
			}
		}
	}

	void RemoveFieldReference(FieldInfo field, int fieldId)
	{
		OnBeforeInspectedObjectUpdated();

		if(fieldId == NonListFieldId)
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

		// Determine node create position 
		Rect rect = new Rect();

		if(last)
		{
			rect.position = last.windowRect.CenterLeft() + new Vector2(0, 300f);
		}
		else
		{
			rect.position = windowRect.CenterLeft() - new Vector2(300f, 0f);
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
		// Without this the ScriptableObject asset for the inspected object will reset on re-opening the editor.
		EditorUtility.SetDirty(inspectedObject);
	}

	void DrawInspectedObject()
	{
		EditorGUILayout.LabelField("Inspected Object");

		bool isNull = inspectedObject == null;

		EditorGUI.BeginChangeCheck();
		EditorGUI.BeginDisabledGroup(!isNull);
		ScriptableObject temp = (ScriptableObject)EditorGUILayout.ObjectField(inspectedObject, typeof(ScriptableObject), false);
		EditorGUI.EndDisabledGroup();
		if(EditorGUI.EndChangeCheck())
		{
			SetInspectedObject(temp);
		}

		if(Event.current.type == EventType.Repaint)
		{
			Rect last = GUILayoutUtility.GetLastRect();
			inspectedObjectRect = ToLocalWindowRect(last);
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

				if(Toggle("-"))
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

	bool Toggle(string text, params GUILayoutOption[] options)
	{
		return GUILayout.Toggle(false, text, "Button", options);
	}

	void DrawInspectedFields()
	{
		foreach(var field in inspectedObjectFields)
		{
			DrawField(field, false);
		}
	}

	void DrawField(FieldInfo field, bool disabled)
	{
		Type type = field.FieldType;

		// Don't draw HideInInspector
		if(Attribute.IsDefined(field, typeof(HideInInspector)))
		{
			return;
		}

		string label = field.Name.ToInspector();

		EditorGUI.BeginDisabledGroup(disabled);

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

		EditorGUI.EndDisabledGroup();
	}

	void DeleteChildNodes()
	{
		// To prevent collection modified error, reverse iterate
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
			if(inspector.isHidden)
			{
				if(Toggle("Expand", GUILayout.Width(50f)))
				{
					inspector.isHidden = false;
					// Do not expand child nodes 
				}
			}
			else
			{
				if(Toggle("Hide", GUILayout.Width(50f)))
				{
					inspector.isHidden = true;
					inspector.HideChildNodes();
				}
			}
		}
	}

	void DrawGotoField(ScriptableObject sObject)
	{
		ScriptableObjectNode inspector = childNodes.FirstOrDefault(node => node.inspectedObject == sObject);

		if(inspector != null && !inspector.isHidden)
		{
			if(Toggle("Go to", GUILayout.Width(50f)))
			{
				NodeEditor.ViewAtCenter(inspector);
			}
		}
	}

	void UpdateFieldContainer(ScriptableObject sObject, FieldInfo fieldInfo, int fieldId)
	{
		if(Event.current.type == EventType.Repaint)
		{
			Rect lastRect = GUILayoutUtility.GetLastRect();
			Rect fieldRect = ToLocalWindowRect(lastRect);
			// Set field info rect here 
			FieldInfoContainer fRect = GetFieldContainer(sObject, fieldInfo, fieldId, true);
			fRect.fieldRect = fieldRect;
		}
	}

	FieldInfoContainer GetFieldContainer(ScriptableObject sObject, FieldInfo fieldInfo, int fieldId, bool createNew)
	{
		FieldInfoContainer container = fieldInfoContainers
			.FirstOrDefault(cont => cont.inspectedField == fieldInfo && cont.fieldId == fieldId);

		if(container == null && createNew)
		{
			container = new FieldInfoContainer();
			container.inspectedField = fieldInfo;
			container.fieldId = fieldId;

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

			container.linkedNode = node;
			fieldInfoContainers.Add(container);
		}

		return container;
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
			UnityEngine.Object obj = EditorGUILayout.ObjectField((UnityEngine.Object)field.GetValue(inspectedObject), type, false);
			if(EditorGUI.EndChangeCheck())
			{
				OnBeforeInspectedObjectUpdated();
				field.SetValue(inspectedObject, obj);
			}
		}
	}

	void DrawScriptableObject(FieldInfo field)
	{
		Type fieldType = field.FieldType;
		int fieldId = NonListFieldId;
		ScriptableObject sObject = field.GetValue(inspectedObject) as ScriptableObject;
		bool isNull = sObject == null;

		if(sObject == inspectedObject)
		{
			Debug.Log("FieldInfo reference is the same as InspectedObject. Abort.");
			return;
		}

		EditorGUILayout.BeginHorizontal();

		if(!isNull)
		{
			if(Toggle("-"))
			{
				RemoveFieldInfo(field, fieldId);
				return;
			}

			UpdateFieldContainer(sObject, field, fieldId);
		}
		else
		{
			DrawCreateField(field, field.FieldType.ToString());
		}

		DrawExpandField(sObject);
		DrawGotoField(sObject); 


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

		if(Toggle("Create", GUILayout.Width(50f)))
		{
			OnBeforeInspectedObjectUpdated();
			ScriptableObject asset = ScriptableObjectUtility.CreateScriptableAsset(createField, className);
			iList[index] = asset;
		}

		createField = EditorGUILayout.TextField(createField);
	}

	void DrawCreateField(FieldInfo field, string className)
	{
		if(Toggle("Create", GUILayout.Width(50f)))
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

			if(Toggle("-", GUILayout.Width(25f)))
			{
				RemoveFieldInfo(field, fieldId);
				EditorGUILayout.EndHorizontal();
				return;
			}

			// Shift this element to left
			if(Toggle("<<", GUILayout.Width(25f)))
			{
				if(i != 0)
				{
					OnBeforeInspectedObjectUpdated();

					if(typeof(ScriptableObject).IsAssignableFrom(type))
					{
						// Update field info rects
						var container1 = fieldInfoContainers.First(fr => fr.inspectedField == field && fr.fieldId == i);
						var container2 = fieldInfoContainers.First(fr => fr.inspectedField == field && fr.fieldId == i - 1);
						container1.fieldId--;
						container2.fieldId++;
					}

					// Update the list 
					object t = iList[i - 1];
					iList[i - 1] = iList[i];
					iList[i] = t;
				}
			}

			// Shift this element to right 
			if(Toggle(">>", GUILayout.Width(25f)))
			{
				if(i != iList.Count - 1)
				{
					OnBeforeInspectedObjectUpdated();

					if(typeof(ScriptableObject).IsAssignableFrom(type))
					{
						// Update field info rects
						var container1 = fieldInfoContainers.First(cont => cont.inspectedField == field && cont.fieldId == i);
						var container2 = fieldInfoContainers.First(cont => cont.inspectedField == field && cont.fieldId == i + 1);
						container1.fieldId++;
						container2.fieldId--;
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
						EditorGUILayout.EndHorizontal();
						return;
					}

					DrawExpandField(sObject);
					DrawGotoField(sObject); 

					bool isNull = sObject == null;

					if(isNull)
					{
						DrawCreateField(iList, i);
					}

					EditorGUI.BeginDisabledGroup(!isNull);
					temp = EditorGUILayout.ObjectField(sObject, type, false);

					if(!isNull)
					{
						UpdateFieldContainer(sObject, field, fieldId);
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

		if(Toggle("+", GUILayout.Width(25f)))
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
