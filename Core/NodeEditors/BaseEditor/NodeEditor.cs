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
	enum ResizeEdge { right, left, top, down }

	public virtual bool AllowDrawSettings => true;
	public Rect FullScreen => new Rect(0f, 0f, Screen.width, Screen.height);
	public static List<Node> nodes = new List<Node>();
	public static EditorWindow editorWindow;
	public Node mouseHoveredNode;
	public Node lastClickedNode;
	public Node curveStartNode;
	public bool mouseDraggingCurve;
	public float zoom = 1.0f;
	public float settingsTabWidthRatio = 0.2f;

	bool ResizeAny => resizeDownEdge || resizeTopEdge || resizeLeftEdge || resizeRightEdge;
	Vector2 WindowDelta => Event.current.delta * GUIDragConstant;
	static Dictionary<string, string> classShortcuts = new Dictionary<string, string>();
	static Vector2 mousePos;
	readonly Color gridMinorColorDark = new Color(0f, 0f, 0f, 0.18f);
	readonly Color gridMajorColorDark = new Color(0f, 0f, 0f, 0.28f);
	Texture2D backgroundTexture;
	Vector2 zoomCoordsOrigin = Vector2.zero;
	Material lineMaterial;
	bool drawSettings;
	bool settingsResizeRightEdge;
	bool resizeTopEdge;
	bool resizeDownEdge;
	bool resizeRightEdge;
	bool resizeLeftEdge;
	bool mouseDrag;
	bool pan;

	public const string DeleteNodeData = "deleteNode";
	const float CurveWidth = 0.2f;
	const float ResizeAreaWidth = 5f;
	const float PanSpeed = 5f;
	const float DefaultNodeWidth = 100f;
	const float DefaultNodeHeight = 100f;
	const float MinSettingsTabRatio = 0.2f;
	const float ZoomMin = 0.1f;
	const float ZoomMax = 10.0f;
	const float GUIDragConstant = 0.5f;

	public virtual void OnGUI()
	{
		GetWindow();

		HandleZoom();
		HandlePan();
		HandleMouseDrag();
		HandleKeyboardInput();

		DrawBGTexture();
		DrawMecanimGrid();

		BeginZoomArea();
		HandleNodeDrawLogic();
		EndZoomArea();

		HandleSettingsTab();
		HandleResetCursor();
	}

	public static void DrawCurve(Vector2 start, Vector2 end, Color color)
	{
		Vector2 startTan = start + Vector2.right * 50f;
		Vector2 endTan = end + Vector2.left * 50f;

		// Draw multiple bezier with different width for increased visual quality 
		for(int i = 0; i < CurveWidth * 10; i++)
		{
			float width = (i + 1) * 2f;
			// Actual Transition Drawing function on the Editor 
			Handles.DrawBezier(start, end, startTan, endTan, color, null, width);

			// Draw Arrow  
			float angle = Vector3.SignedAngle(Vector3.right, end - start, Vector3.right);

			if((end - start).y > 0f)
			{
				angle = -angle;
			}

			Quaternion rot = Quaternion.Euler(0, -90, 0) * Quaternion.AngleAxis(angle, Vector3.left);
			Vector3 mid = (start + end) / 2f;
			mid.z = -100f;

			Handles.ConeHandleCap(0, mid, rot, 25f, EventType.Repaint);
		}
	}

	public static void DrawCurve(Rect start, Rect end, Color color)
	{
		Vector2 startPos = new Vector2(start.x + start.width, start.y + start.height / 2f);
		Vector2 endPos = new Vector2(end.x + end.width, end.y + end.height / 2f);

		DrawCurve(startPos, endPos, color);
	}

	public static void ViewAtCenter(Node node)
	{
		Vector2 delta = node.windowRect.Center() - editorWindow.position.Center();
		nodes.ForEach(n => n.windowRect.position -= delta);
	}

	public virtual void GetWindow()
	{
		// Example override method:
		// if(!editorWindow) { editorWindow = GetWindow<YourWindow>(); }
	}

	public virtual void DrawSettings()
	{
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel, GUILayout.Width(75f));

		if(GUILayout.Toggle(false, "Hide", "Button", GUILayout.Width(50f)))
		{
			drawSettings = false;
		}
	}

	void DrawNode(Node node, int id)
	{
		node.windowRect = GUILayout.Window(id, node.windowRect, WindowFunction, node.WindowTitle);
	}

	public virtual void PaintNode(Node node)
	{
		GUI.color = node.windowColor;
	}

	public virtual void ShowNodeCreatorMenu()
	{
	}

	public virtual void ShowNodeOptionsMenu()
	{
		// Show Node options on the menu 
		GenericMenu menu = new GenericMenu();

		menu.AddItem(new GUIContent("Delete Node"), false, ContextCallback, DeleteNodeData);

		menu.ShowAsContext();
	}

	public virtual void ContextCallback(object obj)
	{
		string callback = obj.ToString();
		mouseHoveredNode = GetMouseHoveredNode();

		if(callback.Equals(DeleteNodeData))
		{
			if(mouseHoveredNode != null)
			{
				DeleteNodeWithMessage(mouseHoveredNode);
			}
		}
	}

	public static T CreateNodeInstance<T>(Rect rect) where T : Node
	{
		T t = (T)CreateInstance(typeof(T));
		t.windowRect = rect;
		t.SetShortcuts(classShortcuts);
		nodes.Add(t);
		return t;
	}

	public static T CreateNodeInstance<T>() where T : Node
	{
		Rect rect = new Rect(mousePos.x, mousePos.y, DefaultNodeWidth, DefaultNodeHeight);
		return CreateNodeInstance<T>(rect);
	}

	public static void DeleteNode(Node node)
	{
		// Does not remove references from ScriptableNodes
		if(nodes.Contains(node))
		{
			node.OnBeforeSelfDeleted();
			nodes.Remove(node);
		}
	}

	public static void DeleteNodeWithMessage(Node node)
	{
		// Removes references from ScriptableNodes  
		if(nodes.Contains(node))
		{
			node.OnBeforeSelfDeleted();
			nodes.Remove(node);
			nodes.ForEach(n => n.OnNodeDeleted(node));
		}
	}

	public static void CreateShortcut(string shortcut, string className)
	{
		// Add new shrotcut to the dictionary
		classShortcuts.Add(shortcut, className);
		// Update shortcuts on each node 
		nodes.ForEach(node => node.SetShortcuts(classShortcuts));
	}

	public static void RemoveShortcut(string shortcut)
	{
		// Remove from shortcuts 
		classShortcuts.Remove(shortcut);
		// Update shortcuts on each node 
		nodes.ForEach(node => node.SetShortcuts(classShortcuts));
	}

	public static void ResetShortcuts()
	{
		// Reset dictionary 
		classShortcuts = new Dictionary<string, string>();
		// Update on each ndoe 
		nodes.ForEach(node => node.SetShortcuts(classShortcuts));
	}

	public virtual void WindowFunction(int id)
	{
		Node node = nodes.ElementAtOrDefault(id);

		if(node)
		{
			node.OnDrawWindow();
			DragWindow(node); 
		}
	}

	public virtual void DrawCurves(Node node)
	{
		node.OnDrawCurves();
	}

	public Node GetMouseHoveredNode()
	{
		return nodes.FirstOrDefault(node => node.windowRect.Contains(mousePos));
	}

	public void DragWindow(Node node)
	{
		bool draggable = !ResizeAny && !node.lockPosition;

		if(draggable)
		{
			GUI.DragWindow();
		}
	}

	void ProcessWindows()
	{
		// Draw window for each node 
		for(int i = 0; i < nodes.Count; i++)
		{
			Node node = nodes[i];

			if(!node.isHidden)
			{
				PaintNode(node);
				DrawNode(node, i);
				DrawCurves(node);
			}
		}
	}

	void HandleSettingsTab()
	{
		if(AllowDrawSettings)
		{
			if(drawSettings)
			{
				BeginSettings();
				DrawSettings();
				EndSettings();
			}
			else
			{
				if(GUILayout.Toggle(false, "Show Settings", "Button", GUILayout.Width(150f)))
				{
					drawSettings = true;
				}
			}
		}
	}

	void HandleResetCursor()
	{
		if(!pan && !resizeLeftEdge && !resizeRightEdge && !resizeTopEdge && !resizeDownEdge)
		{
			ResetCursor();
		}
	}

	void ResetCursor()
	{
		EditorGUIUtility.AddCursorRect(FullScreen, MouseCursor.Arrow);
		Repaint();
	}

	void HandleMouseDrag()
	{
		Event e = Event.current;

		if(e.type == EventType.MouseDrag && Event.current.button == 0)
		{
			mouseDrag = true;
		}
		else if(e.type == EventType.MouseUp)
		{
			mouseDrag = false;
		}
	}

	void HandleKeyboardInput()
	{
		Event e = Event.current;

		// Delete last clicked node on Delete key down 
		if(e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
		{
			DeleteNodeWithMessage(lastClickedNode);
			// Use the event 
			e.Use();
		}
	}

	void HandlePan()
	{
		Event e = Event.current;

		// If already panning 
		if(!pan)
		{
			if(e.type == EventType.MouseDrag &&
				(e.button == 0 && e.modifiers == EventModifiers.Alt) ||
				e.button == 2)
			{
				pan = true;
			}
		}
		// If not panning 
		else if(e.type == EventType.MouseUp)
		{
			pan = false;
		}

		if(pan)
		{
			EditorGUIUtility.AddCursorRect(FullScreen, MouseCursor.Pan);

			nodes.ForEach(node => node.windowRect.position += e.delta * PanSpeed);

			if(e.Usable())
			{
				e.Use();
			}
		}
	}

	void DrawBGTexture()
	{
		if(backgroundTexture == null)
		{
			Color bgColor = new Color(0.3f, 0.3f, 0.3f);
			backgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			backgroundTexture.SetPixel(0, 0, bgColor);
			backgroundTexture.Apply();
		}

		Rect rect = new Rect(0, 0, maxSize.x, maxSize.y);
		GUI.DrawTexture(rect, backgroundTexture, ScaleMode.StretchToFill);
	}

	Rect GetResizedWindow(Rect window, ResizeEdge res, ref bool resizeTracker)
	{
		// PS: yMin is the top of the rect, yMax is bottom (Reversed) 
		Event e = Event.current;
		Rect resizeArea = window;

		if(res == ResizeEdge.right)
		{
			resizeArea.xMin = resizeArea.xMax - ResizeAreaWidth;
			resizeArea.xMax = resizeArea.xMax + ResizeAreaWidth;
			EditorGUIUtility.AddCursorRect(resizeArea, MouseCursor.ResizeHorizontal);

		}
		else if(res == ResizeEdge.left)
		{
			resizeArea.xMax = resizeArea.xMin + ResizeAreaWidth;
			resizeArea.xMin = resizeArea.xMin - ResizeAreaWidth;
			EditorGUIUtility.AddCursorRect(resizeArea, MouseCursor.ResizeHorizontal);
		}
		else if(res == ResizeEdge.top)
		{
			resizeArea.yMin = resizeArea.yMin - ResizeAreaWidth;
			resizeArea.yMax = resizeArea.yMin + ResizeAreaWidth;
			EditorGUIUtility.AddCursorRect(resizeArea, MouseCursor.ResizeVertical);
		}
		else if(res == ResizeEdge.down)
		{
			resizeArea.yMin = resizeArea.yMax - ResizeAreaWidth;
			resizeArea.yMax = resizeArea.yMax + ResizeAreaWidth;
			EditorGUIUtility.AddCursorRect(resizeArea, MouseCursor.ResizeVertical);
		}

		if(e.type == EventType.MouseUp)
		{
			resizeTracker = false;
		}

		bool mouseWithinResizeArea = false;

		if(res == ResizeEdge.left || res == ResizeEdge.right)
		{
			mouseWithinResizeArea = e.mousePosition.x > resizeArea.xMin && e.mousePosition.x < resizeArea.xMax;
		}
		else
		{
			mouseWithinResizeArea = e.mousePosition.y > resizeArea.yMin && e.mousePosition.y < resizeArea.yMax;
		}

		// Resize window if mouse is being dragged within resizor bounds
		if((mouseWithinResizeArea && mouseDrag) || resizeTracker)
		{
			if(res == ResizeEdge.right)
			{
				window.xMax = e.mousePosition.x;
			}
			else if(res == ResizeEdge.left)
			{
				window.xMin = e.mousePosition.x;
			}
			else if(res == ResizeEdge.top)
			{
				window.yMin = e.mousePosition.y;
			}
			else if(res == ResizeEdge.down)
			{
				window.yMax = e.mousePosition.y;
			}

			Repaint();
			resizeTracker = true;

			// Use Event on window drag
			if(e.Usable())
			{
				e.Use();
			}
		}

		return window;
	}

	void BeginSettings()
	{
		EditorGUILayout.BeginHorizontal();
		GUI.color = Color.white;
		Rect windowRect = editorWindow.position;
		Rect rect = new Rect(0f, 0f, windowRect.width * settingsTabWidthRatio, windowRect.height);
		// Resize the rect in horizontal 
		rect = GetResizedWindow(rect, ResizeEdge.right, ref settingsResizeRightEdge);
		// Update settings width ratio
		settingsTabWidthRatio = Mathf.Clamp(rect.width / windowRect.width, MinSettingsTabRatio, 1f);
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
		if(AllowDrawSettings && drawSettings)
		{
			// If settings tab is being drawn, prevent overlap
			Rect windowRect = editorWindow.position;
			Rect rect = new Rect(windowRect.width * settingsTabWidthRatio, 0f, FullScreen.width, FullScreen.height);
			EditorZoomArea.Begin(zoom, rect);
		}
		else
		{
			// Make full screen zoom area
			EditorZoomArea.Begin(zoom, FullScreen);
		}
	}

	void EndZoomArea()
	{
		EditorZoomArea.End();
	}

	void HandleNodeDrawLogic()
	{
		Event e = Event.current;
		mousePos = e.mousePosition;
		mouseHoveredNode = GetMouseHoveredNode();

		// If right mouse was pressed and not currently making transition 
		if(e.button == 1 && e.type == EventType.MouseDown && !mouseDraggingCurve)
		{
			if(mouseHoveredNode == null)
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
		else if(e.button == 0 && e.type == EventType.MouseDown)
		{
			if(mouseHoveredNode != null)
			{
				// Update last clicked node 
				lastClickedNode = mouseHoveredNode;
			}

			if(!mouseDraggingCurve)
			{
				if(mouseHoveredNode != null)
				{
					Node startNode = mouseHoveredNode.GetNodeOnPosition(mousePos);

					if(startNode != null)
					{
						// Break transition from input to output node 
						curveStartNode = startNode;
						// Continue transition from input node
						mouseDraggingCurve = true;
					}
				}
			}

			if(mouseDraggingCurve)
			{
				// If clicked on window and the current node is not the same as output node 
				if(mouseHoveredNode != null && !mouseHoveredNode.Equals(curveStartNode))
				{
					// Set input of output node to be current node 
					mouseHoveredNode.MakeLink(curveStartNode, mousePos);
					// Reset 
					ResetCurve();
				}

				// If the clicked content is not window, reset selected node and making transition
				if(mouseHoveredNode == null)
				{
					ResetCurve();
				}

				// Use the event 
				e.Use();
			}
		}

		if(mouseDraggingCurve && curveStartNode != null)
		{
			Rect mouseRect = new Rect(e.mousePosition.x, e.mousePosition.y, 10f, 10f);
			// Draw transition curve when making transition 
			DrawCurve(curveStartNode.windowRect, mouseRect, Color.white);
			// Make GUI Repaint so that curve updates with mouse position 
			Repaint();
		}


		if(lastClickedNode != null)
		{
			// Record changes done to this object 
			Undo.RecordObject(lastClickedNode, "LastClickedNodeModification");

			if(!lastClickedNode.lockPosition)
			{
				// Snap, Resize last clicked node 
				PositionNode(lastClickedNode);
			}

			// If mouse is dragging 
			if(mouseDrag && !lastClickedNode.lockPosition)
			{
				if(lastClickedNode.groupDrag && lastClickedNode == mouseHoveredNode)
				{
					// Group drag 
					lastClickedNode.GroupDrag(WindowDelta);
				}
				else
				{
					// Move hidden nodes to protect their layout 
					lastClickedNode.DragHiddenNodes(WindowDelta);
				}
			}
		}

		// Start drawing windows 
		BeginWindows();

		ProcessWindows();

		// End drawing windows 
		EndWindows();
	}

	void PositionNode(Node node)
	{
		// Check for resizing 
		if(node.allowResizeHorizontal)
		{
			if(!resizeLeftEdge && !resizeTopEdge && !resizeDownEdge)
			{
				node.windowRect = GetResizedWindow(node.windowRect, ResizeEdge.right, ref resizeRightEdge);
			}
			if(!resizeRightEdge && !resizeTopEdge && !resizeDownEdge)
			{
				node.windowRect = GetResizedWindow(node.windowRect, ResizeEdge.left, ref resizeLeftEdge);
			}
		}

		if(node.allowResizeVertical)
		{
			if(!resizeDownEdge && !resizeLeftEdge && !resizeRightEdge)
			{
				node.windowRect = GetResizedWindow(node.windowRect, ResizeEdge.top, ref resizeTopEdge);
			}
			if(!resizeTopEdge && !resizeLeftEdge && !resizeRightEdge)
			{
				node.windowRect = GetResizedWindow(node.windowRect, ResizeEdge.down, ref resizeDownEdge);
			}
		}

		// Check for snapping 
		if(node.snap)
		{
			node.windowRect = SnapPosition(node.windowRect, node.snapValue);
		}
	}

	void ResetCurve()
	{
		mouseDraggingCurve = false;
		curveStartNode = null;
	}

	Rect SnapPosition(Rect rect, int snapVal)
	{
		if(!pan)
		{
			// Snap rect position 
			rect.xMin = Snap(rect.xMin, snapVal);
			rect.xMax = Snap(rect.xMax, snapVal);
			rect.yMin = Snap(rect.yMin, snapVal);
			rect.yMax = Snap(rect.yMax, snapVal);
		}

		return rect;
	}

	int Snap(float value, int snap)
	{
		if(snap > 0)
		{
			return ((int)value / snap) * snap;
		}

		return (int)value;
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
			Shader shader = Shader.Find("Lines/Colored Blended");
			lineMaterial = new Material(shader);
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

		// Don't draw when repainting 
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
		Event e = Event.current;
		// Allow adjusting the zoom with the mouse wheel as well. In this case, use the mouse coordinates
		// as the zoom center instead of the top left corner of the zoom area. This is achieved by
		// maintaining an origin that is used as offset when drawing any GUI elements in the zoom area.
		if(e.type == EventType.ScrollWheel)
		{
			Vector2 screenCoordsMousePos = Event.current.mousePosition;
			Vector2 delta = Event.current.delta;
			Vector2 zoomCoordsMousePos = ScreenCoordsToZoomCoords(screenCoordsMousePos, FullScreen);
			float zoomDelta = -delta.y / 150.0f;
			float oldZoom = zoom;
			zoom += zoomDelta;
			zoom = Mathf.Clamp(zoom, ZoomMin, ZoomMax);
			zoomCoordsOrigin += (zoomCoordsMousePos - zoomCoordsOrigin) - (oldZoom / zoom) * (zoomCoordsMousePos - zoomCoordsOrigin);

			// Use current event
			e.Use();
		}
	}

	Vector2 ScreenCoordsToZoomCoords(Vector2 screenCoords, Rect zoomArea)
	{
		return (screenCoords - zoomArea.TopLeft()) / zoom + zoomCoordsOrigin;
	}
}
