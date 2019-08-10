using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using UnityEngine.Events; 
using UnityEditor.Animations;

public class NodeEditor : EditorWindow
{
	// ZoomArea make for fullscreen now 
	protected Rect ZoomArea => new Rect(0f, 0f, Screen.width, Screen.height); 
	bool PossiblyPanning => Event.current.modifiers == EventModifiers.Alt || Event.current.keyCode == KeyCode.LeftAlt;

	static protected List<BaseNode> nodes = new List<BaseNode>();
	static protected EditorWindow editorWindow;
	protected BaseNode lastClickedNode; 
	protected BaseNode transitionStartNode;
	protected bool mouseDraggingTransitionCurve;
	protected float zoom = 1.0f;    
	protected float settingsTabWidthRatio = 0.2f;

	static Color handlesColor = Color.black;
	static float transitionWidth = 0.2f; 
	static float nodeWidth = 300f;
	static float nodeHeight = 300f;
	static Vector2 mousePos;
	Texture2D bgTexture; 
	Vector2 zoomCoordsOrigin = Vector2.zero;
	Material lineMaterial;
	bool settingsDraggingRight;
	bool settingsDraggingLeft;
	bool snapToGrid = true;
	int snapValue = 10; 
	readonly float panSpeed = 5f; 
	readonly string deleteNodeData = "deleteNode";
	readonly string makeTransitionData = "makeTransition";
	readonly Color gridMinorColorDark = new Color(0f, 0f, 0f, 0.18f);
	readonly Color gridMajorColorDark = new Color(0f, 0f, 0f, 0.28f);

	const float MinSettingsTabRatio = 0.2f; 
	const float ZoomMin = 0.1f;
	const float ZoomMax = 10.0f;


	public virtual void OnGUI()
	{
		GetWindow(); 

		HandleZoom();
		HandlePan();
		HandleDeleteNodesWithKey(); 

		DrawBGTexture(); 
		DrawMecanimGrid();

		BeginZoomArea(); 
		HandleNodeLogic();
		EndZoomArea();

		BeginSettings(); 
		DrawSettings();
		EndSettings(); 
	}

	public static void DrawTransitionCurve(Vector2 start, Vector2 end)
	{
		Vector2 startTan = start + Vector2.right * 50f;
		Vector2 endTan = end + Vector2.left * 50f;

		// Draw multiple bezier with different width for increased visual quality 
		for(int i = 0; i < transitionWidth * 10; i++)
		{
			float width = (i + 1) * 2f;
			// Actual Transition Drawing function on the Editor 
			Handles.DrawBezier(start, end, startTan, endTan, handlesColor, null, width);
		}
	}

	public static void DrawTransitionCurve(Rect start, Rect end)
	{
		Vector2 startPos = new Vector2(start.x + start.width, start.y + start.height / 2f);
		Vector2 endPos = new Vector2(end.x + end.width, end.y + end.height / 2f);

		DrawTransitionCurve(startPos, endPos); 
	}

	public virtual void GetWindow()
	{
		// Example override method in here 
		// if(!editorWindow) { editorWindow = GetWindow<YourWindow>(); }
	}

	public virtual void DrawSettings()
	{
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

		snapToGrid = GUILayout.Toggle(snapToGrid, new GUIContent("Snap to grid"));

		if(snapToGrid)
		{
			snapValue = EditorGUILayout.IntSlider(new GUIContent("Snap Value"), snapValue, 10, 100);
		}

		// NodeWidth Setting
		nodeWidth = EditorGUILayout.Slider("Node Width", nodeWidth, 300f, 1000f);
		// NodeHeight Setting
		nodeHeight = EditorGUILayout.Slider("Node Height", nodeHeight, 150f, 1000f);
		// TransitionCurveWidth Setting 
		transitionWidth = EditorGUILayout.Slider("Transition Width", transitionWidth, 0.1f, 3f);

		// TransitionCurveColor Setting
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Transition Tint", GUILayout.Width(100f), GUILayout.Height(20f));
		handlesColor = EditorGUILayout.ColorField(handlesColor, GUILayout.Width(50f), GUILayout.Height(20f));
		EditorGUILayout.EndHorizontal();
	}

	public virtual void DrawNode(BaseNode node, int id)
	{
		// Actual Window Drawing function on the Editor 
		node.windowRect = GUI.Window(id, node.windowRect, WindowFunction, node.WindowTitle);
	}

	public virtual void PaintNode(BaseNode node)
	{
		// Nodes will be white unless this method is overridden
		// Ex: GUI.color = wantedColor 
	}

	public virtual void ShowNodeCreatorMenu()
	{
	}

	public virtual void ShowNodeOptionsMenu()
	{
		// Show Node options on the menu 
		GenericMenu menu = new GenericMenu();

		menu.AddItem(new GUIContent("Make Transition"), false, ContextCallback, makeTransitionData);
		menu.AddSeparator("");
		menu.AddItem(new GUIContent("Delete Node"), false, ContextCallback, deleteNodeData);

		menu.ShowAsContext();
	}

	public virtual void ContextCallback(object obj)
	{
		string callback = obj.ToString(); 

		if(callback.Equals(makeTransitionData))
		{
			int selectedIndex = GetNodeIndexOnMousePos();
			bool clickedOnNode = selectedIndex > -1;

			if(clickedOnNode)
			{
				// Update currently selected node 
				transitionStartNode = nodes[selectedIndex];
				mouseDraggingTransitionCurve = true;
			}
		}
		else if(callback.Equals(deleteNodeData))
		{
			int selectedIndex = GetNodeIndexOnMousePos();
			bool clickedOnNode = selectedIndex > -1;

			if(clickedOnNode)
			{
				DeleteNode(selectedIndex); 
			}
		}
	}

	public static T CreateNode<T>(Rect rect) where T : BaseNode
	{
		T t = (T)CreateInstance(typeof(T)); 
		t.windowRect = rect;
		nodes.Add(t);
		return t;
	}

	public static T CreateNode<T>() where T : BaseNode
	{
		// Create on mousePos by default 
		Rect rect = new Rect(mousePos.x, mousePos.y, nodeWidth, nodeHeight);
		return CreateNode<T>(rect); 
	}

	public static void DeleteNode(int index)
	{
		BaseNode node = nodes.ElementAtOrDefault(index);

		DeleteNode(node); 
	}

	public static void DeleteNode(BaseNode node)
	{
		if(nodes.Contains(node))
		{
			node.OnBeforeSelfDeleted();
			nodes.Remove(node);
			nodes.ForEach(n => n.OnNodeDeleted(node));
		}
	}

	protected int GetNodeIndexOnMousePos()
	{
		// Find the node containing the mouse position
		int value = -1;

		for(int i = 0; i < nodes.Count; i++)
		{
			BaseNode node = nodes[i];

			if(node.windowRect.Contains(mousePos))
			{
				value = i;
				break;
			}
		}

		return value;
	}

	void HandleDeleteNodesWithKey()
	{
		// Delete last clicked node on Delete key down 
		if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
		{
			DeleteNode(lastClickedNode); 
			// Use the event 
			Event.current.Use();
		}
	}

	void HandlePan()
	{
		if(PossiblyPanning)
		{
			EditorGUIUtility.AddCursorRect(new Rect(0, 0, Screen.width, Screen.height), MouseCursor.Pan);
		}

		bool panning = Event.current.type == EventType.MouseDrag &&
			(Event.current.button == 0 && Event.current.modifiers == EventModifiers.Alt) ||
			Event.current.button == 2;

		if(panning)
		{
			foreach(var node in nodes)
			{
				node.windowRect.position += Event.current.delta * panSpeed; 
			}

			// Use the current event unless its type is layout 
			if(Event.current.type != EventType.Layout)
			{
				Event.current.Use();
			}
		}
	}

	void DrawBGTexture()
	{
		if(bgTexture == null)
		{
			Color bgColor = new Color(0.3f, 0.3f, 0.3f);
			bgTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			bgTexture.SetPixel(0, 0, bgColor);
			bgTexture.Apply();
		}

		GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), bgTexture, ScaleMode.StretchToFill);
	}

	Rect GetHorizontalResizedWindow(Rect window, bool right)
	{
		// Resize the window depending on mouse input 
		float detectionRange = 5f;
		Rect windowResize = window;

		if(right)
		{
			windowResize.xMin = windowResize.xMax - detectionRange;
			windowResize.xMax += detectionRange;
		}
		else
		{
			windowResize.xMax = windowResize.xMin + detectionRange;
			windowResize.xMin -= detectionRange;
		}

		Event e = Event.current;
		EditorGUIUtility.AddCursorRect(windowResize, MouseCursor.ResizeHorizontal);

		// if mouse is no longer dragging, stop tracking direction of drag
		if(e.type == EventType.MouseUp)
		{
			settingsDraggingLeft = false;
			settingsDraggingRight = false;
		}

		bool mouseWithinResizor = e.mousePosition.x > windowResize.xMin && e.mousePosition.x < windowResize.xMax;

		// resize window if mouse is being dragged within resizor bounds
		if(mouseWithinResizor && e.type == EventType.MouseDrag && e.button == 0 || settingsDraggingLeft || settingsDraggingRight)
		{
			if(right == !settingsDraggingLeft)
			{
				window.width = e.mousePosition.x + e.delta.x;
				Repaint();
				settingsDraggingRight = true;
			}
			else if(!right == !settingsDraggingRight)
			{
				window.width = window.width - (e.mousePosition.x + e.delta.x);
				Repaint();
				settingsDraggingLeft = true;
			}
		}

		return window;
	}

	void BeginSettings()
	{
		EditorGUILayout.BeginHorizontal();
		GUI.color = Color.white;
		Rect wRect = editorWindow.position;
		Rect rect = new Rect(0f, 0f, wRect.width * settingsTabWidthRatio, wRect.height);
		// Resize the rect in horizontal 
		rect = GetHorizontalResizedWindow(rect, true);
		// Update settings width ratio
		settingsTabWidthRatio = Mathf.Clamp(rect.width / wRect.width, MinSettingsTabRatio, 1f); 
		// Begin drawing settings area 
		GUILayout.BeginArea(rect);
		GUI.Box(rect, "");
	}

	void EndSettings()
	{
		GUILayout.EndArea();
		EditorGUILayout.EndHorizontal();
	}

	void BeginZoomArea()
	{
		// Zoomable area begin 
		EditorZoomArea.Begin(zoom, ZoomArea);
	}

	void EndZoomArea()
	{
		// Zoomable area end 
		EditorZoomArea.End();
	}

	void HandleNodeLogic()
	{
		Event e = Event.current;
		mousePos = e.mousePosition;
		int selectedIndex = GetNodeIndexOnMousePos();
		bool clickedOnNode = selectedIndex > -1;

		// If right mouse was pressed and not currently making transition 
		if(e.button == 1 && e.type == EventType.MouseDown && !mouseDraggingTransitionCurve)
		{
			if(!clickedOnNode)
			{
				// If the clicked content is not window, show node creator menu
				ShowNodeCreatorMenu();
			}
			else
			{
				// If the clicked content is on window, show node options menu 
				ShowNodeOptionsMenu();
			}

			// Use the event 
			e.Use();
		}
		// If the left mouse was pressed and making transition 
		else if(e.button == 0 && e.type == EventType.MouseDown && mouseDraggingTransitionCurve)
		{
			BaseNode transitionEndNode = nodes.ElementAtOrDefault(selectedIndex);
			// If clicked on window and the current node is not the same as output node 
			if(clickedOnNode && !transitionEndNode.Equals(transitionStartNode))
			{
				Debug.Log("Make link"); 

				// Set input of output node to be current node 
				transitionEndNode.MakeLink(transitionStartNode, mousePos);
				// Reset 
				Reset();
			}

			// If the clicked content is not window, reset selected node and making transition
			if(!clickedOnNode)
			{
				Reset();
			}

			// Use the event 
			e.Use();
		}
		// If the left mouse was pressed and not making transition 
		else if(e.button == 0 && e.type == EventType.MouseDown)
		{
			lastClickedNode = nodes.ElementAtOrDefault(selectedIndex); 

			if(!mouseDraggingTransitionCurve)
			{
				// If clicked on window 
				if(clickedOnNode)
				{
					// Find the clicked input node on the output node 
					BaseNode outputNode = nodes[selectedIndex];
					BaseInputNode clickedInputNode = outputNode.GetInputNodeClickedOn(mousePos);

					if(clickedInputNode != null)
					{
						// Break transition from input to output node 
						transitionStartNode = clickedInputNode;
						// Continue transition from input node
						mouseDraggingTransitionCurve = true;
					}
				}
			}
		}

		if(mouseDraggingTransitionCurve && transitionStartNode != null)
		{
			Rect mouseRect = new Rect(e.mousePosition.x, e.mousePosition.y, 10f, 10f);
			// Draw transition curve when making transition 
			DrawTransitionCurve(transitionStartNode.windowRect, mouseRect);
			// Make GUI Repaint so that curve updates with mouse position 
			Repaint();
		}

		foreach(BaseNode node in nodes)
		{
			// Draw curves for each previous linked nodes 
			node.DrawCurves();
		}

		// Start drawing windows 
		BeginWindows();

		// Draw window for each node 
		for(int i = 0; i < nodes.Count; i++)
		{
			BaseNode node = nodes[i];

			PaintNode(node);
			PositionNode(node); 
			DrawNode(node, i);
		}

		// End drawing windows 
		EndWindows();
	}

	void PositionNode(BaseNode node)
	{
		// Update node width/height
		node.windowRect = new Rect(node.windowRect.x, node.windowRect.y, nodeWidth, nodeHeight);

		if(snapToGrid && !PossiblyPanning)
		{
			// Snap rect position 
			int snapX = ((int)node.windowRect.position.x / snapValue) * snapValue;
			int snapY = ((int)node.windowRect.position.y / snapValue) * snapValue;
			node.windowRect.position = new Vector2(snapX, snapY);
		}
	}

	void Reset()
	{
		mouseDraggingTransitionCurve = false;
		transitionStartNode = null;
	}

	void WindowFunction(int id)
	{
		BaseNode node = nodes.ElementAtOrDefault(id); 

		if(node)
		{
			// Write all window related text on the node window 
			node.DrawWindow();
			// Make windows draggable 
			GUI.DragWindow();
		}
	}

	void DrawGridLines(float gridSize, Color gridColor)
	{
		GL.Color(gridColor);
		for(float x = 0.0f; x < Screen.width; x += gridSize)
		{
			DrawLine(new Vector2(x, 0.0f), new Vector2(x, Screen.height));
		}

		GL.Color(gridColor);

		for(float y = 0.0f; y < Screen.height; y += gridSize)
		{
			DrawLine(new Vector2(0.0f, y), new Vector2(Screen.width, y));
		}
	}

	void DrawLine(Vector2 p1, Vector2 p2)
	{
		GL.Vertex(p1);
		GL.Vertex(p2);
	}

	void CreateLineMaterial()
	{
		if(!lineMaterial)
		{
			lineMaterial = new Material(Shader.Find("Lines/Colored Blended"));
			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
		}
	}

	void DrawMecanimGrid()
	{
		if(!lineMaterial)
		{
			CreateLineMaterial();
		}

		if(Event.current.type != EventType.Repaint)
		{
			return;
		}

		lineMaterial.SetPass(0);

		GL.PushMatrix();
		GL.Begin(GL.LINES);

		DrawGridLines(10.0f, gridMinorColorDark);
		DrawGridLines(50.0f, gridMajorColorDark);

		GL.End();
		GL.PopMatrix();
	}

	void HandleZoom()
	{
		// Allow adjusting the zoom with the mouse wheel as well. In this case, use the mouse coordinates
		// as the zoom center instead of the top left corner of the zoom area. This is achieved by
		// maintaining an origin that is used as offset when drawing any GUI elements in the zoom area.
		if(Event.current.type == EventType.ScrollWheel)
		{
			Vector2 screenCoordsMousePos = Event.current.mousePosition;
			Vector2 delta = Event.current.delta;
			Vector2 zoomCoordsMousePos = ConvertScreenCoordsToZoomCoords(screenCoordsMousePos);
			float zoomDelta = -delta.y / 150.0f;
			float oldZoom = zoom;
			zoom += zoomDelta;
			zoom = Mathf.Clamp(zoom, ZoomMin, ZoomMax);
			zoomCoordsOrigin += (zoomCoordsMousePos - zoomCoordsOrigin) - (oldZoom / zoom) * (zoomCoordsMousePos - zoomCoordsOrigin);

			Event.current.Use();
		}
	}

	Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
	{
		return (screenCoords - ZoomArea.TopLeft()) / zoom + zoomCoordsOrigin;
	}
}
