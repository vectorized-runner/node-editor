using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventExtensions 
{
	/// <summary>
	/// Should you use this event if necessary? Events of type Repaint and Layout should not be used.
	/// </summary>
	/// <param name="e"></param>
	/// <returns></returns>
	public static bool Usable(this Event e)
	{
		return e.type != EventType.Layout && e.type != EventType.Repaint;
	}
}
