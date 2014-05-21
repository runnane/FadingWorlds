using fwlib;

namespace FadingWorldsClient.GameObjects.Blocks
{
	internal class Grass : Block {
		public Grass(Position2D pos)
			: base(Textures.Grass, pos) {
			Type = BlockType.Grass;
		}
	}
}