using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Puchipro6Visualizer.Game {
    class ReplayMatchData {
        public ReplayPlayerData Player1ReplayData { get; set; }

        public ReplayPlayerData Player2ReplayData { get; set; }

        public GameConfig GameConfig { get; set; }

        /// <summary>
        ///     この試合開始時のSpecialRandインスタンスのコピー
        /// </summary>
        public SpecialRand GameRandom { get; set; }

        public override string ToString() {
            var result = new StringBuilder();

            result.AppendLine(GameConfig.ToString());

            result.AppendLine(GameRandom.ToString());

            result.AppendLine(GameConfig.Player1WonCount + " " + GameConfig.Player2WonCount);

            result.AppendLine(Player1ReplayData.LeftTimeOnLaunched + " " + Player2ReplayData.LeftTimeOnLaunched);

            var outputs = new[] {
                Player1ReplayData.OutputLines,
                Player2ReplayData.OutputLines
            };

            var times = new[] {Player1ReplayData.LeftThinkTimes, Player2ReplayData.LeftThinkTimes};

            var counts = outputs.Select(output => output.Count).ToList();
            result.AppendLine(counts[0] + " " + counts[1]);

            for (var i = 0; i < 2; ++i) {
                for (var j = 0; j < outputs[i].Count; ++j) {
                    result.AppendLine(outputs[i][j]);
                    result.AppendLine(times[i][j].ToString());
                }
            }

            /*
            foreach (var s in outputs.SelectMany(output => output.Where(s => s.Length != 0))) {
                result.AppendLine(s);
            }
            */

            return result.ToString().Trim('\n', '\r');
        }

        public void ReadFromStreamReader(StreamReader reader) {
            {
                var gameInfo = reader.ReadLine().Trim().Split(' ');
                if (gameInfo.Length != 7) {
                    throw new InvalidDataException();
                }

                int column, row, minchain, colorsNum, randomSeed, player1Won, player2Won;
                if (!int.TryParse(gameInfo[0], out column) ||
                    !int.TryParse(gameInfo[1], out row) ||
                    !int.TryParse(gameInfo[2], out minchain) ||
                    !int.TryParse(gameInfo[3], out colorsNum) ||
                    !int.TryParse(gameInfo[4], out randomSeed) ||
                    !int.TryParse(gameInfo[5], out player1Won) ||
                    !int.TryParse(gameInfo[6], out player2Won)) {
                    throw new InvalidDataException();
                }

                GameConfig = new GameConfig {
                    Column = column,
                    Row = row,
                    MinChain = minchain,
                    ColorsNumber = colorsNum,
                    RandomSeed = randomSeed
                };
            }

            {
                uint x, y, z, w;
                var rawSeed = reader.ReadLine().Trim().Split(' ');
                if (!uint.TryParse(rawSeed[0], out x) ||
                    !uint.TryParse(rawSeed[1], out y) ||
                    !uint.TryParse(rawSeed[2], out z) ||
                    !uint.TryParse(rawSeed[3], out w)) {
                    throw new InvalidDataException();
                }

                GameRandom = new SpecialRand(x, y, z, w);
            }

            Player1ReplayData = Player1ReplayData ?? new ReplayPlayerData();
            Player2ReplayData = Player2ReplayData ?? new ReplayPlayerData();

            var wonCounts = reader.ReadLine().Trim().Split(' ');
            if (wonCounts.Length != 2) {
                throw new InvalidDataException();
            }

            var rawLunchTimes = reader.ReadLine().Trim().Split(' ');
            if (rawLunchTimes.Length != 2) {
                throw new InvalidDataException();
            }

            int player1LunchTime;
            int player2LunchTime;
            if (!int.TryParse(rawLunchTimes[0], out player1LunchTime) ||
                !int.TryParse(rawLunchTimes[1], out player2LunchTime)) {
                throw new InvalidDataException();
            }

            Player1ReplayData.LeftTimeOnLaunched = player1LunchTime;
            Player2ReplayData.LeftTimeOnLaunched = player2LunchTime;

            var inputLineNums = new int[2];
            var rawInputNums = reader.ReadLine().Trim().Split(' ');
            if (!int.TryParse(rawInputNums[0], out inputLineNums[0]) ||
                !int.TryParse(rawInputNums[1], out inputLineNums[1])) {
                throw new InvalidDataException();
            }

            var inputLines = new[]
            {new List<string>(inputLineNums[0]), new List<string>(inputLineNums[1])};

            var totalThinkTimes = new[]
            {new List<int>(inputLineNums[0]), new List<int>(inputLineNums[1])};

            for (var i = 0; i < 2; ++i) {
                for (var j = 0; j < inputLineNums[i]; ++j) {
                    inputLines[i].Add(reader.ReadLine().Trim());

                    var raw = reader.ReadLine().Trim();
                    int time;
                    if (!int.TryParse(raw, out time)) {
                        throw new InvalidDataException();
                    }
                    totalThinkTimes[i].Add(time);
                }
            }

            Player1ReplayData.OutputLines = inputLines[0];
            Player1ReplayData.LeftThinkTimes = totalThinkTimes[0];
            Player2ReplayData.OutputLines = inputLines[1];
            Player2ReplayData.LeftThinkTimes = totalThinkTimes[1];
        }

        public List<string> GetPlayerOutputLines(int index)
            => index == 0 ? Player1ReplayData.OutputLines : Player2ReplayData.OutputLines;

        public static bool TryParsePoint(string raw, out Point result) {
            var temp = raw.Trim().Split(' ');
            if (temp.Length != 2) {
                result = Point.Zero;
                return false;
            }

            int x, y;
            if (!int.TryParse(temp[0], out x) ||
                !int.TryParse(temp[1], out y)) {
                result = Point.Zero;
                return false;
            }

            result = new Point(x, y);
            return true;
        }
    }
}