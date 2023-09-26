using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    public class HexTile : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshPro _text;

        public Vector3Int CubeIndex { get; private set; }

        public void SetTile(Vector3Int cubeIndex)
        {
			CubeIndex = cubeIndex;

            _text.SetText($"{CubeIndex.x},{CubeIndex.y},{CubeIndex.z}");
        }
    }
}