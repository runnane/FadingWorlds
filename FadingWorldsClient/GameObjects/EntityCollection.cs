using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fwlib;

namespace FadingWorldsClient.GameObjects
{
	public class EntityCollection : ICollection<Entity> {
		private readonly List<Entity> _collection;

		public List<Entity> Entities {
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
			lock (this) {
				_collection.Add(item);
			}
		}

		public void Clear() {
			lock (this) {
				_collection.Clear();
			}
		}

		public bool Contains(Entity item) {
			return _collection.Contains(item);
		}

		public void CopyTo(Entity[] array, int arrayIndex) {
			throw new NotImplementedException();
		}

		public bool Remove(Entity item) {
			lock (this) {
				return _collection.Remove(item);
			}
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


		public bool RemoveById(string id)
		{
			return Remove(_collection.FirstOrDefault(ent => ent.Id == id));
		}


		public Entity this[int index] {
			get { return _collection[index]; }
			set {
				lock (this) {
					_collection[index] = value;
				}
			}
		}

		public Entity this[string id] {
			get { return _collection.FirstOrDefault(e => e.Id == id); }
			//set {
			//  if (this[id] == null) {
			//    _collection.Add(value);
			//  }
			//  else {
			//    _collection.Remove(this[id]);
			//    _collection.Add(value);
			//  }
			//}
		}

		public bool Remove(string o) {
			Entity ot = GetById(o);
			if (ot != null) {
				return _collection.Remove(ot);
			}
			return false;
		}

		public bool HasBlockingEntities {
			get { return _collection.Count(w => w.IsBlocking) > 0; }
		}

		public bool HasPlayer {
			get { return _collection.Count(w => w.EntityType == EntityType.Player) > 0; }
		}

		public Entity Player {
			get { return _collection.SingleOrDefault(w => w.EntityType == EntityType.Player); }
		}

		public Entity LivingEntity {
			get { return _collection.SingleOrDefault(w => w.EntityType != EntityType.Object); }
		}

		public bool HasLivingEntities {
			get { return _collection.Count(w => w.EntityType != EntityType.Object) > 0; }
		}

		public void RemoveAt(int i) {
			_collection.RemoveAt(i);
		}
	}
}