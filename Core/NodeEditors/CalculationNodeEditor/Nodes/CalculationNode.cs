 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CalculationNode : OperationNode
{
	public enum CalculationType
	{
		Addition,
		Substraction,
		Division,
		Multiplication
	}

	CalculationType calculationType;

	public override string WindowTitle => "Calculation Node"; 

	public override void DrawWindow()
	{
		calculationType = (CalculationType)EditorGUILayout.EnumPopup("Calculation Type", calculationType);

		base.DrawWindow();
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
}