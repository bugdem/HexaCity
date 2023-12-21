using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using GameEngine.Util;

namespace GameEngine.Core
{
	[Serializable]
	public class UnityGenericEvent<T> : UnityEvent<T>
	{
		public int EventCount { get; private set; }

		public void Register(UnityAction<T> call)
		{
			AddListener(call);
			EventCount++;
		}

		public void UnRegister(UnityAction<T> call)
		{
			RemoveListener(call);
			EventCount++;
		}
	}

	/*
	[Serializable]
	public class KeyCodeEvent : UnityEvent<KeyCode> { }
	*/

	[Serializable]
	public class KeyCodeGenericEvent : UnityGenericEvent<KeyEventValue> { }

	[Serializable]
	public class MouseKeyCodeGenericEvent : UnityGenericEvent<MouseKeyEventValue> { }

	[DefaultExecutionOrder(-100)]
	public class KeyListener : Singleton<KeyListener>
    {
		[SerializeField] private KeyCodeGenericEvent _keyGenericEvent;
		[SerializeField] private MouseKeyCodeGenericEvent _mouseKeyGenericEvent;

		// To reduce for loop for keys, KeyCodeGenericEvent is created.
		public KeyCodeGenericEvent KeyGenericEvent => _keyGenericEvent;
		public MouseKeyCodeGenericEvent MouseKeyGenericEvent => _mouseKeyGenericEvent;

		private void Update()
		{
			if (KeyGenericEvent.EventCount > 0)
				OutputKey(KeyUtil.GetCurrentKeysAllEvent(), KeyGenericEvent);

			if (MouseKeyGenericEvent.EventCount > 0)
				OutputMouseKey(KeyUtil.GetCurrentMouseKeysAllEvent(), MouseKeyGenericEvent);
		}

		private void OutputKey(IEnumerable<KeyEventValue> keys, KeyCodeGenericEvent output)
		{
			if (keys == null) return;
			foreach (var key in keys)
				output.Invoke(key);
		}

		private void OutputMouseKey(IEnumerable<MouseKeyEventValue> keys, MouseKeyCodeGenericEvent output)
		{
			if (keys == null) return;
			foreach (var key in keys)
				output.Invoke(key);
		}

		/*
		[SerializeField] private KeyCodeEvent _keyDownEvent;
		[SerializeField] private KeyCodeEvent _keyHoldEvent;
		[SerializeField] private KeyCodeEvent _keyUpEvent;

		public KeyCodeEvent KeyDownEvent => _keyDownEvent;
		public KeyCodeEvent KeyHoldEvent => _keyHoldEvent;
		public KeyCodeEvent KeyUpEvent => _keyUpEvent;
		

		private bool _useKeyDown;
		private bool _useKeyHold;
		private bool _useKeyUp;

		void Update()
		{
			if (_useKeyDown) Output(KeyUtil.GetCurrentKeysDown(), _keyDownEvent);
			if (_useKeyHold) Output(KeyUtil.GetCurrentKeys(), _keyHoldEvent);
			if (_useKeyUp) Output(KeyUtil.GetCurrentKeysUp(), _keyUpEvent);
		}

		void OnValidate()
		{
			_useKeyDown = _keyDownEvent.GetPersistentEventCount() > 0;
			_useKeyHold = _keyHoldEvent.GetPersistentEventCount() > 0;
			_useKeyUp = _keyUpEvent.GetPersistentEventCount() > 0;
		}

		void Output(IEnumerable<KeyCode> keys, KeyCodeEvent output)
		{
			if (keys == null) return;
			foreach (var key in keys)
				output.Invoke(key);
		}
		*/
	}
}