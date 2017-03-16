using System;
using System.Threading.Tasks;
using System.Windows.Input;
using MerjTek.WpfIntegration.Xaml;
using Microsoft.Xna.Framework;
using Cursor = System.Windows.Forms.Cursor;
using Point = System.Windows.Point;

namespace Puchipro6Visualizer.Game {
    class HumanPlayer : Player {
        private readonly Camera _camera;
        private readonly MonoGameControl _gameControl;

        public HumanPlayer(Field field, MonoGameControl gameControl, Camera camera, GameMain gameMain) : base(field, gameMain) {
            _gameControl = gameControl;
            _camera = camera;
            _gameControl.MouseLeftButtonUp += GameControlOnMouseLeftButtonUp;
            _gameControl.MouseMove += GameControlOnMouseMove;
        }

        private void GameControlOnMouseMove(object sender, MouseEventArgs mouseEventArgs) {
            if (!IsRunning) return;

            var screenPoint = new Point(Cursor.Position.X, Cursor.Position.Y);
            var controlPoint = _gameControl.PointFromScreen(screenPoint);
            var ratioX = _camera.WorldWidth / _gameControl.ActualWidth;
            var ratioY = _camera.WorldHeight / _gameControl.ActualHeight;
            var worldPosition = new Vector2((float)(controlPoint.X * ratioX),
                (float)(controlPoint.Y * ratioY));
            Microsoft.Xna.Framework.Point bordPos;

            if (!CurrentField.ToBordPointFromRenderPosition(worldPosition, out bordPos) ||
                !CurrentField.IsInFieldRange(bordPos.X, bordPos.Y)) {
                return;
            }

            foreach (var ball in CurrentField.GetBalls()) {
                ball.IsOnMouse = false;
            }

            var gameConfig = CurrentField.GameConfig;
            {
                var visit = new bool[gameConfig.Column + 1, gameConfig.Row + 1];
                var tempCount = CurrentField.DfsDestroyableBalls(bordPos.X, bordPos.Y, visit);
                if (tempCount < gameConfig.MinChain) {
                    return;
                }
            }

            {
                var visit = new bool[CurrentField.Column + 1, CurrentField.Row + 1];
                HighlightSelectedBalls(bordPos.X, bordPos.Y, visit);
            }
        }

        private void HighlightSelectedBalls(int x, int y, bool[,] visit) {
            int[] dx = {-1, 0, 1, 0};
            int[] dy = {0, 1, 0, -1};
            visit[x, y] = true;
            var ball = CurrentField.GetBall(x, y);
            if (ball is OjamaBall) return;
            var color = ball.Color;
            ball.IsOnMouse = true;

            for (var i = 0; i < 4; ++i) {
                var tx = x + dx[i];
                var ty = y + dy[i];
                if (!CurrentField.IsInFieldRange(tx, ty) ||
                    visit[tx, ty] ||
                    CurrentField.GetBall(tx, ty).Color != color) continue;

                HighlightSelectedBalls(tx, ty, visit);
            }
        }

        private void GameControlOnMouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs mouseButtonEventArgs) {
            if (!IsRunning) return;

            var screenPoint = new Point(Cursor.Position.X, Cursor.Position.Y);
            var controlPoint = _gameControl.PointFromScreen(screenPoint);
            var ratioX = _camera.WorldWidth / _gameControl.ActualWidth;
            var ratioY = _camera.WorldHeight / _gameControl.ActualHeight;
            var worldPosition = new Vector2((float) (controlPoint.X * ratioX),
                (float) (controlPoint.Y * ratioY));
            Microsoft.Xna.Framework.Point bordPos;

            if (!CurrentField.ToBordPointFromRenderPosition(worldPosition, out bordPos) ||
                !CurrentField.IsInFieldRange(bordPos.X, bordPos.Y)) return;

            var gameConfig = CurrentField.GameConfig;
            var visit = new bool[gameConfig.Column + 1, gameConfig.Row + 1];
            var tempCount = CurrentField.DfsDestroyableBalls(bordPos.X, bordPos.Y, visit);
            if (tempCount < gameConfig.MinChain) {
                return;
            }

            var ball = CurrentField.GetBall(bordPos.X, bordPos.Y);
            if (ball is OjamaBall) return;
            OutPut = bordPos;
            IsRunning = false;
        }

        public override int LeftMilliseconds => int.MaxValue;

        public override void Dispose() {
            _gameControl.MouseLeftButtonUp -= GameControlOnMouseLeftButtonUp;

            base.Dispose();
        }

        public override async void InitializeGame() {}

        public override async void StartTurn() {
            IsRunning = true;

            while (IsRunning) {
                Task.Delay(1);
            }
        }
    }
}