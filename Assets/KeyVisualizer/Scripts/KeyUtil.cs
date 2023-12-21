using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameEngine.Util
{
	[System.Serializable]
	public enum KeyEventType : byte
	{
		KeyDown = 0,
		KeyPress,
		KeyUp
	}

	public struct KeyEventValue
	{
		public KeyEventType EventType;
		public KeyCode Key;

		public KeyEventValue(KeyEventType eventType, KeyCode key)
		{
			EventType = eventType;
			Key = key;
		}
	}

	public struct MouseKeyEventValue
	{
		public KeyEventType EventType;
		public int ButtonIndex;

		public MouseKeyEventValue(KeyEventType eventType, int buttonIndex)
		{
			EventType = eventType;
			ButtonIndex = buttonIndex;
		}
	}

	public static class KeyUtil
	{
		static readonly KeyCode[] _keyCodes =
			System.Enum.GetValues(typeof(KeyCode))
				.Cast<KeyCode>()
				.Where(k => k < KeyCode.Mouse0)
				.ToArray();

		static readonly int[] _mouseKeyCodes = new int[] { 0, 1, 2 };

		public static bool IsAnyKeyDown(params KeyCode[] interestingCodes)
		{
			return Enumerable.Intersect(GetCurrentKeys(), interestingCodes).Any();
		}

		public static bool IsAllKeyDown(params KeyCode[] interestingCodes)
		{
			return Enumerable.SequenceEqual(GetCurrentKeys(), interestingCodes);
		}

		public static IEnumerable<KeyCode> GetCurrentKeysDown()
		{
			for (int i = 0; i < _keyCodes.Length; i++)
				if (Input.GetKeyDown(_keyCodes[i]))
					yield return _keyCodes[i];
		}

		public static IEnumerable<KeyCode> GetCurrentKeys()
		{
			if (Input.anyKeyDown)
			{
				for (int i = 0; i < _keyCodes.Length; i++)
					if (Input.GetKey(_keyCodes[i]))
						yield return _keyCodes[i];
			}
		}

		public static IEnumerable<KeyCode> GetCurrentKeysUp()
		{
			for (int i = 0; i < _keyCodes.Length; i++)
				if (Input.GetKeyUp(_keyCodes[i]))
					yield return _keyCodes[i];
		}

		public static IEnumerable<KeyEventValue> GetCurrentKeysAllEvent()
		{
			for (int i = 0; i < _keyCodes.Length; i++)
			{
				if (Input.GetKeyDown(_keyCodes[i]))
					yield return new KeyEventValue(KeyEventType.KeyDown, _keyCodes[i]);
				else if (Input.GetKey(_keyCodes[i]))
					yield return new KeyEventValue(KeyEventType.KeyPress, _keyCodes[i]);
				else if (Input.GetKeyUp(_keyCodes[i]))
					yield return new KeyEventValue(KeyEventType.KeyUp, _keyCodes[i]);
			}
		}

		public static IEnumerable<MouseKeyEventValue> GetCurrentMouseKeysAllEvent()
		{
			for (int i = 0; i < _mouseKeyCodes.Length; i++)
			{
				if (Input.GetMouseButtonDown(_mouseKeyCodes[i]))
					yield return new MouseKeyEventValue(KeyEventType.KeyDown, _mouseKeyCodes[i]);
				else if (Input.GetMouseButton(_mouseKeyCodes[i]))
					yield return new MouseKeyEventValue(KeyEventType.KeyPress, _mouseKeyCodes[i]);
				else if (Input.GetMouseButtonUp(_mouseKeyCodes[i]))
					yield return new MouseKeyEventValue(KeyEventType.KeyUp, _mouseKeyCodes[i]);
			}
		}
	}
}