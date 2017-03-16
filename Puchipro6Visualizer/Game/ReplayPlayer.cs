using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Puchipro6Visualizer.Game {
    class ReplayPlayer : Player {
        private ReplayPlayerData _replayData;

        public ReplayPlayer(Field field, GameMain gameMain, ReplayPlayerData replayPlayerData)
            : base(field, gameMain) {
            _replayData = replayPlayerData;
        }

        public int CurrentHead { get; set; }

        public int TurnsCount
            => _replayData.OutputLines.Count - 1;

        public string Name
            => _replayData.OutputLines[0];

        public override int LeftMilliseconds { get; }

        public override void InitializeGame() {
            var aiLogger = CurrentField.AiLogger;
            aiLogger.OutputLogger.WriteLine(_replayData.OutputLines[0]);
            CurrentHead = 1;
        }

        public override void StartTurn() {
            IsRunning = true;
            OutPut = Point.Zero;
            if (CurrentHead < _replayData.OutputLines.Count) {
                Point result;
                var aiLogger = CurrentField.AiLogger;
                aiLogger.InputLogger.WriteLine(CurrentField.GenerateTurnInfoString());
                aiLogger.OutputLogger.WriteLine(_replayData.OutputLines[CurrentHead]);

                if (!ReplayMatchData.TryParsePoint(_replayData.OutputLines[CurrentHead], out result)) {
                    CurrentHead = int.MaxValue;
                    IsRunning = false;
                    return;
                }
                OutPut = result;
                CurrentHead++;
            }

            IsRunning = false;
        }

        public Point GetOutput(int turn) {
            Point result;
            if (_replayData.OutputLines.Count <= turn + 1) return Point.Zero;

            if (!ReplayMatchData.TryParsePoint(_replayData.OutputLines[turn + 1], out result)) {
                return Point.Zero;
            }
            return result;
        }
    }
}