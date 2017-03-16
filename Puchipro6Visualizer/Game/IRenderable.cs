using Microsoft.Xna.Framework;

namespace Puchipro6Visualizer.Game {
    interface IRenderable {
        void Update(GameTime gameTime);
        void Render(GameTime gameTime);
    }
}