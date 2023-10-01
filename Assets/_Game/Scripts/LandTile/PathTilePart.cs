using ClocknestGames.Library.Utils;
using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    public class PathSetting
    {
        public int PartIndexFrom;
        public int PartIndexTo;
        public SplineComputer Path;
    }

    public abstract class PathTilePart : LandTilePart
    {

	}
}