using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MerjTek.WpfIntegration.Xaml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Puchipro6Visualizer.Game {
    class Field : IRenderable, IDisposable {
        /// <summary>
        ///     盤面の状態を表す列挙型
        /// </summary>
        public enum FieldState {
            /// <summary>
            ///     待機状態
            /// </summary>
            Idle,

            /// <summary>
            ///     AIが実行中
            /// </summary>
            RunnigPlayer,

            /// <summary>
            ///     AIの実行が完了
            /// </summary>
            CompletedPlayer,

            /// <summary>
            ///     玉を消すアニメーション中
            /// </summary>
            DestroyingBalls,

            /// <summary>
            ///     玉落下のアニメーション中
            /// </summary>
            FallingBalls,

            /// <summary>
            ///     消せる玉がなくなった
            /// </summary>
            Finished
        }


        protected const float FieldWidth = 325.0f;
        protected const float FieldHeight = 500.0f;
        private readonly Ball[,] _balls;
        private readonly EntityManager _entityManager = new EntityManager();
        private readonly Queue<OjamaBall>[] _ojamaQueues;
        protected readonly float BallSize;
        private SpriteFont _font;

        public Field(
            float px, float py,
            GameConfig gameConfig, AiLogger aiLogger, GameMain gameMain, SpriteBatch spriteBatch = null,
            Camera camera = null,
            MonoGameControl gameControl = null, string fileName = null) {
            SpriteBatch = spriteBatch;
            Camera = camera;
            GameMain = gameMain;
            GameConfig = gameConfig;
            AiLogger = aiLogger;
            X = px;
            Y = py;

            _ojamaQueues = new Queue<OjamaBall>[Column + 1];
            for (var i = 1; i <= Column; ++i) {
                _ojamaQueues[i] = new Queue<OjamaBall>();
            }

            _balls = new Ball[Column, Row];
            BallSize = Math.Min(FieldWidth / Column, FieldHeight / Row);

            Initialize();

            if (fileName == null) {
                Player = new HumanPlayer(this, gameControl, Camera, GameMain);
            } else {
                Player = new AiPlayer(this, GameMain, fileName);
            }
        }

        public Field(
            float px, float py,
            GameConfig gameConfig, AiLogger aiLogger, GameMain gameMain, SpriteBatch spriteBatch, Camera camera,
            MonoGameControl gameControl, ReplayPlayerData replayPlayerData) {
            SpriteBatch = spriteBatch;
            Camera = camera;
            GameMain = gameMain;
            GameConfig = gameConfig;
            AiLogger = aiLogger;
            X = px;
            Y = py;

            _ojamaQueues = new Queue<OjamaBall>[Column + 1];
            for (var i = 1; i <= Column; ++i) {
                _ojamaQueues[i] = new Queue<OjamaBall>();
            }

            _balls = new Ball[Column, Row];
            BallSize = Math.Min(FieldWidth / Column, FieldHeight / Row);

            Initialize();

            Player = new ReplayPlayer(this, GameMain, replayPlayerData);
        }

        protected bool InvalidOutput { get; set; }

        public Player Player { get; }

        public float X { get; }
        public float Y { get; }
        public int Index { get; set; }
        public GameConfig GameConfig { get; }
        public Field OtherField { get; set; }
        public FieldState State { get; protected set; } = FieldState.Idle;
        public AiLogger AiLogger { get; }

        public virtual int TurnsCount => (Player as ReplayPlayer).TurnsCount;

        /// <summary>
        ///     玉の列数を表す。
        /// </summary>
        public int Column => GameConfig.Column;

        /// <summary>
        ///     玉の行数を表す。
        /// </summary>
        public int Row => GameConfig.Row;


        public SpriteBatch SpriteBatch { get; set; }
        public Camera Camera { get; }

        public GameMain GameMain { get; }

        public void Dispose() {
            Player?.Dispose();
        }

        public virtual void Update(GameTime gameTime) {
            _entityManager.Update(gameTime);

            if (State == FieldState.CompletedPlayer) {
                State = FieldState.DestroyingBalls;

                var logger = GameMain.GameLogger;
                var line = Player.OutPut + " ";
                if (IsInFieldRange(Player.OutPut.X, Player.OutPut.Y)) {
                    var ojamas = DestroyBalls(Player.OutPut);
                    if (ojamas == null) {
                        InvalidOutput = true;
                        logger.WriteLine("玉を1つも消せなかったため負け");
                    } else {
                        ThrowOjama(ojamas);
                        logger.WriteLine(line + GetBall(Player.OutPut.X, Player.OutPut.Y).Color);
                    }
                } else {
                    logger.WriteLine(line + " AIの不正出力のため負け");
                    InvalidOutput = true;
                }

                if (GameMain.EnableSkippingRender && CheckFinished()) {
                    State = FieldState.Finished;
                    return;
                }

                State = GameMain.EnableSkippingRender
                    ? FieldState.Idle
                    : FieldState.FallingBalls;
            } else if (State == FieldState.FallingBalls &&
                       !GameMain.EnableSkippingRender) {
                var falling = false;
                foreach (var ball in _balls) {
                    ball.Update(gameTime);
                    falling = falling || ball.IsFalling;
                }

                if (!falling) {
                    if (CheckFinished()) {
                        State = FieldState.Finished;
                    } else {
                        State = FieldState.Idle;
                    }
                }
            }
        }

        public virtual void Render(GameTime gameTime) {
            foreach (var ball in _balls) {
                ball.Render(gameTime);
            }

            _entityManager.Render(gameTime);

            RenderDebugInfo();
        }

        private void Initialize() {
            var dx = (FieldWidth - BallSize * Column) / 2.0f;
            var dy = (FieldHeight - BallSize * Row) / 2.0f;
            for (var y = 0; y < Row; ++y) {
                for (var x = 0; x < Column; ++x) {
                    _balls[x, y] = new Ball(SpriteBatch, Camera, GameMain, this) {
                        X = BallSize * x + X + dx,
                        Y = BallSize * y + Y + dy,
                        Size = BallSize,
                        Color = GameConfig.GetRandomColor(GameMain.FixedRandom)
                    };
                    if (SpriteBatch != null) {
                        _balls[x, y].LoadContent();
                    }
                }
            }

            if (SpriteBatch != null) {
                var ojamaDisplay = new OjamaQueueDisplay(SpriteBatch, Camera, GameMain, this) {
                    X = X,
                    Y = Y
                };
                ojamaDisplay.LoadContent();
                _entityManager.Add(ojamaDisplay);
            }
        }

        public void LoadContent() {
            _font = GameMain.Content.Load<SpriteFont>("SegoeUILight");
        }

        public async void InitializeGame() {
            State = FieldState.RunnigPlayer;
            if (GameMain.IsBuildingCaches) {
                Player.InitializeGame();
            } else {
                await Task.Run(() => { Player.InitializeGame(); });
            }
            if (Player.IsDisposed) {
                State = FieldState.Finished;
                var gameLogger = GameMain.GameLogger;
                gameLogger.WriteLine("AIの準備が完了しなかったため負け");
                return;
            }
            State = FieldState.Idle;
        }

        public async void StartTurn() {
            if (State != FieldState.Idle || Player.IsRunning) {
                throw new InvalidOperationException("");
            }

            State = FieldState.RunnigPlayer;
            if (GameMain.IsBuildingCaches) {
                Player.StartTurn();
            } else {
                await Task.Run(() => { Player.StartTurn(); });
            }
            State = FieldState.CompletedPlayer;
        }

        public bool IsInFieldRange(int x, int y)
            => (0 < x) && (0 < y) && (x <= Column) && (y <= Row);

        /// <summary>
        ///     指定した列のQueueに入っているおじゃまを返す．
        /// </summary>
        /// <param name="x">列</param>
        /// <returns></returns>
        public List<OjamaBall> GetOjamasInQueue(int x)
            => _ojamaQueues[x].ToList();

        protected Queue<OjamaBall> GetOjamaQueue(int x)
            => _ojamaQueues[x];


        /// <summary>
        ///     消せる玉が無くなってゲームが終了したか判定する．
        /// </summary>
        /// <returns>ゲームが終了したか</returns>
        private bool CheckFinished() {
            if (InvalidOutput) {
                return true;
            }

            var count = 0;
            var visit = new bool[Column + 1, Row + 1];

            for (var y = 1; y <= Row; ++y) {
                for (var x = 1; x <= Column; ++x) {
                    if (visit[x, y] || GetBall(x, y) is OjamaBall) continue;

                    visit[x, y] = true;
                    count = Math.Max(count, DfsDestroyableBalls(x, y, visit));
                }
            }

            return count < GameConfig.MinChain;
        }

        /// <summary>
        ///     消すことが可能な玉の数をDFSで数える．
        /// </summary>
        /// <returns>消すことが可能な玉の数</returns>
        public int DfsDestroyableBalls(int x, int y, bool[,] visit) {
            int[] dx = {-1, 0, 1, 0};
            int[] dy = {0, -1, 0, 1};
            var color = GetBall(x, y).Color;
            var result = 1;
            visit[x, y] = true;

            for (var i = 0; i < 4; ++i) {
                var tx = x + dx[i];
                var ty = y + dy[i];

                if (!IsInFieldRange(tx, ty) || visit[tx, ty]) continue;
                var next = GetBall(tx, ty);
                if (next.Color != color) continue;

                result += DfsDestroyableBalls(tx, ty, visit);
            }

            return result;
        }

        /// <summary>
        ///     おじゃまをランダムな列のqueueに入れる。
        /// </summary>
        /// <param name="ojama">おじゃま</param>
        protected virtual void EnqueueOjama(OjamaBall ojama) {
            var random = GameMain.FixedRandom;
            _ojamaQueues[random.Next(1, Column + 1)].Enqueue(ojama);
        }

        /// <summary>
        ///     描画領域内での座標から玉の盤面座標を取得する。
        /// </summary>
        /// <param name="renderPosition">描画領域内での座標</param>
        /// <param name="result">取得した座標</param>
        /// <returns>取得した場合はtrue、どの玉にも当てはまらない場合はfalseを返す。</returns>
        public bool ToBordPointFromRenderPosition(Vector2 renderPosition, out Point result) {
            var fieldX = X + (FieldWidth - BallSize * Column) / 2.0f;
            var fieldY = Y + (FieldHeight - BallSize * Row) / 2.0f;

            //頑張って二分探索
            Func<float, float, int, int> search = (bordPos, target, length) => {
                target -= bordPos;
                var l = 1;
                var r = length + 1;
                while (1 <= r - l) {
                    var mid = (l + r) / 2;

                    if (((mid - 1) * BallSize <= target) && (target < mid * BallSize)) {
                        return mid;
                    }
                    if (mid * BallSize <= target) {
                        l = mid + 1;
                        continue;
                    }
                    if (target < (mid - 1) * BallSize) {
                        r = mid;
                        continue;
                    }

                    //ここには到達しないはず
                    throw new InvalidOperationException();
                }

                return int.MinValue;
            };

            var resultX = search(fieldX, renderPosition.X, Column);
            var resultY = search(fieldY, renderPosition.Y, Row);
            result = new Point(resultX, resultY);
            return resultX != int.MinValue && resultY != int.MinValue;
        }

        public Ball GetBall(int x, int y)
            => _balls[x - 1, y - 1];

        public IEnumerable<Ball> GetBalls()
            => _balls.Cast<Ball>(); 

        protected void SetBall(int x, int y, Ball ball) {
            if (!GameMain.EnableSkippingRender) {
                ball.LoadContent();
            }
            _balls[x - 1, y - 1] = ball;
        }

        public string GenerateTurnInfoString() {
            var turnInfo = new StringBuilder();
            var fields = new[] {this, OtherField};
            var gameConfig = GameConfig;

            turnInfo.AppendLine(Player.LeftMilliseconds.ToString());

            for (var x = 1; x <= gameConfig.Column; ++x) {
                var ojamaBalls = GetOjamasInQueue(x);
                turnInfo.Append(ojamaBalls.Count);

                foreach (var ojama in ojamaBalls) {
                    turnInfo.Append(" " + (ojama.IsHard ? -2 : -1));
                }

                turnInfo.AppendLine();
            }

            for (var y = 1; y <= gameConfig.Row; ++y) {
                if (y != 1)
                    turnInfo.AppendLine();

                for (var x = 1; x <= gameConfig.Column; ++x) {
                    var ball = GetBall(x, y);
                    int id;
                    if (ball is OjamaBall) {
                        var ojama = ball as OjamaBall;
                        id = ojama.IsHard ? -2 : -1;
                    } else {
                        id = gameConfig.GetColorId(ball.Color);
                    }

                    if (x != 1)
                        turnInfo.Append(" ");
                    turnInfo.Append(id);
                }
            }

            turnInfo.AppendLine();

            return turnInfo.ToString();
        }

        /// <summary>
        ///     自分と相手の今までの連戦の勝利数を，空白区切りで取得する．
        /// </summary>
        /// <returns></returns>
        public string GetWonLoseInfoString() {
            var wonCount = Index == 0 ? GameConfig.Player1WonCount : GameConfig.Player2WonCount;
            var rivalWonCount = Index == 0 ? GameConfig.Player2WonCount : GameConfig.Player1WonCount;

            return wonCount + " " + rivalWonCount;
        }


        /// <summary>
        ///     指定した座標の玉を消す。
        /// </summary>
        /// <param name="point">消す玉の座標</param>
        /// <returns>送られるおじゃまの数 ただし消せない時はnull</returns>
        private OjamaCalculator DestroyBalls(Point point) {
            var targetBall = GetBall(point.X, point.Y);
            if (targetBall is OjamaBall) return null;
            var color = targetBall.Color;
            OjamaCalculator result;

            int[] dx = {-1, 0, 1, 0};
            int[] dy = {0, -1, 0, 1};
            var destroyed = new List<Point> {point};

            var visit = new bool[Column + 1, Row + 1];
            var queue = new Queue<Point>();
            queue.Enqueue(point);
            while (0 < queue.Count) {
                var p = queue.Dequeue();
                visit[p.X, p.Y] = true;

                for (var i = 0; i < 4; ++i) {
                    var tx = p.X + dx[i];
                    var ty = p.Y + dy[i];
                    if (!IsInFieldRange(tx, ty) || visit[tx, ty]) continue;
                    var ball = GetBall(tx, ty);
                    if (ball.Color != color)
                        continue;
                    var tp = new Point(tx, ty);
                    destroyed.Add(tp);
                    queue.Enqueue(tp);
                    visit[tx, ty] = true;
                }
            }

            if (destroyed.Count < GameConfig.MinChain) return null;

            var sum = new int[Column + 1, Row + 2];

            {
                var destroyedOjama = new List<Point>();
                var countLvDown = 0;
                foreach (var p in destroyed) {
                    for (var i = 0; i < 4; ++i) {
                        var tx = p.X + dx[i];
                        var ty = p.Y + dy[i];
                        if (!IsInFieldRange(tx, ty)) continue;
                        var ball = GetBall(tx, ty);
                        if (!(ball is OjamaBall) || ball.IsDeath) continue;
                        var ojama = ball as OjamaBall;
                        if (ojama.IsHard) {
                            ojama.IsHard = false;
                            countLvDown++;
                        } else {
                            destroyedOjama.Add(new Point(tx, ty));
                            ball.IsDeath = true;
                            sum[tx, ty - 1]++;
                        }
                    }
                    GetBall(p.X, p.Y).IsDeath = true;
                    sum[p.X, p.Y - 1]++;
                }
                result = new OjamaCalculator(countLvDown, destroyedOjama.Count, destroyed.Count);
                destroyed.AddRange(destroyedOjama);
            }

            if (!GameMain.EnableSkippingRender) {
                SpawnBallDestroyParticles(targetBall, destroyed);
            }

            for (var y = Row; 0 <= y; --y) {
                for (var x = 1; x <= Column; ++x) {
                    sum[x, y] += sum[x, y + 1];
                }
            }

            var topY = GetBall(1, 1).Y;
            for (var x = 1; x <= Column; ++x) {
                if (sum[x, 0] == 0) continue;

                for (var y = Row; 1 <= y; --y) {
                    var ball = GetBall(x, y);
                    if (ball.IsDeath) continue;
                    ball.TargetY = GetBall(x, y + sum[x, y]).Y;
                    SetBall(x, y + sum[x, y], ball);
                }

                for (var y = 1; y <= sum[x, 0]; ++y) {
                    var oldBall = GetBall(x, y);
                    var newY = -(sum[x, 0] - y + 1) * BallSize + topY;

                    if (_ojamaQueues[x].Count == 0) {
                        SetBall(x, y, new Ball(SpriteBatch, Camera, GameMain, this) {
                            X = oldBall.X,
                            Y = newY,
                            TargetY = oldBall.Y,
                            Size = BallSize,
                            Color = GetNextColor(x, y)
                        });
                    } else {
                        var ojama = _ojamaQueues[x].Dequeue();
                        ojama.X = oldBall.X;
                        ojama.Y = newY;
                        ojama.TargetY = oldBall.Y;
                        SetBall(x, y, ojama);
                    }
                }
            }

            return result;
        }

        protected virtual Color GetNextColor(int x, int y) => GameConfig.GetRandomColor(GameMain.FixedRandom);


        /// <summary>
        ///     玉の破壊時のパーティクルを表示する．
        /// </summary>
        /// <param name="orig">発火の起点となった玉</param>
        /// <param name="destroyed">破壊された玉</param>
        private void SpawnBallDestroyParticles(Ball orig, List<Point> destroyed) {
            foreach (var ball in destroyed.Select(p => GetBall(p.X, p.Y))) {
                _entityManager.Add(new BallParticle(SpriteBatch, Camera, GameMain, this, ball));
            }
            var cross = new CrossParticle(SpriteBatch, Camera, GameMain, this) {
                X = orig.X,
                Y = orig.Y
            };
            cross.LoadContent();
            _entityManager.Add(cross);
        }


        /// <summary>
        ///     デバッグ用の情報を描画する。
        /// </summary>
        [Conditional("DEBUG")]
        private void RenderDebugInfo() {
            var scale = new Vector2(Camera.RatioX, Camera.RatioY) / 1.5f;

            foreach (var ball in _balls) {
                var info = new StringBuilder();
                var color = ball.Color;
                info.Append(color.R.ToString("X"));
                info.Append(color.G.ToString("X"));
                info.Append(color.B.ToString("X"));
                var renderPos = Camera.ToRenderPosition(new Vector2(ball.X, ball.Y));
                SpriteBatch.DrawString(_font, info, renderPos, Color.Black, 0.0f, Vector2.One, scale,
                    SpriteEffects.None, 0.0f);
            }
        }

        #region ゲームバランス調整用関数

        /// <summary>
        ///     おじゃまを相手のフィールドに送る
        /// </summary>
        /// <param name="ojamas">送るおじゃまの量と種類</param>
        private void ThrowOjama(OjamaCalculator ojamas) {
            var n = ojamas.Calculate();

            for (var i = 0; i < n; ++i) {
                var ojama = new OjamaBall(SpriteBatch, Camera, GameMain, this) {
                    IsHard = ojamas.IsHard(),
                    Size = BallSize
                };
                OtherField.EnqueueOjama(ojama);
            }
        }

        #endregion
    }
}