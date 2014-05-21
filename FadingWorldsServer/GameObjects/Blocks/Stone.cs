using FadingWorldsServer.ServerObjects;
using fwlib;

namespace FadingWorldsServer.GameObjects.Blocks
{
	class Stone : Block
	{
		public Stone(Position2D pos) {

			Init(pos);
			IsBlocking = true;
			Type = BlockType.Stone;

		}
	}
}
