using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OperationNode : BaseInputNode
{
	protected BaseInputNode input1;
	protected BaseInputNode input2;
	protected Rect input1Rect;
	protected Rect input2Rect;

	public override void DrawWindow()
	{
		base.DrawWindow();
		DrawInputLabels();
	}

	public override void OnNodeDeleted(BaseNode node)
	{
		if(node.Equals(input1))
		{
			input1 = null;
		}
		if(node.Equals(input2))
		{
			input2 = null;
		}
	}

	public override BaseNode GetNodeOnPosition(Vector2 clickPos)
	{
		BaseInputNode clickedNode = null;
		clickPos = AdjustClickPos(clickPos);

		if(input1Rect.Contains(clickPos))
		{
			clickedNode = input1;
			input1 = null;
		}
		else if(input1Rect.Contains(clickPos))
		{
			clickedNode = input2;
			input2 = null;
		}

		return clickedNode;
	}

	public override void DrawCurves()
	{
		if(input1)
		{
			Rect endRect = ToWindowRect(input1Rect);
			NodeEditor.DrawTransitionCurve(input1.windowRect, endRect);
		}

		if(input2)
		{
			Rect endRect = ToWindowRect(input2Rect);
			NodeEditor.DrawTransitionCurve(input2.windowRect, endRect);
		}
	}

	public override void MakeLink(BaseNode node, Vector2 clickPos)
	{
		if(node is BaseInputNode)
		{
			clickPos = AdjustClickPos(clickPos);

			if(input1Rect.Contains(clickPos))
			{
				input1 = node as BaseInputNode;
			}
			else if(input2Rect.Contains(clickPos))
			{
				input2 = node as BaseInputNode;
			}
		}
	}

	void DrawInputLabels()
	{
		string input1Title = "None";
		string input2Title = "None";

		if(input1)
		{
			input1Title = input1.GetResult();
		}
		if(input2)
		{
			input2Title = input2.GetResult();
		}

		GUILayout.Label("Input 1: " + input1Title);

		if(Event.current.type == EventType.Repaint)
		{
			// Get new created Input 1 Label 
			input1Rect = GUILayoutUtility.GetLastRect();
		}

		GUILayout.Label("Input 2: " + input2Title);

		if(Event.current.type == EventType.Repaint)
		{
			// Get new created Input 2 Label 
			input2Rect = GUILayoutUtility.GetLastRect();
		}

		Debug.Log(GUILayoutUtility.GetLastRect()); 
	}
}
