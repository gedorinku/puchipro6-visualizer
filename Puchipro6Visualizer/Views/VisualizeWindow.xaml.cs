using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MerjTek.WpfIntegration.MonoGame;
using MetroRadiance.UI.Controls;
using Microsoft.Xna.Framework;
using Puchipro6Visualizer.Game;
using Application = System.Windows.Forms.Application;

namespace Puchipro6Visualizer.Views {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class VisualizeWindow : IDisposable {
        //protected AiLogger Ai1AiLogger { get; set; }
        //protected AiLogger Ai2AiLogger { get; set; }
        protected AiLogger[] AiLoggers { get; } = new AiLogger[2];
        protected Logger GameLogger { get; set; }
        protected GameMain GameMain { get; set; }

        private int _currentMatch;
        private bool _isFinishedGame;
        private bool _isForceClosing;
        private bool _isUser = true;
        private bool _paused;
        private bool _isBuilding;
        private GameMain _persistentGameMain;
        private ReplayGameData _playingReplayGameData;
        private int _preTurn = -1;
        private int _preGameIndex = -1;

        public VisualizeWindow() {
            GameConfig = new GameConfig {
                Column = 9,
                Row = 15,
                ColorsNumber = 3,
                MinChain = 1
            };

            InitializeComponent();

            var speedString = GameMain.Speed.Select(speed => "x" + speed).ToList();
            SpeedComboBox.ItemsSource = speedString;
            SpeedComboBox.SelectedIndex = 0;
        }

        public GameConfig GameConfig { get; set; }

        public virtual void Dispose() {
            _isForceClosing = true;
            Close();
        }

        public virtual void Visualize(string replayFileName) {
            Activate();
            GameMain?.Dispose();

            _playingReplayGameData = new ReplayGameData();
            _playingReplayGameData.Load(replayFileName);

            var player1ReplayData = _playingReplayGameData.Matches[0].Player1ReplayData;
            var player1Name = player1ReplayData.OutputLines[0];
            Player1NameTextBlock.Text = player1Name;

            var player2ReplayData = _playingReplayGameData.Matches[0].Player2ReplayData;
            var player2Name = player2ReplayData.OutputLines[0];
            Player2NameTextBlock.Text = player2Name;

            GameConfig = _playingReplayGameData.Matches[0].GameConfig;

            var temp = new List<int>();
            for (var i = 0; i < _playingReplayGameData.MatchesCount; ++i) {
                temp.Add(i);
            }
            _isUser = false;
            GameComboBox.ItemsSource = temp;

            _currentMatch = 0;
            BuildGame(0);

            GameLogger = new Logger {
                EnableWritingConsole = true
            };

            AiLoggers[0] = new AiLogger();

            AiLoggers[1] = new AiLogger();

            StartGame(0);
        }

        protected virtual void GameControl_OnLoaded(object sender, GraphicsDeviceEventArgs e) {}

        protected virtual void GameControl_OnRender(object sender, GraphicsDeviceEventArgs e) {
            e.GraphicsDevice.Clear(Color.CornflowerBlue);

            if (_paused) {
                _persistentGameMain?.Update();
            } else {
                GameMain?.Update();
            }

            if ((GameMain != null) &&
                !_isFinishedGame &&
                GameMain.LazyFinished) {
                Application.DoEvents();

                GameLogger.WriteLine(GameMain.CurrentState.ToString());

                if (_currentMatch == _playingReplayGameData.MatchesCount - 1) {
                    _isFinishedGame = true;
                    return;
                }

                Player1InputTextBox.Text = "";
                Player1OutputTextBox.Text = "";
                Player2InputTextBox.Text = "";
                Player2OutputTextBox.Text = "";

                _currentMatch++;

                var player1ReplayData = _playingReplayGameData.Matches[_currentMatch].Player1ReplayData;
                var player1Name = player1ReplayData.OutputLines[0];
                Player1NameTextBlock.Text = player1Name;

                var player2ReplayData = _playingReplayGameData.Matches[_currentMatch].Player2ReplayData;
                var player2Name = player2ReplayData.OutputLines[0];
                Player2NameTextBlock.Text = player2Name;

                BuildGame(_currentMatch);
                StartGame(_currentMatch);
            }

            if (!_isBuilding) {
                UpdateTurnChangeComponent();
            }
        }

        protected virtual void UpdateTurnChangeComponent() {
            var temp = _paused ? _persistentGameMain : GameMain;
            if (temp == null) return;

            PauseButton.Content = _paused ? "再開" : "一時停止";

            PreviousTurnButton.IsEnabled = 0 < temp.CurrentTurnCount;
            NextTurnButton.IsEnabled = temp.CurrentTurnCount < temp.TurnsCount - 1;

            PreviousGameButton.IsEnabled = 0 < _currentMatch;
            NextGameButton.IsEnabled = _currentMatch < _playingReplayGameData.MatchesCount - 1;

            _isUser = false;
            TurnComboBox.SelectedIndex = temp.CurrentTurnCount;
            GameComboBox.SelectedIndex = _currentMatch;
            _isUser = true;

            if (temp.CurrentTurnCount != _preTurn || _currentMatch != _preGameIndex) {
                _preTurn = temp.CurrentTurnCount;
                _preGameIndex = _currentMatch;

                string player1, player2;
                _persistentGameMain.GetTurnInfoInputString(temp.CurrentTurnCount, out player1, out player2);

                Player1InputTextBox.Text = player1;
                Player2InputTextBox.Text = player2;

                var match = _playingReplayGameData.Matches[_currentMatch];
                var outputs1 = match.Player1ReplayData.OutputLines;
                if (1 < outputs1.Count) {
                    Player1OutputTextBox.Text =
                        outputs1[Math.Min(temp.CurrentTurnCount + 1, outputs1.Count - 1)];
                } else {
                    Player1OutputTextBox.Text = "";
                }

                var outputs2 = match.Player2ReplayData.OutputLines;
                if (1 < outputs2.Count) {
                    Player2OutputTextBox.Text =
                        outputs2[Math.Min(temp.CurrentTurnCount + 1, outputs2.Count - 1)];
                } else {
                    Player2OutputTextBox.Text = "";
                }
            }
        }

        private void BuildGame(int gameIndex) {
            _isBuilding = true;

            GameComboBox.IsEnabled = false;
            TurnComboBox.IsEnabled = false;
            PauseButton.IsEnabled = false;
            PreviousGameButton.IsEnabled = false;
            PreviousTurnButton.IsEnabled = false;
            NextGameButton.IsEnabled = false;
            NextTurnButton.IsEnabled = false;

            _persistentGameMain?.Dispose();

            var random = _playingReplayGameData.Matches[gameIndex].GameRandom;

            _persistentGameMain = GameMain.Run(GameConfig, _playingReplayGameData.Matches[gameIndex],
                new Logger(), new[] {new AiLogger(), new AiLogger()}, true, GameControl, GameControl,
                GameControl.GraphicsDevice, true, new SpecialRand(random));
            _persistentGameMain.BuildPersistentFieldCaches();

            _isUser = false;
            var temp = new List<int>();
            Console.Error.WriteLine(_persistentGameMain.TurnsCount + "built!");
            Console.WriteLine(1);
            for (var i = 0; i < _persistentGameMain.TurnsCount; ++i) {
                temp.Add(i);
            }
            TurnComboBox.ItemsSource = temp;
            _isUser = true;
        }

        private void StartGame(int gameIndex) {
            GameMain?.Dispose();
            _paused = false;
            _currentMatch = gameIndex;

            var random = _playingReplayGameData.Matches[gameIndex].GameRandom;

            GameMain = GameMain.Run(GameConfig, _playingReplayGameData.Matches[gameIndex],
                GameLogger, AiLoggers, false, GameControl, GameControl,
                GameControl.GraphicsDevice, false, new SpecialRand(random));

            GameComboBox.IsEnabled = true;
            TurnComboBox.IsEnabled = true;
            PauseButton.IsEnabled = true;
            PreviousGameButton.IsEnabled = true;
            PreviousTurnButton.IsEnabled = true;
            NextGameButton.IsEnabled = true;
            NextTurnButton.IsEnabled = true;

            _isBuilding = false;
        }

        #region ボタンとかのイベントハンドラ

        private void GameControl_OnSizeChanged(object sender, SizeChangedEventArgs e) {
            const double ratio = 3.0d / 4.0d;
            GameControl.Height = e.NewSize.Width * ratio;
        }

        protected virtual void VisualizeWindow_OnClosing(object sender, CancelEventArgs e) {
            _persistentGameMain?.Dispose();
            _persistentGameMain = null;
            GameMain?.Dispose();
            GameMain = null;

            AiLoggers[0]?.Dispose();
            AiLoggers[1]?.Dispose();
            GameLogger?.Dispose();

            if (_isForceClosing) return;
            e.Cancel = true;
            Hide();
        }


        private void GameLoggerOnOnLogAdded(string str) {
            Window.Dispatcher.BeginInvoke(new Action(() => {
                GameLogTextBox.AppendText(str);
                GameLogTextBox.ScrollToEnd();
            }));
        }

        private void Player1OutputLoggerOnOnLogAdded(string str) {
            Window.Dispatcher.BeginInvoke(new Action(() => {
                Player1OutputTextBox.AppendText(str);
                Player1OutputTextBox.ScrollToEnd();
            }));
        }

        private void Player1InputLoggerOnOnLogAdded(string str) {
            Window.Dispatcher.BeginInvoke(new Action(() => {
                Player1InputTextBox.AppendText(str);
                Player1InputTextBox.ScrollToEnd();
            }));
        }

        private void Player2OutputLoggerOnOnLogAdded(string str) {
            Window.Dispatcher.BeginInvoke(new Action(() => {
                Player2OutputTextBox.AppendText(str);
                Player2OutputTextBox.ScrollToEnd();
            }));
        }

        private void Player2InputLoggerOnOnLogAdded(string str) {
            Window.Dispatcher.BeginInvoke(new Action(() => {
                Player2InputTextBox.AppendText(str);
                Player2InputTextBox.ScrollToEnd();
            }));
        }

        private void PreviousTurnButton_OnClick(object sender, RoutedEventArgs e) {
            if (_paused) {
                if (0 < _persistentGameMain.CurrentTurnCount) {
                    _persistentGameMain.GoToTurn(_persistentGameMain.CurrentTurnCount - 1);
                }
            } else {
                _paused = true;
                if (0 < GameMain.CurrentTurnCount) {
                    _persistentGameMain.GoToTurn(GameMain.CurrentTurnCount - 1);
                }
            }

            UpdateTurnChangeComponent();
        }

        private void NextTurnButton_OnClick(object sender, RoutedEventArgs e) {
            if (_paused) {
                if (_persistentGameMain.CurrentTurnCount < _persistentGameMain.TurnsCount - 1) {
                    _persistentGameMain.GoToTurn(_persistentGameMain.CurrentTurnCount + 1);
                }
            } else {
                _paused = true;
                if (GameMain.CurrentTurnCount < GameMain.TurnsCount - 1) {
                    _persistentGameMain.GoToTurn(GameMain.CurrentTurnCount + 1);
                }
            }

            UpdateTurnChangeComponent();
        }

        private void PreviousGameButton_OnClick(object sender, RoutedEventArgs e) {
            if (_currentMatch <= 0) return;
            _currentMatch--;
            BuildGame(_currentMatch);
            StartGame(_currentMatch);

            UpdateTurnChangeComponent();
        }

        private void NextGameButton_OnClick(object sender, RoutedEventArgs e) {
            if (_playingReplayGameData.MatchesCount - 1 <= _currentMatch) return;
            _currentMatch++;
            BuildGame(_currentMatch);
            StartGame(_currentMatch);

            UpdateTurnChangeComponent();
        }

        private void PauseButton_OnClick(object sender, RoutedEventArgs e) {
            if (_paused) {
                _paused = false;
                BuildGame(_currentMatch);
                StartGame(_currentMatch);

                UpdateTurnChangeComponent();
            } else {
                _paused = true;
                _persistentGameMain.GoToTurn(GameMain.CurrentTurnCount);
            }
        }

        private void TurnComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!_isUser) return;

            var comboBox = sender as PromptComboBox;
            _paused = true;
            _persistentGameMain.GoToTurn(comboBox.SelectedIndex);

            _isUser = true;
        }

        private void GameComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!_isUser) return;

            var comboBox = sender as PromptComboBox;

            //SelectedIndexが戻っちゃうので（？？）
            var index = comboBox.SelectedIndex;
            BuildGame(index);
            StartGame(index);

            _isUser = false;
            comboBox.SelectedIndex = index;
            _isUser = true;
        }

        private void SpeedComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = sender as PromptComboBox;
            GameMain.CurrentSpeed = GameMain.Speed[comboBox.SelectedIndex];
        }

        #endregion
    }
}