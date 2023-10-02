using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClocknestGames.Game.Core
{
	[CustomEditor(typeof(TileManager))]
	public class TileManagerEditor : OdinEditor
    {
		private TileManager _tileManager;

		protected override void OnEnable()
		{
			base.OnEnable();

			_tileManager = target as TileManager;
		}

		private void OnSceneGUI()
		{
			if (Application.isPlaying) return;

			if (_tileManager.IsEditorSceneControlsActive)
			{
				if (Input.GetMouseButtonDown(0)) 
				{
					Debug.Log("Editor mouse click!");

					var plane = new Plane(HexGrid.Get().transform.up, HexGrid.Get().transform.position);
					Ray ray = HandleUtility.GUIPointToWorldRay(Mouse.current.position.ReadValue());
					LandTile landTileOnPoint = null;
					HexTile hexTileOnPoint = null;
					if (plane.Raycast(ray, out float enter))
					{
						Vector3 hitPoint = ray.GetPoint(enter);
						hexTileOnPoint = HexGrid.Get().GetTileFromPosition(hitPoint);
						if (hexTileOnPoint != null)
						{
							landTileOnPoint = _tileManager.StartingTiles.FirstOrDefault(x => x.PlacedCubeIndex == hexTileOnPoint.CubeIndex);
						}
					}

					// Delete selected tile.
					if (Input.GetKeyDown(KeyCode.LeftControl))
					{
						if (landTileOnPoint != null)
						{
							_tileManager.StartingTiles.Remove(landTileOnPoint);
							GameObject.DestroyImmediate(landTileOnPoint.gameObject);
						}
					}
					// Add new tile.
					else
					{
						if (hexTileOnPoint != null && landTileOnPoint == null)
						{
							var newLandTile = _tileManager.CreateTile(hexTileOnPoint);
							_tileManager.StartingTiles.Add(newLandTile);
						}
					}
				}
			}
		}
	}
}