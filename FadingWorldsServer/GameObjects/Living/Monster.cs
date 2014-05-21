using fwlib;

namespace FadingWorldsServer.GameObjects.Living
{
	internal class Monster : LivingEntity {
		public Monster() {
			EntityType = EntityType.Monster;
			RandomMovement = true;
			AttackPlayerIfClose = true;
		}
	}
}