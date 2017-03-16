using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MerjTek.WpfIntegration.MonoGame;
using Microsoft.Xna.Framework;
using Puchipro6Visualizer.Game;
using Microsoft.Xna.Framework.Media;

namespace Puchipro6Visualizer.Views {
    class VisualizeVsHumanWindow : VisualizeWindow {
        private readonly Random _random = new Random();
        private Song _bgm;
        private AiProperty[] _aiProperties;

        public VisualizeVsHumanWindow(GameConfig gameConfig) {
            GameConfig = gameConfig;

            GameComboBox.IsEnabled = false;
            TurnComboBox.IsEnabled = false;
            NextGameButton.IsEnabled = false;
            PreviousGameButton.IsEnabled = false;
            NextTurnButton.IsEnabled = false;
            PreviousTurnButton.IsEnabled = false;
            PauseButton.IsEnabled = false;
        }

        public void Visualize(AiProperty player1AiProperty, AiProperty player2AiProperty) {
            Activate();

            _aiProperties = new[] {player1AiProperty, player2AiProperty};

            GameLogger = new Logger {
                EnableWritingConsole = true
            };

            AiLoggers[0] = new AiLogger();
            AiLoggers[1] = new AiLogger();

            Player1NameTextBlock.Text = "Player1";
            Player2NameTextBlock.Text = "Player2";

            StartGame();
        }

        public override void Dispose() {
            MediaPlayer.Stop();
            base.Dispose();
        }

        protected override void UpdateTurnChangeComponent() {}

        protected override void GameControl_OnRender(object sender, GraphicsDeviceEventArgs e) {
            e.GraphicsDevice.Clear(Color.CornflowerBlue);

            if (GameMain == null) return;
            GameMain.Update();
            if (GameMain.CurrentTurnCount <= 0) return;
            for (var i = 0; i < 2; ++i) {
                if (_aiProperties[i].IsHuman) continue;

                var name = AiLoggers[i].OutputLogger.ToString().Split('\n')[0];
                if (i == 0) Player1NameTextBlock.Text = name;
                else Player2NameTextBlock.Text = name;
            }
        }

        protected override void VisualizeWindow_OnClosing(object sender, CancelEventArgs e) {
            base.VisualizeWindow_OnClosing(sender, e);
            MediaPlayer.Stop();
        }

        private void StartGame() {
            GameMain?.Dispose();
            GameConfig.RandomSeed = _random.Next();
            var random = new SpecialRand((uint) GameConfig.RandomSeed);

            var fileNames = new[] {_aiProperties[0].FileName, _aiProperties[1].FileName};
            GameMain = GameMain.Run(GameConfig, fileNames, false, GameLogger, AiLoggers, random, GameControl,
                GameControl, GameControl.GraphicsDevice);
            _bgm = GameMain.Content.Load<Song>("music");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(_bgm);
        }
    }
}
