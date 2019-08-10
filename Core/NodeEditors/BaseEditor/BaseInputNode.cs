using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseInputNode : BaseNode
{
	public override void DrawCurves() {}

	public virtual string GetResult()
	{
		return "None"; 
	}
}
