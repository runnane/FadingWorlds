using fwlib;

namespace FadingWorldsClient.GameObjects.Blocks
{
	internal class Stone : Block {
		public Stone(Position2D pos)
			: base(Textures.Stone, pos) {
			IsBlocking = true;
			Type = BlockType.Stone;
		}
	}
}