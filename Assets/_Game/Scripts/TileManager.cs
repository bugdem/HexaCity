using ClocknestGames.Library.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    public class TileManager : Singleton<TileManager>
    {
		[Header("Editor")]
		[SerializeField, Range(0, 5)] private int _editorEdgeIndex;

		private HexTile _selectedTile = null;
		private Plane _gridTouchPlane;
		private Vector3 _touchWorldPosition;

		private List<HexTile> _tilesOnRadius;

		private void Start()
		{
			_gridTouchPlane = new Plane(HexGrid.Instance.transform.up, HexGrid.Instance.transform.position);
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

		private void OnScreenTouchDown()
		{
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

					HexGrid.Instance.AddTilesAroundTile(hexTileToSelect);

					// tilesOnRadius = HexGrid.Instance.GetTilesInsideRadius(hexTileToSelect, 2);
				}
			}

			_selectedTile = hexTileToSelect;
			_tilesOnRadius = tilesOnRadius;
		}

		private void OnEnable()
		{
			InputManager.OnTouchDown += OnScreenTouchDown;
		}

		private void OnDisable()
		{
			InputManager.OnTouchDown -= OnScreenTouchDown;	
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