using ClocknestGames.Library.Utils;
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

		public HexTile PlacedTile { get; private set; }
		public int PlacementRotationIndex { get; private set; }
		public List<LandTilePart> Parts { get; private set; } = new(7);

		public bool GenerateTile(List<LandTilePartType> newTileParts)
		{
			_landTileParts = newTileParts;

			return GenerateTile();
		}

		[Button]
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
				Parts.Clear();
			}
			else
			{
				_partParent.gameObject.RemoveAllChild(false);
				Parts.Clear();
			}

			int edgeStartIndex = 0;
			for (int partIndex = 0; partIndex <= 6; partIndex++)
			{
				var landTileType = _landTileParts[partIndex];
				var isCenterPart = partIndex == 6;
				var landTilePart = TileManager.Instance.CreateLandTilePart(landTileType, isCenterPart);
				landTilePart.transform.SetParent(_partParent);
				landTilePart.transform.localPosition = Vector3.zero;
				landTilePart.transform.localEulerAngles = !isCenterPart ? Vector3.up * ((60f * (partIndex + edgeStartIndex)) % 360f) : Vector3.zero;
				landTilePart.Initialize(this, partIndex);

				Parts.Add(landTilePart);
			}

			for (int partIndex = 0; partIndex <= 6; partIndex++)
			{
				Parts[partIndex].Layout();
			}

			return true;
		}

		public List<T> GetParts<T>() where T: LandTilePart
		{
			List<T> parts = new();
			foreach (var part in Parts)
			{
				if (part is T castedPart)
					parts.Add(castedPart);
			}

			return parts;
		}

		public Edge GetPartEdge(LandTilePart tilePart)
		{
			return HexGrid.Instance.GetEdge(transform.position, tilePart.PartIndex);
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

		public void OnPlacedOnTile(HexTile tile, int rotationIndex)
		{
			PlacedTile = tile;
			PlacementRotationIndex = rotationIndex;
		}
	}
}