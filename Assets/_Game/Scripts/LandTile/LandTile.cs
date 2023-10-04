using ClocknestGames.Library.Utils;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
	public enum LandTilePartType
	{
		Grass = 0,
		Forest,
		Mine,
		Farm,
		Rail,
		River,
		Water,
		House
	}

    public class LandTile : MonoBehaviour
    {
		[SerializeField] private Transform _partParent;
		[SerializeField] private Transform _pathParent;
		[InfoBox("Land Type for every part in hexagon tile. Last element is center part.")]
		[ListDrawerSettings(IsReadOnly = true)]
		[SerializeField] private List<LandTilePartType> _landTileParts = new() {
																		LandTilePartType.Grass,
																		LandTilePartType.Grass,
																		LandTilePartType.Grass,
																		LandTilePartType.Grass,
																		LandTilePartType.Grass,
																		LandTilePartType.Grass,
																		LandTilePartType.Grass
																	};

		[Header("Editor")]
		[SerializeField, ReadOnly] private Vector3Int _placedCubeIndex;
		[SerializeField, ReadOnly] private int _placementRotationIndex;
		[SerializeField, ReadOnly] private HexTile _placedTile;
		[SerializeField, ReadOnly] private List<LandTilePart> _parts;
		[SerializeField, ReadOnly] private List<PathSetting> _paths;

		public Vector3Int PlacedCubeIndex
		{
			get => _placedCubeIndex;
			private set => _placedCubeIndex = value;
		}

		public int PlacementRotationIndex
		{
			get => _placementRotationIndex;
			private set => _placementRotationIndex = value;
		}

		public HexTile PlacedTile
		{
			get => _placedTile;
			private set => _placedTile = value;
		}

		public bool IsPlaced => PlacedTile != null;



#if UNITY_EDITOR
		[HorizontalGroup("Split", 0.5f)]
		[Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1)]
		public void ChangeGeneratedTileEditor()
		{
			TileManager.Get().ChangeGeneratedTileEditor(_landTileParts);
			GenerateTile(_landTileParts);
		}

		[VerticalGroup("Split/right")]
		[Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
		public void GenerateTileEditor()
		{
			bool isGenerated = GenerateTile();
			transform.localScale = Vector3.one * HexGrid.Get().TileSize;
		}
#endif

		public bool GenerateTile(List<LandTilePartType> newTileParts)
		{
			_landTileParts = newTileParts;

			return GenerateTile();
		}

		public bool GenerateTile()
		{
			if (!ValidateTileSetting(_landTileParts))
			{
				Debug.LogWarning("Tile settings are not correct!");
				return false;
			}

			if (Application.isEditor)
			{
				_partParent.gameObject.RemoveAllChild(true);
				_pathParent.gameObject.RemoveAllChild(true);
			}
			else
			{
				_partParent.gameObject.RemoveAllChild(false);
				_pathParent.gameObject.RemoveAllChild(false);
			}

			_parts = new(7);
			_paths = null;

			bool pathExists = false;
			int pathFirstIndex = 0;
			LandTilePartType pathType = LandTilePartType.Rail;
			for (int partIndex = 0; partIndex <= 6; partIndex++)
			{
				var landTilePartType = _landTileParts[partIndex];
				var isCenterPart = partIndex == 6;
				float rotationAngle = 60f * partIndex;
				var landTilePart = TileManager.Get().CreateLandTilePart(landTilePartType, isCenterPart);
				landTilePart.transform.SetParent(_partParent);
				landTilePart.transform.localPosition = Vector3.zero;
				landTilePart.transform.localScale = Vector3.one;
				landTilePart.transform.localEulerAngles = !isCenterPart ? Vector3.up * rotationAngle : Vector3.zero;
				landTilePart.Initialize(this, partIndex);

				if (!isCenterPart && IsPathPart(landTilePartType))
				{
					if (!pathExists)
					{
						pathExists = true;
						pathType = landTilePartType;
						pathFirstIndex = partIndex;
						_paths = new();
					}
					else
					{
						var pathSetting = new PathSetting();
						int partDiff = partIndex - pathFirstIndex;
						if (partDiff >= 3)
						{
							pathSetting.PartIndexFrom = partIndex;
							pathSetting.PartIndexTo = pathFirstIndex;
							partDiff = 6 - partDiff;
						}
						else
						{
							pathSetting.PartIndexFrom = pathFirstIndex;
							pathSetting.PartIndexTo = partIndex;
						}

						pathSetting.Path = Instantiate(TileManager.Get().GetRoadPrefab(pathType, partDiff), _pathParent).GetComponent<SplineComputer>();
						pathSetting.Path.transform.localPosition = Vector3.zero;
						pathSetting.Path.transform.localEulerAngles = Vector3.up * pathSetting.PartIndexFrom * 60f;

						_paths.Add(pathSetting);
					}
				}

				_parts.Add(landTilePart);
			}

			for (int partIndex = 0; partIndex <= 6; partIndex++)
			{
				_parts[partIndex].Layout();
			}

			return true;
		}

		private bool IsPathPart(LandTilePartType partType)
		{
			return partType == LandTilePartType.Rail 
				|| partType == LandTilePartType.River;
		}

		public List<T> GetParts<T>() where T: LandTilePart
		{
			List<T> parts = new();
			foreach (var part in _parts)
			{
				if (part is T castedPart)
					parts.Add(castedPart);
			}

			return parts;
		}

		public Edge GetPartEdge(LandTilePart tilePart)
		{
			return HexGrid.Get().GetEdge(transform.position, tilePart.PartIndex);
		}

		public bool ValidateTileSetting(List<LandTilePartType> landTiles)
		{
			return true;
			/*
			int railCount = 0;
			int riverCount = 0;
			for (int index = 0; index < 6; index++)
			{
				if (landTiles[index] == LandTilePartType.Rail)
					railCount++;
				else if (landTiles[index] == LandTilePartType.River)
					riverCount++;
			}

			if (railCount == 1 || riverCount == 1)
				*/
		}

		public void OnPlacedOnTileEditor(Vector3Int cubeIndex, int rotationIndex)
		{
			PlacementRotationIndex = rotationIndex;
			PlacedCubeIndex = cubeIndex;
			PlacedTile = HexGrid.Get().GetTile(cubeIndex);
		}

		public void OnPlacedOnTile(HexTile tile, int rotationIndex)
		{
			PlacementRotationIndex = rotationIndex;
			PlacedCubeIndex = tile.CubeIndex;
			PlacedTile = tile;

			if (_paths != null)
			{
				// Add path node junctions.
				for (int index = 0; index < _paths.Count; index++)
				{
					var path = _paths[index];

				}
			}

			for (int partIndex = 0; partIndex <= 6; partIndex++)
			{
				_parts[partIndex].OnPlaced();
			}
		}

		public int ConvertDirectionToPartIndex(int direction)
		{
			// If center tile, return it.
			if (direction == 6) return direction;
			// Remove rotation from direction.
			return (6 - direction + PlacementRotationIndex) % 6;
		}

		public int ConvertPartIndexToDirection(int partIndex)
		{
			// If center tile, return it.
			if (partIndex == 6) return partIndex;
			// Add rotation to part index.
			return (PlacementRotationIndex + partIndex) % 6;
		}

		public LandTilePart GetLandTilePartOnDirection(int direction)
		{
			return _parts[ConvertDirectionToPartIndex(direction)];
		}

		public LandTilePart GetNeighbourLandTilePart(int direction)
		{
			var partEdge = HexGrid.Get().GetEdge(transform.position, direction);
			var neighbourEdge = HexGrid.Get().GetNeigbourEdge(partEdge);
			var neighbourLandTile = TileManager.Get().GetPlacedLandTile(neighbourEdge.hexCubeIndex);
			if (neighbourLandTile != null)
			{
				return neighbourLandTile.GetLandTilePartOnDirection(neighbourEdge.index);
			}
			return null;
		}
	}
}