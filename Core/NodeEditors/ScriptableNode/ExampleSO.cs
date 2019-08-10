using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ExampleSO", menuName = "ExampleSO")]
public class ExampleSO : ScriptableObject
{
	public string content;
	public string date;

	public ExampleSO exampleSo; 
	public List<ExampleSO> exampleSos = new List<ExampleSO>(); 
}
