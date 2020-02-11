using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SrtStudio {
    public class Dialogs {
        public static MessageBoxResult UnsavedChanges() {
            var result = MessageBox.Show(
                $"Do you want to save changes you made to {Path.GetFileName(Project.FileName)}? \n\nYour changes will be lost if you don't save them.",
                "Save changes?",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Exclamation
            );
            return result;
        }

        public static MessageBoxResult RestoreBackup() {
            var result = MessageBox.Show(
                    $"Program didn't safely exit last time, \ndo you want to restore {Path.GetFileName(Settings.Data.LastProject)}?",
                    "Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
            );
            return result;
        }
    }
}
