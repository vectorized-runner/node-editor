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

	public virtual bool DrawSettingsTab => false; 
	public Rect FullScreen => new Rect(0f, 0f, Screen.width, Screen.height);
	public static List<BaseNode> nodes = new List<BaseNode>();
	public static EditorWindow editorWindow;
	public BaseNode mouseHoveredNode;
	public BaseNode lastClickedNode;
	public BaseNode transitionStartNode;
	public bool mouseDraggingTransitionCurve;
	public float zoom = 1.0f;
	public float settingsTabWidthRatio = 0.2f;

	bool PossiblyPanning => Event.current.modifiers == EventModifiers.Alt || Event.current.keyCode == KeyCode.LeftAlt;
	bool ResizeAny => resizeDownEdge || resizeTopEdge || resizeLeftEdge || resizeRightEdge; 
	static Color handlesColor = Color.black;
	static Vector2 mousePos;
	readonly Color gridMinorColorDark = new Color(0f, 0f, 0f, 0.18f);
	readonly Color gridMajorColorDark = new Color(0f, 0f, 0f, 0.28f);
	Texture2D bgTexture;
	Vector2 zoomCoordsOrigin = Vector2.zero;
	Material lineMaterial;
	bool settingsResizeRightEdge;
	bool resizeTopEdge; 
	bool resizeDownEdge; 
	bool resizeRightEdge;
	bool resizeLeftEdge;
	bool mouseDrag;

	public const string DeleteNodeData = "deleteNode";
	const float ResizeAreaWidth = 5f;
	const float PanSpeed = 5f;
	const float NodeWidth = 100f;
	const float NodeHeight = 100f;
	const float TransitionWidth = 0.1f;
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
		HandleEditorLogic();
		EndZoomArea();

		if(DrawSettingsTab)
		{
			BeginSettings();
			DrawSettings();
			EndSettings();
		}

		if(!PossiblyPanning && !resizeLeftEdge && !resizeRightEdge && !resizeTopEdge && !resizeDownEdge)
		{
			ResetCursor(); 
		}
	}

	public static void DrawTransitionCurve(Vector2 start, Vector2 end)
	{
		Vector2 startTan = start + Vector2.right * 50f;
		Vector2 endTan = end + Vector2.left * 50f;

		// Draw multiple bezier with different width for increased visual quality 
		for(int i = 0; i < TransitionWidth * 10; i++)
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
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Transition Tint", GUILayout.Width(100f), GUILayout.Height(20f));
		handlesColor = EditorGUILayout.ColorField(handlesColor, GUILayout.Width(50f), GUILayout.Height(20f));
		EditorGUILayout.EndHorizontal();
	}

	public virtual void DrawNode(BaseNode node, int id)
	{
		// Actual Window Drawing function on the Editor 
		node.windowRect = GUILayout.Window(id, node.windowRect, WindowFunction, node.WindowTitle);
	}

	public virtual void PaintNode(BaseNode node)
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
				DeleteNodeWithMsg(mouseHoveredNode);
			}
		}
	}

	public static T CreateNodeInstance<T>(Rect rect) where T : BaseNode
	{
		T t = (T)CreateInstance(typeof(T));
		t.windowRect = rect;
		nodes.Add(t);
		return t;
	}

	public static T CreateNodeInstance<T>() where T : BaseNode
	{
		// Create on mousePos by default 
		Rect rect = new Rect(mousePos.x, mousePos.y, NodeWidth, NodeHeight);
		return CreateNodeInstance<T>(rect);
	}

	public static void DeleteNode(BaseNode node)
	{
		// Does not remove references from ScriptableNodes
		if(nodes.Contains(node))
		{
			node.OnBeforeSelfDeleted();
			nodes.Remove(node);
		}
	}

	public static void DeleteNodeWithMsg(BaseNode node)
	{
		// Removes references from ScriptableNodes  
		if(nodes.Contains(node))
		{
			node.OnBeforeSelfDeleted();
			nodes.Remove(node);
			nodes.ForEach(n => n.OnNodeDeleted(node));
		}
	}

	public BaseNode GetMouseHoveredNode()
	{
		return nodes.FirstOrDefault(node => node.windowRect.Contains(mousePos));
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
			DeleteNodeWithMsg(lastClickedNode);
			// Use the event 
			e.Use();
		}
	}

	void HandlePan()
	{
		Event e = Event.current; 

		if(PossiblyPanning)
		{
			EditorGUIUtility.AddCursorRect(FullScreen, MouseCursor.Pan);
		}

		bool panning = e.type == EventType.MouseDrag &&
			(e.button == 0 && e.modifiers == EventModifiers.Alt) ||
			e.button == 2;

		if(panning)
		{
			foreach(var node in nodes)
			{
				node.windowRect.position += e.delta * PanSpeed;
			}

			// Use the current event unless its type is layout 
			if(e.type != EventType.Layout)
			{
				e.Use();
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

		Rect rect = new Rect(0, 0, maxSize.x, maxSize.y);
		GUI.DrawTexture(rect, bgTexture, ScaleMode.StretchToFill);
	}

	Rect GetResizedWindow(Rect window, ResizeEdge res, ref bool resizeTracker)
	{
		// PS: yMin is the top of the rect, yMax is bottom (Reversed) 
		Event e = Event.current;
		// Resize the window depending on mouse input 
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

		// if mouse is no longer dragging, stop tracking direction of drag
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
		rect = GetResizedWindow(rect, ResizeEdge.right, ref settingsResizeRightEdge);
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
		EditorZoomArea.Begin(zoom, FullScreen);
	}

	void EndZoomArea()
	{
		// Zoomable area end 
		EditorZoomArea.End();
	}

	void HandleEditorLogic()
	{
		Event e = Event.current;
		mousePos = e.mousePosition;
		mouseHoveredNode = GetMouseHoveredNode();

		// If right mouse was pressed and not currently making transition 
		if(e.button == 1 && e.type == EventType.MouseDown && !mouseDraggingTransitionCurve)
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

			if(!mouseDraggingTransitionCurve)
			{
				if(mouseHoveredNode != null)
				{
					BaseNode startNode = mouseHoveredNode.GetNodeOnPosition(mousePos);

					if(startNode != null)
					{
						// Break transition from input to output node 
						transitionStartNode = startNode;
						// Continue transition from input node
						mouseDraggingTransitionCurve = true;
					}
				}
			}

			if(mouseDraggingTransitionCurve)
			{
				// If clicked on window and the current node is not the same as output node 
				if(mouseHoveredNode != null && !mouseHoveredNode.Equals(transitionStartNode))
				{
					// Set input of output node to be current node 
					mouseHoveredNode.MakeLink(transitionStartNode, mousePos);
					// Reset 
					ResetTransition();
				}

				// If the clicked content is not window, reset selected node and making transition
				if(mouseHoveredNode == null)
				{
					ResetTransition();
				}

				// Use the event 
				e.Use();
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

		if(lastClickedNode != null)
		{
			// Record changes done to this object 
			Undo.RecordObject(lastClickedNode, "LastClickedNodeModification"); 

			if(!lastClickedNode.lockPosition)
			{
				// Snap, Resize last clicked node 
				PositionNode(lastClickedNode);
			}

			if(mouseDrag && lastClickedNode.groupDrag && !lastClickedNode.lockPosition)
			{
				lastClickedNode.GroupDrag(e.delta * GUIDragConstant);
			}
		}

		// Start drawing windows 
		BeginWindows();

		// Draw window for each node 
		for(int i = 0; i < nodes.Count; i++)
		{
			BaseNode node = nodes[i];

			if(!node.hidden)
			{
				PaintNode(node);
				DrawNode(node, i);
			}
		}

		// End drawing windows 
		EndWindows();
	}

	void PositionNode(BaseNode node)
	{
		if(node.allowResize)
		{
			if(!resizeLeftEdge && !resizeTopEdge && !resizeDownEdge)
			{
				node.windowRect = GetResizedWindow(node.windowRect, ResizeEdge.right, ref resizeRightEdge);
			}
			if(!resizeRightEdge && !resizeTopEdge && !resizeDownEdge)
			{
				node.windowRect = GetResizedWindow(node.windowRect, ResizeEdge.left, ref resizeLeftEdge);
			}
			if(!resizeDownEdge && !resizeLeftEdge && !resizeRightEdge)
			{
				node.windowRect = GetResizedWindow(node.windowRect, ResizeEdge.top, ref resizeTopEdge);
			}
			if(!resizeTopEdge && !resizeLeftEdge && !resizeRightEdge)
			{
				node.windowRect = GetResizedWindow(node.windowRect, ResizeEdge.down, ref resizeDownEdge);
			}
		}

		if(node.snap)
		{
			node.windowRect = SnapPosition(node.windowRect, node.snapValue);
		}
	}

	void ResetTransition()
	{
		mouseDraggingTransitionCurve = false;
		transitionStartNode = null;
	}

	Rect SnapPosition(Rect rect, int snapVal)
	{
		if(!PossiblyPanning)
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

	void WindowFunction(int id)
	{
		// Write on the window using this function 
		BaseNode node = nodes.ElementAtOrDefault(id);

		if(node)
		{
			node.DrawWindow();

			if(!ResizeAny && !nodes[id].lockPosition)
			{
				GUI.DragWindow();
			}
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

		// Only when repainting
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
