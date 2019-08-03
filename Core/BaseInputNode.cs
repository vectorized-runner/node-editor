using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseInputNode : BaseNode
{
	public override void DrawCurves()
	{
		throw new System.NotImplementedException();
	}

	public virtual string GetResult()
	{
		return "None"; 
	}
}
