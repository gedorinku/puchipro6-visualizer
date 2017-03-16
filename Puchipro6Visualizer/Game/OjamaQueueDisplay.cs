using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    class OjamaQueueDisplay : EntityBase {
        private SpriteFont _font;
        private Texture2D _hardOjamaTexture;
        private Texture2D _ojamaTexture;

        public OjamaQueueDisplay(SpriteBatch spriteBatch, Camera camera, GameMain gameMain, Field currentField)
            : base(spriteBatch, camera, gameMain, currentField) {}

        public override void Update(GameTime gameTime) {}

        public override void Render(GameTime gameTime) {
            var scale = new Vector2(Camera.RatioX, Camera.RatioY) * 32.0f / _ojamaTexture.Width;
            var gameConfig = GameMain.GameConfig;

            for (var x = 1; x <= gameConfig.Column; ++x) {
                var ojamas = CurrentField.GetOjamasInQueue(x);
                var ball = CurrentField.GetBall(x, 1);
                var pos = Camera.ToRenderPosition(new Vector2(ball.X, 0.0f));
                SpriteBatch.Draw(_ojamaTexture, pos, scale: scale * 0.8f, color: Color.Black);

                var ojamaCount = ojamas.Count(ojama => !ojama.IsHard);
                SpriteBatch.DrawString(_font, "x" + ojamaCount, pos,
                    Color.White, 0.0f, Vector2.Zero, scale * 4.0f, SpriteEffects.None, 0.0f);

                pos.Y += _ojamaTexture.Height * scale.Y;
                var hardOjamaCount = ojamas.Count(ojama => ojama.IsHard);
                SpriteBatch.Draw(_hardOjamaTexture, pos, scale: scale * 0.8f, color: Color.Black);
                SpriteBatch.DrawString(_font, "x" + hardOjamaCount, pos,
                    Color.White, 0.0f, Vector2.Zero, scale * 4.0f, SpriteEffects.None, 0.0f);
            }
        }

        public override void LoadContent() {
            _font = GameMain.Content.Load<SpriteFont>("SegoeUILight");

            var content = GameMain.Content;
            _ojamaTexture = content.Load<Texture2D>("ball");
            _hardOjamaTexture = content.Load<Texture2D>("ojama_hard");
        }
    }
}