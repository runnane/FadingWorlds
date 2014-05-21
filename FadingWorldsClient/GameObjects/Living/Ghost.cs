using fwlib;

namespace FadingWorldsClient.GameObjects.Living
{
	internal class Ghost : LivingEntity {
		public Ghost(string id)
			: base(Textures.Ghost, id)
		{
			Health = 25;
			MaxHealth = 25;
			ExperienceValue = 700;
			EntityType = EntityType.Monster;
			AttackPlayerIfClose = true;
			RegenSpeed = 2.0f;
			ArmorClass = 2;
			AttackPower = 5;
		}
	}
}