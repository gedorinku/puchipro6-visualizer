using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    class Ball : EntityBase {
        private Texture2D _ballTexture;

        private Color _color = Color.Blue;
        private int _fallingTime;
        private float _motionY;

        private float _targetY = float.NaN;

        public Ball(Ball origin)
            : base(origin.SpriteBatch, origin.Camera, origin.GameMain, origin.CurrentField) {
            X = origin.X;
            Y = origin.Y;
            Color = origin.Color;
            Size = origin.Size;
            _fallingTime = origin._fallingTime;
            _motionY = origin._motionY;
            TargetY = origin.TargetY;
            BallTexture = origin.BallTexture;
        }

        public Ball(SpriteBatch spriteBatch, Camera camera, GameMain gameMain, Field currentField)
            : base(spriteBatch, camera, gameMain, currentField) {}

        public Color Color {
            get { return _color; }
            set {
                if (value.A == 0) throw new ArgumentException();
                _color = value;
            }
        }

        public float Size { get; set; } = 64;
        public bool IsFalling { get; private set; }
        public bool IsOnMouse { get; set; }

        /// <summary>
        ///     この座標まで玉は落下する。
        /// </summary>
        public float TargetY {
            get { return _targetY; }
            set {
                _fallingTime = 0;
                _targetY = value;
            }
        }

        public Texture2D BallTexture {
            get {
                if (_ballTexture == null) {
                    LoadContent();
                }
                return _ballTexture;
            }
            set { _ballTexture = value; }
        }

        public override void LoadContent() {
            BallTexture = GameMain.Content?.Load<Texture2D>("ball");
        }

        public override void Update(GameTime gameTime) {
            Fall();
        }

        public override void Render(GameTime gameTime) {
            if (IsDeath) return;

            var pos = Camera.ToRenderPosition(new Vector2(X, Y));
            var scale = new Vector2(Camera.RatioX, Camera.RatioY) * Size / BallTexture.Width;
            if (IsOnMouse) {
                SpriteBatch.Draw(BallTexture, pos, scale: scale, color: Color.White);
            }
            SpriteBatch.Draw(BallTexture, pos, scale: scale, color: Color);
        }


        private void Fall() {
            if (float.IsNaN(TargetY) || (Y == TargetY)) {
                IsFalling = false;
                return;
            }

            if (GameMain.EnableSkippingRender) {
                Y = TargetY;
            }

            _fallingTime++;
            IsFalling = true;

            if (_fallingTime < 60 / GameMain.CurrentSpeed) {
                return;
            }

            var gravity = 0.3f * (float) GameMain.CurrentSpeed;
            _motionY += gravity;
            if (TargetY < Y + _motionY) {
                Y = TargetY;
                IsFalling = false;
                return;
            }

            Y += _motionY;
        }
    }
}