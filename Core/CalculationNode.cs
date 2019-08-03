using System.Collections;
using System.Collections.Generic;
using UnityEngine;
<<<<<<< HEAD
using UnityEditor; 

public class CalculationNode : BaseInputNode
{
	public enum CalculationType
	{
		Addition,
		Substraction, 
		Division, 
		Multiplication
	}

	CalculationType calculationType;  
	BaseInputNode input1;
	BaseInputNode input2;
	Rect input1Rect;
	Rect input2Rect; 

	public CalculationNode()
	{
		windowTitle = "Calculation Node";
		hasInputs = true;
	}

	public override void DrawWindow()
	{
		base.DrawWindow();

		Event e = Event.current;
		calculationType = (CalculationType)EditorGUILayout.EnumPopup("Calculation Type", calculationType);

		string input1Title = "None"; 

		if(input1)
		{
			input1Title = input1.GetResult(); 
		}

		GUILayout.Label("Input 1: " + input1Title); 

		if(e.type == EventType.Repaint)
		{
			input1Rect = GUILayoutUtility.GetLastRect();
		}

		string input2Title = "None"; 

		if(input2)
		{
			input2Title = input2.GetResult(); 
		}

		GUILayout.Label("Input 2: " + input2Title); 

		if(e.type == EventType.Repaint)
		{
			input2Rect = GUILayoutUtility.GetLastRect(); 
		}
	}

	public override void SetInput(BaseInputNode inputNode, Vector2 clickPos)
	{
		clickPos.x -= windowRect.x;
		clickPos.y -= windowRect.y;

		if(input1Rect.Contains(clickPos))
		{
			input1 = inputNode; 
		}
		else if(input2Rect.Contains(clickPos))
		{
			input2 = inputNode; 
		}
	}

	public override void DrawCurves()
	{
		if(input1)
		{
			Rect rect = windowRect;
			rect.x += input1Rect.x;
			rect.y += input1Rect.y + input1Rect.height / 2f;
			rect.width = 1f;
			rect.height = 1f;

			NodeEditor.DrawNodeCurve(input1.windowRect, rect); 
		}

		if(input2)
		{
			Rect rect = windowRect;
			rect.x += input2Rect.x;
			rect.y += input2Rect.y + input2Rect.height / 2f;
			rect.width = 1f;
			rect.height = 1f;

			NodeEditor.DrawNodeCurve(input2.windowRect, rect); 
		}
	}

	public override string GetResult()
	{
		float input1Value = 0f;
		float input2Value = 0f; 

		if(input1)
		{
			float.TryParse(input1.GetResult(), out input1Value); 
		}
		if(input2)
		{
			float.TryParse(input2.GetResult(), out input2Value); 
		}

		string result = "false"; 

		switch(calculationType)
		{
			case CalculationType.Addition:
			result = (input1Value + input2Value).ToString(); 
			break;
			case CalculationType.Division:
			result = input2Value == 0f ? null : (input1Value / input2Value).ToString();  
			break;
			case CalculationType.Multiplication:
			result = (input1Value * input2Value).ToString(); 
			break;
			case CalculationType.Substraction:
			result = (input1Value - input2Value).ToString(); 
			break; 
		}

		return result; 
	}

	public override BaseInputNode ClickedOnInput(Vector2 clickPos)
	{
		BaseInputNode returnVal = null;

		clickPos.x -= windowRect.x;
		clickPos.y -= windowRect.y;

		if(input1Rect.Contains(clickPos))
		{
			returnVal = input1;
			input1 = null; 
		}

		if(input2Rect.Contains(clickPos))
		{
			returnVal = input2;
			input2 = null; 
		}

		return returnVal; 
	}

	public override void NodeDeleted(BaseNode node)
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
=======

public class CalculationNode : MonoBehaviour
{
	// TODO Handle calculation node 
>>>>>>> master
}
