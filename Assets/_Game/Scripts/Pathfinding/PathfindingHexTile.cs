using UnityEngine;

namespace ClocknestGames.Game.Core
{
    public class PathfindingHexTile
    {
		public Vector3Int CubeIndex;

		public int GCost;
		public int HCost;
		public int FCost { get; private set; }

		public bool IsWalkable { get; private set; }
		public PathfindingHexTile CameFromNode;

		public PathfindingHexTile(Vector3Int cubeIndex)
		{
			CubeIndex = cubeIndex;
			IsWalkable = true;

			GCost = 999999; 
			HCost = 999999;
		}

		public void CalculateFCost()
		{
			FCost = GCost + HCost;
		}

		public void SetIsWalkable(bool isWalkable)
		{
			IsWalkable = isWalkable;
		}
	}
}