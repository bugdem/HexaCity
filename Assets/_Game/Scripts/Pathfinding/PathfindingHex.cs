using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    public class PathfindingHex
    {
		private struct PathInfo
		{
			public Vector3 CubeIndex;
			public byte Direction;

			public PathInfo(Vector3 cubeIndex, byte direction)
			{
				CubeIndex = cubeIndex;
				Direction = direction;
			}
		}

		public LandTilePartType PathType { get; private set; }

		private List<PathfindingHexTile> _openList;
		private Dictionary<PathInfo, PathfindingHexTile> _closedList;
		private Dictionary<Vector3Int, PathfindingHexTile> _allNodes;

		public PathfindingHex(LandTilePartType pathType)
		{
			PathType = pathType;
		}

		public List<PathfindingHexTile> FindPath(Vector3Int startCubeIndex, Vector3Int endCubeIndex)
		{
			PathfindingHexTile startNode = new(startCubeIndex);
			PathfindingHexTile endNode = new(endCubeIndex);

			_openList = new List<PathfindingHexTile> { startNode };
			_closedList = new Dictionary<PathInfo, PathfindingHexTile>();
			_allNodes = new Dictionary<Vector3Int, PathfindingHexTile>
			{
				{  startCubeIndex, startNode },
				{  endCubeIndex, endNode }
			};

			startNode.GCost = 0;
			startNode.HCost = CalculateDistanceCost(startNode, endNode);
			startNode.CalculateFCost();

			while (_openList.Count > 0)
			{
				PathfindingHexTile currentNode = GetLowestFCostNode(_openList);
				if (currentNode.CubeIndex == endNode.CubeIndex)
				{
					// Reached final node
					return CalculatePath(endNode);
				}

				_openList.Remove(currentNode);

				var currentTile = TileManager.Get().GetPlacedLandTile(currentNode.CubeIndex);

				ProcessNeighbours(currentNode.CubeIndex, (neighbourTile, directionToNeighbour) =>
				{
					var pathInfo = new PathInfo(currentNode.CubeIndex, (byte)directionToNeighbour);
					if (_closedList.ContainsKey(pathInfo)) return;

					if (!_allNodes.TryGetValue(neighbourTile.PlacedCubeIndex, out PathfindingHexTile neighbourNode))
					{
						neighbourNode = new PathfindingHexTile(neighbourTile.PlacedCubeIndex);
						_allNodes.Add(neighbourTile.PlacedCubeIndex, neighbourNode);
					}

					bool hasPathToNeighbour = currentTile.HasPathTileOnDirection(directionToNeighbour, PathType);
					byte neighbourDirection = (byte)((directionToNeighbour + 3) % 6);
					_closedList.Add(new PathInfo(neighbourNode.CubeIndex, neighbourDirection), neighbourNode);
					_closedList.Add(new PathInfo(currentNode.CubeIndex, (byte)directionToNeighbour), currentNode);

					if (!hasPathToNeighbour)
					{
						return;
					}

					int tentativeGCost = currentNode.GCost + CalculateDistanceCost(currentNode, neighbourNode);
					if (tentativeGCost < neighbourNode.GCost)
					{
						neighbourNode.CameFromNode = currentNode;
						neighbourNode.GCost = tentativeGCost;
						neighbourNode.HCost = CalculateDistanceCost(neighbourNode, endNode);
						neighbourNode.CalculateFCost();

						if (!_openList.Contains(neighbourNode))
						{
							_openList.Add(neighbourNode);
						}
					}
				});
			}

			// Out of nodes on the openList
			return null;
		}

		private void ProcessNeighbours(Vector3Int cubeIndex, Action<LandTile,int> onNeighbourTile)
		{
			for (int index = 0; index < 6; index++)
			{
				Vector3Int neighbourCubeIndex = HexGrid.GetNeighbourCubeIndex(cubeIndex, index);
				var landTile = TileManager.Get().GetPlacedLandTile(neighbourCubeIndex);
				if (landTile != null) onNeighbourTile.Invoke(landTile, index);
			}
		}

		private List<PathfindingHexTile> CalculatePath(PathfindingHexTile endNode)
		{
			List<PathfindingHexTile> path = new() { endNode };
			PathfindingHexTile currentNode = endNode;
			while (currentNode.CameFromNode != null)
			{
				path.Add(currentNode.CameFromNode);
				currentNode = currentNode.CameFromNode;
			}
			path.Reverse();
			return path;
		}

		private int CalculateDistanceCost(PathfindingHexTile a, PathfindingHexTile b)
		{
			return HexGrid.GetDistance(a.CubeIndex, b.CubeIndex);
		}

		private PathfindingHexTile GetLowestFCostNode(List<PathfindingHexTile> pathNodeList)
		{
			PathfindingHexTile lowestFCostNode = pathNodeList[0];
			for (int i = 1; i < pathNodeList.Count; i++)
			{
				if (pathNodeList[i].FCost < lowestFCostNode.FCost)
				{
					lowestFCostNode = pathNodeList[i];
				}
			}
			return lowestFCostNode;
		}
	}
}