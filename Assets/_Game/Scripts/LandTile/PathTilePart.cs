using ClocknestGames.Library.Utils;
using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    [System.Serializable]
    public class PathSetting
    {
        public int PartIndexFrom;
        public int PartIndexTo;
        public Path Path;
        public bool IsPathReversed;
    }

    public abstract class PathTilePart : LandTilePart
    {

	}
}