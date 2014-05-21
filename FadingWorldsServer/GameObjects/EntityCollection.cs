using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FadingWorldsServer.GameObjects
{
	public class EntityCollection : ICollection<Entity> {
		private readonly List<Entity> _collection;

		public List<Entity> Entities
		{
			get { return _collection; }
		}


		public EntityCollection() {
			_collection = new List<Entity>();
		}

		public IEnumerator<Entity> GetEnumerator() {
			return _collection.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public void Add(Entity item) {
			_collection.Add(item);
		}

		public void Clear() {
			_collection.Clear();
		}

		public bool Contains(Entity item) {
			return _collection.Contains(item);
		}

		public void CopyTo(Entity[] array, int arrayIndex) {
			throw new NotImplementedException();
		}

		public bool Remove(Entity item) {
			return _collection.Remove(item);
		}

		public int Count {
			get { return _collection.Count; }
		}

		public bool IsReadOnly {
			get { throw new NotImplementedException(); }
		}


		public Entity GetById(string id) {
			return _collection.FirstOrDefault(ent => ent.Id == id);
		}

		public bool RemoveById(string id) {
			return Remove(_collection.FirstOrDefault(ent => ent.Id == id));
		}


		public Entity this[int index] {
			get { return _collection[index]; }
			set { _collection[index] = value; }
		}

		public Entity this[string id] {
			get { return _collection.FirstOrDefault(e => e.Id == id); }
			set {
				if (this[id] == null) {
					_collection.Add(value);
				}
				else {
					_collection.Remove(this[id]);
					_collection.Add(value);
				}
			}
		}

		public bool Remove(string o) {
			Entity ot = GetById(o);
			if (ot != null) {
				return _collection.Remove(ot);
			}
			return false;
		}

		public string MakeDump() {
			String sb = "";
			foreach (Entity entity in _collection) {
				sb += entity.Id + "/" + entity.Position.X + "/" + entity.Position.Y + "/" + entity.GetType().ToString() + "#";
			}
			return sb;
		}
	}
}