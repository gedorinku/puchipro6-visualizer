using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Puchipro6Visualizer.Game;
using Application = System.Windows.Application;
using MessageBox = System.Windows.Forms.MessageBox;
using TabControl = System.Windows.Controls.TabControl;

namespace Puchipro6Visualizer.Views {
    /// <summary>
    ///     MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow {
        private readonly string _replayFileFirectory =
            Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
            "/puchipro6/replay/";

        private readonly TaskFactory _taskFactory;
        private AiLogger _ai1AiLogger;
        private AiLogger _ai2AiLogger;
        private Logger _gameLogger;
        private GameMain _gameMain;
        private Task _gameTask;

        private int _matchesCount = 1;


        private int _preSelectedTabIndex = -1;
        private readonly Random _random = new Random();
        private CancellationToken _token;
        private CancellationTokenSource _tokenSource;
        private VisualizeWindow _visualizeWindow;

        public MainWindow() {
            GameConfig = new GameConfig {
                Column = 9,
                Row = 15,
                ColorsNumber = 3,
                MinChain = 5
            };

            _taskFactory = new TaskFactory();
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            _gameLogger = new Logger();

            InitializeComponent();

            //var uri = new Uri("pack://application:,,,/Resources/License.txt");
            var uri = new Uri("../Resources/License.txt", UriKind.Relative);
            var resourceStream = Application.GetResourceStream(uri);

            using (var reader = new StreamReader(resourceStream.Stream)) {
                var text = reader.ReadToEnd();
                LicenseTextBox.Text = text;
            }
        }

        public GameConfig GameConfig { get; set; }

        public int MatchesCount {
            get { return _matchesCount; }
            set { _matchesCount = Math.Max(1, value); }
        }

        public string Player1FileName { get; set; }

        public string Player2FileName { get; set; }

        public string TournamentPlayerFileName { get; set; }

        private void Player1OpenFileButton_Click(object sender, RoutedEventArgs e) {
            Player1FileNameTextBox.Text = OpenFile();
        }

        private void Player2OpenFileButton_Click(object sender, RoutedEventArgs e) {
            Player2FileNameTextBox.Text = OpenFile();
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            try {
                _tokenSource?.Cancel();
                _gameTask?.Wait();
                _gameMain?.Dispose();
                _visualizeWindow?.Dispose();
            }
            catch (Exception exception) {
                Console.WriteLine(exception);
            }
        }

        private void RunButton_OnClick(object sender, RoutedEventArgs e) {
            StopButton.IsEnabled = true;
            RunButton.IsEnabled = false;
            ColumnTextBox.IsEnabled = false;
            RowTextBox.IsEnabled = false;
            RandomTextBox.IsEnabled = false;
            MinChainTextBox.IsEnabled = false;
            ColorNumberTextBox.IsEnabled = false;
            MatchNumTextBox.IsEnabled = false;

            ReplayButton.IsEnabled = false;

            RunGameVsHuman();
            return;

            var f1 = Player1FileName;
            var f2 = Player2FileName;

            if ((f1.Length == 0) || (f2.Length == 0)) {
                var message = "実行ファイル名が無効です．";
                var title = "Puchipro6 Visualizer";
                MessageBox.Show(message, title,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                StopGame();
                return;
            }

            var fileNames = new[] {
                f1, f2
            };


            _gameLogger = new Logger();
#if DEBUG
            _gameLogger.EnableWritingConsole = true;
#endif
            _gameLogger.OnLogAdded += GameLoggerOnOnLogAdded;
            GameLogTextBox.Text = "初期化中...\n";

            BattleProgressTextBox.Text = "";

            _ai1AiLogger = new AiLogger();
            _ai1AiLogger.InputLogger.OnLogAdded += Player1InputLoggerOnOnLogAdded;
            _ai1AiLogger.OutputLogger.OnLogAdded += Player1OutputLoggerOnOnLogAdded;
            _ai1AiLogger.ErrorOutputLogger.OnLogAdded += Player1ErrorOutputLoggerOnOnLogAdded;

            Player1InputTextBox.Text = "";
            Player1OutputTextBox.Text = "";
            Player1ErrorOutputTextBox.Text = "";

            _ai2AiLogger = new AiLogger();
            _ai2AiLogger.InputLogger.OnLogAdded += Player2InputLoggerOnOnLogAdded;
            _ai2AiLogger.OutputLogger.OnLogAdded += Player2OutputLoggerOnOnLogAdded;
            _ai2AiLogger.ErrorOutputLogger.OnLogAdded += Player2ErrorOutputLoggerOnOnLogAdded;

            Player2InputTextBox.Text = "";
            Player2OutputTextBox.Text = "";
            Player2ErrorOutputTextBox.Text = "";

            _gameMain = GameMain.Run(GameConfig, fileNames, true, _gameLogger,
                new[] {_ai1AiLogger, _ai2AiLogger}, new SpecialRand((uint) GameConfig.RandomSeed));
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            _gameTask = _taskFactory.StartNew(RunGame, _token);
        }

        private async void StopButton_OnClick(object sender, RoutedEventArgs e) {
            _tokenSource.Cancel();
            try {
                await _taskFactory.StartNew(() => { _gameTask?.Wait(); });
            }
            catch (AggregateException exception)
                when (exception.InnerExceptions.Any(item => item is OperationCanceledException)) {}

            StopGame();
        }

        private void RunGameVsHuman() {
            Window.Dispatcher.BeginInvoke(new Action(() => {
                RandomTextBox.Text = _random.Next() + "";

                var visualizeWindow = new VisualizeVsHumanWindow(GameConfig);
                visualizeWindow.Show();
                var player1Property = Player1IsHumanCheckBox.IsChecked.GetValueOrDefault()
                    ? AiProperty.CreateHumanProperty()
                    : AiProperty.CreateProperty(Player1FileName);
                var player2Property = Player2IsHumanCheckBox.IsChecked.GetValueOrDefault()
                    ? AiProperty.CreateHumanProperty()
                    : AiProperty.CreateProperty(Player2FileName);
                visualizeWindow.Visualize(player1Property, player2Property);
                _visualizeWindow?.Close();
                _visualizeWindow = visualizeWindow;

                StopGame();
            }));
        }

        private void RunGame() {
            var replayGameData = new ReplayGameData();

            var currentRandom = new SpecialRand((uint) GameConfig.RandomSeed);
            GameConfig.Player1WonCount = 0;
            GameConfig.Player2WonCount = 0;

            for (var i = 0; i < MatchesCount; ++i)
                while (true) {
                    Thread.Sleep(30);
                    if (_token.IsCancellationRequested) return;
                    _gameMain.Update();

                    if (_gameMain.CurrentState != GameMain.GameStateEnum.Running) {
                        _gameMain.GameLogger.WriteLine(_gameMain.CurrentState.ToString());

                        _ai1AiLogger.WaitEvents();
                        _ai2AiLogger.WaitEvents();
                        _gameLogger.WaitEvent();

                        var aiPlayer1 = _gameMain.GetPlayer(0) as AiPlayer;
                        var aiPlayer2 = _gameMain.GetPlayer(1) as AiPlayer;

                        var player1Replay = new ReplayPlayerData {
                            LeftTimeOnLaunched = aiPlayer1.LeftTimeOnLaunched,
                            OutputLines = aiPlayer1.AiOutputs,
                            LeftThinkTimes = aiPlayer1.LeftThinkTimes
                        };

                        var player2Replay = new ReplayPlayerData {
                            LeftTimeOnLaunched = aiPlayer2.LeftTimeOnLaunched,
                            OutputLines = aiPlayer2.AiOutputs,
                            LeftThinkTimes = aiPlayer2.LeftThinkTimes
                        };

                        var match = new ReplayMatchData {
                            Player1ReplayData = player1Replay,
                            Player2ReplayData = player2Replay,
                            GameRandom = currentRandom,
                            GameConfig = new GameConfig(GameConfig)
                        };

                        replayGameData.Matches.Add(match);
                        currentRandom = new SpecialRand(_gameMain.FixedRandom);

                        Window.Dispatcher.BeginInvoke(new Action(() => {
                            System.Windows.Forms.Application.DoEvents();

                            if (_gameMain.CurrentState == GameMain.GameStateEnum.Player1Won)
                                GameConfig.Player1WonCount++;
                            else if (_gameMain.CurrentState == GameMain.GameStateEnum.Player2Won)
                                GameConfig.Player2WonCount++;

                            BattleProgressTextBox.AppendText("Player1 " + GameConfig.Player1WonCount +
                                                             "-" +
                                                             GameConfig.Player2WonCount +
                                                             " Player2\n");
                            BattleProgressTextBox.ScrollToEnd();
                        })).Wait();

                        _gameMain.Dispose();

                        if (i == MatchesCount - 1) break;

                        var fileNames = new[]
                            {Player1FileName, Player2FileName};

                        _gameMain = GameMain.Run(GameConfig, fileNames, true, _gameLogger,
                            new[] {_ai1AiLogger, _ai2AiLogger}, _gameMain.FixedRandom);

                        break;
                    }
                }

            var currentTime = DateTime.Now;

            var replayFileName = new StringBuilder();
            replayFileName.Append(_replayFileFirectory);
            replayFileName.Append(currentTime.Year);
            replayFileName.Append($"{currentTime.Month:D2}");
            replayFileName.Append($"{currentTime.Day:D2}_");
            replayFileName.Append($"{currentTime.Hour:D2}");
            replayFileName.Append($"{currentTime.Minute:D2}");
            replayFileName.Append($"{currentTime.Second:D2}_");
            replayFileName.Append(GameConfig.Player1WonCount + "-" + GameConfig.Player2WonCount +
                                  ".txt");

            replayGameData.Save(replayFileName.ToString());

            Window.Dispatcher.BeginInvoke(new Action(() => {
                if (_visualizeWindow == null) _visualizeWindow = new VisualizeWindow();
                else _visualizeWindow.Close();
                _visualizeWindow.Show();
                _visualizeWindow.Visualize(replayFileName.ToString());

                StopGame();
            }));
        }

        private void StopGame() {
            _gameMain?.Dispose();
            _gameMain = null;

            _ai1AiLogger?.Dispose();
            _ai2AiLogger?.Dispose();
            _gameLogger?.Dispose();

            StopButton.IsEnabled = false;
            RunButton.IsEnabled = true;

            ReplayButton.IsEnabled = true;

            ColumnTextBox.IsEnabled = true;
            RowTextBox.IsEnabled = true;
            RandomTextBox.IsEnabled = true;
            MinChainTextBox.IsEnabled = true;
            ColorNumberTextBox.IsEnabled = true;
            MatchNumTextBox.IsEnabled = true;
        }


        private void Player1ErrorOutputLoggerOnOnLogAdded(string str) {
            Window.Dispatcher.BeginInvoke(new Action(() => {
                Player1ErrorOutputTextBox.AppendText(str);
                Player1ErrorOutputTextBox.ScrollToEnd();
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

        private void Player2ErrorOutputLoggerOnOnLogAdded(string str) {
            Window.Dispatcher.BeginInvoke(new Action(() => {
                Player2ErrorOutputTextBox.AppendText(str);
                Player2ErrorOutputTextBox.ScrollToEnd();
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

        private void GameLoggerOnOnLogAdded(string str) {
            Window.Dispatcher.BeginInvoke(new Action(() => {
                GameLogTextBox.AppendText(str);
                GameLogTextBox.ScrollToEnd();
            }));
        }

        private static string OpenFile() {
            var fileDialog = new OpenFileDialog {
                Title = "実行ファイルを開く",
                Filter = "実行ファイル|*.exe"
            };
            fileDialog.ShowDialog();
            return fileDialog.FileName;
        }

        private void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var tab = sender as TabControl;
            if (_preSelectedTabIndex == tab.SelectedIndex) return;

            _preSelectedTabIndex = tab.SelectedIndex;

            if ((tab.SelectedIndex == 1) && Directory.Exists(_replayFileFirectory)) {
                var files =
                    Directory.GetFiles(_replayFileFirectory)
                        .Select(s => new ReplayFile {FilePath = s})
                        .ToList();
                var data = new ObservableCollection<ReplayFile>(files);
                ReplayDataGrid.ItemsSource = data;
            }
        }

        private void ReplayDataGrid_OnAutoGeneratingColumn(object sender,
            DataGridAutoGeneratingColumnEventArgs e) {
            switch (e.PropertyName) {
                case "FileName":
                    e.Column.Header = "リプレイファイル名";
                    break;
                default:
                    e.Cancel = true;
                    break;
            }
        }

        private void ReplayButton_OnClick(object sender, RoutedEventArgs e) {
            if (ReplayDataGrid.SelectedValue == null) return;
            var replayFileName = ReplayDataGrid.SelectedItem as ReplayFile;

            if ((_visualizeWindow == null) || !_visualizeWindow.IsVisible) {
                _visualizeWindow?.Close();
                _visualizeWindow = new VisualizeWindow();
                _visualizeWindow.Show();
            }
            _visualizeWindow.Visualize(replayFileName.FilePath);
        }

        private void PlayerFileNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e) {
            if (_gameMain != null) return;
            RunButton.IsEnabled = IsReadyToRun();
        }

        private bool IsReadyToRun()
            =>
            ((Player1FileName != null) ||
             ((Player1IsHumanCheckBox.IsChecked != null) && Player1IsHumanCheckBox.IsChecked.Value)) &&
            ((Player2FileName != null) ||
             ((Player2IsHumanCheckBox.IsChecked != null) && Player2IsHumanCheckBox.IsChecked.Value));

        private async void RunTournamentButton_OnClick(object sender, RoutedEventArgs e) {
            RunTournamentButton.IsEnabled = false;

            await _taskFactory.StartNew(() => {
                var tournamentRunner = new TournamentRunner();
                tournamentRunner.GameConfig = GameConfig;
                tournamentRunner.Run("tournament.txt");
            });

            RunTournamentButton.IsEnabled = true;
        }

        private void Player1IsHumanCheckBox_OnChanged(object sender, RoutedEventArgs e) {
            var isChecked = Player1IsHumanCheckBox.IsChecked;
            if (isChecked == null) return;
            Player1FileNameTextBox.IsEnabled = !isChecked.Value;
            Player1OpenFileButton.IsEnabled = !isChecked.Value;
            RunButton.IsEnabled = IsReadyToRun();
        }

        private void Player2IsHumanCheckBox_OnChenged(object sender, RoutedEventArgs e) {
            var isChecked = Player2IsHumanCheckBox.IsChecked;
            if (isChecked == null) return;
            Player2FileNameTextBox.IsEnabled = !isChecked.Value;
            Player2OpenFileButton.IsEnabled = !isChecked.Value;
            RunButton.IsEnabled = IsReadyToRun();
        }
    }
}