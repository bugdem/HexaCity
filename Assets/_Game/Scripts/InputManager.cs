using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ClocknestGames.Library.Utils;

namespace ClocknestGames.Game.Core
{
	[DefaultExecutionOrder(-1)]
	public class InputManager : PersistentSingleton<InputManager>
	{
		public static bool IsTouchDown { get; private set; }
		public static bool IsTouchUp { get; private set; }
		public static bool WasTouching { get; private set; }
		public static bool IsTouching { get; private set; }
		public static Vector2 TouchPosition { get; private set; }
		public static Vector2 TouchStartPosition { get; private set; }
		public static Vector2 TouchEndPosition { get; private set; }
		public static double TouchStartTime { get; private set; }
		public static double TouchEndTime { get; private set; }

		public delegate void TouchHandler();
		public static event TouchHandler OnTouchDown;
		public static event TouchHandler OnTouchUp;
		public static event TouchHandler OnTapped;

		private TouchControls _touchControls;

		protected override void Awake()
		{
			base.Awake();
			if (!_enabled) return;

			_touchControls = new TouchControls();
		}

		private void Update()
		{
			if (IsTouchDown)
				IsTouchDown = false;

			if (IsTouchUp)
				IsTouchUp = false;

			if (WasTouching && !IsTouching)
			{
				WasTouching = IsTouching;
				IsTouchUp = true;
			}
			else if (!WasTouching && IsTouching)
			{
				WasTouching = IsTouching;
				IsTouchDown = true;
			}

			TouchPosition = _touchControls.Player.TouchPosition.ReadValue<Vector2>();
		}

		private void Start()
		{
			_touchControls.Player.Touch.started += OnTouchStarted;
			_touchControls.Player.Touch.canceled += OnTouchCanceled;

			_touchControls.Player.Tap.performed += OnTapPerformed;
		}

		private void OnTapPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
		{
			OnTapped?.Invoke();
		}

		private void OnTouchStarted(UnityEngine.InputSystem.InputAction.CallbackContext context)
		{
			// Debug.Log("Touch Started!");

			IsTouching = true;

			TouchStartPosition = _touchControls.Player.TouchPosition.ReadValue<Vector2>();
			TouchStartTime = context.startTime;
			TouchPosition = TouchStartPosition;

			OnTouchDown?.Invoke();
		}

		private void OnTouchCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
		{
			// Debug.Log("Touch Ended!");

			TouchEndPosition = _touchControls.Player.TouchPosition.ReadValue<Vector2>();
			TouchEndTime = context.time;
			IsTouching = false;

			OnTouchUp?.Invoke();
		}

		private void ResetValues()
		{
			IsTouchDown = false;
			IsTouchUp = false;
			WasTouching = false;
			IsTouching = false;

			TouchPosition = Vector2.zero;
			TouchStartPosition = Vector2.zero;

			OnTouchDown = null;
			OnTouchUp = null;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			ResetValues();
		}

		private void OnEnable()
		{
			_touchControls.Enable();

			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnDisable()
		{
			_touchControls.Disable();

			SceneManager.sceneLoaded -= OnSceneLoaded;
		}
	}
}