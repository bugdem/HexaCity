using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;

namespace ClocknestGames.Game.Core
{
	[ExecuteInEditMode]
	public class TileVisualizer : MonoBehaviour
	{
		[InfoBox("Enables tile editing controls. If enabled, mouse left click on a hex tile will add new tile while ctrl + mouse left click removes it.")]
		[SerializeField] private TileVisualizerMode _mode = TileVisualizerMode.AddRemoveTile;
		[SerializeField] private bool _isEditorSceneControlsActive = false;
		[SerializeField] private bool _isControlsActiveOnPlay = true;
		[SerializeField, HideInInspector] private bool _isCtrlKeyDown;
		
		private LandTile _selectedLandTile;

		public bool IsEditorSceneControlsActive
		{
			get => _isEditorSceneControlsActive;
			set => _isEditorSceneControlsActive = value;
		}

		public enum TileVisualizerMode
		{
			AddRemoveTile,
			FindPath
		}

		private void OnEnable()
		{
			if (!Application.isEditor)
			{
				Destroy(this);
				return;
			}
			SceneView.duringSceneGui += OnScene;
		}

		private void OnScene(SceneView scene)
		{
			var tileManager = FindObjectOfType<TileManager>();

			Event e = Event.current;
			if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftControl)
			{
				_isCtrlKeyDown = true;
			}
			if (e.type == EventType.KeyUp && e.keyCode == KeyCode.LeftControl)
			{
				_isCtrlKeyDown = false;
			}

			bool isLeftMouseClicked = e.type == EventType.MouseDown && e.button == 0;
			bool isRightMouseClicked = e.type == EventType.MouseDown && e.button == 1;
			bool isScrolled = e.type == EventType.ScrollWheel;
			int scrollDirection = isScrolled && e.delta.y < 0 ? -1 : 1;
			Vector2 mousePosition = e.mousePosition;

			if ((!_isControlsActiveOnPlay && Application.isPlaying) || !IsEditorSceneControlsActive) return;

			if (_mode == TileVisualizerMode.AddRemoveTile)
			{
				// Retrieve the control Id
				int controlId = GUIUtility.GetControlID(FocusType.Passive);
				if (isLeftMouseClicked || isScrolled)
				{
					var plane = new Plane(HexGrid.Get().transform.up, HexGrid.Get().transform.position);
					Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
					LandTile landTileOnPoint = null;
					HexTile hexTileOnPoint = null;
					if (plane.Raycast(ray, out float enter))
					{
						Vector3 hitPoint = ray.GetPoint(enter);
						hexTileOnPoint = HexGrid.Get().GetTileFromPosition(hitPoint);
						if (hexTileOnPoint != null)
						{
							landTileOnPoint = tileManager.GetPlacedLandTile(hexTileOnPoint.CubeIndex);
						}
					}

					if (isLeftMouseClicked)
					{
						// Delete selected tile.
						if (_isCtrlKeyDown)
						{
							if (landTileOnPoint != null)
							{
								Vector3Int targetCubeIndex = landTileOnPoint.PlacedCubeIndex;
								Undo.IncrementCurrentGroup();

								var hexTiles = HexGrid.Get().GetTilesInsideRadius(targetCubeIndex, false);
								foreach (var hexTile in hexTiles.ToArray())
								{
									bool removeHexTile = true;
									var placedLandTiles = tileManager.GetPlacedLandTiles();
									foreach (var placedLandTile in placedLandTiles.Values)
									{
										if (placedLandTile == landTileOnPoint)
											continue;

										if (HexGrid.GetDistance(hexTile.CubeIndex, placedLandTile.PlacedCubeIndex) <= HexGrid.Get().RadiusOnPlace)
										{
											removeHexTile = false;
											break;
										}
									}

									if (removeHexTile && HexGrid.GetDistance(Vector3Int.zero, hexTile.CubeIndex) > HexGrid.Get().RadiusOnStart)
									{
										Undo.RegisterCompleteObjectUndo(HexGrid.Get(), $"Remove Hex Tile(HexGrid): {targetCubeIndex}");
										HexGrid.Get().RemoveHexTile(hexTile.CubeIndex, false);
										Undo.DestroyObjectImmediate(hexTile.gameObject);
									}
								}

								Undo.RecordObject(tileManager, $"Remove Land Tile(TileManager): {landTileOnPoint.PlacedCubeIndex}");
								tileManager.RemoveLandTile(landTileOnPoint.PlacedCubeIndex, false);
								Undo.DestroyObjectImmediate(landTileOnPoint.gameObject);

								EditorUtility.SetDirty(tileManager);
								Undo.SetCurrentGroupName($"Remove land tile: {landTileOnPoint.PlacedCubeIndex}");

								// Tell the UI your event is the main one to use, it override the selection in  the scene view
								GUIUtility.hotControl = controlId;
								e.Use();
							}
						}
						// Add new tile.
						else
						{
							if (hexTileOnPoint != null)
							{
								if (landTileOnPoint == null)
								{
									Undo.IncrementCurrentGroup();

									Undo.RecordObject(tileManager, $"Create Land Tile(TileManager): {hexTileOnPoint.CubeIndex}");
									var newLandTile = tileManager.CreateTile(hexTileOnPoint);
									newLandTile.name = $"Land {hexTileOnPoint.CubeIndex.x},{hexTileOnPoint.CubeIndex.y},{hexTileOnPoint.CubeIndex.z}";
									Undo.RegisterCreatedObjectUndo(newLandTile.gameObject, $"Create Land Tile: {hexTileOnPoint.CubeIndex}");

									Undo.RecordObject(HexGrid.Get(), $"Create Hex Tile(HexGrid): {hexTileOnPoint.CubeIndex}");
									var newHexTiles = HexGrid.Get().AddTilesAroundTile(hexTileOnPoint);
									foreach (var newHexTile in newHexTiles.Values)
										Undo.RegisterCreatedObjectUndo(newHexTile.gameObject, $"Create Hex Tile: {newHexTile.CubeIndex}");

									EditorUtility.SetDirty(tileManager);
									Undo.SetCurrentGroupName($"Add land tile: {newLandTile.PlacedCubeIndex}");

									Selection.activeGameObject = newLandTile.gameObject;
									GUIUtility.hotControl = controlId;
									e.Use();
								}
								else
								{
									Selection.activeGameObject = landTileOnPoint.gameObject;
									GUIUtility.hotControl = controlId;
									e.Use();
								}
							}
						}
					}
					else if (isScrolled)
					{
						if (landTileOnPoint != null)
						{
							int placingLandTileRotationIndex = landTileOnPoint.PlacementRotationIndex;
							placingLandTileRotationIndex += scrollDirection;
							if (placingLandTileRotationIndex < 0) placingLandTileRotationIndex = 5;
							else if (placingLandTileRotationIndex > 5) placingLandTileRotationIndex = 0;

							Undo.IncrementCurrentGroup();

							Undo.RegisterFullObjectHierarchyUndo(landTileOnPoint.transform, $"Rotate Land Tile: {landTileOnPoint.PlacedCubeIndex}");
							landTileOnPoint.transform.localEulerAngles = Vector3.up * placingLandTileRotationIndex * 60f;

							Undo.RecordObject(landTileOnPoint, $"Rotate Index Land Tile: {landTileOnPoint.PlacedCubeIndex}");
							landTileOnPoint.OnPlacedOnTileEditor(landTileOnPoint.PlacedCubeIndex, placingLandTileRotationIndex);
							EditorUtility.SetDirty(landTileOnPoint);

							// Name undo group
							Undo.SetCurrentGroupName($"Rotate and rotation index change: {landTileOnPoint.PlacedCubeIndex}");

							GUIUtility.hotControl = controlId;
							e.Use();
						}
					}
				}
			}
			else if (_mode == TileVisualizerMode.FindPath)
			{
				if (isLeftMouseClicked || isRightMouseClicked)
				{
					var plane = new Plane(HexGrid.Get().transform.up, HexGrid.Get().transform.position);
					Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
					LandTile landTileOnPoint = null;
					HexTile hexTileOnPoint = null;
					if (plane.Raycast(ray, out float enter))
					{
						Vector3 hitPoint = ray.GetPoint(enter);
						hexTileOnPoint = HexGrid.Get().GetTileFromPosition(hitPoint);
						if (hexTileOnPoint != null)
						{
							landTileOnPoint = tileManager.GetPlacedLandTile(hexTileOnPoint.CubeIndex);
						}
					}

					if (isLeftMouseClicked)
					{
						_selectedLandTile = landTileOnPoint;
					}
					else if (_selectedLandTile != null && landTileOnPoint != null)
					{
						PathfindingHex pathfinding = new PathfindingHex(LandTilePartType.Rail);
						var paths = pathfinding.FindPath(_selectedLandTile.PlacedCubeIndex, landTileOnPoint.PlacedCubeIndex);
						if (paths != null && paths.Count > 1)
						{
							for (int index = 1; index < paths.Count; index++)
							{
								var firstPath = paths[index - 1];
								var secondPath = paths[index];

								var firstTile = tileManager.GetPlacedLandTile(firstPath.CubeIndex);
								var secondTile = tileManager.GetPlacedLandTile(secondPath.CubeIndex);

								Debug.DrawLine(firstTile.transform.position + Vector3.up * 1f, secondTile.transform.position + Vector3.up * 1f, Color.red, 5f);
							}
						}
					}

				}
			}
		}

		private void OnValidate()
		{
			EditorUtility.SetDirty(this);
		}
	}
}
#endif