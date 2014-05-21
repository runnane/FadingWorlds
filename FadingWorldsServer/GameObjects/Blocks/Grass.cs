using FadingWorldsServer.ServerObjects;
using fwlib;

namespace FadingWorldsServer.GameObjects.Blocks
{
	internal class Grass : Block {
		public Grass(Position2D pos) {
			Init(pos);
			Type = BlockType.Grass;
		}
	}
}