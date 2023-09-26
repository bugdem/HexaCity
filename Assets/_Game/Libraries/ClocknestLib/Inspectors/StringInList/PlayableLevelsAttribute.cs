using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ClocknestGames.Library.Editor
{
	public class PlayableLevelsAttribute : PropertyAttribute
	{
		public delegate string[] GetStringList();

		public PlayableLevelsAttribute(params string[] list)
		{
			List = list;
		}

		public PlayableLevelsAttribute(object[] parameters = null)
		{
			var methodName = "PlayableSceneNames";
			var typeOf = typeof(StringInListData);
			var method = typeOf.GetMethod(methodName);
			if (method != null)
			{
				List = method.Invoke(null, parameters) as string[];
			}
			else
			{
				Debug.LogError("NO SUCH METHOD " + methodName + " FOR " + typeOf);
			}
		}

		public string[] List
		{
			get;
			private set;
		}
	}
}