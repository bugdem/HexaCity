using ClocknestGames.Library.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace ClocknestGames.Game.Core
{
	[Serializable]
	public class HexGridDictionary : UnitySerializedDictionary<Vector3Int, HexTile> { }

	// Hexagon grid generator class.
	// To see formulas used, check https://www.redblobgames.com/grids/hexagons/
	public class HexGrid : Singleton<HexGrid>
    {
		[SerializeField, Min(1)] private int _radiusOnStart = 2;
		[SerializeField, Min(1)] private int _radiusOnPlace = 2;
		[SerializeField] private Vector3Int _border = new Vector3Int(10, 10, 10);
		[SerializeField] private float _tileSize = 1f;
		[SerializeField] private bool _scaleHexWithTileSize = false;
		[SerializeField, EnableIf("_scaleHexWithTileSize"), Range(0.1f, 1f)] private float _hexScale = 1f;
		[SerializeField] private HexTile _tilePrefab;
		[SerializeField] private HexGridDictionary _tiles;

		public float TileSize => _tileSize;
		public float TileRadius => TileSize * .5f;

		// private Dictionary<Vector3Int, HexTile> _tiles = new();
		private Layout _gridLayout;

		[HorizontalGroup("Split", 0.5f)]
		[Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1)]
		private void LayoutGrid()
		{
			ClearGrid();

			if (_tilePrefab == null) return;

			// Place hex tiles by horizontal and vertical.
			/*
			for (int x = 0; x < _gridSize.x; x++)
			{
				for (int y = 0; y < _gridSize.y; y++)
				{
					var tileIndex = new Vector2Int(x, y);
					var newTile = Instantiate(_tilePrefab, transform);
					newTile.gameObject.name = $"Hex {x},{y}";
					newTile.transform.position = GetTilePositionFromIndex(tileIndex);
					newTile.SetTile(tileIndex);

					_tiles.Add(GetCubeIndex(tileIndex), newTile);
				}
			}
			*/

			AddTilesAroundTile(Vector3Int.zero, _radiusOnStart);
		}

		[VerticalGroup("Split/right")]
		[Button(ButtonSizes.Large), GUIColor(1, 0, 0)]
		private void ClearGrid()
		{
			bool destroyChildImmediately = !Application.isPlaying;
			transform.gameObject.RemoveAllChild(destroyChildImmediately);

			_gridLayout = CreateLayout();

			_tiles.Clear();
		}

		public void AddTilesAroundTile(HexTile aroundTile)
		{
			AddTilesAroundTile(aroundTile, _radiusOnPlace);
		}

		public void AddTilesAroundTile(HexTile aroundTile, int radius)
		{
			AddTilesAroundTile(GetCubeIndex(aroundTile), radius);
		}

		public HexTile GetTile(Vector3Int cubeIndex)
		{
			_tiles.TryGetValue(cubeIndex, out HexTile hexTile);
			return hexTile;
		}

		public HexTile GetTileFromPosition(Vector3 position)
		{
			var hex = GetHexFromPosition(position);
			// Debug.Log($"HexQRS: {hex.q},{hex.r},{hex.s}");
			_tiles.TryGetValue(hex.ToVector3Int(), out HexTile hexTile);
			return hexTile;
		}

		public List<HexTile> GetTilesInsideRadius(HexTile aroundTile, int radius)
		{
			return GetTilesInsideRadius(aroundTile.CubeIndex, radius);
		}

		private List<HexTile> GetTilesInsideRadius(Vector3Int aroundCubeIndex, int radius)
		{
			List<HexTile> tiles = new();

			for (int ringIndex = 0; ringIndex < radius + 1; ringIndex ++)
			{
				tiles.AddRange(GetTilesOnRadius(aroundCubeIndex, ringIndex));
			}

			return tiles;
		}

		public List<HexTile> GetTilesOnRadius(HexTile aroundTile, int radius)
		{
			return GetTilesOnRadius(aroundTile.CubeIndex, radius);
		}

		private List<HexTile> GetTilesOnRadius(Vector3Int aroundCubeIndex, int radius)
		{
			List<HexTile> tiles = new();

			int layerCount = radius;
			Hex startHex = aroundCubeIndex.ToHex();
			Hex currentHex = startHex.Add(Hex.directions[4].Scale(layerCount));

			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < layerCount; j++)
				{
					Vector3Int cubeIndex = currentHex.ToVector3Int();
					if (_tiles.TryGetValue(cubeIndex, out HexTile hexTile))
						tiles.Add(hexTile);

					currentHex = currentHex.Neighbor(i);
				}
			}

			return tiles;
		}

		public void AddTilesAroundTile(Vector3Int aroundCubeIndex, int radius)
		{
			int layerCount = radius;
			Hex startHex = aroundCubeIndex.ToHex();

			for (int x = -layerCount; x <= layerCount; x++)
			{
				for (int y = Mathf.Max(-layerCount, -x - layerCount); y <= Mathf.Min(layerCount, -x + layerCount); y++)
				{
					var z = -x - y;

					var aroundHex = (new Hex(x, y, z)).Add(startHex);
					var cubeIndex = aroundHex.ToVector3Int();
					if (_tiles.ContainsKey(cubeIndex)) continue;
					if (cubeIndex.x < -_border.x || cubeIndex.x > _border.x
						|| cubeIndex.y < -_border.y || cubeIndex.y > _border.y
						|| cubeIndex.z < -_border.z || cubeIndex.z > _border.z)
						continue;

					HexTile newTile;
					if (Application.isEditor)
						newTile = UnityEditor.PrefabUtility.InstantiatePrefab(_tilePrefab, transform) as HexTile;
					else
						newTile = Instantiate(_tilePrefab, transform);

					newTile.gameObject.name = $"Hex {cubeIndex.x},{cubeIndex.y},{cubeIndex.z}";
					newTile.transform.position = GetTilePositionFromCubeIndex(cubeIndex);
					if (_scaleHexWithTileSize)
						newTile.transform.localScale = GetTileScale() * _hexScale;
					newTile.SetTile(cubeIndex);

					_tiles.Add(cubeIndex, newTile);
				}
			}
		}

		public Vector3 GetTileScale()
		{
			return Vector3.one * TileSize;
		}

		private Vector3 GetTilePositionFromIndex(Vector2Int tileIndex)
		{
			/*
			int column = tileIndex.x;
			int row = tileIndex.y;

			bool shouldOffset = (column % 2) == 0;
			float width = 2f * _tileSize;
			float height = Mathf.Sqrt(3f) * _tileSize;

			float horizontalDistance = width * 0.75f;
			float verticalDistance = height;

			float offset = shouldOffset ? height * .5f : 0f;
			float xPosition = column * horizontalDistance;
			float yPosition = (row * verticalDistance) - offset;

			return new Vector3(xPosition, 0f, -yPosition);
			*/

			var cubeIndex = GetCubeIndex(tileIndex);
			return GetTilePositionFromCubeIndex(cubeIndex);
		}

		private Vector3 GetTilePositionFromCubeIndex(Vector3Int cubeIndex)
		{
			var hex = cubeIndex.ToHex();
			var point = GetLayout().HexToPixel(hex);
			return new Vector3((float)point.x, transform.position.y, (float)point.y);
		}

		public Vector3Int GetCubeIndexFromPosition(Vector3 position)
		{
			return GetHexFromPosition(position).ToVector3Int();
		}

		private Hex GetHexFromPosition(Vector3 position)
		{
			return GetLayout().PixelToHex(new Point(position.x, position.z)).HexRound();
		}

		public Vector3 GetPositionFromCubeIndex(Vector3Int cubeIndex)
		{
			return GetPositionFromHex(cubeIndex.ToHex());
		}

		private Vector3 GetPositionFromHex(Hex hex)
		{
			var point = GetLayout().HexToPixel(hex);
			return new Vector3((float)point.x, transform.position.y, (float)point.y);
		}

		private Layout GetLayout()
		{
			return CGExec.RunInMode<Layout>(() => _gridLayout, () => CreateLayout());
		}

		private Layout CreateLayout()
		{
			return new Layout(Layout.flat, new Point(TileRadius, TileRadius), new Point(transform.position.x, transform.position.z));
		}

		// Converts tile index with 2 axes into cube index with 3 axes.
		// Cube index(coordinate) provides more operations such as add/subtract corrdinates, multiply/divide by a scalar.
		// For more, search Cube coordinates for hexagons in https://www.redblobgames.com/grids/hexagons/.
		private Vector3Int GetCubeIndex(Vector2Int tileIndex)
		{
			var q = tileIndex.x - (tileIndex.y + (tileIndex.y % 2)) / 2;
			var r = tileIndex.y;
			return new Vector3Int(q, r, -q-r);
		}

		private Vector3Int GetCubeIndex(HexTile hexTile)
		{
			// return GetCubeIndex(hexTile.TileIndex);
			return hexTile.CubeIndex;
		}

		public List<Vector3> GetCorners(HexTile hexTile)
		{
			List<Vector3> corners = new();
			Hex centerTile = hexTile.CubeIndex.ToHex();
			Point center = GetLayout().HexToPixel(centerTile);
			for (int i = 0; i < 6; i++)
			{
				Point offset = GetLayout().HexCornerOffset(i);
				corners.Add(new Vector3((float)(center.x + offset.x), transform.position.y, (float)(center.y + offset.y)));
			}
			return corners;
		}

		public Edge GetEdge(HexTile hexTile, int edgeIndex)
		{
			return GetEdge(hexTile.transform.position, edgeIndex);
		}

		public Edge GetEdge(Vector3 hexPosition, int edgeIndex)
		{
			Vector3 point1 = GetCornerPoint(hexPosition, edgeIndex);
			Vector3 point2 = GetCornerPoint(hexPosition, (edgeIndex + 1) % 6);
			return new Edge(GetCubeIndexFromPosition(hexPosition), edgeIndex, point1, point2);
		}

		public Vector3 GedEdgeCenter(Vector3 worldPosition, int edgeIndex)
		{
			var edge = GetEdge(worldPosition, edgeIndex);
			return (edge.point1 + edge.point2) * .5f;
		}

		public Vector3 GetCornerPoint(Vector3 worldPosition, int pointIndex)
		{
			Point center = new Point(worldPosition.x, worldPosition.z);
			Point offset = GetLayout().HexCornerOffset(pointIndex);
			return new Vector3((float)(center.x + offset.x), worldPosition.y, (float)(center.y + offset.y));
		}

		public Vector3 GetCornerPoint(HexTile hexTile, int pointIndex)
		{
			return GetCornerPoint(hexTile.transform.position, pointIndex);
			/*
			Hex centerTile = hexTile.CubeIndex.ToHex();
			Point center = _gridLayout.HexToPixel(centerTile);
			Point offset = _gridLayout.HexCornerOffset(pointIndex);
			return new Vector3((float)(center.x + offset.x), hexTile.transform.position.y, (float)(center.y + offset.y));
			*/
		}

		public Edge GetNeigbourEdge(Edge edge)
		{
			int edgeIndex = (edge.index + 3) % 6;
			Hex currentHex = edge.hexCubeIndex.ToHex();
			Hex neighbourHex = currentHex.Neighbor((edge.index + 1) % 6);
			return GetEdge(GetPositionFromHex(neighbourHex), edgeIndex);

			/*
			if (_tiles.TryGetValue(neighbourHex.ToVector3Int(), out HexTile neighbourTile))
				return GetEdge(neighbourTile, edgeIndex);

			return new Edge();
			*/
		}

		private void OnEnable()
		{
			LayoutGrid();
		}

		private void OnValidate()
		{
			if (Application.isPlaying)
			{
				LayoutGrid();
			}
		}
	}

	static class Extensions
	{
		public static Vector3Int ToVector3Int(this Hex hex)
		{
			return new Vector3Int(hex.q, hex.r, hex.s);
		}

		public static Hex ToHex(this Vector3Int vector3Int)
		{
			return new Hex(vector3Int.x, vector3Int.y, vector3Int.z);
		}
	}

	public struct Edge
	{
		public Edge(Vector3Int hexCubeIndex, int index, Vector3 point1, Vector3 point2)
		{
			this.hexCubeIndex = hexCubeIndex;
			this.index = index;
			this.point1 = point1;
			this.point2 = point2;
		}
		public readonly Vector3Int hexCubeIndex;
		public readonly int index;
		public readonly Vector3 point1;
		public readonly Vector3 point2;
	}


	struct Point
	{
		public Point(double x, double y)
		{
			this.x = x;
			this.y = y;
		}
		public readonly double x;
		public readonly double y;
	}

	struct Hex
	{
		public Hex(int q, int r, int s)
		{
			this.q = q;
			this.r = r;
			this.s = s;
			if (q + r + s != 0) throw new ArgumentException("q + r + s must be 0");
		}
		public readonly int q;
		public readonly int r;
		public readonly int s;

		public Hex Add(Hex b)
		{
			return new Hex(q + b.q, r + b.r, s + b.s);
		}


		public Hex Subtract(Hex b)
		{
			return new Hex(q - b.q, r - b.r, s - b.s);
		}


		public Hex Scale(int k)
		{
			return new Hex(q * k, r * k, s * k);
		}


		public Hex RotateLeft()
		{
			return new Hex(-s, -q, -r);
		}


		public Hex RotateRight()
		{
			return new Hex(-r, -s, -q);
		}

		static public List<Hex> directions = new List<Hex> { new Hex(1, 0, -1), new Hex(1, -1, 0), new Hex(0, -1, 1), new Hex(-1, 0, 1), new Hex(-1, 1, 0), new Hex(0, 1, -1) };

		static public Hex Direction(int direction)
		{
			return Hex.directions[direction];
		}


		public Hex Neighbor(int direction)
		{
			return Add(Hex.Direction(direction));
		}

		static public List<Hex> diagonals = new List<Hex> { new Hex(2, -1, -1), new Hex(1, -2, 1), new Hex(-1, -1, 2), new Hex(-2, 1, 1), new Hex(-1, 2, -1), new Hex(1, 1, -2) };

		public Hex DiagonalNeighbor(int direction)
		{
			return Add(Hex.diagonals[direction]);
		}

		public int Length()
		{
			return (int)((Math.Abs(q) + Math.Abs(r) + Math.Abs(s)) / 2);
		}


		public int Distance(Hex b)
		{
			return Subtract(b).Length();
		}

	}

	struct FractionalHex
	{
		public FractionalHex(double q, double r, double s)
		{
			this.q = q;
			this.r = r;
			this.s = s;
			if (Math.Round(q + r + s) != 0) throw new ArgumentException("q + r + s must be 0");
		}
		public readonly double q;
		public readonly double r;
		public readonly double s;

		public Hex HexRound()
		{
			int qi = (int)(Math.Round(q));
			int ri = (int)(Math.Round(r));
			int si = (int)(Math.Round(s));
			double q_diff = Math.Abs(qi - q);
			double r_diff = Math.Abs(ri - r);
			double s_diff = Math.Abs(si - s);
			if (q_diff > r_diff && q_diff > s_diff)
			{
				qi = -ri - si;
			}
			else
				if (r_diff > s_diff)
			{
				ri = -qi - si;
			}
			else
			{
				si = -qi - ri;
			}
			return new Hex(qi, ri, si);
		}


		public FractionalHex HexLerp(FractionalHex b, double t)
		{
			return new FractionalHex(q * (1.0 - t) + b.q * t, r * (1.0 - t) + b.r * t, s * (1.0 - t) + b.s * t);
		}


		static public List<Hex> HexLinedraw(Hex a, Hex b)
		{
			int N = a.Distance(b);
			FractionalHex a_nudge = new FractionalHex(a.q + 1e-06, a.r + 1e-06, a.s - 2e-06);
			FractionalHex b_nudge = new FractionalHex(b.q + 1e-06, b.r + 1e-06, b.s - 2e-06);
			List<Hex> results = new List<Hex> { };
			double step = 1.0 / Math.Max(N, 1);
			for (int i = 0; i <= N; i++)
			{
				results.Add(a_nudge.HexLerp(b_nudge, step * i).HexRound());
			}
			return results;
		}

	}

	struct OffsetCoord
	{
		public OffsetCoord(int col, int row)
		{
			this.col = col;
			this.row = row;
		}
		public readonly int col;
		public readonly int row;
		static public int EVEN = 1;
		static public int ODD = -1;

		static public OffsetCoord QoffsetFromCube(int offset, Hex h)
		{
			int col = h.q;
			int row = h.r + (int)((h.q + offset * (h.q & 1)) / 2);
			if (offset != OffsetCoord.EVEN && offset != OffsetCoord.ODD)
			{
				throw new ArgumentException("offset must be EVEN (+1) or ODD (-1)");
			}
			return new OffsetCoord(col, row);
		}


		static public Hex QoffsetToCube(int offset, OffsetCoord h)
		{
			int q = h.col;
			int r = h.row - (int)((h.col + offset * (h.col & 1)) / 2);
			int s = -q - r;
			if (offset != OffsetCoord.EVEN && offset != OffsetCoord.ODD)
			{
				throw new ArgumentException("offset must be EVEN (+1) or ODD (-1)");
			}
			return new Hex(q, r, s);
		}


		static public OffsetCoord RoffsetFromCube(int offset, Hex h)
		{
			int col = h.q + (int)((h.r + offset * (h.r & 1)) / 2);
			int row = h.r;
			if (offset != OffsetCoord.EVEN && offset != OffsetCoord.ODD)
			{
				throw new ArgumentException("offset must be EVEN (+1) or ODD (-1)");
			}
			return new OffsetCoord(col, row);
		}


		static public Hex RoffsetToCube(int offset, OffsetCoord h)
		{
			int q = h.col - (int)((h.row + offset * (h.row & 1)) / 2);
			int r = h.row;
			int s = -q - r;
			if (offset != OffsetCoord.EVEN && offset != OffsetCoord.ODD)
			{
				throw new ArgumentException("offset must be EVEN (+1) or ODD (-1)");
			}
			return new Hex(q, r, s);
		}

	}

	struct DoubledCoord
	{
		public DoubledCoord(int col, int row)
		{
			this.col = col;
			this.row = row;
		}
		public readonly int col;
		public readonly int row;

		static public DoubledCoord QdoubledFromCube(Hex h)
		{
			int col = h.q;
			int row = 2 * h.r + h.q;
			return new DoubledCoord(col, row);
		}


		public Hex QdoubledToCube()
		{
			int q = col;
			int r = (int)((row - col) / 2);
			int s = -q - r;
			return new Hex(q, r, s);
		}


		static public DoubledCoord RdoubledFromCube(Hex h)
		{
			int col = 2 * h.q + h.r;
			int row = h.r;
			return new DoubledCoord(col, row);
		}


		public Hex RdoubledToCube()
		{
			int q = (int)((col - row) / 2);
			int r = row;
			int s = -q - r;
			return new Hex(q, r, s);
		}

	}

	struct Orientation
	{
		public Orientation(double f0, double f1, double f2, double f3, double b0, double b1, double b2, double b3, double start_angle)
		{
			this.f0 = f0;
			this.f1 = f1;
			this.f2 = f2;
			this.f3 = f3;
			this.b0 = b0;
			this.b1 = b1;
			this.b2 = b2;
			this.b3 = b3;
			this.start_angle = start_angle;
		}
		public readonly double f0;
		public readonly double f1;
		public readonly double f2;
		public readonly double f3;
		public readonly double b0;
		public readonly double b1;
		public readonly double b2;
		public readonly double b3;
		public readonly double start_angle;
	}

	struct Layout
	{
		public Layout(Orientation orientation, Point size, Point origin)
		{
			this.orientation = orientation;
			this.size = size;
			this.origin = origin;
		}
		public readonly Orientation orientation;
		public readonly Point size;
		public readonly Point origin;
		static public Orientation pointy = new Orientation(Math.Sqrt(3.0), Math.Sqrt(3.0) / 2.0, 0.0, 3.0 / 2.0, Math.Sqrt(3.0) / 3.0, -1.0 / 3.0, 0.0, 2.0 / 3.0, 0.5);
		static public Orientation flat = new Orientation(3.0 / 2.0, 0.0, Math.Sqrt(3.0) / 2.0, Math.Sqrt(3.0), 2.0 / 3.0, 0.0, -1.0 / 3.0, Math.Sqrt(3.0) / 3.0, 0.0);

		public Point HexToPixel(Hex h)
		{
			Orientation M = orientation;
			double x = (M.f0 * h.q + M.f1 * h.r) * size.x;
			double y = (M.f2 * h.q + M.f3 * h.r) * size.y;
			return new Point(x + origin.x, y + origin.y);
		}


		public FractionalHex PixelToHex(Point p)
		{
			Orientation M = orientation;
			Point pt = new Point((p.x - origin.x) / size.x, (p.y - origin.y) / size.y);
			double q = M.b0 * pt.x + M.b1 * pt.y;
			double r = M.b2 * pt.x + M.b3 * pt.y;
			return new FractionalHex(q, r, -q - r);
		}


		public Point HexCornerOffset(int corner)
		{
			Orientation M = orientation;
			double angle = 2.0 * Math.PI * (M.start_angle - corner) / 6.0;
			return new Point(size.x * Math.Cos(angle), size.y * Math.Sin(angle));
		}


		public List<Point> PolygonCorners(Hex h)
		{
			List<Point> corners = new List<Point> { };
			Point center = HexToPixel(h);
			for (int i = 0; i < 6; i++)
			{
				Point offset = HexCornerOffset(i);
				corners.Add(new Point(center.x + offset.x, center.y + offset.y));
			}
			return corners;
		}

	}

	// Tests
	struct Tests
	{
		static public void EqualHex(String name, Hex a, Hex b)
		{
			if (!(a.q == b.q && a.s == b.s && a.r == b.r))
			{
				Tests.Complain(name);
			}
		}

		static public void EqualOffsetcoord(String name, OffsetCoord a, OffsetCoord b)
		{
			if (!(a.col == b.col && a.row == b.row))
			{
				Tests.Complain(name);
			}
		}

		static public void EqualDoubledcoord(String name, DoubledCoord a, DoubledCoord b)
		{
			if (!(a.col == b.col && a.row == b.row))
			{
				Tests.Complain(name);
			}
		}

		static public void EqualInt(String name, int a, int b)
		{
			if (!(a == b))
			{
				Tests.Complain(name);
			}
		}

		static public void EqualHexArray(String name, List<Hex> a, List<Hex> b)
		{
			Tests.EqualInt(name, a.Count, b.Count);
			for (int i = 0; i < a.Count; i++)
			{
				Tests.EqualHex(name, a[i], b[i]);
			}
		}

		static public void TestHexArithmetic()
		{
			Tests.EqualHex("hex_add", new Hex(4, -10, 6), new Hex(1, -3, 2).Add(new Hex(3, -7, 4)));
			Tests.EqualHex("hex_subtract", new Hex(-2, 4, -2), new Hex(1, -3, 2).Subtract(new Hex(3, -7, 4)));
		}

		static public void TestHexDirection()
		{
			Tests.EqualHex("hex_direction", new Hex(0, -1, 1), Hex.Direction(2));
		}

		static public void TestHexNeighbor()
		{
			Tests.EqualHex("hex_neighbor", new Hex(1, -3, 2), new Hex(1, -2, 1).Neighbor(2));
		}

		static public void TestHexDiagonal()
		{
			Tests.EqualHex("hex_diagonal", new Hex(-1, -1, 2), new Hex(1, -2, 1).DiagonalNeighbor(3));
		}

		static public void TestHexDistance()
		{
			Tests.EqualInt("hex_distance", 7, new Hex(3, -7, 4).Distance(new Hex(0, 0, 0)));
		}

		static public void TestHexRotateRight()
		{
			Tests.EqualHex("hex_rotate_right", new Hex(1, -3, 2).RotateRight(), new Hex(3, -2, -1));
		}

		static public void TestHexRotateLeft()
		{
			Tests.EqualHex("hex_rotate_left", new Hex(1, -3, 2).RotateLeft(), new Hex(-2, -1, 3));
		}

		static public void TestHexRound()
		{
			FractionalHex a = new FractionalHex(0.0, 0.0, 0.0);
			FractionalHex b = new FractionalHex(1.0, -1.0, 0.0);
			FractionalHex c = new FractionalHex(0.0, -1.0, 1.0);
			Tests.EqualHex("hex_round 1", new Hex(5, -10, 5), new FractionalHex(0.0, 0.0, 0.0).HexLerp(new FractionalHex(10.0, -20.0, 10.0), 0.5).HexRound());
			Tests.EqualHex("hex_round 2", a.HexRound(), a.HexLerp(b, 0.499).HexRound());
			Tests.EqualHex("hex_round 3", b.HexRound(), a.HexLerp(b, 0.501).HexRound());
			Tests.EqualHex("hex_round 4", a.HexRound(), new FractionalHex(a.q * 0.4 + b.q * 0.3 + c.q * 0.3, a.r * 0.4 + b.r * 0.3 + c.r * 0.3, a.s * 0.4 + b.s * 0.3 + c.s * 0.3).HexRound());
			Tests.EqualHex("hex_round 5", c.HexRound(), new FractionalHex(a.q * 0.3 + b.q * 0.3 + c.q * 0.4, a.r * 0.3 + b.r * 0.3 + c.r * 0.4, a.s * 0.3 + b.s * 0.3 + c.s * 0.4).HexRound());
		}

		static public void TestHexLinedraw()
		{
			Tests.EqualHexArray("hex_linedraw", new List<Hex> { new Hex(0, 0, 0), new Hex(0, -1, 1), new Hex(0, -2, 2), new Hex(1, -3, 2), new Hex(1, -4, 3), new Hex(1, -5, 4) }, FractionalHex.HexLinedraw(new Hex(0, 0, 0), new Hex(1, -5, 4)));
		}

		static public void TestLayout()
		{
			Hex h = new Hex(3, 4, -7);
			Layout flat = new Layout(Layout.flat, new Point(10.0, 15.0), new Point(35.0, 71.0));
			Tests.EqualHex("layout", h, flat.PixelToHex(flat.HexToPixel(h)).HexRound());
			Layout pointy = new Layout(Layout.pointy, new Point(10.0, 15.0), new Point(35.0, 71.0));
			Tests.EqualHex("layout", h, pointy.PixelToHex(pointy.HexToPixel(h)).HexRound());
		}

		static public void TestOffsetRoundtrip()
		{
			Hex a = new Hex(3, 4, -7);
			OffsetCoord b = new OffsetCoord(1, -3);
			Tests.EqualHex("conversion_roundtrip even-q", a, OffsetCoord.QoffsetToCube(OffsetCoord.EVEN, OffsetCoord.QoffsetFromCube(OffsetCoord.EVEN, a)));
			Tests.EqualOffsetcoord("conversion_roundtrip even-q", b, OffsetCoord.QoffsetFromCube(OffsetCoord.EVEN, OffsetCoord.QoffsetToCube(OffsetCoord.EVEN, b)));
			Tests.EqualHex("conversion_roundtrip odd-q", a, OffsetCoord.QoffsetToCube(OffsetCoord.ODD, OffsetCoord.QoffsetFromCube(OffsetCoord.ODD, a)));
			Tests.EqualOffsetcoord("conversion_roundtrip odd-q", b, OffsetCoord.QoffsetFromCube(OffsetCoord.ODD, OffsetCoord.QoffsetToCube(OffsetCoord.ODD, b)));
			Tests.EqualHex("conversion_roundtrip even-r", a, OffsetCoord.RoffsetToCube(OffsetCoord.EVEN, OffsetCoord.RoffsetFromCube(OffsetCoord.EVEN, a)));
			Tests.EqualOffsetcoord("conversion_roundtrip even-r", b, OffsetCoord.RoffsetFromCube(OffsetCoord.EVEN, OffsetCoord.RoffsetToCube(OffsetCoord.EVEN, b)));
			Tests.EqualHex("conversion_roundtrip odd-r", a, OffsetCoord.RoffsetToCube(OffsetCoord.ODD, OffsetCoord.RoffsetFromCube(OffsetCoord.ODD, a)));
			Tests.EqualOffsetcoord("conversion_roundtrip odd-r", b, OffsetCoord.RoffsetFromCube(OffsetCoord.ODD, OffsetCoord.RoffsetToCube(OffsetCoord.ODD, b)));
		}

		static public void TestOffsetFromCube()
		{
			Tests.EqualOffsetcoord("offset_from_cube even-q", new OffsetCoord(1, 3), OffsetCoord.QoffsetFromCube(OffsetCoord.EVEN, new Hex(1, 2, -3)));
			Tests.EqualOffsetcoord("offset_from_cube odd-q", new OffsetCoord(1, 2), OffsetCoord.QoffsetFromCube(OffsetCoord.ODD, new Hex(1, 2, -3)));
		}

		static public void TestOffsetToCube()
		{
			Tests.EqualHex("offset_to_cube even-", new Hex(1, 2, -3), OffsetCoord.QoffsetToCube(OffsetCoord.EVEN, new OffsetCoord(1, 3)));
			Tests.EqualHex("offset_to_cube odd-q", new Hex(1, 2, -3), OffsetCoord.QoffsetToCube(OffsetCoord.ODD, new OffsetCoord(1, 2)));
		}

		static public void TestDoubledRoundtrip()
		{
			Hex a = new Hex(3, 4, -7);
			DoubledCoord b = new DoubledCoord(1, -3);
			Tests.EqualHex("conversion_roundtrip doubled-q", a, DoubledCoord.QdoubledFromCube(a).QdoubledToCube());
			Tests.EqualDoubledcoord("conversion_roundtrip doubled-q", b, DoubledCoord.QdoubledFromCube(b.QdoubledToCube()));
			Tests.EqualHex("conversion_roundtrip doubled-r", a, DoubledCoord.RdoubledFromCube(a).RdoubledToCube());
			Tests.EqualDoubledcoord("conversion_roundtrip doubled-r", b, DoubledCoord.RdoubledFromCube(b.RdoubledToCube()));
		}

		static public void TestDoubledFromCube()
		{
			Tests.EqualDoubledcoord("doubled_from_cube doubled-q", new DoubledCoord(1, 5), DoubledCoord.QdoubledFromCube(new Hex(1, 2, -3)));
			Tests.EqualDoubledcoord("doubled_from_cube doubled-r", new DoubledCoord(4, 2), DoubledCoord.RdoubledFromCube(new Hex(1, 2, -3)));
		}

		static public void TestDoubledToCube()
		{
			Tests.EqualHex("doubled_to_cube doubled-q", new Hex(1, 2, -3), new DoubledCoord(1, 5).QdoubledToCube());
			Tests.EqualHex("doubled_to_cube doubled-r", new Hex(1, 2, -3), new DoubledCoord(4, 2).RdoubledToCube());
		}

		static public void TestAll()
		{
			Tests.TestHexArithmetic();
			Tests.TestHexDirection();
			Tests.TestHexNeighbor();
			Tests.TestHexDiagonal();
			Tests.TestHexDistance();
			Tests.TestHexRotateRight();
			Tests.TestHexRotateLeft();
			Tests.TestHexRound();
			Tests.TestHexLinedraw();
			Tests.TestLayout();
			Tests.TestOffsetRoundtrip();
			Tests.TestOffsetFromCube();
			Tests.TestOffsetToCube();
			Tests.TestDoubledRoundtrip();
			Tests.TestDoubledFromCube();
			Tests.TestDoubledToCube();
		}

		static public void Complain(String name)
		{
			Console.WriteLine("FAIL " + name);
		}
	}
}