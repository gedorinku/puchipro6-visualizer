using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Puchipro6Visualizer.Game {
    class AiPlayer : Player {
        private static readonly TimeSpan TotalTimeLimit = TimeSpan.FromSeconds(200);
        private readonly string _fileName;
        private readonly ConcurrentQueue<string> _inputQueue;
        private readonly Process _playerAi;
        private readonly CancellationToken _token;
        private readonly CancellationTokenSource _tokenSource;
        private readonly Task _writeTask;
        private TimeSpan _totalThinkTime;

        public AiPlayer(Field currentField, GameMain gameMain, string fileName)
            : base(currentField, gameMain) {
            _playerAi = new Process();
            GenericProcessManager.Add(_playerAi);
            _fileName = fileName;
            _inputQueue = new ConcurrentQueue<string>();

            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            _writeTask = Task.Factory.StartNew(WriteStanderdInputFromQueue, _token);
        }

        public override int LeftMilliseconds
            => (int) (TotalTimeLimit.TotalMilliseconds - _totalThinkTime.TotalMilliseconds);

        public int LeftTimeOnLaunched { get; private set; }

        public List<int> LeftThinkTimes { get; } = new List<int>();

        public List<string> AiOutputs { get; } = new List<string>();

        public override void Dispose() {
            if (IsDisposed) return;
            IsDisposed = true;
            IsRunning = false;
            _tokenSource.Cancel();
            GenericProcessManager.Destroy(_playerAi);
        }

        public override void InitializeGame() {
            IsRunning = true;

            _playerAi.StartInfo.FileName = _fileName;
            _playerAi.StartInfo.UseShellExecute = false;
            _playerAi.StartInfo.CreateNoWindow = true;
            _playerAi.StartInfo.RedirectStandardInput = true;
            _playerAi.StartInfo.RedirectStandardError = true;
            _playerAi.StartInfo.RedirectStandardOutput = true;
            _playerAi.ErrorDataReceived += PlayerAiOnErrorDataReceived;

            var gameLogger = GameMain.GameLogger;

            _playerAi.Start();
            _playerAi.BeginErrorReadLine();
            gameLogger.WriteLine("AIのプロセスを開始");

            var w = CurrentField.GameConfig.Column;
            var h = CurrentField.GameConfig.Row;
            var m = CurrentField.GameConfig.MinChain;
            var n = CurrentField.GameConfig.ColorsNumber;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            _inputQueue.Enqueue(w + " " + h + " " + m + " " + n);
            _inputQueue.Enqueue(CurrentField.Index.ToString());
            _inputQueue.Enqueue(CurrentField.GetWonLoseInfoString());

            var task = Task.Factory.StartNew(
                () => {
                    gameLogger.Write("AIからのAI名入力待ち->");
                    var input = _playerAi.StandardOutput.ReadLine();
                    var aiLogger = CurrentField.AiLogger;
                    aiLogger.OutputLogger.WriteLine(input);
                    AiOutputs.Add(input ?? "");

                    if (_token.IsCancellationRequested) {
                        return;
                    }

                    if (input != null) {
                        stopWatch.Stop();
                        LeftTimeOnLaunched = LeftMilliseconds;
                        LeftThinkTimes.Add(LeftMilliseconds);
                        gameLogger.WriteLine("AIの初期化は正常終了 AI名：" + input + " (" +
                                             stopWatch.ElapsedMilliseconds + "ms)");
                        return;
                    }
                    stopWatch.Stop();
                    LeftTimeOnLaunched = LeftMilliseconds;
                    LeftThinkTimes.Add(LeftMilliseconds);
                    gameLogger.WriteLine("AIからのAI名入力に失敗" + " (" +
                                         stopWatch.ElapsedMilliseconds + "ms)");
                    Dispose();
                }, _token);

            try {
                if (!task.Wait(10000)) {
                    gameLogger.WriteLine("AI名入力の時間制限超過:" + task.Status);
                    Dispose();
                    return;
                }
            } catch (AggregateException) {
                if (_token.IsCancellationRequested) {
                    return;
                }
                throw;
            }

            IsRunning = false;
        }

        private void PlayerAiOnErrorDataReceived(object sender,
            DataReceivedEventArgs dataReceivedEventArgs) {
            var aiLogger = CurrentField.AiLogger;
            aiLogger.ErrorOutputLogger.WriteLine(dataReceivedEventArgs.Data);
        }

        public override void StartTurn() {
            IsRunning = true;

            var turnInfo = new StringBuilder();
            turnInfo.Append(CurrentField.GenerateTurnInfoString());
            turnInfo.Append(OtherField.GenerateTurnInfoString());

            var gameLogger = GameMain.GameLogger;
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            _inputQueue.Enqueue(turnInfo.ToString());

            var task = Task.Factory.StartNew(() => {
                gameLogger.Write("AI入力待ち->");
                int x, y;
                var aiLogger = CurrentField.AiLogger;
                var input = _playerAi.StandardOutput.ReadLine();
                aiLogger.OutputLogger.WriteLine(input);
                AiOutputs.Add(input ?? "");

                var lines = input?.Split(' ');

                if (_token.IsCancellationRequested) {
                    return;
                }

                stopWatch.Stop();
                _totalThinkTime += stopWatch.Elapsed;
                LeftThinkTimes.Add(LeftMilliseconds);

                if (lines == null || lines.Length != 2 ||
                    !int.TryParse(lines[0], out x) ||
                    !int.TryParse(lines[1], out y)) {
                    stopWatch.Stop();
                    _totalThinkTime += stopWatch.Elapsed;
                    LeftThinkTimes.Add(LeftMilliseconds);
                    OutPut = new Point();
                    Dispose();
                    gameLogger.WriteLine("AIの出力が無効 (" + stopWatch.ElapsedMilliseconds + "ms, left " +
                                         LeftMilliseconds / 1000 + "s)");
                    return;
                }

                if (TotalTimeLimit < _totalThinkTime) {
                    OutPut = new Point();
                    gameLogger.WriteLine("合計思考時間の時間制限超過:" + x + " " + y + " (" +
                                         stopWatch.ElapsedMilliseconds + "ms, left " +
                                         LeftMilliseconds / 1000 + "s)");
                    return;
                }
                gameLogger.WriteLine("OK:" + x + " " + y + " (" + stopWatch.ElapsedMilliseconds +
                                     "ms, left " + LeftMilliseconds / 1000 + "s)");
                OutPut = new Point(x, y);
            });

            try {
                if (!task.Wait(10000)) {
                    gameLogger.WriteLine("1ターンの時間制限超過:" + task.Status);
                    Dispose();
                    return;
                }
            } catch (AggregateException exception)
                when (exception.InnerExceptions.Any(e => e is OperationCanceledException)) {
                return;
            }

            gameLogger.WriteLine("ターン終了:" + OutPut + " 入力待ちタスク:" + task.Status);
            IsRunning = false;
        }

        /// <summary>
        ///     Queueから一行ずつAIの標準入力に書き込む
        /// </summary>
        private void WriteStanderdInputFromQueue() {
            var inputLogger = CurrentField.AiLogger.InputLogger;

            while (true) {
                Thread.Sleep(10);

                if (_token.IsCancellationRequested) {
                    return;
                }

                if (_inputQueue.IsEmpty) continue;

                string line;
                if (!_inputQueue.TryDequeue(out line)) continue;
                _playerAi.StandardInput.WriteLine(line);
                inputLogger.WriteLine(line);
            }
        }
    }
}