using System;
using System.Collections.Generic;
using System.Threading;
using FadingWorldsClient.GameObjects;
using FadingWorldsClient.GameObjects.Blocks;
using FadingWorldsClient.GameObjects.Living;
using fwlib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FadingWorldsClient
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class FadingWorldsApp : Game {
		public static FadingWorldsApp Instance { get; set; }

	    internal SpriteBatch SpriteBatch;
		internal Dictionary<string, SpriteFont> Fonts;
		public Texture2D EmptyTexture;

		public int Screenwidth = 1280;
		public int Screenheight = 720;

		public EntityCollection BlockObjects;
		public EntityCollection GameObjects;
		public EntityCollection TemporaryObjects;

		//public OtherPlayers OtherPlayers;

		public bool GravityEnabled { get; set; }
		public TimeSpan Timer = TimeSpan.Zero;
		public Dictionary<String, bool> ButtonList;

		public readonly Vector2 Gravity = new Vector2(0, 3f);

		public Grid TheGrid;

		public int BlockWidth = 39;
		public int BlockHeight = 20;

		public int BlockSize = 32;

		public Player ThePlayer;

		public float SpawnSpeed = 4f;
		public float TimeSinceSpawn;

		public bool IsRunning { get; set; }
		public bool IsLoaded { get; set; }
		public ContentManager TheContent;

		public bool GameIsWon;
		public bool BossSpawned;

		private readonly string[] _parms;

		internal Dictionary<string, SoundEffect> Sounds;

		internal Loader TheLoader;


		public FadingWorldsApp(Loader l, string[] strings) {
		    IsRunning = true;
			IsLoaded = false;
			_parms = strings;
			Instance = this;
			var graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

			graphics.PreferredBackBufferWidth = Screenwidth;
			graphics.PreferredBackBufferHeight = Screenheight;
			TheLoader = l;
		}

		public Vector2 GetVectorByPos(Position2D pos) {
			return new Vector2(pos.X*BlockSize + (BlockSize/2), pos.Y*BlockSize + (BlockSize/2));
		}


		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize() {
			Fonts = new Dictionary<string, SpriteFont>();
			Sounds = new Dictionary<string, SoundEffect>();
			BlockObjects = new EntityCollection();
			GameObjects = new EntityCollection();
			TemporaryObjects = new EntityCollection();
			ButtonList = new Dictionary<string, bool>();
			//OtherPlayers = new OtherPlayers();
			TheContent = Content;
			base.Initialize();
			TheLoader.State = GameState.Starting;
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent() {
			// Create a new SpriteBatch, which can be used to draw textures.

			SpriteBatch = new SpriteBatch(GraphicsDevice);

			MakeGrid(int.Parse(_parms[0]), int.Parse(_parms[1]), _parms[2]);

			Fonts["Benegraphic"] = Content.Load<SpriteFont>("Benegraphic");
			Fonts["Tempesta"] = Content.Load<SpriteFont>("Tempesta10");
			Fonts["Tempesta7"] = Content.Load<SpriteFont>("Tempesta7"); 

			EmptyTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
			EmptyTexture.SetData(new[] {Color.White});
			var username = TheLoader.connectionLoop.GetLoggedInUser();

			ThePlayer = new Player(Textures.Hero, username) {Desc = username, Id = username};

		    //GameObjects.Add(ThePlayer);
			IsLoaded = true;
		}


		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent() {
			// TODO: Unload any non ContentManager content here
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime) {
			if (!IsRunning || !IsLoaded)
				return;
			Timer += gameTime.ElapsedGameTime;

			// Up
			if (Keyboard.GetState().IsKeyDown(Keys.Up) && !ButtonList["Up"]) {
				ThePlayer.TryMove(Direction.Up);
				//_keyMoved = true;
				ButtonList["Up"] = true;
			}
			else if (!Keyboard.GetState().IsKeyDown(Keys.Up)) {
				ButtonList["Up"] = false;
			}


			// DOwn
			if (Keyboard.GetState().IsKeyDown(Keys.Down) && !ButtonList["Down"]) {
				ThePlayer.TryMove(Direction.Down);
				//_keyMoved = true;
				ButtonList["Down"] = true;
			}
			else if (!Keyboard.GetState().IsKeyDown(Keys.Down)) {
				ButtonList["Down"] = false;
			}

			// Left
			if (Keyboard.GetState().IsKeyDown(Keys.Left) && !ButtonList["Left"]) {
				//		_keyMoved = true;
				ThePlayer.TryMove(Direction.Left);
				ButtonList["Left"] = true;
			}
			else if (!Keyboard.GetState().IsKeyDown(Keys.Left)) {
				ButtonList["Left"] = false;
			}


			//Right
			if (Keyboard.GetState().IsKeyDown(Keys.Right) && !ButtonList["Right"]) {
				//		_keyMoved = true;
				ThePlayer.TryMove(Direction.Right);
				ButtonList["Right"] = true;
			}
			else if (!Keyboard.GetState().IsKeyDown(Keys.Right)) {
				ButtonList["Right"] = false;
			}
			if (Keyboard.GetState().IsKeyDown(Keys.X)) {
				Exit();
			}

			// Update blocks
			if (BlockObjects != null) {
				lock (BlockObjects) {
					foreach (Entity block in BlockObjects) {
						block.Update(gameTime);
					}
				}
			}

			// Update grid entities
			//if (TheGrid != null) {
			//  lock (TheGrid) {
			//    foreach (Entity entity in TheGrid.GetEntities()) {
			//      entity.Update(gameTime);
			//    }
			//  }
			//}

			// Update players, monsters and objects
			if (GameObjects != null) {
				lock (GameObjects) {
					for (int i = GameObjects.Count - 1; i >= 0; i--) {
						var gameObject = GameObjects[i] as Entity;
						gameObject.Update(gameTime);
					}
				}
			}

			if (TemporaryObjects != null)
				lock (TemporaryObjects) {
					for (int i = TemporaryObjects.Count - 1; i >= 0; i--) {
						var t = TemporaryObjects[i] as LimitedEntity;
						t.TimeElapsed += (float) gameTime.ElapsedGameTime.TotalSeconds;
						t.Update(gameTime);

						if (t.TimeElapsed > t.TimeLeft) {
							TemporaryObjects.RemoveAt(i);
						}
					}
				}

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime) {
			if (!IsRunning || !IsLoaded)
				return;
			GraphicsDevice.Clear(Color.Black);
			SpriteBatch.Begin();

			// Blocks
			lock (BlockObjects) {
				foreach (var gameObject in BlockObjects) {
					gameObject.Draw(SpriteBatch);
				}
			}

			// Players, objects etc
			lock (GameObjects) {
				foreach (var gameObject in GameObjects) {
					gameObject.Draw(SpriteBatch);
				}
			}

			// Update grid entities
			//if (TheGrid != null) {
			//  lock (TheGrid) {
			//    foreach (Entity entity in TheGrid.GetEntities()) {
			//      entity.Draw(SpriteBatch);
			//    }
			//  }
			//}


			// temporary items
			lock (TemporaryObjects) {
				foreach (var gameObject in TemporaryObjects) {
					gameObject.Draw(SpriteBatch);
				}
			}

			SpriteBatch.DrawString(Fonts["Tempesta"],
			                       "Level  : " + ThePlayer.Level +
			                       " (" + ThePlayer.ExperiencePoints +
			                       "/" + ThePlayer.NextLevelAt + ")\nHP      : " + ThePlayer.Health +
			                       "/" + ThePlayer.MaxHealth +
			                       "\nMana  : " + ThePlayer.Mana +
			                       "/" + ThePlayer.MaxMana,
			                       new Vector2(32, 660),
			                       Color.White);

			SpriteBatch.DrawString(Fonts["Tempesta"],
			                       "Gold  : " + ThePlayer.Gold +
														 "\nId  : " + ThePlayer.Id + 
														 "\nDMG  : " + ThePlayer.Weapon,
														 new Vector2(250, 660),
			                       Color.White);

			SpriteBatch.DrawString(Fonts["Tempesta"],
			                       "AC  : " + ThePlayer.ArmorClass +
			                       "\nAP  : " + ThePlayer.AttackPower +
			                       "\nPOS : " + ThePlayer.Position.X + "x" + ThePlayer.Position.Y,
			                       new Vector2(400, 660),
			                       Color.White);


			SpriteBatch.End();
			base.Draw(gameTime);
		}


        protected override void OnExiting(object sender, EventArgs args)
        {
            //TheLoader.SetVisible(true);
            //TheLoader.Exit();
            TheLoader.Disconnect();
            base.OnExiting(sender, args);
        }

		public void WaitForMap() {
			while (true) {
				if (TheGrid == null) {
					Thread.Sleep(500);
				}
				else {
					Run();
					break;
				}
			}
		}

		/// <summary>
		/// Greate the map from mapData
		/// </summary>
		/// <param name="blockWidth"></param>
		/// <param name="blockHeight"></param>
		/// <param name="mapData"></param>
		public void MakeGrid(int blockWidth, int blockHeight, string mapData) {
			if (mapData == "")
				return;
			TheGrid = new Grid(blockWidth, blockHeight);
			int row = 0;
			int col = 0;
			TheGrid.Matrix.Add(new List<Block>());
			foreach (char c in mapData) {
				if (TheGrid.Matrix.Count - 1 < col) {
					TheGrid.Matrix.Add(new List<Block>());
				}

				var pos = new Position2D(col, row);
				Block box = new Grass(pos);
				if (c == 'S') {
					box = new Stone(pos);
				}
				TheGrid.Matrix[col].Add(box);
				lock (BlockObjects) {
					BlockObjects.Add(box);
				}
				box.Location = GetVectorByPos(pos);

				row++;
				if (row == blockHeight) {
					col++;
					row = 0;
				}
			}
		}
	}
}