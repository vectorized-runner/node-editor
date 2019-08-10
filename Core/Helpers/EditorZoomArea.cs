using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorZoomArea
{
	const float EditorWindowTabHeight = 21.0f;
	static Matrix4x4 prevGuiMatrix;

	public static void Begin(float zoomScale, Rect screenCoordsArea)
	{
		// End the group Unity begins automatically for an EditorWindow to clip out the window tab. 
		// This allows us to draw outside of the size of the EditorWindow.
		GUI.EndGroup();

		Rect clippedArea = screenCoordsArea.ScaleSizeBy(1.0f / zoomScale, screenCoordsArea.TopLeft());

		clippedArea.y += EditorWindowTabHeight;
		GUI.BeginGroup(clippedArea);

		prevGuiMatrix = GUI.matrix;
		Matrix4x4 translation = Matrix4x4.TRS(clippedArea.TopLeft(), Quaternion.identity, Vector3.one);
		Matrix4x4 scale = Matrix4x4.Scale(new Vector3(zoomScale, zoomScale, 1.0f));
		GUI.matrix = translation * scale * translation.inverse * GUI.matrix;
	}

	public static void End()
	{
		GUI.matrix = prevGuiMatrix;
		GUI.EndGroup();
		GUI.BeginGroup(new Rect(0.0f, EditorWindowTabHeight, Screen.width, Screen.height));
	}
}