using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    class VictoryDisplay : EntityBase {
        private Texture2D _texture;
        private SpriteFont _boldFont;
        private int _animateTicks;

        public VictoryDisplay(SpriteBatch spriteBatch, Camera camera, GameMain gameMain,
            Field currentField) : base(spriteBatch, camera, gameMain, currentField) {
            
        }

        public bool IsAnimating { get; set; }

        public string DisplayString { get; set; } = "hoge";

        public override void Update(GameTime gameTime) {
            if (GameMain.CurrentState == GameMain.GameStateEnum.Running)
                return;

            _animateTicks++;
            if (180 / GameMain.CurrentSpeed < _animateTicks) {
                IsAnimating = false;
                return;
            }
            IsAnimating = true;
        }

        public override void Render(GameTime gameTime) {
            if (GameMain.CurrentState == GameMain.GameStateEnum.Running)
               return;

            var pos = Camera.ToRenderPosition(new Vector2(X, Y));
            var boxPos = Camera.ToRenderPosition(new Vector2(CurrentField.X, pos.Y));
            SpriteBatch.Draw(_texture, boxPos, Color.FromNonPremultiplied(255, 255, 255, 100));
            SpriteBatch.DrawString(_boldFont, DisplayString, pos, Color.Black, 0.0f, Vector2.Zero,
                new Vector2(0.3f), SpriteEffects.None, 0.0f);
        }

        public override void LoadContent() {
            _texture = new Texture2D(SpriteBatch.GraphicsDevice, 340, 30);

            var data = new Color[_texture.Width * _texture.Height];
            for (var i = 0; i < data.Length; ++i) {
                data[i] = Color.White;
            }
            _texture.SetData(data);

            _boldFont = GameMain.Content.Load<SpriteFont>("SegoeUILargeBold");
        }
    }
}
