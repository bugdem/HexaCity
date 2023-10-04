using ClocknestGames.Library.Utils;
using DG.Tweening;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using System;
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

	[System.Serializable]
	public class LandTilePartPathSetting
	{
		public LandTilePartType PathType;
		public List<SplineComputer> Paths;
	}

	[Serializable]
	public class LandTileDictionary : UnitySerializedDictionary<Vector3Int, LandTile> { }

	public class TileManager : Singleton<TileManager>
    {
		[SerializeField] private LandTile _landTilePrefab;
		[SerializeField] private Transform _landTileContainer;
		[SerializeField] private List<LandTilePartSetting> _landTilePartSettings;
		[SerializeField] private List<LandTilePartPathSetting> _landTilePartPathSettings;
		[SerializeField] private float _swipeMininumDistance = .2f;
		[SerializeField] private float _swipeMaximumTime = 1f;
		[SerializeField] private float _swipeDirectionThreshold = .9f;
		[SerializeField, ReadOnly] private LandTileDictionary _placedTiles;

		[Header("Editor")]
		[SerializeField, Range(0, 5)] private int _editorEdgeIndex;

		private HexTile _selectedTile = null;
		private Plane _gridTouchPlane;
		private Vector3 _touchWorldPosition;
		private List<HexTile> _tilesOnRadius;
		private Dictionary<LandTilePartType, LandTilePartSetting> _landTilePartSettingDic = new();
		private List<LandTile> _tilesWaitingToBePlaced = new();
		private LandTile _placingLandTile;
		private int _placingLandTileRotationIndex;
		private Tweener _placingLandTileRotateTweener;

		private HashSet<LandTilePartType> _landTilePartPool = new()
		{
			LandTilePartType.Forest,
			LandTilePartType.House,
			LandTilePartType.Grass,
			LandTilePartType.Mine
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
			_gridTouchPlane = new Plane(HexGrid.Get().transform.up, HexGrid.Get().transform.position);

			foreach (var placedTile in _placedTiles.Values)
			{
				placedTile.OnPlacedOnTile(placedTile.PlacedTile, placedTile.PlacementRotationIndex);
			}

			for (int index = 0; index < 4; index++)
			{
				_tilesWaitingToBePlaced.Add(CreateTile());
			}
		}

		private void Update()
		{
			_gridTouchPlane = new Plane(HexGrid.Get().transform.up, HexGrid.Get().transform.position);
			// CGDebug.DrawPlane(HexGrid.Instance.transform.position, _gridTouchPlane.normal);
			Ray ray = new Ray(CameraHandler.Get().transform.position, CameraHandler.Get().transform.forward);
			if (_gridTouchPlane.Raycast(ray, out float enter))
			{
				Vector3 hitPoint = ray.GetPoint(enter);
				CameraHandler.Get().SetRotatePivotPosition(hitPoint);
			}
		}

#if UNITY_EDITOR
		public void ChangeGeneratedTileEditor(List<LandTilePartType> newTileParts)
		{
			if (Application.isPlaying)
			{
				_tilesWaitingToBePlaced[0].GenerateTile(newTileParts);
			}
		}
#endif

		/// <summary>
		/// Removes land tile on index.
		/// </summary>
		/// <param name="cubeIndex">Index that tile.</param>
		/// <param name="destroyTile">Destroys tile if true(Always true in runtime).</param>
		/// <returns></returns>
		public bool RemoveLandTile(Vector3Int cubeIndex, bool destroyTile = true)
		{
			if (_placedTiles.TryGetValue(cubeIndex, out LandTile landTile))
			{
				_placedTiles.Remove(cubeIndex);

				// destroyTile variable only works on editor to prevent inconsistency in runtime.
				CGExec.RunInMode(() => GameObject.Destroy(landTile.gameObject)
							, () => {
								if (destroyTile)
									GameObject.DestroyImmediate(landTile.gameObject);
								});
				return true;
			}
			return false;
		}

		[HorizontalGroup("Split", 0.5f)]
		[Button(ButtonSizes.Large), GUIColor(1f, 0f, 0f)]
		public void RemoveAllLandTiles()
		{
			foreach (var landTileKey in _placedTiles.Keys.ToList())
			{
				RemoveLandTile(landTileKey);
			}
		}

		public LandTileDictionary GetPlacedLandTiles()
		{
			LandTileDictionary newPlacedTiles = new();
			foreach (var placedTile in _placedTiles)
				newPlacedTiles.Add(placedTile.Key, placedTile.Value);
			return newPlacedTiles;
		}

		public SplineComputer GetRoadPrefab(LandTilePartType pathType, int partIndexDiff)
		{
			return _landTilePartPathSettings.First(x => x.PathType == pathType).Paths[partIndexDiff - 1];
		}

		public LandTile CreateTile(HexTile onHexTile = null)
		{
			return CreateTile(GenerateLandTilePartTypes(), onHexTile);
		}

		public LandTile CreateTile(List<LandTilePartType> partTypes, HexTile onHexTile = null)
		{
			LandTile newTile = CGExec.RunInMode<LandTile>(() => Instantiate(_landTilePrefab, _landTileContainer)
														, () => UnityEditor.PrefabUtility.InstantiatePrefab(_landTilePrefab, _landTileContainer) as LandTile);
			newTile.GenerateTile(partTypes);
			newTile.transform.localScale = HexGrid.Get().GetTileScale();
			newTile.gameObject.SetActive(false);
			if (onHexTile != null)
			{
				newTile.OnPlacedOnTileEditor(onHexTile.CubeIndex, 0);
				_placedTiles.Add(onHexTile.CubeIndex, newTile);
				PlaceLandTileOnHexTile(newTile, onHexTile);
			}

			return newTile;
		}

		public List<LandTilePartType> GenerateLandTilePartTypes()
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

		public LandTile GetPlacedLandTile(Vector3Int cubeIndex)
		{
			_placedTiles.TryGetValue(cubeIndex, out var landTile);
			return landTile;
		}

		public void PlaceLandTileOnHexTile(LandTile landTile, HexTile hexTile)
		{
			landTile.transform.rotation = hexTile.transform.rotation;
			landTile.transform.position = hexTile.transform.position + hexTile.transform.up * .1f * HexGrid.Get().TileSize;
			landTile.gameObject.SetActive(true);
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
			// Pointer must be touched up near touch down position to act as a tap event.
			if (Vector2.Distance(InputManager.TouchStartPosition, InputManager.TouchEndPosition) >= _swipeMininumDistance)
				return;

			// Tap event is triggered event tapping on UI objects like button.
			// So if we tap on any UI element, do not process.
			if (EventSystem.current.IsPointerOverGameObject(0))
			{
				Debug.Log("Pointer Over UI!");
				return;
			}

			HexTile hexTileToSelect = null;
			List<HexTile> tilesOnRadius = null;

			var ray = CameraHandler.Get().CurrentCamera.ScreenPointToRay(InputManager.TouchPosition);
			if (_gridTouchPlane.Raycast(ray, out float enter))
			{
				Vector3 hitPoint = ray.GetPoint(enter);
				_touchWorldPosition = hitPoint;

				hexTileToSelect = HexGrid.Get().GetTileFromPosition(hitPoint);
				if (hexTileToSelect != null)
				{
					Debug.Log($"Hex: {hexTileToSelect.CubeIndex.x},{hexTileToSelect.CubeIndex.y},{hexTileToSelect.CubeIndex.z}");

					// HexGrid.Instance.AddTilesAroundTile(hexTileToSelect);

					// tilesOnRadius = HexGrid.Instance.GetTilesInsideRadius(hexTileToSelect, 2);

					// Check if selected tile is an empty tile.
					_placingLandTileRotationIndex = 0;
					if (!_placedTiles.ContainsKey(hexTileToSelect.CubeIndex))
					{
						CameraHandler.Get().IsControlEnabled = false;

						// If player is too quick to select while in rotate animation, finish rotation immediately.
						if (_placingLandTileRotateTweener != null)
							_placingLandTileRotateTweener.Kill(true);

						_placingLandTile = _tilesWaitingToBePlaced[0];
						PlaceLandTileOnHexTile(_placingLandTile, hexTileToSelect);
					}
					else
					{
						if (_placingLandTile != null)
						{
							_placingLandTile.gameObject.SetActive(false);
							_placingLandTile = null;
						}

						CameraHandler.Get().IsControlEnabled = true;
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

			HexGrid.Get().AddTilesAroundTile(_selectedTile);

			_tilesWaitingToBePlaced.RemoveAt(0);
			_tilesWaitingToBePlaced.Add(CreateTile());
			// TODO: Reorder UI.

			_placedTiles.Add(_selectedTile.CubeIndex, placingTile);
			_placingLandTileRotationIndex = 0;
			_selectedTile = null;
			_placingLandTile = null;

			CameraHandler.Get().IsControlEnabled = true;
		}

		public void OnPlacementCancelButtonClicked()
		{
			_placingLandTileRotationIndex = 0;
			_placingLandTile.gameObject.SetActive(false);
			_placingLandTile = null;
			_selectedTile = null;

			CameraHandler.Get().IsControlEnabled = true;
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
			HexGrid hexGrid = HexGrid.Get();
			Vector3 _selectedPosition = transform.position;

			if (Application.isPlaying)
			{
				if (_selectedTile != null)
					_selectedPosition = _selectedTile.transform.position;
			}

			float tileSize = hexGrid.TileSize;

			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(_touchWorldPosition, tileSize * .25f);

			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(_selectedPosition, tileSize * .5f);

			Gizmos.color = Color.red;
			var edge = hexGrid.GetEdge(_selectedPosition, _editorEdgeIndex);
			Gizmos.DrawLine(edge.point1, edge.point2);
			Gizmos.color = Color.yellow;
			// Gizmos.DrawWireSphere(edge.point1, .1f);
			// Gizmos.DrawWireSphere(edge.point2, .1f);

			var neighbourEdge = hexGrid.GetNeigbourEdge(edge);
			Vector3 neighbourTilePosition = hexGrid.GetPositionFromCubeIndex(neighbourEdge.hexCubeIndex);
			bool drawNeighbourTile = true;
			if (Application.isPlaying)
			{
				drawNeighbourTile = hexGrid.GetTile(neighbourEdge.hexCubeIndex) != null;
			}

			if (drawNeighbourTile)
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawWireCube(neighbourTilePosition, Vector3.one * tileSize * .5f);
				Gizmos.DrawWireSphere(neighbourEdge.point1, tileSize * .1f);
				Gizmos.DrawWireSphere(neighbourEdge.point2, tileSize * .1f);
			}

			if (_tilesOnRadius != null)
			{
				Gizmos.color = Color.blue;
				foreach (var tile in _tilesOnRadius)
				{
					Gizmos.DrawWireSphere(tile.transform.position, tileSize * .5f);
				}
			}
		}
	}
}