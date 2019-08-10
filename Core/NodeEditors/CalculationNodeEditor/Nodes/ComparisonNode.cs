using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ComparisonNode : OperationNode
{
	public enum ComparisonType
	{
		Greater,
		Less,
		Equal
	}

	public override string WindowTitle => "Comparison Node"; 

	ComparisonType comparisonType;

	public override void DrawWindow()
	{
		comparisonType = (ComparisonType)EditorGUILayout.EnumPopup("Comparison Type", comparisonType);

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

		switch(comparisonType)
		{
			case ComparisonType.Equal:
			if(Mathf.Approximately(input1Value, input2Value))
			{
				result = "true";
			}
			break;
			case ComparisonType.Greater:
			if(input1Value > input2Value)
			{
				result = "true";
			}
			break;
			case ComparisonType.Less:
			if(input1Value < input2Value)
			{
				result = "true";
			}
			break;
		}

		return result;
	}
}
