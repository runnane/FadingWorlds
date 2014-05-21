using FadingWorldsServer.ServerObjects;
using fwlib;

namespace FadingWorldsServer.GameObjects
{
	public class LimitedEntity : Entity {
		public float TimeLeft = 0.5f;
		public float TimeElapsed;

		public LimitedEntity( Position2D pos, float timeToShow)
{
			TimeLeft = timeToShow;
			Position = pos;
		}
	}
}