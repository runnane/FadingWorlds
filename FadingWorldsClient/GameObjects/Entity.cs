using System;
using System.Collections.Generic;
using FadingWorldsClient.GameObjects.Living;
using fwlib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FadingWorldsClient.GameObjects
{
	public class Entity {
		public Position2D Position;
		public String Id { get; set; }
		public bool IsBlocking = false;
		private string _desc;
		public Vector2 Location { get; set; }
		public Sprite Sprite;
		internal float AngleFloat;
		public EntityType EntityType;
		internal float TimeElapsedSinceTick;
		internal float TickRate = 0.1f;

		public string Desc {
			get {
				if (_desc == string.Empty)
					return Id;
				return _desc;
			}
			set { _desc = value; }
		}


		public float Angle {
			get { return AngleFloat; }
			set { AngleFloat = MathHelper.WrapAngle(value); }
		}

		internal Vector2 CenterPoint {
			get { return new Vector2(32/2.0f, 32/2.0f); }
		}

		internal Rectangle BoxObject {
			get { return new Rectangle(0, 0, 32, 32); }
		}

		internal Vector2 Direction {
			get {
				var v = new Vector2 {X = (float) Math.Sin(Angle), Y = (float) Math.Cos(Angle)*-1};
				return v;
			}
		}


		public Entity(Textures tex, string id) {
			EntityType = EntityType.Object;
			IsBlocking = false;
			Id = id;
			SetTexture(tex);
			Angle = 0f;
			Position = new Position2D(1, 1);
			Location = new Vector2(FadingWorldsGameWindow.Instance.Screenwidth/2f, FadingWorldsGameWindow.Instance.Screenheight/2f);
		}

		internal void SetTexture(Textures tex) {
			Sprite = new Sprite(FadingWorldsGameWindow.Instance.Content, "sprites", tex);
		}


		internal virtual void Draw(SpriteBatch spriteBatch) {
			Sprite.Draw(spriteBatch, Location);
		}

		internal virtual string Description {
			get {
				return "obj-angle=" + MathHelper.ToDegrees(Angle) + "\nobj-direction=" + Direction +
				       "\nobj-location=" + Location;
			}
		}


		internal virtual void Update(GameTime gameTime) {
			TimeElapsedSinceTick += (float) gameTime.ElapsedGameTime.TotalSeconds;
			if (TimeElapsedSinceTick > TickRate) {
				Tick();
				TimeElapsedSinceTick -= TickRate;
			}
		}

		internal virtual void Tick()
		{
				
		}

		public MoveResult CanMove(Position2D pos) {
			if (FadingWorldsGameWindow.Instance.TheGrid.GetBlockAt(pos).IsBlocking)
				return MoveResult.CannotMoveBlocked;
			if (FadingWorldsGameWindow.Instance.TheGrid.GetBlockAt(pos).Entities.LivingEntity != null) {
				return MoveResult.CannotMoveLivingEntityInTheWay;
			}
			return MoveResult.Moved;
		}


		public MoveResult MoveTo(Position2D newpos) {
			var result = CanMove(newpos);
			if (result == MoveResult.Moved) {
				if ((this is Player)) {
                    //FadingWorldsGameWindow.Instance.TheLoader.connectionLoop.SendCommand("mv|self|" + newpos.X + "|" + newpos.Y);
                    FadingWorldsGameWindow.Instance.TheLoader.ConnectionLoop.SendPayload(new NetworkPayload
                    {
                        Type = PayloadType.Move,
                        Params = new List<string>{ "self", "" + newpos.X, "" + newpos.Y }

                    });
				}
				FadingWorldsGameWindow.Instance.TheGrid.GetBlockAt(Position).MoveEntity(this,
				                                                                 FadingWorldsGameWindow.Instance.TheGrid.GetBlockAt(newpos));
				//FadingWorldsGameWindow.Instance.TheGrid.GetBlockAt(Position).Entities.Remove(this);
				//FadingWorldsGameWindow.Instance.TheGrid.GetBlockAt(newpos).Entities.Add(this);
				Position = newpos;
			}
			PostMove(result, newpos);
			return result;
		}



		public MoveResult TryMove(Direction d)
		{
			switch (d)
			{
				case fwlib.Direction.Up:
					Position2D newpos1 = new Position2D(Position.X, Position.Y);
					if (Position.Y == 0)
					{
						newpos1.Y = FadingWorldsGameWindow.Instance.TheGrid.Height - 1;
					}
					else
					{
						newpos1.Y--;
					}
					return MoveTo(newpos1);

                case fwlib.Direction.Down:
					Position2D newpos2 = new Position2D(Position.X, Position.Y);
					if (Position.Y >= FadingWorldsGameWindow.Instance.TheGrid.Height - 1)
					{
						newpos2.Y = 0;
					}
					else
					{
						newpos2.Y++;
					}
					return MoveTo(newpos2);

                case fwlib.Direction.Right:
					Position2D newpos3 = new Position2D(Position.X, Position.Y);
					if (Position.X >= FadingWorldsGameWindow.Instance.TheGrid.Width - 1)
					{
						newpos3.X = 0;
					}
					else
					{
						newpos3.X++;
					}
					return MoveTo(newpos3);
                case fwlib.Direction.Left:
					Position2D newpos4 = new Position2D(Position.X, Position.Y);
					if (Position.X == 0)
					{
						newpos4.X = FadingWorldsGameWindow.Instance.TheGrid.Width - 1;
					}
					else
					{
						newpos4.X--;
					}
					return MoveTo(newpos4);
			}
			throw new Exception("Unknown move enum");
		}

		internal virtual void PostMove(MoveResult mr, Position2D pos) {}

	}
}