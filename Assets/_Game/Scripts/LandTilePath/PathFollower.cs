using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    [RequireComponent(typeof(SplineFollower))]
    public class PathFollower : MonoBehaviour
    {
		[SerializeField] private LandTile _startLandTile;
		[SerializeField] private SplineComputer _startSpline;

		[SerializeField] private LandTile _targetLandTile;
		[SerializeField] private SplineComputer _targetSpline;


		private SplineFollower _follower;
		private float _followSpeed;

		private LandTile _currentLandTile;
		private PathSetting _currentPathSetting;

		// Move
		private PathfindingHex _pathfinding;
		private LandTile _moveTarget;
		private PathSetting _moveTargetPathSetting;
		private List<PathfindingHexTile> _movePath;
		private int _currentMovePathIndex;
		private int _followDirection = 1;
		private Node _lastVisitedNode;

		private void Awake()
		{
			_follower = GetComponent<SplineFollower>();
			_follower.follow = false;
			_follower.onNode += OnNodePassed;
			_followSpeed = Mathf.Abs(_follower.followSpeed);
			_pathfinding = new PathfindingHex(LandTilePartType.Rail);
		}

		private void Start()
		{
			InitializeOnTile(_startLandTile, _startSpline);

			if (_targetLandTile != null)
				Move(_targetLandTile, _targetSpline);
		}

		public void InitializeOnTile(LandTile landTile, SplineComputer splineComputer)
		{
			_currentLandTile = landTile;
			_currentPathSetting = landTile.GetPathFromSpline(splineComputer);

			_follower.spline = splineComputer;
			_follower.SetPercent(0.5f);
		}

		public bool Move(LandTile moveTarget, SplineComputer targetSplineComputer)
		{
			_moveTarget = moveTarget;
			_moveTargetPathSetting = moveTarget.GetPathFromSpline(targetSplineComputer);
			_currentMovePathIndex = 1;

			_movePath = _pathfinding.FindPath(_currentLandTile.PlacedCubeIndex, _moveTarget.PlacedCubeIndex);
			bool willMove = _movePath != null && _movePath.Count > 0;
			if (willMove)
			{
				var nextPathNode = _movePath[_currentMovePathIndex];
				_followDirection = GetMoveDirectionTo(_currentLandTile, _currentPathSetting, nextPathNode);
				_follower.followSpeed = _followSpeed * _followDirection;
			}
			
			_follower.follow = willMove;
			return willMove;
		}

		private void OnNodePassed(List<SplineTracer.NodeConnection> passed)
		{
			SplineTracer.NodeConnection nodeConnection = passed[0];
			if (_lastVisitedNode == nodeConnection.node) return;
			_lastVisitedNode = nodeConnection.node;

			if (_currentMovePathIndex >= _movePath.Count)
			{
				_follower.follow = false;
				return;
			}

			double nodePercent = (double)nodeConnection.point / (_follower.spline.pointCount - 1);
			double followerPercent = _follower.UnclipPercent(_follower.result.percent);
			float distancePastNode = _follower.spline.CalculateLength(nodePercent, followerPercent);

			// Find which connection to switch.
			PathfindingHexTile nodeToMove = _movePath[_currentMovePathIndex];
			LandTile tileToMove = TileManager.Get().GetPlacedLandTile(nodeToMove.CubeIndex);
			PathSetting pathSettingToMove = null;
			int followDirectionOnSpline = 1;
			if (_currentMovePathIndex + 1 < _movePath.Count)
			{
				pathSettingToMove = tileToMove.GetPathFromConnectingNeighbours(_currentLandTile.PlacedCubeIndex, _movePath[_currentMovePathIndex + 1].CubeIndex);
				followDirectionOnSpline = GetMoveDirectionTo(tileToMove, pathSettingToMove, _movePath[_currentMovePathIndex + 1]);
			}
			else
			{
				pathSettingToMove = tileToMove.GetPathFromSpline(_targetSpline);
				followDirectionOnSpline = GetMoveDirectionFrom(tileToMove, pathSettingToMove, _movePath[_currentMovePathIndex]);
			}

			_currentMovePathIndex++;
			_followDirection = followDirectionOnSpline;
			_currentLandTile = tileToMove;
			_currentPathSetting = pathSettingToMove;

			Node.Connection[] connections = nodeConnection.node.GetConnections();
			var newConnection = connections.First((x) => x.spline == pathSettingToMove.Path.SplineComputer);
			_follower.spline = newConnection.spline;
			double newNodePercent = (double)newConnection.pointIndex / (newConnection.spline.pointCount - 1);
			double newPercent = newConnection.spline.Travel(newNodePercent, distancePastNode, _follower.direction);
			_follower.SetPercent(newPercent);
			_follower.followSpeed = _followSpeed * _followDirection;
		}

		private int GetMoveDirectionTo(LandTile currentLandTile, PathSetting pathSetting, PathfindingHexTile toPathNode)
		{
			var directionToNextPath = (toPathNode.CameFromDirection + 3) % 6;
			int directionTo = currentLandTile.ConvertPartIndexToDirection(pathSetting.PartIndexTo);
			int direction = directionToNextPath == directionTo ? 1 : -1;
			return direction;
		}

		private int GetMoveDirectionFrom(LandTile currentLandTile, PathSetting pathSetting, PathfindingHexTile fromPathNode)
		{
			var directionFromPreviousPath = fromPathNode.CameFromDirection;
			int directionFrom = currentLandTile.ConvertPartIndexToDirection(pathSetting.PartIndexFrom);
			int direction = directionFromPreviousPath == directionFrom ? 1 : -1;
			return direction;
		}
	}
}