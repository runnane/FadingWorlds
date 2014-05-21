using System.Linq;
using fwlib;

namespace FadingWorldsServer.GameObjects.Blocks
{

	public class Block : Entity {
		public bool IsBlocking;

		public EntityCollection Items;
		public EntityCollection Entities;
		public BlockType Type;
		public Block() {}

		public void Init(Position2D pos) {
			Position = pos;
			Items = new EntityCollection();
			Entities = new EntityCollection();
		}

		public bool HasEntity {
			get { return Entities.Any();  }
		}

		public Block( Position2D pos)
		{
			Position = pos;
		}

		
	}
}