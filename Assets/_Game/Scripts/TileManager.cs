using ClocknestGames.Library.Utils;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ClocknestGames.Game.Core
{
	[System.Serializable]
	public class LandTilePartSetting
	{
		public LandTilePartType Type;
		public LandTilePart Prefab;
	}

    public class TileManager : Singleton<TileManager>
    {
		[SerializeField] private LandTile _landTilePrefab;
		[SerializeField] private List<LandTilePartSetting> _landTilePartSettings;
		[SerializeField] private float _swipeMininumDistance = .2f;
		[SerializeField] private float _swipeMaximumTime = 1f;
		[SerializeField] private float _swipeDirectionThreshold = .9f;

		[Header("Editor")]
		[SerializeField, Range(0, 5)] private int _editorEdgeIndex;

		private HexTile _selectedTile = null;
		private Plane _gridTouchPlane;
		private Vector3 _touchWorldPosition;
		private List<HexTile> _tilesOnRadius;
		private Dictionary<LandTilePartType, LandTilePartSetting> _landTilePartSettingDic = new();

		private Dictionary<Vector3Int, LandTile> _placedTiles = new();
		private List<LandTile> _tilesWaitingToBePlaced = new();
		private LandTile _placingLandTile;
		private int _placingLandTileRotationIndex;
		private Tweener _placingLandTileRotateTweener;

		private HashSet<LandTilePartType> _landTilePartPool = new()
		{
			LandTilePartType.Forest,
			LandTilePartType.House,
			LandTilePartType.Grass
		};

		protected override void Awake()
		{
			base.Awake();

			foreach (var tilePart in _landTilePartSettings)
			{
				_landTilePartSettingDic.Add(tilePart.Type, tilePart);
			}
		}

		private void Start()
		{
			_gridTouchPlane = new Plane(HexGrid.Instance.transform.up, HexGrid.Instance.transform.position);

			for (int index = 0; index < 4; index++)
			{
				_tilesWaitingToBePlaced.Add(CreateTile());
			}
		}

		private void Update()
		{
			// CGDebug.DrawPlane(HexGrid.Instance.transform.position, _gridTouchPlane.normal);
			Ray ray = new Ray(CameraHandler.Instance.transform.position, CameraHandler.Instance.transform.forward);
			if (_gridTouchPlane.Raycast(ray, out float enter))
			{
				Vector3 hitPoint = ray.GetPoint(enter);
				CameraHandler.Instance.SetRotatePivotPosition(hitPoint);
			}
		}

		private LandTile CreateTile()
		{
			LandTile newTile = Instantiate(_landTilePrefab);
			newTile.GenerateTile(GenerateLandTilePartTypes());
			newTile.gameObject.SetActive(false);

			return newTile;
		}

		private List<LandTilePartType> GenerateLandTilePartTypes()
		{
			List<LandTilePartType> partTypes = new List<LandTilePartType>();
			for (int index = 0; index < 7; index++)
			{
				partTypes.Add(_landTilePartPool.PickRandom());
			}
			return partTypes;
		}

		public LandTilePart CreateLandTilePart(LandTilePartType partType, bool isCenterPart)
		{
			// var landTilePartSetting = _landTilePartSettings[partType]; 
			var landTilePartSetting = Application.isEditor ? _landTilePartSettings.FirstOrDefault(x => x.Type == partType) 
															: _landTilePartSettingDic[partType]; ;
			var prefab = landTilePartSetting.Prefab;
			var landTilePart = Instantiate(prefab);
			return landTilePart;
		}

		private void OnScreenTouchDown()
		{

		}

		private void OnScreenTouchUp()
		{
			double timePassed = InputManager.TouchEndTime - InputManager.TouchStartTime;
			if (Vector2.Distance(InputManager.TouchStartPosition, InputManager.TouchEndPosition) >= _swipeMininumDistance
				&& timePassed <= _swipeMaximumTime)
			{
				Vector2 direction = (InputManager.TouchStartPosition - InputManager.TouchEndPosition).normalized;
				OnScreenSwiped(direction, timePassed);
			}
		}

		private void OnScreenTapped()
		{
			// Tap event is triggered event tapping on UI objects like button.
			// So if we tap on any UI element, do not process.
			if (EventSystem.current.IsPointerOverGameObject(0))
			{
				Debug.Log("Pointer Over UI!");
				return;
			}

			HexTile hexTileToSelect = null;
			List<HexTile> tilesOnRadius = null;

			var ray = Camera.main.ScreenPointToRay(InputManager.TouchPosition);
			if (_gridTouchPlane.Raycast(ray, out float enter))
			{
				Vector3 hitPoint = ray.GetPoint(enter);
				_touchWorldPosition = hitPoint;

				hexTileToSelect = HexGrid.Instance.GetTileFromPosition(hitPoint);
				if (hexTileToSelect != null)
				{
					Debug.Log($"Hex: {hexTileToSelect.CubeIndex.x},{hexTileToSelect.CubeIndex.y},{hexTileToSelect.CubeIndex.z}");

					// HexGrid.Instance.AddTilesAroundTile(hexTileToSelect);

					// tilesOnRadius = HexGrid.Instance.GetTilesInsideRadius(hexTileToSelect, 2);

					// Check if selected tile is an empty tile.
					_placingLandTileRotationIndex = 0;
					if (!_placedTiles.ContainsKey(hexTileToSelect.CubeIndex))
					{
						CameraHandler.Instance.IsControlEnabled = false;

						// If player is too quick to select while in rotate animation, finish rotation immediately.
						if (_placingLandTileRotateTweener != null)
							_placingLandTileRotateTweener.Kill(true);

						_placingLandTile = _tilesWaitingToBePlaced[0];
						_placingLandTile.transform.rotation = hexTileToSelect.transform.rotation;
						_placingLandTile.transform.position = hexTileToSelect.transform.position + hexTileToSelect.transform.up * .05f;
						_placingLandTile.gameObject.SetActive(true);
					}
					else
					{
						if (_placingLandTile != null)
						{
							_placingLandTile.gameObject.SetActive(false);
							_placingLandTile = null;
						}

						CameraHandler.Instance.IsControlEnabled = true;
					}
				}
			}

			_selectedTile = hexTileToSelect;
			_tilesOnRadius = tilesOnRadius;
		}

		private void OnScreenSwiped(Vector2 direction, double time)
		{
			if (_placingLandTile != null && Mathf.Abs(direction.x) > 0)
			{
				Vector2Int clampedSwipeDirection = GetClampedSwipeDirection(direction);
				_placingLandTileRotationIndex += clampedSwipeDirection.x;
				if (_placingLandTileRotationIndex < 0) _placingLandTileRotationIndex = 5;
				else if (_placingLandTileRotationIndex > 5) _placingLandTileRotationIndex = 0;

				if (_placingLandTileRotateTweener != null)
					_placingLandTileRotateTweener.Kill();

				_placingLandTileRotateTweener = _placingLandTile.transform.DORotate(Vector3.up * _placingLandTileRotationIndex * 60f, .5f)
																			.SetEase(Ease.OutCubic)
																			.OnComplete(() => _placingLandTileRotateTweener = null);
			}
		}

		public Vector2Int GetClampedSwipeDirection(Vector2 direction)
		{
			if (Vector2.Dot(Vector2.right, direction) > _swipeDirectionThreshold)
				return Vector2Int.right;
			else if (Vector2.Dot(Vector2.left, direction) > _swipeDirectionThreshold)
				return Vector2Int.left;
			else if (Vector2.Dot(Vector2.up, direction) > _swipeDirectionThreshold)
				return Vector2Int.up;
			else
				return Vector2Int.down;
		}

		public void OnPlacementApproveButtonClicked()
		{
			var placingTile = _tilesWaitingToBePlaced[0];
			placingTile.OnPlacedOnTile(_selectedTile, _placingLandTileRotationIndex);

			HexGrid.Instance.AddTilesAroundTile(_selectedTile);

			_tilesWaitingToBePlaced.RemoveAt(0);
			_tilesWaitingToBePlaced.Add(CreateTile());
			// TODO: Reorder UI.

			_placedTiles.Add(_selectedTile.CubeIndex, placingTile);
			_placingLandTileRotationIndex = 0;
			_selectedTile = null;
			_placingLandTile = null;

			CameraHandler.Instance.IsControlEnabled = true;
		}

		public void OnPlacementCancelButtonClicked()
		{
			_placingLandTileRotationIndex = 0;

			CameraHandler.Instance.IsControlEnabled = true;
		}

		private void OnEnable()
		{
			InputManager.OnTouchDown += OnScreenTouchDown;
			InputManager.OnTouchUp += OnScreenTouchUp;
			InputManager.OnTapped += OnScreenTapped;
		}

		private void OnDisable()
		{
			InputManager.OnTouchDown -= OnScreenTouchDown;
			InputManager.OnTouchUp -= OnScreenTouchUp;
			InputManager.OnTapped -= OnScreenTapped;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(_touchWorldPosition, .25f);

			if (_selectedTile != null)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawWireSphere(_selectedTile.transform.position, .5f);

				Gizmos.color = Color.red;
				var edge = HexGrid.Instance.GetEdge(_selectedTile, _editorEdgeIndex);
				Gizmos.DrawLine(edge.point1, edge.point2);
				Gizmos.color = Color.yellow;
				// Gizmos.DrawWireSphere(edge.point1, .1f);
				// Gizmos.DrawWireSphere(edge.point2, .1f);

				var neighbourEdge = HexGrid.Instance.GetNeigbourEdge(edge);
				if (neighbourEdge.hexTile != null)
				{
					Gizmos.color = Color.blue;
					Gizmos.DrawWireCube(neighbourEdge.hexTile.transform.position, Vector3.one * .5f);
					Gizmos.DrawWireSphere(neighbourEdge.point1, .1f);
					Gizmos.DrawWireSphere(neighbourEdge.point2, .1f);
				}
			}

			if (_tilesOnRadius != null)
			{
				Gizmos.color = Color.blue;
				foreach (var tile in _tilesOnRadius)
				{
					Gizmos.DrawWireSphere(tile.transform.position, .5f);
				}
			}
		}
	}
}