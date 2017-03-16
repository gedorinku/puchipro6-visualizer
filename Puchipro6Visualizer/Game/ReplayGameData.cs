using System;
using System.Collections.Generic;
using System.IO;

namespace Puchipro6Visualizer.Game {
    class ReplayGameData {
        private static readonly string ReplayFileDirectory =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/puchipro6/replay";

        public ReplayGameData() {
            Matches = new List<ReplayMatchData>();
        }

        public List<ReplayMatchData> Matches { get; }

        /// <summary>
        ///     試合数を返す．
        /// </summary>
        public int MatchesCount => Matches.Count;

        public void Save(string fileName) {
            if (!Directory.Exists(ReplayFileDirectory)) {
                Directory.CreateDirectory(ReplayFileDirectory);
            }

            using (var writer = new StreamWriter(fileName)) {
                writer.WriteLine(MatchesCount);

                foreach (var match in Matches) {
                    writer.WriteLine(match.ToString());
                }
            }
        }

        public void Load(string fileName) {
            using (var reader = new StreamReader(fileName)) {
                int matchesCount;
                if (!int.TryParse(reader.ReadLine().Trim(), out matchesCount)) {
                    throw new InvalidDataException(fileName);
                }

                for (var i = 0; i < matchesCount; ++i) {
                    var match = new ReplayMatchData();
                    match.ReadFromStreamReader(reader);
                    Matches.Add(match);
                }
            }
        }
    }
}