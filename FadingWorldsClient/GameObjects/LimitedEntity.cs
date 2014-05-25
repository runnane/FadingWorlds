using fwlib;

namespace FadingWorldsClient.GameObjects
{
	public class LimitedEntity : Entity {
		public float TimeLeft = 0.5f;
		public float TimeElapsed;

		public LimitedEntity(Textures texturename, Position2D pos, float timeToShow)
			: base(texturename, Helper.RandomString(20)) {
			TimeLeft = timeToShow;
			Position = pos;
			Location = FadingWorldsGameWindow.Instance.GetVectorByPos(pos);

		}
	}
}