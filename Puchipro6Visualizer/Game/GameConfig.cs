using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Puchipro6Visualizer.Game {
    public class GameConfig {
        private Dictionary<Color, int> _colorIds;
        private int _column;

        private int _minChain;

        private int _row;

        public GameConfig() {
            ColorsNumber = 4;
        }

        public GameConfig(GameConfig gameConfig) {
            Player1WonCount = gameConfig.Player1WonCount;
            Player2WonCount = gameConfig.Player2WonCount;
            Column = gameConfig.Column;
            Row = gameConfig.Row;
            ColorsNumber = gameConfig.ColorsNumber;
            MinChain = gameConfig.MinChain;
            RandomSeed = gameConfig.RandomSeed;
        }

        /// <summary>
        ///     玉の列数を表す。
        /// </summary>
        public int Column {
            get { return _column; }
            set { _column = Math.Max(1, value); }
        }

        /// <summary>
        ///     玉の行数を表す。
        /// </summary>
        public int Row {
            get { return _row; }
            set { _row = Math.Max(1, value); }
        }

        /// <summary>
        ///     玉を消すときに玉がつながっている必要がある最小値を表す。
        /// </summary>
        public int MinChain {
            get { return _minChain; }
            set { _minChain = Math.Max(1, value); }
        }

        /// <summary>
        ///     使用する玉の色の数を表す。
        /// </summary>
        public int ColorsNumber {
            get { return Colors.Length; }
            set {
                var temp = Math.Max(1, value);
                Colors = new Color[temp];
                var dh = 360.0f / temp;
                _colorIds = new Dictionary<Color, int>();

                for (var i = 0; i < temp; ++i) {
                    Colors[i] = HsvColor.ToRgb(new HsvColor(dh * i, 1.0f, 1.0f));
                    _colorIds.Add(Colors[i], i + 1);
                }
            }
        }

        /// <summary>
        ///     使用する玉の色を表す。
        /// </summary>
        public Color[] Colors { get; private set; }

        /// <summary>
        ///     ゲームで使用する乱数のシード値を表す．
        /// </summary>
        public int RandomSeed { get; set; }

        /// <summary>
        ///     今までの連戦でPlayer1が勝利した数．
        /// </summary>
        public int Player1WonCount { get; set; }

        /// <summary>
        ///     今までの連戦でPlayer2が勝利した数．
        /// </summary>
        public int Player2WonCount { get; set; }

        /// <summary>
        ///     使用する玉の色からランダムに一色選び返す。
        /// </summary>
        /// <returns>ランダムな色</returns>
        public Color GetRandomColor(SpecialRand random) => Colors[random.Next(ColorsNumber)];

        /// <summary>
        ///     指定した玉の色の一意の識別番号(1以上ColorsNumber以下)を返す。
        /// </summary>
        /// <param name="color">ゲームで使用されている色</param>
        /// <returns>色の一意の識別番号</returns>
        public int GetColorId(Color color) {
            int result;
            if (_colorIds.TryGetValue(color, out result)) {
                return result;
            }

            var message = "指定された色（" + color + "）はゲームで使用されていません。";
            throw new ArgumentException(message);
        }

        public override string ToString()
            =>
                Column + " " + Row + " " + MinChain + " " + ColorsNumber + " " + RandomSeed + " " +
                Player1WonCount + " " + Player2WonCount;
    }
}