using fwlib;

namespace FadingWorldsClient.GameObjects.Items
{
	internal class Bag : Item {
		public Bag(string id)
			: base(Textures.BlueBag, id) {
			Type = ItemType.Gold;
		}
	}
}