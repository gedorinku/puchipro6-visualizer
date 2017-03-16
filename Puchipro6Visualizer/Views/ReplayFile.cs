using System.IO;

namespace Puchipro6Visualizer.Views {
    class ReplayFile {
        public string FilePath { get; set; }

        public string FileName
            => Path.GetFileName(FilePath);

        public bool Equals(ReplayFile other) {
            return string.Equals(FilePath, other.FilePath);
        }

        public override int GetHashCode() {
            return FilePath?.GetHashCode() ?? 0;
        }
    }
}