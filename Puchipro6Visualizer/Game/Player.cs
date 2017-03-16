using System;
using System.Text;
using Microsoft.Xna.Framework;

namespace Puchipro6Visualizer.Game {
    abstract class Player : IDisposable {
        private volatile bool _isRunning;

        protected Player(Field field, GameMain gameMain) {
            CurrentField = field;
            GameMain = gameMain;
        }

        public GameMain GameMain { get; }

        public Field CurrentField { get; }

        public Field OtherField => CurrentField.OtherField;

        public bool IsRunning {
            get { return _isRunning; }
            protected set { _isRunning = value; }
        }

        public bool IsDisposed { get; protected set; }

        public Point OutPut { get; protected set; }

        public abstract int LeftMilliseconds { get; }

        public virtual void Dispose() {}

        public abstract void InitializeGame();

        public abstract void StartTurn();
    }
}