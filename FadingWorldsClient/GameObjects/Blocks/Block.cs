using System.Collections;
using fwlib;
using Microsoft.Xna.Framework.Graphics;

namespace FadingWorldsClient.GameObjects.Blocks
{
	public class Block : Entity {
		public EntityCollection Entities;
		public BlockType Type;

		public Block(Textures tex, Position2D pos)
			: base(tex, Helper.RandomString(20)) {
			Position = pos;
			Entities = new EntityCollection();
		}

		public IEnumerable GetEntities() {
			return Entities.Entities;
		}

		internal override void Draw(SpriteBatch spriteBatch) {
			Sprite.Draw(spriteBatch, Location);
			lock (Entities) {
				foreach (Entity item in Entities) {
					item.Sprite.Draw(spriteBatch, Location);
				}
			}
		}

		internal void MoveEntity(Entity entity, Block block) {
			lock (Entities) {
				Entities.Remove(entity);
				block.Entities.Add(entity);
			}
		}
	}
}