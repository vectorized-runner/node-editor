using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; 

public class InputNode : BaseInputNode
{
	public enum InputType
	{
		Number, 
		Randomization 
	}

	InputType inputType;
	string randomFrom;
	string randomTo;
	string inputValue;

	public InputNode()
	{
		windowTitle = "Input Node";
	}

	public override void DrawWindow()
	{
		base.DrawWindow();

		inputType = (InputType)EditorGUILayout.EnumPopup("Input Type : ", inputType); 

		if(inputType == InputType.Number)
		{
			inputValue = EditorGUILayout.TextField("Value", inputValue); 
		}
		else if(inputType == InputType.Randomization)
		{
			randomFrom = EditorGUILayout.TextField("From", randomFrom);
			randomTo = EditorGUILayout.TextField("To", randomTo); 

			if(GUILayout.Button("Calculate Random"))
			{
				CalculateRandom(); 
			}
		}

	}

	public override string GetResult()
	{
		return inputValue;	
	}

	public override void DrawCurves()
	{
	}

	void CalculateRandom()
	{
		float.TryParse(randomFrom, out float rFrom);
		float.TryParse(randomTo, out float rTo);

		float value = (int)Random.Range(rFrom, rTo);

		inputValue = value.ToString(); 
	}

}
