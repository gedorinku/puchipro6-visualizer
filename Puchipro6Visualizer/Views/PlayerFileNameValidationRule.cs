using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Puchipro6Visualizer.Views {
    public class PlayerFileNameValidationRule : ValidationRule {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            var fileName = value as string;

            if (string.IsNullOrEmpty(fileName)) {
                return new ValidationResult(false, "ファイル名を入力してください");
            }

            if (!File.Exists(fileName)) {
                return new ValidationResult(false, "ファイルは存在しません");
            }

            return ValidationResult.ValidResult;
        }
    }
}