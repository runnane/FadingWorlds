using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FadingWorldsServer.GameObjects
{
	public class EntityCollection : List<Entity> {
        //private readonly List<Entity> _collection;

        //public List<Entity> Entities
        //{
        //    get { return _collection; }
        //}


        //public EntityCollection() {
        //    _collection = new List<Entity>();
        //}

        //public IEnumerator<Entity> GetEnumerator() {
        //    return _collection.GetEnumerator();
        //}

        //IEnumerator IEnumerable.GetEnumerator() {
        //    return GetEnumerator();
        //}

        //public void Add(Entity item) {
        //    _collection.Add(item);
        //}

        //public void Clear() {
        //    this.Clear();
        //}

        //public bool Contains(Entity item) {
        //    return this.Contains(item);
        //}

        //public void CopyTo(Entity[] array, int arrayIndex) {
        //    throw new NotImplementedException();
        //}

        //public bool Remove(Entity item) {
        //    return this.Remove(item);
        //}

        //public int Count {
        //    get { return this.Count; }
        //}

        //public bool IsReadOnly {
        //    get { throw new NotImplementedException(); }
        //}


		public Entity GetById(string id) {
			return this.FirstOrDefault(ent => ent.Id == id);
		}

		public bool RemoveById(string id) {
            return Remove(this.FirstOrDefault(ent => ent.Id == id));
		}


        //public Entity this[int index] {
        //    get { return this[index]; }
        //    set { this[index] = value; }
        //}

		public Entity this[string id] {
            get { return this.FirstOrDefault(e => e.Id == id); }
			set {
				if (this[id] == null) {
                    this.Add(value);
				}
				else {
                    this.Remove(this[id]);
                    this.Add(value);
				}
			}
		}

		public bool Remove(string o) {
			Entity ot = GetById(o);
			if (ot != null) {
                return this.Remove(ot);
			}
			return false;
		}

		public string MakeDump() {
			String sb = "";
            foreach (Entity entity in this)
            {
				sb += entity.Id + "/" + entity.Position.X + "/" + entity.Position.Y + "/" + entity.GetType().ToString() + "#";
			}
			return sb;
		}
	}
}