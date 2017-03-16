using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MerjTek.WpfIntegration.Xaml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    class GameMain : IDisposable {
        public enum GameStateEnum {
            Running,
            Paused,
            Player1Won,
            Player2Won,
            Draw
        }

        public enum TurnEnum {
            None,
            Player1,
            Player2
        }

        private readonly Camera _camera;
        private readonly Field[] _fields;
        private readonly SpriteBatch _spriteBatch;
        private readonly VictoryDisplay[] _victoryDisplays = new VictoryDisplay[2];

        private readonly Stopwatch _stopwatch = new Stopwatch();
        public bool IsBuildingCaches { get; private set; }
        private TimeSpan _preTotalTime;

        private GameMain(
            GameConfig gameConfig, string[] fileNames, bool enableSkippingRender,
            Logger gameLogger, AiLogger[] aiLoggers, SpecialRand fixeRandom = null,
            IServiceProvider serviceProvider = null, MonoGameControl gameControl = null,
            GraphicsDevice graphicsDevice = null) {
            EnableSkippingRender = enableSkippingRender;
            GameLogger = gameLogger;

            if (!EnableSkippingRender) {
                if (Content == null) {
                    Content = new ContentManager(serviceProvider, "Content");
                }

                _spriteBatch = new SpriteBatch(graphicsDevice);
                _camera = new Camera(graphicsDevice);
            }

            GameConfig = gameConfig;
            FixedRandom = fixeRandom ?? new SpecialRand((uint) GameConfig.RandomSeed);

            _fields = new Field[2];
            for (var i = 0; i < 2; ++i) {
                var tx = 50 * (i + 1) + 325 * i;
                var ty = 100;
                _fields[i] = new Field(tx, ty, GameConfig, aiLoggers[i], this, _spriteBatch, _camera,
                    gameControl, fileNames[i]);
                _fields[i].Index = i;
            }
            _fields[0].OtherField = _fields[1];
            _fields[1].OtherField = _fields[0];

            CurrentTurn = TurnEnum.None;
        }

        private GameMain(GameConfig gameConfig, ReplayMatchData replayData, Logger gameLogger,
            AiLogger[] aiLoggers, bool isPersistent, IServiceProvider serviceProvider,
            MonoGameControl gameControl, GraphicsDevice graphicsDevice, bool isBuildingCaches, SpecialRand fixeRandom = null) {
            IsPersistent = isPersistent;
            GameLogger = gameLogger;
            IsBuildingCaches = isBuildingCaches;

            if (Content == null) {
                Content = new ContentManager(serviceProvider, "Content");
            }

            _spriteBatch = new SpriteBatch(graphicsDevice);
            _camera = new Camera(graphicsDevice);

            GameConfig = gameConfig;
            FixedRandom = fixeRandom ?? new SpecialRand((uint) GameConfig.RandomSeed);

            _fields = new Field[2];

            var replayPlayerData = new[]
            {replayData.Player1ReplayData, replayData.Player2ReplayData};
            for (var i = 0; i < 2; ++i) {
                var tx = 50 * (i + 1) + 325 * i;
                var ty = 100;

                if (IsPersistent) {
                    _fields[i] = new PersistentField(tx, ty, GameConfig, aiLoggers[i], this,
                        _spriteBatch, _camera, gameControl, replayPlayerData[i]);
                } else {
                    _fields[i] = new Field(tx, ty, GameConfig, aiLoggers[i], this, _spriteBatch,
                        _camera, gameControl, replayPlayerData[i]);
                }

                _fields[i].Index = i;
            }
            _fields[0].OtherField = _fields[1];
            _fields[1].OtherField = _fields[0];

            CurrentTurn = TurnEnum.None;
        }

        public GameConfig GameConfig { get; }

        public static ContentManager Content { get; private set; }

        public static double[] Speed { get; } = {1.0d, 1.5d, 2.0d, 3.0d, 10.0d, 810.0d};

        public static double CurrentSpeed { get; set; } = Speed[0];

        /// <summary>
        ///     固定された乱数シードを使用するSpecialRandインスタンス．
        ///     リプレイの互換性を維持するため，プレイの再現に必要ない乱数の発生にはUnfixedRandomプロパティを使用すること．
        /// </summary>
        public SpecialRand FixedRandom { get; }

        /// <summary>
        ///     固定されていない乱数シードを使用するRandomインスタンス．
        ///     プレイの再現に必要な乱数の発生にはFixedRandomプロパティを使用すること．
        /// </summary>
        public static Random UnfixedRandom { get; } = new Random();

        public Logger GameLogger { get; }

        public TurnEnum CurrentTurn { get; private set; } = TurnEnum.Player1;

        public GameStateEnum CurrentState { get; private set; } = GameStateEnum.Running;

        public bool EnableSkippingRender { get; set; }

        public bool IsPersistent { get; }

        public bool LazyFinished { get; private set; }

        public int CurrentTurnCount { get; protected set; }

        public virtual int TurnsCount => Math.Max(_fields[0].TurnsCount, _fields[1].TurnsCount);

        public void Dispose() {
            if (_fields != null) {
                foreach (var field in _fields) {
                    field.Dispose();
                }
            }
        }

        public static GameMain Run(GameConfig gameConfig, string[] fileNames,
            bool enableSkippingRender,
            Logger gameLogger, AiLogger[] aiLoggers, SpecialRand fixedRandom = null,
            IServiceProvider serviceProvider = null, MonoGameControl gameControl = null,
            GraphicsDevice graphicsDevice = null) {
            var gameMain = new GameMain(gameConfig, fileNames, enableSkippingRender, gameLogger,
                aiLoggers, fixedRandom, serviceProvider, gameControl, graphicsDevice);

            if (!gameMain.EnableSkippingRender) {
                gameMain.LaodContent();
            }

            foreach (var field in gameMain._fields) {
                field.InitializeGame();
            }
            gameMain._stopwatch.Start();

            return gameMain;
        }

        public static GameMain Run(GameConfig gameConfig, ReplayMatchData replayData,
            Logger gameLogger, AiLogger[] aiLoggers, bool isPersistent,
            IServiceProvider serviceProvider, MonoGameControl gameControl,
            GraphicsDevice graphicsDevice, bool isBuildingCaches, SpecialRand fixedRandom = null) {
            var gameMain = new GameMain(gameConfig, replayData, gameLogger, aiLoggers, isPersistent,
                serviceProvider, gameControl, graphicsDevice, isBuildingCaches, fixedRandom);

            gameMain.LaodContent();

            foreach (var field in gameMain._fields) {
                field.InitializeGame();
            }
            gameMain._stopwatch.Start();

            return gameMain;
        }

        private void LaodContent() {
            foreach (var field in _fields) {
                field.LoadContent();
            }

            for (var i = 0; i < 2; ++i) {
                _victoryDisplays[i] = new VictoryDisplay(_spriteBatch, _camera, this, _fields[i]) {
                    X = _fields[i].X + 125,
                    Y = _fields[i].Y - 40
                };
                _victoryDisplays[i].LoadContent();
            }
        }

        private void Render(GameTime gameTime) {
            _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
            foreach (var field in _fields) {
                field.Render(gameTime);
            }

            foreach (var victoryDisplay in _victoryDisplays) {
                victoryDisplay?.Render(gameTime);
            }
            _spriteBatch.End();
        }

        public void Update() {
            var elapsed = _stopwatch.Elapsed - _preTotalTime;
            var gameTime = new GameTime(_stopwatch.Elapsed, elapsed);
            _preTotalTime = _stopwatch.Elapsed;

            foreach (var field in _fields) {
                field.Update(gameTime);
            }

            if (CurrentState != GameStateEnum.Paused) {
                if (_fields[0].State == Field.FieldState.Idle &&
                    _fields[1].State == Field.FieldState.Idle && CurrentTurn == TurnEnum.None) {
                    _fields[0].StartTurn();
                    CurrentTurn = TurnEnum.Player1;
                } else if ((_fields[0].State == Field.FieldState.Idle ||
                     _fields[0].State == Field.FieldState.Finished) &&
                    _fields[1].State == Field.FieldState.Idle && CurrentTurn == TurnEnum.Player1) {
                    _fields[1].StartTurn();
                    CurrentTurn = TurnEnum.Player2;
                } else if (_fields[1].State == Field.FieldState.Idle &&
                           _fields[0].State == Field.FieldState.Idle &&
                           CurrentTurn == TurnEnum.Player2) {
                    _fields[0].StartTurn();
                    CurrentTurnCount++;
                    CurrentTurn = TurnEnum.Player1;
                }

                if (_fields[0].State == Field.FieldState.Finished &&
                    _fields[1].State == Field.FieldState.Finished) {
                    CurrentState = GameStateEnum.Draw;

                    foreach (
                        var victoryDisplay in
                            _victoryDisplays.Where(victoryDisplay => victoryDisplay != null)) {
                        victoryDisplay.DisplayString = "Draw";
                    }
                }
                if (_fields[0].State == Field.FieldState.Idle &&
                    _fields[1].State == Field.FieldState.Finished) {
                    CurrentState = GameStateEnum.Player1Won;

                    if (_victoryDisplays.All(displays => displays != null)) {
                        _victoryDisplays[0].DisplayString = "Won";
                        _victoryDisplays[1].DisplayString = "Lost";
                    }
                }
                if (_fields[0].State == Field.FieldState.Finished &&
                    _fields[1].State == Field.FieldState.Idle) {
                    CurrentState = GameStateEnum.Player2Won;

                    if (_victoryDisplays.All(displays => displays != null)) {
                        _victoryDisplays[0].DisplayString = "Lost";
                        _victoryDisplays[1].DisplayString = "Won";
                    }
                }

                if (CurrentTurnCount == 200) {
                    ForceJudge();
                }
            }

            if (!EnableSkippingRender && !IsBuildingCaches) {
                var flg = true;
                foreach (var victoryDisplay in _victoryDisplays) {
                    victoryDisplay?.Update(gameTime);
                    flg = flg && CurrentState != GameStateEnum.Running &&
                          (!victoryDisplay?.IsAnimating ?? false);
                }
                LazyFinished = flg;

                Render(gameTime);
            }
        }

        private void ForceJudge() {
            var player1Evaluation = EvaluatePlayer(0);
            var player2Evaluation = EvaluatePlayer(1);

            if (player1Evaluation == player2Evaluation) {
                CurrentState = GameStateEnum.Draw;
                foreach (
                        var victoryDisplay in
                            _victoryDisplays.Where(victoryDisplay => victoryDisplay != null)) {
                    victoryDisplay.DisplayString = "Draw";
                }
            } else if (player1Evaluation < player2Evaluation) {
                CurrentState = GameStateEnum.Player1Won;

                if (_victoryDisplays.All(displays => displays != null)) {
                    _victoryDisplays[0].DisplayString = "Won";
                    _victoryDisplays[1].DisplayString = "Lost";
                }
            } else {
                CurrentState = GameStateEnum.Player2Won;

                if (_victoryDisplays.All(displays => displays != null)) {
                    _victoryDisplays[0].DisplayString = "Lost";
                    _victoryDisplays[1].DisplayString = "Won";
                }
            }
        }

        private int EvaluatePlayer(int index) {
            var ojamaCount = 0;
            var field = _fields[index];

            for (var x = 1; x <= GameConfig.Column; ++x) {
                for (var y = 1; y <= GameConfig.Row; ++y) {
                    var ojama = field.GetBall(x, y) as OjamaBall;
                    if (ojama == null) continue;

                    ojamaCount += ojama.IsHard ? 2 : 1;
                }

                var queues = field.GetOjamasInQueue(x);

                foreach (var ojamaBall in queues) {
                    ojamaCount += ojamaBall.IsHard ? 2 : 1;
                }
            }

            return ojamaCount;
        }



        /// <summary>
        ///     リプレイを最後まで再生して，PersistentFieldのキャッシュを構築する．
        /// </summary>
        public void BuildPersistentFieldCaches() {
            if (!_fields.All(field => field is PersistentField)) {
                throw new InvalidOperationException("現在のフィールドはPersistentFieldではないです．");
            }

            EnableSkippingRender = true;
            IsBuildingCaches = true;

            while (CurrentState == GameStateEnum.Running) {
                Update();
            }
            Update();

            EnableSkippingRender = false;
            IsBuildingCaches = false;
        }

        /// <summary>
        ///     任意のターンに移行する．
        /// </summary>
        /// <param name="turn">0以上のターン番号</param>
        public void GoToTurn(int turn) {
            if (!_fields.All(field => field is PersistentField)) {
                throw new InvalidOperationException("現在のフィールドはPersistentFieldではありません．");
            }

            foreach (var temp in _fields.Select(field => field as PersistentField)) {
                temp.GoToTurn(turn);
            }

            CurrentState = GameStateEnum.Paused;
            CurrentTurn = TurnEnum.Player1;
            CurrentTurnCount = turn;
        }

        public void GetTurnInfoInputString(int turn, out string player1, out string player2) {
            if (!_fields.All(field => field is PersistentField)) {
                throw new InvalidOperationException("現在のフィールドはPersistentFieldではありません．");
            }

            var fields = new[] {_fields[0] as PersistentField, _fields[1] as PersistentField};

            player1 = fields[0].GetFieldInfoString(turn) + "\n" +
                      fields[1].GetFieldInfoString(turn);
            player2 = fields[1].GetFieldInfoString(turn) + "\n" +
                      fields[0].GetFieldInfoString(turn + 1);
        }

        public Player GetPlayer(int index) => _fields[index].Player;
    }
}