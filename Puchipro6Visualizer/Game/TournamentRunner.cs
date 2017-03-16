using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Puchipro6Visualizer.Game {
    class TournamentRunner {
        private readonly List<string> _contestantFileNames = new List<string>();

        public GameConfig GameConfig { get; set; }

        public void Run(string configFileName) {
            LoadContestant(configFileName);

            var tree = new TournamentTree(_contestantFileNames, GameConfig);

            var currentTime = DateTime.Now;
            var replayFileName = new StringBuilder();
            replayFileName.Append("tournament-");
            replayFileName.Append(currentTime.Year);
            replayFileName.Append($"{currentTime.Month:D2}");
            replayFileName.Append($"{currentTime.Day:D2}_");
            replayFileName.Append($"{currentTime.Hour:D2}");
            replayFileName.Append($"{currentTime.Minute:D2}");
            replayFileName.Append($"{currentTime.Second:D2}_");

            tree.RunTournament(replayFileName.ToString());
        }

        private void LoadContestant(string configFileName) {
            string raw;
            using (var reader = new StreamReader(configFileName)) {
                raw = reader.ReadToEnd();
            }

            var fileNames = raw.Split('\n')
                .Select(s => s.Trim())
                .Where(s => s.Length != 0)
                .ToList();
            foreach (var fileName in fileNames) {
                if (!File.Exists(fileName)) {
                    throw new FileNotFoundException(fileName);
                }
                _contestantFileNames.Add(fileName);
            }
        }


        private class TournamentTree {
            private const string DummyFileName = "|dummy|";

            private List<string> _nodes;
            private readonly Random _random = new Random();

            public TournamentTree(List<string> contestantFileNames, GameConfig gameConfig) {
                GameConfig = gameConfig;
                Count = 1;
                while (Count - 1 < contestantFileNames.Count) Count *= 2;

                var temp = new string[Count + 1];
                for (var i = 0; i < Count - contestantFileNames.Count; ++i) {
                    temp[i * 2] = DummyFileName;
                }

                {
                    var k = 0;
                    foreach (var fileName in contestantFileNames) {
                        if (temp[k] == DummyFileName) k++;
                        temp[k] = fileName;
                        k++;
                    }
                }

                _nodes = new string[Count * 2].ToList();

                {
                    var k = Count - 1;
                    foreach (var fileName in temp) {
                        _nodes[k] = fileName;
                        k++;
                    }
                }
            }

            public int Count { get; }

            public GameConfig GameConfig { get; }

            public void RunTournament(string replayPath) {
                QueryWinner(0, replayPath + @"\");
            }

            private string QueryWinner(int k, string replayPath) {
                if (_nodes[k] != null && _nodes[k] != "") return _nodes[k];

                var player1FileName = QueryWinner(k * 2 + 1, replayPath + @"\1\");
                var player2FileName = QueryWinner(k * 2 + 2, replayPath + @"\2\");

                if (player1FileName == DummyFileName) return player2FileName;
                if (player2FileName == DummyFileName) return player1FileName;

                return _nodes[k] = Match(player1FileName, player2FileName, replayPath);
            }

            private string Match(string player1FileName, string player2FileName, string replayPath) {
                var replayGameData = new ReplayGameData();
                var gameConfig = new GameConfig(GameConfig) {
                    Player1WonCount = 0,
                    Player2WonCount = 0,
                    RandomSeed = Environment.TickCount
                };

                var player1WonCount = 0;
                var player2WonCount = 0;

                var fileNames = new[]
                {player1FileName, player2FileName};

                GameMain gameMain = null;

                var flipped = false;

                for (var i = 0; (i < 4) || (player1WonCount == player2WonCount); ++i) {
                    fileNames[0] = flipped ? player2FileName : player1FileName;
                    fileNames[1] = flipped ? player1FileName : player2FileName;
                    gameConfig.Player1WonCount = flipped ? player2WonCount : player1WonCount;
                    gameConfig.Player2WonCount = flipped ? player1WonCount : player2WonCount;

                    var currentRandom = gameMain?.FixedRandom ??
                                        new SpecialRand((uint) gameConfig.RandomSeed);
                    gameMain?.Dispose();
                    var gameLogger = new Logger();
                    gameLogger.EnableWritingConsole = true;
                    var aiLoggers = new[] { new AiLogger(), new AiLogger() };
                    gameMain = GameMain.Run(GameConfig, fileNames, true, gameLogger, aiLoggers,
                        new SpecialRand(currentRandom));

                    while (gameMain.CurrentState == GameMain.GameStateEnum.Running) {
                        Thread.Sleep(30);
                        gameMain.Update();
                    }

                    Console.WriteLine("終わり");
                    if (gameMain.CurrentState == GameMain.GameStateEnum.Player1Won) {
                        if (flipped) player2WonCount++;
                        else player1WonCount++;
                    } else if (gameMain.CurrentState == GameMain.GameStateEnum.Player2Won) {
                        if (flipped) player1WonCount++;
                        else player2WonCount++;
                    }

                    aiLoggers[0].WaitEvents();
                    aiLoggers[1].WaitEvents();
                    gameLogger.WaitEvent();

                    var aiPlayer1 = gameMain.GetPlayer(0) as AiPlayer;
                    var aiPlayer2 = gameMain.GetPlayer(1) as AiPlayer;

                    var player1Replay = new ReplayPlayerData {
                        LeftTimeOnLaunched = aiPlayer1.LeftTimeOnLaunched,
                        LeftThinkTimes = aiPlayer1.LeftThinkTimes,
                        OutputLines = aiPlayer1.AiOutputs
                    };

                    var player2Replay = new ReplayPlayerData {
                        LeftTimeOnLaunched = aiPlayer2.LeftTimeOnLaunched,
                        LeftThinkTimes = aiPlayer2.LeftThinkTimes,
                        OutputLines = aiPlayer2.AiOutputs
                    };

                    var match = new ReplayMatchData {
                        Player1ReplayData = player1Replay,
                        Player2ReplayData = player2Replay,
                        GameConfig = new GameConfig(gameConfig),
                        GameRandom = currentRandom
                    };

                    replayGameData.Matches.Add(match);

                    if (i < 4) {
                        flipped = !flipped;
                    } else {
                        flipped = _random.Next(0, 2) == 0;
                    }

                    if ((i < 4) && (3 - i < Math.Abs(player1WonCount - player2WonCount))) {
                        break;
                    }
                }

                Directory.CreateDirectory(replayPath);
                replayGameData.Save(replayPath + "replay.txt");

                return player1WonCount < player2WonCount ? player2FileName : player1FileName;
            }
        }
    }
}