using System;
using System.Collections.Generic;
using System.Text;
using MerjTek.WpfIntegration.Xaml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    class PersistentField : Field {
        private readonly CrossParticle _crossParticle;
        private readonly List<TurnCache> _turnCaches = new List<TurnCache>();
        private readonly ReplayPlayerData _replayPlayerData;
        private bool _built;
        private bool _isCachedTurn;

        public PersistentField(float px, float py, GameConfig gameConfig, AiLogger aiLogger,
            GameMain gameMain, SpriteBatch spriteBatch, Camera camera, MonoGameControl gameControl,
            ReplayPlayerData replayPlayerData)
            : base(
                px, py, gameConfig, aiLogger, gameMain, spriteBatch, camera, gameControl,
                replayPlayerData) {
            _crossParticle = new CrossParticle(SpriteBatch, Camera, GameMain, this) {
                IsPersistent = true
            };
            _crossParticle.LoadContent();

            _turnCaches.Add(new TurnCache(this));
            _replayPlayerData = replayPlayerData;
        }

        public int CurrentTurn { get; private set; }

        public override int TurnsCount => _turnCaches.Count;

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if (GameMain.CurrentTurn == GameMain.TurnEnum.Player2 && !_isCachedTurn) {
                _isCachedTurn = true;
                Console.WriteLine((Player as ReplayPlayer).Name + " " + GameMain.CurrentTurn);
                CurrentTurn++;
                if (!_built) {
                    _turnCaches.Add(new TurnCache(this));
                }
            } else if (GameMain.CurrentTurn == GameMain.TurnEnum.Player1) {
                _isCachedTurn = false;
            } else if (State != FieldState.Idle) {
                _isCachedTurn = false;
                if (State == FieldState.Finished) {
                    _built = true;
                }
            }
        }

        public override void Render(GameTime gameTime) {
            base.Render(gameTime);

            var player = Player as ReplayPlayer;
            var output = player.GetOutput(CurrentTurn);

            if (!IsInFieldRange(output.X, output.Y)) return;

            var temp = GetBall(output.X, output.Y);
            _crossParticle.X = temp.X;
            _crossParticle.Y = temp.Y;
            _crossParticle.Render(gameTime);
        }

        /// <summary>
        ///     任意のターンに移行する．
        /// </summary>
        /// <param name="turn">0以上TurnsCount未満のターンを表す整数</param>
        public void GoToTurn(int turn) {
            var cache = _turnCaches[Math.Min(turn, _turnCaches.Count - 1)];

            var dx = (FieldWidth - BallSize * Column) / 2.0f;
            var dy = (FieldHeight - BallSize * Row) / 2.0f;

            for (var y = 1; y <= GameConfig.Row; ++y) {
                for (var x = 1; x <= GameConfig.Column; ++x) {
                    var id = cache.GetBallInfo(x, y);
                    if (id < 0) {
                        SetBall(x, y, new OjamaBall(SpriteBatch, Camera, GameMain, this) {
                            X = BallSize * (x - 1) + X + dx,
                            Y = BallSize * (y - 1) + Y + dy,
                            Size = BallSize,
                            Color = Color.Black,
                            IsHard = id == -2
                        });
                    } else {
                        SetBall(x, y, new Ball(SpriteBatch, Camera, GameMain, this) {
                            X = BallSize * (x - 1) + X + dx,
                            Y = BallSize * (y - 1) + Y + dy,
                            Size = BallSize,
                            Color = GameConfig.Colors[id - 1]
                        });
                    }
                }
            }

            for (var x = 1; x <= GameConfig.Column; ++x) {
                var ojamas = cache.GetOjamaQueue(x);
                var queue = GetOjamaQueue(x);
                queue.Clear();

                foreach (var ojamaBall in ojamas) {
                    queue.Enqueue(ojamaBall);
                }
            }

            State = cache.State;

            var replay = Player as ReplayPlayer;
            replay.CurrentHead = turn + 1;
            InvalidOutput = false;
            CurrentTurn = turn;
        }

        public string GetFieldInfoString(int turn) {
            turn = Math.Min(turn, TurnsCount - 1);
            var builder = new StringBuilder();
            var cache = _turnCaches[turn];

            builder.AppendLine(_replayPlayerData.LeftThinkTimes[turn].ToString());

            for (var x = 1; x <= GameConfig.Column; ++x) {
                var ojamaBalls = cache.GetOjamaQueue(x);
                builder.Append(ojamaBalls.Count);

                foreach (var ojama in ojamaBalls) {
                    builder.Append(" " + (ojama.IsHard ? -2 : -1));
                }

                builder.AppendLine();
            }

            for (var y = 1; y <= GameConfig.Row; ++y) {
                if (y != 1)
                    builder.AppendLine();

                for (var x = 1; x <= GameConfig.Column; ++x) {
                    var id = cache.GetBallInfo(x, y);
                    if (x != 1)
                        builder.Append(" ");
                    builder.Append(id);
                }
            }

            return builder.ToString();
        }

        private class TurnCache {
            private readonly int[,] _balls;
            private readonly List<OjamaBall>[] _ojamaQueues;

            public TurnCache(Field field) {
                _balls = new int[field.Column + 1, field.Row + 1];
                _ojamaQueues = new List<OjamaBall>[field.Column + 1];

                for (var x = 1; x <= field.Column; ++x) {
                    var temp = field.GetOjamasInQueue(x);
                    for (var i = 0; i < temp.Count; ++i) {
                        temp[i] = new OjamaBall(temp[i]);
                    }
                    _ojamaQueues[x] = temp;

                    for (var y = 1; y <= field.Row; ++y) {
                        var ball = field.GetBall(x, y);
                        if (ball is OjamaBall) {
                            //_balls[x, y] = new OjamaBall(ball as OjamaBall);
                            var ojama = ball as OjamaBall;
                            _balls[x, y] = ojama.IsHard ? -2 : -1;
                        } else {
                            //_balls[x, y] = new Ball(ball);
                            _balls[x, y] = field.GameConfig.GetColorId(ball.Color);
                        }
                    }
                }

                State = field.State;
            }

            public FieldState State { get; }

            public int GetBallInfo(int x, int y)
                => _balls[x, y];

            public List<OjamaBall> GetOjamaQueue(int x)
                => _ojamaQueues[x];
        }
    }
}