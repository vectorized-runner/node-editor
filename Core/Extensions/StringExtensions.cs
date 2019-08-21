using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public static class StringExtensions 
{
	public static string ToInspector(this string str)
	{
		return str.SplitCamelCase().UppercaseFirst();
	}

	static string SplitCamelCase(this string str)
	{
		return Regex.Replace(
			Regex.Replace(
				str,
				@"(\P{Ll})(\P{Ll}\p{Ll})",
				"$1 $2"
			),
			@"(\p{Ll})(\P{Ll})",
			"$1 $2"
		);
	}

	static string UppercaseFirst(this string str)
	{
		// Check for empty string.
		if(string.IsNullOrEmpty(str))
		{
			return string.Empty;
		}
		// Return char and concat substring.
		return char.ToUpper(str[0]) + str.Substring(1);
	}
}
