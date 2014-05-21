namespace FadingWorldsServer.GameObjects.Living
{
	internal class Skeleton : Monster {
		public Skeleton() {
			Health = 15;
			MaxHealth = 15;
			ExperienceValue = 120;
			RegenSpeed = 0.1f;
			ArmorClass = 2;
			AttackPower = 5;
			Weapon = "1";
		}
	}
}