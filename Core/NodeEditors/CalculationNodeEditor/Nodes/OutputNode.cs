using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; 

public class OutputNode : BaseNode
{
	BaseInputNode inputNode;
	Rect inputAcceptRect;

	public override string WindowTitle => "Output Node"; 

	public override void DrawWindow()
	{
		base.DrawWindow();

		Event e = Event.current;

		GUILayout.Label("");
		GUILayout.Label("");
		GUILayout.Label("");
		GUILayout.Label("");



		DrawLabels(e); 
	}

	public override void DrawCurves()
	{
		if(inputNode)
		{
			Rect rect = ToWindowRect(inputAcceptRect);
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

	public override BaseNode GetNodeOnPosition(Vector2 clickPos)
	{
		BaseInputNode clickedNode = null;
		clickPos = AdjustClickPos(clickPos); 

		if(inputAcceptRect.Contains(clickPos))
		{
			clickedNode = inputNode;
			inputNode = null; 
		}

		return clickedNode; 
	}

	public override void MakeLink(BaseNode node, Vector2 clickPos)
	{
		clickPos = AdjustClickPos(clickPos); 

		if(inputAcceptRect.Contains(clickPos) && node is BaseInputNode)
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
			inputAcceptRect = GUILayoutUtility.GetLastRect();
		}

		GUILayout.Label("Result");
	}
}
