namespace Puchipro6Visualizer.Game {
    public class AiProperty {
        public string FileName { get; private set; }

        public bool IsHuman { get; private set; }


        public static AiProperty CreateHumanProperty() => new AiProperty {IsHuman = true, FileName = null};

        public static AiProperty CreateProperty(string fileName) => new AiProperty {FileName = fileName};
    }
}