using Dreamteck.Splines;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    [RequireComponent(typeof(SplineComputer))]
    public class Path : MonoBehaviour
    {
        [SerializeField, ReadOnly] private LandTilePartType _pathType;
        [SerializeField, HideInInspector] private LandTile _landTile;
        [SerializeField, HideInInspector] private PathSetting _pathSetting;

        public SplineComputer SplineComputer 
        { 
            get
            {
                if (_splineComputer == null)
                    _splineComputer = GetComponent<SplineComputer>();
                return _splineComputer;
            }
        }
        public LandTile LandTile => _landTile;
        public PathSetting PathSetting => _pathSetting;
        public LandTilePartType PathType => _pathType;

        private SplineComputer _splineComputer;

		private void Awake()
		{
			_splineComputer = GetComponent<SplineComputer>();
		}

		public void Initialize(LandTile landTile, PathSetting pathSetting, LandTilePartType pathType)
        {
            _landTile = landTile;
            _pathSetting = pathSetting;
            _pathType = pathType;
        }
    }
}