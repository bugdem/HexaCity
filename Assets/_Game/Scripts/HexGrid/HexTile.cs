using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    public class HexTile : MonoBehaviour
    {
		[SerializeField] private Vector3Int _cubeIndex;
		[SerializeField] private TMPro.TextMeshPro _text;

        public Vector3Int CubeIndex => _cubeIndex;

		public void SetTile(Vector3Int cubeIndex)
        {
			_cubeIndex = cubeIndex;

            _text.SetText($"{CubeIndex.x},{CubeIndex.y},{CubeIndex.z}");
        }
    }
}