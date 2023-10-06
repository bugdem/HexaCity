using DG.Tweening;
using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
	public class PathJunctionConnection
	{
		public LandTilePart Part;
		public List<PathSetting> Paths;
	}

    [RequireComponent(typeof(Node))]
    public class PathJunction : MonoBehaviour
    {
		public LandTilePartType PathType { get; private set; }
		public PathJunctionConnection LandTileConnection1 { get; private set; }
		public PathJunctionConnection LandTileConnection2 { get; private set; }
		
		public Node Junction { get; private set; }

		public void Initialize(LandTilePartType pathType, PathJunctionConnection connection1, PathJunctionConnection connection2)
		{
			Junction = GetComponent<Node>();

			PathType = pathType;
			LandTileConnection1 = connection1;
			LandTileConnection2 = connection2;

			Vector3 connectionPoint1 = GetPointPosition(connection1);
			Vector3 connectionPoint2 = GetPointPosition(connection2);
			Vector3 junctionPoint = (connectionPoint1 + connectionPoint2) * .5f;
			transform.position = junctionPoint;

			AddConnection(LandTileConnection1);
			AddConnection(LandTileConnection2);

			var splineNodeConnection = Junction.GetConnections()[1];
			splineNodeConnection.invertTangents = !splineNodeConnection.invertTangents;
			Junction.UpdateConnectedComputers();
		}

		private void AddConnection(PathJunctionConnection connection)
		{
			foreach (var pathSetting in connection.Paths)
			{
				// Get point index on spline for connection. It is first or last point on spline.
				int pointIndex = pathSetting.PartIndexFrom == connection.Part.PartIndex ? 0 : pathSetting.Path.pointCount - 1;
				Junction.AddConnection(pathSetting.Path, pointIndex);
			}
		}

		private Vector3 GetPointPosition(PathJunctionConnection connection)
		{
			var pathSetting = connection.Paths[0];
			int pointIndex = pathSetting.PartIndexFrom == connection.Part.PartIndex ? 0 : pathSetting.Path.pointCount - 1;
			return pathSetting.Path.GetPoint(pointIndex).position;
		}
	}
}