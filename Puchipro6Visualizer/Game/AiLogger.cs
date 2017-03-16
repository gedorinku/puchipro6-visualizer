using System;
using System.Threading.Tasks;

namespace Puchipro6Visualizer.Game {
    class AiLogger : IDisposable {
        private static readonly TaskFactory _taskFactory = new TaskFactory();

        public AiLogger() {
            OutputLogger = new Logger();
            InputLogger = new Logger();
            ErrorOutputLogger = new Logger();
        }

        public AiLogger(Logger outputLogger, Logger inputLogger, Logger errorOutputLogger) {
            OutputLogger = outputLogger;
            InputLogger = inputLogger;
            ErrorOutputLogger = errorOutputLogger;
        }

        public Logger OutputLogger { get; }
        public Logger InputLogger { get; }
        public Logger ErrorOutputLogger { get; }

        public void Dispose() {
            OutputLogger.Dispose();
            InputLogger.Dispose();
            ErrorOutputLogger.Dispose();
        }

        /// <summary>
        ///     全Loggerの次のイベントの処理が完了するまで同期的に待機する．
        /// </summary>
        public void WaitEvents() {
            var outputTask = _taskFactory.StartNew(() => { OutputLogger.WaitEvent(); });
            var inputTask = _taskFactory.StartNew(() => { InputLogger.WaitEvent(); });
            var errorOutputTask = _taskFactory.StartNew(() => { ErrorOutputLogger.WaitEvent(); });

            outputTask.Wait();
            inputTask.Wait();
            errorOutputTask.Wait();
        }
    }
}