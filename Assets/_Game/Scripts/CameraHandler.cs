using UnityEngine;
using System.Collections;
using ClocknestGames.Library.Utils;
using DG.Tweening;

namespace ClocknestGames.Game.Core
{
	public class CameraHandler : Singleton<CameraHandler>
	{
		[SerializeField] private float _panSpeed = 20f;
		[SerializeField] private float _panFollowSpeed = 10f;
		[SerializeField] private float _rotateSpeed = 180f;
		[SerializeField] private float _zoomSpeedTouch = 0.1f;
		[SerializeField] private float _zoomSpeedMouse = 25f;

		private static readonly float[] _boundsX = new float[] { -10f, 5f };
		private static readonly float[] _boundsZ = new float[] { -18f, -4f };
		private static readonly float[] _zoomBounds = new float[] { 20f, 85f };

		public Camera MainCamera => _camera;
		public bool IsControlEnabled { get; set; } = true;
		public bool IsPanEnabled { get; set; } = true;
		public bool IsRotateEnabled { get; set; } = true;
		public bool IsZoomEnabled { get; set; } = true;

		private Camera _camera;

		private Vector3 _lastPanPosition;
		private Vector3 _lastRotatePosition;
		private int _panFingerId; // Touch mode only
		private bool _wasZoomingLastFrame; // Touch mode only
		private Vector2[] _lastZoomPositions; // Touch mode only
		private Vector3 _rotatePivotPosition;
		private Tweener _moveTweener;

		protected override void Awake()
		{
			base.Awake();

			_camera = GetComponent<Camera>();
		}

		void Update()
		{
			if (Input.touchSupported)
			{
				HandleTouch();
			}
			else
			{
				HandleMouse();
			}
		}

		void HandleTouch()
		{
			switch (Input.touchCount)
			{
				case 1: // Panning
					_wasZoomingLastFrame = false;

					// If the touch began, capture its position and its finger ID.
					// Otherwise, if the finger ID of the touch doesn't match, skip it.
					Touch touch = Input.GetTouch(0);
					if (touch.phase == TouchPhase.Began)
					{
						_lastPanPosition = touch.position;
						_panFingerId = touch.fingerId;
					}
					else if (touch.fingerId == _panFingerId && touch.phase == TouchPhase.Moved)
					{
						PanCamera(touch.position);
					}
					break;

				case 2: // Zooming
					Vector2[] newPositions = new Vector2[] { Input.GetTouch(0).position, Input.GetTouch(1).position };
					if (!_wasZoomingLastFrame)
					{
						_lastZoomPositions = newPositions;
						_wasZoomingLastFrame = true;
					}
					else
					{
						// Zoom based on the distance between the new positions compared to the 
						// distance between the previous positions.
						float newDistance = Vector2.Distance(newPositions[0], newPositions[1]);
						float oldDistance = Vector2.Distance(_lastZoomPositions[0], _lastZoomPositions[1]);
						float offset = newDistance - oldDistance;

						ZoomCamera(offset, _zoomSpeedTouch);

						_lastZoomPositions = newPositions;
					}
					break;

				default:
					_wasZoomingLastFrame = false;
					break;
			}
		}

		void HandleMouse()
		{
			// On mouse down, capture it's position.
			// Otherwise, if the mouse is still down, pan the camera.
			if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
			{
				_lastPanPosition = Input.mousePosition;
			}
			else if (Input.GetMouseButton(0) || Input.GetMouseButton(2))
			{
				PanCamera(Input.mousePosition);
			}
			// Rotate camera.
			else if (Input.GetMouseButtonDown(1))
			{
				_lastRotatePosition = Input.mousePosition;
			}
			else if (Input.GetMouseButton(1))
			{
				RotateCamera(Input.mousePosition);
			}

			// Check for scrolling to zoom the camera
			float scroll = Input.GetAxis("Mouse ScrollWheel");
			ZoomCamera(scroll, _zoomSpeedMouse);
		}

		void PanCamera(Vector3 newPanPosition)
		{
			if (!IsControlEnabled || !IsPanEnabled) return;
			if (Vector3.Distance(newPanPosition, _lastPanPosition) <= 10f) return;

			// Determine how much to move the camera
			Vector3 offset = _camera.ScreenToViewportPoint(_lastPanPosition - newPanPosition);
			Vector3 desiredDirection = Vector3.Normalize(new Vector3(transform.forward.x, 0, transform.forward.z));

			Vector3 targetPosition = transform.position;
			targetPosition += desiredDirection * offset.y * _panSpeed;
			targetPosition += offset.x * transform.right * _panSpeed;

			//Append the new pos to the `transform.position`.
			// transform.position = targetPosition;

			if (_moveTweener != null)
				_moveTweener.Kill();

			_moveTweener = transform.DOMove(targetPosition, _panFollowSpeed).SetSpeedBased(true).SetEase(Ease.OutCubic).OnComplete(() =>
			{
				_moveTweener = null;
			});

			// Ensure the camera remains within bounds.
			Vector3 pos = transform.position;
			pos.x = Mathf.Clamp(transform.position.x, _boundsX[0], _boundsX[1]);
			pos.z = Mathf.Clamp(transform.position.z, _boundsZ[0], _boundsZ[1]);
			// transform.position = pos;

			// Cache the position
			_lastPanPosition = newPanPosition;
		}

		void RotateCamera(Vector3 newRotatePosition)
		{
			if (!IsControlEnabled || !IsRotateEnabled) return;

			// Determine how much to rotate the camera
			Vector3 offset = _camera.ScreenToViewportPoint(_lastRotatePosition - newRotatePosition);
			transform.RotateAround(_rotatePivotPosition, Vector3.up, _rotateSpeed * offset.x);

			// Cache the position
			_lastRotatePosition = newRotatePosition;
		}

		void ZoomCamera(float offset, float speed)
		{
			if (!IsControlEnabled || !IsZoomEnabled) return;
			if (offset == 0) return;

			_camera.fieldOfView = Mathf.Clamp(_camera.fieldOfView - (offset * speed), _zoomBounds[0], _zoomBounds[1]);
		}

		public void SetRotatePivotPosition(Vector3 pivotPosition)
		{
			_rotatePivotPosition = pivotPosition;
		}
	}
}