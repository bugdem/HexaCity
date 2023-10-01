using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    public abstract class LandTilePart : MonoBehaviour
    {
        [SerializeField] protected Transform _centerPoint;
        [SerializeField] protected Transform _cornerPoint;
        [SerializeField] protected Transform _content;

        public abstract LandTilePartType PartType { get; }

        public LandTile LandTile { get; private set; }
        public int PartIndex { get; private set; }
        public bool IsCenterPart => PartIndex == 6;

        public void Initialize(LandTile landTile, int partIndex)
        {
            LandTile = landTile;
            PartIndex = partIndex;

            bool isCenterPart = IsCenterPart;
			_content.transform.SetParent(isCenterPart ? _centerPoint : _cornerPoint);
            _content.transform.localPosition = Vector3.zero;
            _content.transform.localRotation = Quaternion.identity;

            _centerPoint.gameObject.SetActive(isCenterPart);
            _cornerPoint.gameObject.SetActive(!isCenterPart);

            OnInitialized();
        }

        public void Layout()
        {
            OnLayout();
        }

        public Edge GetEdge()
        {
            return LandTile.GetPartEdge(this);
        }

        protected virtual void OnInitialized() { }
        protected virtual void OnLayout() { }
    }
}