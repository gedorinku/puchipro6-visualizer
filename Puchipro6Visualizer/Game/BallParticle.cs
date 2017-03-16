using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    class BallParticle : EntityBase {
        private readonly Ball _ball;
        private Color _color;
        private float _renderRate;

        public BallParticle(SpriteBatch spriteBatch, Camera camera, GameMain gameMain, Field currentField, Ball ball)
            : base(spriteBatch, camera, gameMain, currentField) {
            _ball = ball;
            _color = ball.Color;
            X = _ball.X;
            Y = _ball.Y;

            _renderRate = 1.0f;
        }

        public override void Update(GameTime gameTime) {}

        public override void Render(GameTime gameTime) {
            if (IsDeath) return;

            var texture = _ball.BallTexture;
            var pos = Camera.ToRenderPosition(new Vector2(X, Y));
            var scale = new Vector2(Camera.RatioX, Camera.RatioY) * _ball.Size / texture.Width *
                        _renderRate;

            SpriteBatch.Draw(texture, pos,
                origin: new Vector2(texture.Width * scale.X / 2.0f, texture.Height * scale.Y / 2.0f),
                scale: scale, color: _color);

            _renderRate += 0.02f * (float) GameMain.CurrentSpeed;
            _color.A = (byte) Math.Max(_color.A - 10, 0);

            if (1.5f <= _renderRate) {
                IsDeath = true;
            }
        }

        public override void LoadContent() {}
    }
}