using fwlib;

namespace FadingWorldsClient.GameObjects.Living
{
	internal class Skeleton : LivingEntity {
		public Skeleton(string id)
			: base(Textures.Skeleton, id) {
			Health = 15;
			MaxHealth = 15;
			ExperienceValue = 120;
			EntityType = EntityType.Monster;
			AttackPlayerIfClose = true;
			RegenSpeed = 2.0f;
			ArmorClass = 2;
			AttackPower = 5;
		}
	}
}