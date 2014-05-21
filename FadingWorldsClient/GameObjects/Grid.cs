using System;
using System.Collections;
using System.Collections.Generic;
using FadingWorldsClient.GameObjects.Blocks;
using fwlib;

namespace FadingWorldsClient.GameObjects
{
	public class Grid {
		public List<List<Block>> Matrix;

		public int Width;
		public int Height;

		public Grid(int width, int height) {
			Matrix = new List<List<Block>>();
			Width = width;
			Height = height;
		}


		public IEnumerable GetEntities() {
			lock (this) {
				for (int i = 0; i < Width; i++) {
					for (int j = 0; j < Height; j++) {
						if (Matrix.Count == Width && Matrix[i].Count == Height) {
							if (Matrix != null && Matrix[i][j] != null && Matrix[i][j].Entities != null &&
							    Matrix[i][j].Entities.Entities != null) {
								foreach (Entity entity in Matrix[i][j].Entities.Entities) {
									yield return entity;
								}
							}
						}
					}
				}
			}
		}


		public void NewBlock(int row, int col) {}

		public Block GetBlockAt(Position2D pos) {
			if (pos.X < 0 || pos.Y < 0)
				return null;
			if (pos.X > Matrix.Count - 1)
				return null;
			if (pos.Y > Matrix[pos.X].Count - 1)
				return null;
			return Matrix[pos.X][pos.Y];
		}

		public Position2D FindRandomEmptyGrassBlock() {
			while (true) {
				int col = Helper.Random(1, Width - 1);
				int row = Helper.Random(1, Height - 1);
				Position2D pos = new Position2D(col, row);
				Block b = GetBlockAt(pos);
				if (b != null && !b.Entities.HasBlockingEntities && b.Type == BlockType.Grass) {
					return pos;
				}
			}
		}
	}
}