using System;
using System.Collections.Generic;
using FadingWorldsServer.GameObjects.Blocks;
using FadingWorldsServer.ServerObjects;
using fwlib;

namespace FadingWorldsServer.GameObjects
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
				if (b != null && !b.HasEntity && b.Type == BlockType.Grass) {
					return pos;
				}
			}
		}

		public void GenerateWorld() {
			Random r = new Random();

			for (int i = 0; i < Width; i++) {
				Matrix.Add(new List<Block>());
				for (int j = 0; j < Height; j++) {
					var pos = new Position2D(i, j);
					Block box = new Grass(pos);
					if (i == 0 || i == Width - 1 || j == 0 || j == Height - 1) {
						box = new Stone(pos);
					}
					else if (r.Next(1000) > 950) {
						box = new Stone(pos);
					}
					Matrix[i].Add(box);
				}
			}
			// done
		}

		public string DumpMapBlocks()
		{
		    string s = "";
            //string s = Width + "|" + Height + "|";
			foreach (List<Block> blocks in Matrix) {
				foreach (Block block in blocks) {
					s += block.Type.ToString().Substring(0, 1);
				}
			}
			return s;
		}
	}
}