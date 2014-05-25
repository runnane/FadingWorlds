using System;
using fwlib;
using Microsoft.Xna.Framework;

namespace FadingWorldsClient.GameObjects
{
	public class MovableEntity : Entity {
		public Vector2 Velocity { get; set; }
		public Vector2 Acceleration { get; set; }
		public Vector2 Target { get; set; }

		public bool RemoveWhenOutOfBounds { get; set; }
		public bool UseTarget { get; set; }

		public float SpeedAngle {
			get { return (float) Math.Atan(Velocity.X/Velocity.Y*-1); }
		}

		public float SpeedLength {
			get { return (float) Math.Abs(Velocity.Y/Math.Sin(SpeedAngle)); }
		}


		public MovableEntity(Textures texturename, string id) : base(texturename, id) {
			RemoveWhenOutOfBounds = true;
			Velocity = new Vector2(0, 0);
			UseTarget = false;
		}

		public bool ShouldBeRemoved() {
			if (!RemoveWhenOutOfBounds)
				return false;
			if (Location.X < 0 || Location.X > FadingWorldsGameWindow.Instance.Screenwidth || Location.Y < 0 || Location.Y > FadingWorldsGameWindow.Instance.Screenheight)
				return true;
			return false;
		}

		public new string Description {
			get {
				return "obj-angle=" + MathHelper.ToDegrees(Angle) + "\nobj-direction=" + Direction + "\nobj-velocity=" + Velocity +
				       "\nobj-location=" + Location + "\nspeed-angle=" + MathHelper.ToDegrees(SpeedAngle) + "\nspeed-length=" +
				       SpeedLength;
			}
		}

		internal override void Update(GameTime gameTime) {
			// Enable gravity
			if (FadingWorldsGameWindow.Instance.GravityEnabled)
			{
				Acceleration += FadingWorldsGameWindow.Instance.Gravity * 0.03f;
			}

			if (UseTarget) {
				Vector2 v = Vector2.Subtract(Target, Location);
				Acceleration = v*0.01f;
			}

			// Add acceleration to velocity
			Velocity += Acceleration;


			// Move object
			Location += Velocity;

			// Reset acceleration
			Acceleration = Vector2.Zero;

			// Remove if out of bounds
			if (ShouldBeRemoved()) {
				lock (FadingWorldsGameWindow.Instance.BlockObjects) {
					FadingWorldsGameWindow.Instance.BlockObjects.Remove(this);
				}
			}
			base.Update(gameTime);
		}
	}
}