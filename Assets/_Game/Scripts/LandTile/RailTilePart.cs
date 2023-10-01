using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Game.Core
{
    public class RailTilePart : PathTilePart
    {
		public override LandTilePartType PartType => LandTilePartType.Rail;
	}
}