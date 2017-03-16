using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    abstract class EntityBase : IRenderable {
        protected EntityBase(SpriteBatch spriteBatch, Camera camera, GameMain gameMain, Field currentField) {
            SpriteBatch = spriteBatch;
            Camera = camera;
            CurrentField = currentField;
            GameMain = gameMain;
        }

        public float X { get; set; }
        public float Y { get; set; }
        public bool IsDeath { get; set; }
        public SpriteBatch SpriteBatch { get; set; }
        public Camera Camera { get; }
        public GameMain GameMain { get; }
        public Field CurrentField { get; }

        public abstract void Update(GameTime gameTime);

        public abstract void Render(GameTime gameTime);

        public abstract void LoadContent();
    }
}