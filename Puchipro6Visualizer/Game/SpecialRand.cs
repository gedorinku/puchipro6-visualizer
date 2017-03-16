using System;

namespace Puchipro6Visualizer.Game {
    public class SpecialRand {
        private uint w;
        private uint x = 362436069;
        private uint y = 521288629;
        private uint z = 123456789;

        public SpecialRand() : this((uint) Environment.TickCount) {}

        public SpecialRand(uint w) {
            this.w = w;
        }

        public SpecialRand(uint _x, uint _y, uint _z, uint _w) {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        public SpecialRand(SpecialRand seed) {
            x = seed.x;
            y = seed.y;
            z = seed.z;
            w = seed.w;
        }

        public int Next(int min, int max) {
            uint t;
            t = x ^ (x << 11);
            x = y;
            y = z;
            z = w;
            w = w ^ (w >> 19) ^ t ^ (t >> 8);
            return (int) (min + (double) w * (max - min) / ((double) uint.MaxValue + 1));
        }

        public int Next(int max) {
            return Next(0, max);
        }

        public override string ToString() => x + " " + y + " " + z + " " + w;
    }
}