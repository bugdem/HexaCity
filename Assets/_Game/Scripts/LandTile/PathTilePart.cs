using ClocknestGames.Library.Utils;
using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    public abstract class PathTilePart : LandTilePart
    {
        [SerializeField] private SplineComputer _spline;

		public SplineComputer Spline => _spline;

		protected override void OnLayout()
		{
			base.OnLayout();

			if (IsCenterPart)
			{
				Node nodeConnector = new GameObject("NodeConnector").AddComponent<Node>();
				nodeConnector.type = Node.Type.Smooth;
				nodeConnector.transform.SetParent(transform.parent);
				nodeConnector.transform.position = LandTile.transform.position.SetY(Spline.transform.position.y);

				var pathTileParts = LandTile.GetParts<PathTilePart>();
				foreach (var tilePart in pathTileParts)
				{
					nodeConnector.AddConnection(tilePart.Spline, 2);
				}
				nodeConnector.UpdatePoints();
			}
			else
			{
				Edge edge = GetEdge();
				Vector3 startPoint = ((edge.point1 + edge.point2) * .5f).SetY(Spline.transform.position.y);
				Vector3 endPoint = Vector3.Lerp(startPoint, LandTile.transform.position.SetY(Spline.transform.position.y), .75f);
				Vector3 middlePoint = ((startPoint + endPoint) * .5f).SetY(Spline.transform.position.y);
				SplinePoint splineStartPoint = new SplinePoint(startPoint);
				SplinePoint splineMiddlePoint = new SplinePoint(middlePoint);
				SplinePoint splineEndPoint = new SplinePoint(endPoint);

				_spline.SetPoint(0, splineStartPoint);
				_spline.SetPoint(1, splineMiddlePoint);
				_spline.SetPoint(2, splineEndPoint);
			}
		}
	}
}