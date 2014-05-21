using fwlib;

namespace FadingWorldsClient.GameObjects.Items
{
	internal class HealthPotion : Item {
		public HealthPotion(string id) : base(Textures.HealthPotion, id) {
			Type = ItemType.Health;
		}
	}
}