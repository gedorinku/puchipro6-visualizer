using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    class Camera {
        public Camera(GraphicsDevice graphicsDevice, int worldWidth = 800, int worldHeight = 600) {
            GraphicsDevice = graphicsDevice;
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public int WorldWidth { get; }
        public int WorldHeight { get; }

        /// <summary>
        ///     描画領域の幅 / WorldWidthを取得する。
        /// </summary>
        public float RatioX {
            get {
                var viewport = GraphicsDevice.Viewport;
                return viewport.Width / (float) WorldWidth;
            }
        }

        /// <summary>
        ///     描画領域の高さ / WorldHeightを取得する。
        /// </summary>
        public float RatioY {
            get {
                var viewport = GraphicsDevice.Viewport;
                return viewport.Height / (float) WorldHeight;
            }
        }

        public Vector2 ToRenderPosition(Vector2 worldPosition)
            => new Vector2(worldPosition.X * RatioX, worldPosition.Y * RatioY);

        public Point ToRenderPositionPoint(Vector2 worldPosition)
            => new Point((int) (worldPosition.X * RatioX), (int) (worldPosition.Y * RatioY));

        public Vector2 ToWorldPosition(Point point)
            => new Vector2(point.X / RatioX, point.Y / RatioY);

        public Vector2 ToWorldPosition(System.Windows.Point point)
            => new Vector2((float) point.X / RatioX, (float) point.Y / RatioY);

        public System.Windows.Point ToWindowsPoint(Point xnaPoint)
            => new System.Windows.Point(xnaPoint.X, xnaPoint.Y);

        public Point ToWindowsPoint(System.Windows.Point windowsPoint)
            => new Point((int) windowsPoint.X, (int) windowsPoint.Y);
    }
}