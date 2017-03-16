using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Puchipro6Visualizer.Game {
    class Logger : IDisposable {
        public delegate void LoggerEventHandler(string str);

        private static readonly TaskFactory TaskFactory = new TaskFactory();
        private readonly Task _eventTask;

        private readonly SynchronizedCollection<string> _log;
        private readonly ConcurrentQueue<string> _tempQueue;
        private readonly CancellationToken _token;
        private readonly CancellationTokenSource _tokenSource;

        private volatile bool _waitFlag;

        public Logger() {
            _log = new SynchronizedCollection<string>();
            _tempQueue = new ConcurrentQueue<string>();

            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;

            _eventTask = TaskFactory.StartNew(() => {
                var invokeEvent = new Action(() => {
                    if (_tempQueue.IsEmpty) {
                        _waitFlag = false;
                        return;
                    }

                    var builder = new StringBuilder();
                    while (!_tempQueue.IsEmpty) {
                        string str;
                        _tempQueue.TryDequeue(out str);
                        builder.Append(str);
                    }
                    OnLogAdded?.Invoke(builder.ToString());

                    if (_waitFlag) {
                        _waitFlag = false;
                    }
                });
                while (true) {
                    Thread.Sleep(500);
                    invokeEvent.Invoke();

                    if (_token.IsCancellationRequested) {
                        invokeEvent.Invoke();
                        return;
                    }
                }
            });
        }

        public bool EnableWritingConsole { get; set; }

        public void Dispose() {
            try {
                _tokenSource.Cancel();
                _eventTask.Wait();
            } catch (Exception exception) {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        ///     ログに追記された時に発生する．
        /// </summary>
        public event LoggerEventHandler OnLogAdded;

        /// <summary>
        ///     ログに文字列を書き込む．
        /// </summary>
        /// <param name="str"></param>
        public void Write(string str) {
            if (EnableWritingConsole) {
                Console.Write(str);
            }
            _log.Add(str);
            _tempQueue.Enqueue(str);
        }

        /// <summary>
        ///     ログに文字列を書き込む．末尾に改行が追加される．
        /// </summary>
        /// <param name="line"></param>
        public void WriteLine(string str) {
            if (EnableWritingConsole) {
                Console.WriteLine(str);
            }
            _log.Add(str + Environment.NewLine);
            _tempQueue.Enqueue(str + Environment.NewLine);
        }

        /// <summary>
        ///     次のイベントが処理されるまで，同期的に待機する．
        /// </summary>
        public void WaitEvent() {
            _waitFlag = true;

            while (_waitFlag) {
                Thread.Sleep(100);
            }
        }

        /// <summary>
        ///     ログの内容を返す．
        /// </summary>
        /// <returns>ログ</returns>
        public override string ToString() {
            var builder = new StringBuilder();
            foreach (var str in _log) {
                builder.Append(str);
            }

            return builder.ToString();
        }
    }
}