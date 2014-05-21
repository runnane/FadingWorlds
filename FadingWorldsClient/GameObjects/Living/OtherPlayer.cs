using fwlib;

namespace FadingWorldsClient.GameObjects.Living
{
	public class OtherPlayer : LivingEntity {
		public bool Disconnected { get; set; }
		
		public OtherPlayer(Textures texturename, string id)
			: base(texturename, id) {
			Health = 10;
			MaxHealth = 10;
			Mana = 0;
			MaxMana = 0;
			Level = 1;
			EntityType = EntityType.Player;
			RegenSpeed = 7;
		}

	}
}