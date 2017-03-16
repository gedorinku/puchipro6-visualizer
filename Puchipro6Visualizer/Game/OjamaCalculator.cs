namespace Puchipro6Visualizer.Game {
    class OjamaCalculator {
        /// <summary>
        ///     色付き玉が消えた回数
        /// </summary>
        private readonly int colorfulErasure;

        /// <summary>
        ///     おじゃまが消えた回数
        /// </summary>
        private readonly int ojamaErasure;

        /// <summary>
        ///     おじゃまが弱体化された回数
        /// </summary>
        private readonly int weakness;

        public OjamaCalculator() {
            weakness = ojamaErasure = colorfulErasure = 0;
        }

        public OjamaCalculator(int _weakness, int _ojamaErasure, int _colorfulErasure) {
            weakness = _weakness;
            ojamaErasure = _ojamaErasure;
            colorfulErasure = _colorfulErasure;
        }

        public bool IsHard() {
            //TODO 調整
            return colorfulErasure >= 35;
        }

        public int Calculate() {
            //TODO 調整
            var ojamas = ojamaErasure + weakness;
            var k = 0.015;
            var l = 0.15;
            if (IsHard()) {
                k *= 0.6;
                l *= 0.6;
            }
            return (int) (colorfulErasure * colorfulErasure * k + ojamas * ojamas * l);
        }
    }
}