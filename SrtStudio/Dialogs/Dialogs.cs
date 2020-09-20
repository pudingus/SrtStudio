using System.IO;
using System.Windows;

namespace SrtStudio
{
    public class Dialogs
    {
        public static MessageBoxResult UnsavedChanges(string projectFilename)
        {
            var result =
                MessageBox.Show(
                    $"Do you want to save changes you made to {projectFilename}? \n\nYour changes will be lost if you don't save them.",
                    "Save changes?",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Exclamation
                );
            return result;
        }

        public static MessageBoxResult RestoreBackup(string lastProject)
        {
            var result =
                MessageBox.Show(
                    $"Program didn't safely exit last time, \ndo you want to restore {lastProject}?",
                    "Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );
            return result;
        }
    }
}
