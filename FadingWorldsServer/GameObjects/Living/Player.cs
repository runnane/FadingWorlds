using FadingWorldsServer.ServerObjects;
using fwlib;
using Microsoft.Xna.Framework;

namespace FadingWorldsServer.GameObjects.Living
{
	public class Player : LivingEntity {
		public override string Id {
			get { return Username; }
		}

		public string Username { get; set; }
		public string Password { get; set; }
		public string Email { get; set; }

		public Player() {
			Health = 20;
			MaxHealth = 20;
			Mana = 0;
			MaxMana = 0;
			Level = 1;
			EntityType = EntityType.Player;
			RegenSpeed = 0.3f;
			Position = new Position2D(-1, -1);
			ExperienceValue = 2000;
			Weapon = "D6";
		}

		public void Reset() {
			Health = 20;
			MaxHealth = 20;
			Mana = 0;
			MaxMana = 0;
			Level = 1;
			RegenSpeed = 0.3f;
			Position = new Position2D(-1, -1);
			ExperienceValue = 2000;
			ExperiencePoints = 0;
			NextLevelAt = 300;
		}

		public override string ToString() {
			return Username == string.Empty ? "Unknown" : Username;
		}

		internal override void Tick() {
			if (ExperiencePoints >= NextLevelAt) {
				int diff = NextLevelAt;
				Level++;
				ArmorClass += 1;
				AttackPower += 1;
				NextLevelAt = NextLevelAt*2;
				int healthIncrease = Helper.Random(1, 5);
				Health += healthIncrease;
				MaxHealth += healthIncrease;
				FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ec|" + Id + "/ac/" + 1 + "/" + ArmorClass);
				FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ec|" + Id + "/ap/" + 1 + "/" + AttackPower);
				FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ec|" + Id + "/level/" + 1 + "/" + Level);
				FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ec|" + Id + "/nextlevel/" + diff + "/" + NextLevelAt);
				FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ec|" + Id + "/hp/" + healthIncrease + "/" + Health);
				FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ec|" + Id + "/maxhp/" + healthIncrease + "/" + MaxHealth);
			}

			base.Tick();
		}
	}
}