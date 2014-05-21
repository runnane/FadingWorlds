using System;
using System.Linq;
using fwlib;

namespace FadingWorldsServer.GameObjects.Living
{
	public class LivingEntity : Entity {
		public string Weapon;
		public int Health;
		public int MaxHealth;
		public int Mana;
		public int MaxMana;
		public bool RandomMovement;
		public bool AttackPlayerIfClose;
		public int ExperienceValue;
		public float TimeToUpdate = 0.5f;
		public float TimeElapsed;
		public float TimeLastAttack = DateTime.Now.Ticks;
		public float AttackSpeed = 1f;
		public int Gold;
		public int ExperiencePoints;
		public int Level;
		public int NextLevelAt = 300;
		private float _regenSpeed = 2;
		public float TimeSinceRegen;
		public string Desc;

		public int ArmorClass = 5;
		public int AttackPower = 1;

		public bool IsBoss;

		public float RegenSpeed {
			get { return (1f/_regenSpeed); }
			set { _regenSpeed = (1f/value); }
		}

		public int FramesPerSecond {
			set { TimeToUpdate = (1f/value); }
		}

		public bool IsDead {
			get { return Health <= 0; }
		}

		protected LivingEntity() {}

		public MoveResult RandomMove() {
			if (AttackPlayerIfClose) {
				Position2D p = FindNearbyPlayer();
				if (p != null) {
					return MoveTo(p);
				}
			}
			int d = Helper.Random(0, 9);
			switch (d) {
				case 0:
					return TryMove(Direction.Up);
				case 1:
					return TryMove(Direction.Down);
				case 2:
					return TryMove(Direction.Left);
				case 3:
					return TryMove(Direction.Right);
				case 4:
				case 5:
				case 6:
					return MoveResult.Moved;
			}
			return MoveResult.NotMoved;
		}

		internal override void Update(GameTimeLight gameTime) {
			base.Update(gameTime);
			if (Health <= 0) {
				FadingWorldsServer.Instance.RemoveEntity(this);
				return;
			}
			if (FadingWorldsServer.Instance.TheGrid.GetBlockAt(Position).Entities.GetById(Id) == null) {
				FadingWorldsServer.Instance.RemoveEntity(this);
				return;
			}


			TimeElapsed += (float) gameTime.ElapsedGameTime.TotalSeconds;
			TimeSinceRegen += (float) gameTime.ElapsedGameTime.TotalSeconds;

			if (RandomMovement) {
				if (TimeElapsed > TimeToUpdate) {
					var v = RandomMove();
					if (v == MoveResult.Moved || v == MoveResult.CannotMoveLivingEntityInTheWay) {
						TimeElapsed -= TimeToUpdate;
					}
				}
			}

			// Regen
			if (_regenSpeed > 0) {
				if (TimeSinceRegen > _regenSpeed) {
					TimeSinceRegen -= _regenSpeed;

					Heal(1);
				}
			}
			base.Update(gameTime);
		}

		internal AttackResult Attack(String mobId) {
			Entity target = FadingWorldsServer.Instance.GameObjects.GetById(mobId);
			if (target == null)
				return AttackResult.InvalidAttack;
			return Attack(target);
		}

		internal AttackResult Attack(Entity mob) {
			LivingEntity target = mob as LivingEntity;
			int d12 = Helper.Random(0, 12);
			if (d12 + AttackPower > target.ArmorClass) {
				int dmg = Helper.Evaluate(Weapon);
				return target.Hurt(dmg);
			}
			return AttackResult.Miss;
		}

		internal AttackResult Attack(Position2D pos) {
			LivingEntity target =
				FadingWorldsServer.Instance.TheGrid.GetBlockAt(pos).Entities.SingleOrDefault() as LivingEntity;
			if (target == null)
				return AttackResult.InvalidAttack;
			return Attack(target);
		}

		internal AttackResult Hurt(int i) {
			if (Health - i <= 0) {
				i = Health;
			}

			Health -= i;
			FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ec|" + Id + "/hp/-" + i + "/" + Health);
			if (Health <= 0) {
				Health = 0;
				OnDeath();
				return AttackResult.Killed;
			}
			return AttackResult.Hit;
		}

		internal void Heal(int i) {
			if (Health >= MaxHealth) {
				return;
			}
			if (Health + i > MaxHealth) {
				i = MaxHealth - Health;
			}
			Health += i;
			FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ec|" + Id + "/hp/" + i + "/" + Health);
		}

		internal void AddXP(int i) {
			ExperiencePoints += i;
			FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ec|" + Id + "/xp/" + i + "/" + ExperiencePoints);
		}

		internal MoveResult MoveTo(Position2D newpos) {
			var result = CanMove(newpos);
			if (result == MoveResult.Moved) {
				FadingWorldsServer.Instance.TheGrid.GetBlockAt(Position).Entities.Remove(this);
				FadingWorldsServer.Instance.TheGrid.GetBlockAt(newpos).Entities.Add(this);
				Position = newpos;
				FadingWorldsServer.Instance.TCPPool.SendMessageToAllButUser(Id, "mv|" + Id + "|" + newpos.X + "|" + newpos.Y);
			}

			PostMove(result, newpos);
			return result;
		}

		internal virtual AttackResult TryAttack(LivingEntity targetEntity) {
			Position2D pos = targetEntity.Position;
			if (EntityType == EntityType.Player &&
			    targetEntity.EntityType == EntityType.Monster) {
				// Player vs monster
				var a = Attack(targetEntity);
				if (a == AttackResult.Killed) {
					// Player killed monster
					AddXP(targetEntity.ExperienceValue);
					Helper.ConsoleWrite(Id + " kill " + targetEntity.Id);
					PostAttack(pos, targetEntity);
				}
				else if (a == AttackResult.Miss) {
					// Player missed monster
					Helper.ConsoleWrite(Id + " miss " + targetEntity.Id);
				}
				else if (a == AttackResult.Hit) {
					// Player hit monster
					Helper.ConsoleWrite(Id + " hit " + targetEntity.Id);
				}
				PostAttack(pos, targetEntity);
			}
			else if (EntityType == EntityType.Monster &&
			         targetEntity.EntityType == EntityType.Player) {
				// Monster vs player
				Helper.ConsoleWrite(Id + " attacks " + targetEntity.Id);
				if (Attack(targetEntity) == AttackResult.Hit) {
					// Monster hit player
					Helper.ConsoleWrite(Id + " hits " + targetEntity.Id);
				}
				PostAttack(pos, targetEntity);
			}
			else if (targetEntity.EntityType == EntityType.Monster &&
			         EntityType == EntityType.Monster) {
				// Monster vs Monster
			}
			else if (targetEntity.EntityType == EntityType.Player &&
			         EntityType == EntityType.Player) {
				// Player vs Player
				var a = Attack(targetEntity);
				if (a == AttackResult.Killed) {
					// Player killed monster
					AddXP(targetEntity.ExperienceValue);
					Helper.ConsoleWrite(Id + " kill " + targetEntity.Id);
					PostAttack(pos, targetEntity);
				}
				else if (a == AttackResult.Miss) {
					// Player missed monster
					Helper.ConsoleWrite(Id + " miss " + targetEntity.Id);
				}
				else if (a == AttackResult.Hit) {
					// Player hit monster
					Helper.ConsoleWrite(Id + " hit " + targetEntity.Id);
					PostAttack(pos, targetEntity);
				}
			}
			return AttackResult.InvalidAttack;
		}

		internal virtual AttackResult TryAttack(string mobId) {
			LivingEntity targetEntity =
				FadingWorldsServer.Instance.GameObjects.GetById(mobId) as LivingEntity;
			if (targetEntity != null) {
				return TryAttack(targetEntity);
			}
			return AttackResult.InvalidAttack;
			;
		}

		internal virtual AttackResult TryAttack(Position2D pos) {
			LivingEntity targetEntity =
				FadingWorldsServer.Instance.TheGrid.GetBlockAt(pos).Entities.SingleOrDefault() as LivingEntity;
			if (targetEntity != null) {
				return TryAttack(targetEntity);
			}
			return AttackResult.InvalidAttack;
			;
		}

		internal virtual void PostMove(MoveResult mv, Position2D pos) {
			if (mv == MoveResult.CannotMoveLivingEntityInTheWay) {
				TryAttack(pos);
			}
		}

		public MoveResult CanMove(Position2D pos) {
			if (FadingWorldsServer.Instance.TheGrid.GetBlockAt(pos).IsBlocking)
				return MoveResult.CannotMoveBlocked;
			if (FadingWorldsServer.Instance.TheGrid.GetBlockAt(pos).Entities.Any()) {
				return MoveResult.CannotMoveLivingEntityInTheWay;
			}
			return MoveResult.Moved;
		}

		public MoveResult TryMove(Direction d) {
			switch (d) {
				case Direction.Up:
					Position2D newpos1 = new Position2D(Position.X, Position.Y);
					if (Position.Y == 0) {
						newpos1.Y = FadingWorldsServer.Instance.TheGrid.Height - 1;
					}
					else {
						newpos1.Y--;
					}
					return MoveTo(newpos1);

				case Direction.Down:
					Position2D newpos2 = new Position2D(Position.X, Position.Y);
					if (Position.Y >= FadingWorldsServer.Instance.TheGrid.Height - 1) {
						newpos2.Y = 0;
					}
					else {
						newpos2.Y++;
					}
					return MoveTo(newpos2);

				case Direction.Right:
					Position2D newpos3 = new Position2D(Position.X, Position.Y);
					if (Position.X >= FadingWorldsServer.Instance.TheGrid.Width - 1) {
						newpos3.X = 0;
					}
					else {
						newpos3.X++;
					}
					return MoveTo(newpos3);
				case Direction.Left:
					Position2D newpos4 = new Position2D(Position.X, Position.Y);
					if (Position.X == 0) {
						newpos4.X = FadingWorldsServer.Instance.TheGrid.Width - 1;
					}
					else {
						newpos4.X--;
					}
					return MoveTo(newpos4);
			}
			throw new Exception("!! Unknown move enum");
		}

		internal virtual void PostAttack(Position2D pos, LivingEntity mob) {}

		internal virtual void OnDeath() {
			FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ec|" + Id + "/die/0/0");
			if (EntityType == EntityType.Monster) {
				FadingWorldsServer.Instance.RemoveEntity(this);
				FadingWorldsServer.Instance.SpawnRandomEntity();
			}
			else {
				FadingWorldsServer.Instance.RemoveEntity(this);
				FadingWorldsServer.Instance.DisconnectUser(this.Id);
			}
		}

		internal override string MakeDump() {
			return "id=" + Id + ",x=" + Position.X + ",y=" + Position.Y + ",maxhp=" + MaxHealth + ",hp=" + Health + ",xp=" +
			       ExperiencePoints + ",level=" + Level + ",nextlevel=" +
			       NextLevelAt + ",mana=" + Mana + ",maxmana=" + MaxMana + ",ac=" + ArmorClass + ",ap=" + AttackPower + ",dmg=" +
			       Weapon + ",type=" +
			       GetType();
			;
		}

		//internal override string MakeDump() {
		//  return Id + "/" + Position.X + "/" + Position.Y + "/hp#" + Health + "#xp#" + ExperiencePoints + "#maxhp#" + MaxHealth + "#mana#" + Mana + "/" + GetType();
		//}
	}
}