using System;
using fwlib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FadingWorldsClient
{
	public class Sprite {
		private Texture2D _mSpriteTexture;
		private readonly int _spriteIndex;
		private Rectangle _size;

		private int PosY {
			get { return _spriteIndex == 0 ? 0 : (int) Math.Floor(_spriteIndex/10.0f); }
		}

		private int PosX {
			get { return _spriteIndex == 0 ? 0 : (_spriteIndex%10); }
		}


		public Sprite(ContentManager theContentManager, string theAssetName, Textures tex) {
			_spriteIndex = (int)tex;
			_mSpriteTexture = theContentManager.Load<Texture2D>(theAssetName);
			_size = new Rectangle(PosX*32, PosY*32, 32, 32);
		}


		public void LoadContent(ContentManager theContentManager, string theAssetName) {}

		//Draw the sprite to the screen
		public void Draw(SpriteBatch theSpriteBatch, Vector2 position) {
			theSpriteBatch.Draw(_mSpriteTexture, position, _size,
			                    Color.White, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
		}
	}
}