using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; 

public class OutputNode : BaseNode
{
	string result;
	BaseInputNode inputNode;
	Rect inputNodeRect;
	
	public OutputNode()
	{
		windowTitle = "Output Node";
		hasInputs = true; 
	}

	public override void DrawWindow()
	{
		base.DrawWindow();

		Event e = Event.current;
		string inputTitle = "None"; 

		if(inputNode)
		{
			inputTitle = inputNode.GetResult(); 
		}

		GUILayout.Label("Input: " + inputTitle); 

		if(e.type == EventType.Repaint)
		{
			inputNodeRect = GUILayoutUtility.GetLastRect(); 
		}

		GUILayout.Label("Result" + result);		
	}

	public override void DrawCurves()
	{
		if(inputNode)
		{
			Rect rect = windowRect;
			rect.x += inputNodeRect.x;
			rect.y += inputNodeRect.y + inputNodeRect.height / 2f;
			rect.width = 1f;
			rect.height = 1f;

			NodeEditor.DrawNodeCurve(inputNode.windowRect, rect);
		}
	}

	public override void NodeDeleted(BaseNode node)
	{
		if(node.Equals(inputNode))
		{
			inputNode = null; 
		}
	}

	public override BaseInputNode ClickedOnInput(Vector2 clickPos)
	{
		BaseInputNode returnValue = null;

		clickPos.x -= windowRect.x;
		clickPos.y -= windowRect.y;

		if(inputNodeRect.Contains(clickPos))
		{
			returnValue = inputNode;
			inputNode = null; 
		}

		return returnValue; 
	}

	public override void SetInput(BaseInputNode inputNode, Vector2 clickPos)
	{
		clickPos.x -= windowRect.x;
		clickPos.y -= windowRect.y; 

		if(inputNodeRect.Contains(clickPos))
		{
			this.inputNode = inputNode; 
		}

	}
}
