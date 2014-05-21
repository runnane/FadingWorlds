namespace FadingWorldsServer.GameObjects.Living
{
	internal class Ghost : Monster {
		public Ghost()
		{
			Health = 25;
			MaxHealth = 25;
			ExperienceValue = 1000;
			RegenSpeed = 0.1f;
			ArmorClass = 2;
			AttackPower = 5;
			Weapon = "D4";
		}
	}
}