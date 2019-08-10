using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; 

public class OutputNode : BaseNode
{
	string result;
	BaseInputNode inputNode;
	Rect inputNodeRect;

	public override string WindowTitle => "Output Node"; 

	public override void DrawWindow()
	{
		base.DrawWindow();

		Event e = Event.current;
		DrawLabels(e); 
	}

	public override void DrawCurves()
	{
		if(inputNode)
		{
			Rect rect = GetTransitionEndRect(inputNodeRect, windowRect); 
			NodeEditor.DrawTransitionCurve(inputNode.windowRect, rect);
		}
	}

	public override void OnNodeDeleted(BaseNode node)
	{
		if(node.Equals(inputNode))
		{
			inputNode = null; 
		}
	}

	public override BaseInputNode GetInputNodeClickedOn(Vector2 clickPos)
	{
		BaseInputNode clickedNode = null;
		clickPos = AdjustClickPos(clickPos); 

		if(inputNodeRect.Contains(clickPos))
		{
			clickedNode = inputNode;
			inputNode = null; 
		}

		return clickedNode; 
	}

	public override void MakeLink(BaseNode node, Vector2 clickPos)
	{
		clickPos = AdjustClickPos(clickPos); 

		if(inputNodeRect.Contains(clickPos) && node is BaseInputNode)
		{
			this.inputNode = node as BaseInputNode; 
		}
	}

	void DrawLabels(Event e)
	{
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
}
