using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    class OjamaBall : Ball {
        private bool _isHard;

        public OjamaBall(OjamaBall ojamaBall)
            : base(
                ojamaBall.SpriteBatch, ojamaBall.Camera, ojamaBall.GameMain, ojamaBall.CurrentField) {
            Color = ojamaBall.Color;
            IsHard = ojamaBall.IsHard;
        }

        public OjamaBall(SpriteBatch spriteBatch, Camera camera, GameMain gameMain, Field currentField)
            : base(spriteBatch, camera, gameMain, currentField) {
            Color = Color.Black;
        }

        public bool IsHard {
            get { return _isHard; }
            set {
                _isHard = value;
                if (GameMain.EnableSkippingRender) return;
                if (_isHard) {
                    BallTexture = GameMain.Content.Load<Texture2D>("ojama_hard");
                    return;
                }
                BallTexture = GameMain.Content.Load<Texture2D>("ball");
            }
        }

        public override void LoadContent() {
            base.LoadContent();

            if (IsHard) {
                BallTexture = GameMain.Content?.Load<Texture2D>("ojama_hard");
            }
        }
    }
}