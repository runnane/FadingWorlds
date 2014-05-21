using fwlib;

namespace FadingWorldsClient.GameObjects.Items
{
	public class Item : Entity {
		public ItemType Type;
		public int GoldValue;
		public int ManaValue;
		public int HealthValue;
		public Item(Textures tex, string id) : base(tex, id) {}
	}
}