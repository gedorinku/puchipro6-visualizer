using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    class CrossParticle : EntityBase {
        private Color _color = Color.White;
        private Texture2D _texture;

        public CrossParticle(SpriteBatch spriteBatch, Camera camera, GameMain gameMain, Field currentField)
            : base(spriteBatch, camera, gameMain, currentField) {}

        public bool IsPersistent { get; set; }

        public override void Update(GameTime gameTime) {}

        public override void Render(GameTime gameTime) {
            var pos = Camera.ToRenderPosition(new Vector2(X, Y));
            var scale = new Vector2(Camera.RatioX, Camera.RatioY) * 32.0f / _texture.Width;
            SpriteBatch.Draw(_texture, pos, scale: scale, color: _color);

            if (IsPersistent) return;

            var da = (int) (-6 * GameMain.CurrentSpeed);
            _color.A = (byte) Math.Max(_color.A + da, 0);
            if (_color.A == 0) {
                IsDeath = true;
            }
        }

        public override void LoadContent() {
            var content = GameMain.Content;
            _texture = content.Load<Texture2D>("cross");
        }
    }
}