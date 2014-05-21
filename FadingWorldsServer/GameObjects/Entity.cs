using System;
using System.Linq;
using fwlib;

namespace FadingWorldsServer.GameObjects
{
	public class Entity {
		public Position2D Position;
		public virtual string Id { get; set; }
		internal float TimeElapsedSinceTick;
		internal float TickRate = 0.1f;
		public EntityType EntityType;

		public Entity() {
			Position = new Position2D(1, 1);
			Id = Helper.RandomString(10);
		}

		internal Position2D FindNearbyPlayer() {
			Position2D op = new Position2D(Position.X, Position.Y);

			op.X -= 1;
			if (FadingWorldsServer.Instance.TheGrid.GetBlockAt(op) != null &&
			    FadingWorldsServer.Instance.TheGrid.GetBlockAt(op).Entities.Entities.Any(e => e.EntityType == EntityType.Player)) {
				return op;
			}
			op.X += 2;
			if (FadingWorldsServer.Instance.TheGrid.GetBlockAt(op) != null &&
			    FadingWorldsServer.Instance.TheGrid.GetBlockAt(op).Entities.Any(e => e.EntityType == EntityType.Player)) {
				return op;
			}
			op.X -= 1;
			op.Y -= 1;
			if (FadingWorldsServer.Instance.TheGrid.GetBlockAt(op) != null &&
			    FadingWorldsServer.Instance.TheGrid.GetBlockAt(op).Entities.Any(e => e.EntityType == EntityType.Player)) {
				return op;
			}
			op.Y += 2;
			if (FadingWorldsServer.Instance.TheGrid.GetBlockAt(op) != null &&
			    FadingWorldsServer.Instance.TheGrid.GetBlockAt(op).Entities.Any(e => e.EntityType == EntityType.Player)) {
				return op;
			}
			return null;
		}


		internal virtual void Update(GameTimeLight gameTime) {
			TimeElapsedSinceTick += (float) gameTime.ElapsedGameTime.TotalSeconds;
			if (TimeElapsedSinceTick > TickRate) {
				Tick();
				TimeElapsedSinceTick -= TickRate;
			}
		}

		internal virtual void Tick() {
			//Console.WriteLine("tick " + this.GetType());
		}


		internal virtual string MakeDump() {
			return "id=" + Id + ",x=" + Position.X + ",y=" + Position.Y + ",type=" + GetType();
		}
	}
}