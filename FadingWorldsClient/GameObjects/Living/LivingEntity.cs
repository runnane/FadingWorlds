using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using fwlib;

namespace FadingWorldsClient.GameObjects.Living
{
	public class LivingEntity : Entity {
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
		public int NextLevelAt = 50;
		public float RegenSpeed = 2;
		public float TimeSinceRegen;
		public string Weapon;

		public int ArmorClass = 5;
		public int AttackPower = 1;

		public bool IsBoss;

		public int FramesPerSecond {
			set { TimeToUpdate = (1f/value); }
		}

		public bool IsDead {
			get { return Health <= 0; }
		}

		public LivingEntity(Textures texturename, string id) : base(texturename, id) {
			IsBlocking = true;
		}

		internal override void Update(GameTime gameTime) {
			Location = FadingWorldsApp.Instance.GetVectorByPos(Position);
			if (Health <= 0) {
				lock (FadingWorldsApp.Instance.GameObjects) {
					if (EntityType == EntityType.Player) {
						// player died
					}
					else if (EntityType == EntityType.Monster) {
						FadingWorldsApp.Instance.GameObjects.Remove(this);
						FadingWorldsApp.Instance.TheGrid.GetBlockAt(Position).Entities.Remove(this);
					}
				}
				return;
			}

			base.Update(gameTime);
		}

		internal override void Draw(SpriteBatch spriteBatch) {
			Sprite.Draw(spriteBatch, Location);
			//DrawInfoBox(spriteBatch);
			DrawHealthBar(spriteBatch);
		}

		internal override void PostMove(MoveResult mv, Position2D pos) {
			base.PostMove(mv, pos);
			if (mv == MoveResult.CannotMoveLivingEntityInTheWay) {
				LivingEntity targetEntity = FadingWorldsApp.Instance.TheGrid.GetBlockAt(pos).Entities.LivingEntity as LivingEntity;
				if (targetEntity != null && (this is Player)) {
					FadingWorldsApp.Instance.TheLoader.connectionLoop.SendCommand("at|" + targetEntity.Id + "|" +
					                                                              targetEntity.Position.X + "|" +
					                                                              targetEntity.Position.Y);
				}
			}
		}

		internal virtual void PostAttack(Position2D pos, LivingEntity mob) {}
		internal virtual void OnDeath() {}

		private void DrawHealthBar(SpriteBatch spriteBatch) {
			int healthPercent = (Health*100)/MaxHealth;
			int width = 30*healthPercent/100;
			Color col;
			if (healthPercent > 70) {
				col = Color.Green;
			}
			else if (healthPercent > 40) {
				col = Color.Yellow;
			}
			else {
				col = Color.Red;
			}


			Vector2 v = FadingWorldsApp.Instance.GetVectorByPos(Position);
			spriteBatch.Draw(FadingWorldsApp.Instance.EmptyTexture, new Rectangle((int) v.X, (int) v.Y - 8, 32, 7),
			                 Color.Black);
			spriteBatch.Draw(FadingWorldsApp.Instance.EmptyTexture, new Rectangle((int) v.X + 1, (int) v.Y - 7, width, 5),
			                 col);
		}

		private void DrawInfoBox(SpriteBatch spriteBatch) {
			Vector2 v = FadingWorldsApp.Instance.GetVectorByPos(Position);
			spriteBatch.Draw(FadingWorldsApp.Instance.EmptyTexture, new Rectangle((int) v.X + 31, (int) v.Y + 4, 122, 32),
			                 Color.Black);
			spriteBatch.Draw(FadingWorldsApp.Instance.EmptyTexture, new Rectangle((int) v.X + 32, (int) v.Y + 5, 120, 30),
			                 Color.White);

			spriteBatch.DrawString(FadingWorldsApp.Instance.Fonts["Tempesta7"], Id,
			                       FadingWorldsApp.Instance.GetVectorByPos(Position) + new Vector2(33, 2),
			                       Color.Black);

			spriteBatch.DrawString(FadingWorldsApp.Instance.Fonts["Tempesta7"], Health + "/" + MaxHealth,
			                       FadingWorldsApp.Instance.GetVectorByPos(Position) + new Vector2(33, 10),
			                       Color.Green);
		}
	}
}