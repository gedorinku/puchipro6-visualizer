using System.Windows;
using System.Windows.Forms;
using MerjTek.WpfIntegration.MonoGame;
using Microsoft.Xna.Framework;
using Puchipro6Visualizer.Game;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Puchipro6Visualizer {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private GameMain _gameMain;

        public MainWindow() {
            GameConfig = new GameConfig {
                Column = 9,
                Row = 15,
                ColorsNumber = 3,
                MinChain = 1
            };

            InitializeComponent();

            Player1IsHumanCheckBox.IsChecked = true;
            Player2IsHumanCheckBox.IsChecked = true;
        }

        public GameConfig GameConfig { get; set; }

        private void GameControl_OnLoaded(object sender, GraphicsDeviceEventArgs e) {}

        private void GameControl_OnRender(object sender, GraphicsDeviceEventArgs e) {
            e.GraphicsDevice.Clear(Color.CornflowerBlue);

            _gameMain?.Render();
        }

        private void GameControl_OnSizeChanged(object sender, SizeChangedEventArgs e) {
            const double ratio = 3.0d / 4.0d;
            GameControl.Height = e.NewSize.Width * ratio;
        }

        private void RunButton_Click(object sender, RoutedEventArgs e) {
            string f1 = null, f2 = null;
            var message = "ファイルが選択されていません";
            var title = "Puchipro6 Visualizar";
            if (Player1IsHumanCheckBox.IsChecked.HasValue && !Player1IsHumanCheckBox.IsChecked.Value) {
                f1 = Player1FileNameTextBox.Text;

                if (f1 == null) {
                    MessageBox.Show(message, title,
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            if (Player2IsHumanCheckBox.IsChecked.HasValue && !Player2IsHumanCheckBox.IsChecked.Value) {
                f2 = Player2FileNameTextBox.Text;

                if (f2 == null) {
                    MessageBox.Show(message, title,
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            var fileNames = new[] {
                f1, f2
            };

            _gameMain = new GameMain(GameControl, GameControl.GraphicsDevice, GameConfig,
                GameControl, fileNames);
            _gameMain.Run();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) {
            _gameMain?.Dispose();
            _gameMain = null;
        }

        private void Player1OpenFileButton_Click(object sender, RoutedEventArgs e) {
            Player1FileNameTextBox.Text = OpenFile();
        }

        private void Player2OpenFileButton_Click(object sender, RoutedEventArgs e) {
            Player2FileNameTextBox.Text = OpenFile();
        }

        private static string OpenFile() {
            var fileDialog = new OpenFileDialog {
                Title = "実行ファイルを開く",
                Filter = "実行ファイル|*.exe"
            };
            fileDialog.ShowDialog();
            return fileDialog.FileName;
        }
    }
}