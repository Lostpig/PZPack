using PZPack.Core.Exceptions;
using System;
using System.IO;
using System.Windows;

namespace PZPack.View.Service;

internal class Alert
{
    public static MessageBoxResult Show(string message, string caption, MessageBoxImage icon)
    {
        return MessageBox.Show(message, caption, MessageBoxButton.OK, icon);
    }
    public static MessageBoxResult ShowException(Exception ex)
    {
        string message = ex switch
        {
            FileNotFoundException exf => 
                string.Format(Translate.EX_FileNotFound, exf.FileName),
            DirectoryNotFoundException => 
                string.Format(Translate.EX_DirectoryNotFound, ex.Message),
            SourceDirectoryIsEmptyException exf => 
                string.Format(Translate.EX_SourceDirectoryIsEmpty, exf.DirectoryPath),
            OutputDirectoryIsNotEmptyException exf =>
                string.Format(Translate.EX_OutputDirectoryIsNotEmpty, exf.DirectoryPath),
            OutputFileAlreadyExistsException exf =>
                string.Format(Translate.EX_OutputFileAlreadyExists, exf.FileName),
            FileVersionNotCompatiblityException exf => 
                string.Format(Translate.EX_FileVersionNotCompatiblity, exf.Version, Core.PZVersion.Current),
            PZSignCheckedException => Translate.EX_PZSignCheckedException,
            PZPasswordIncorrectException => Translate.EX_PZPasswordIncorrect,
            _ => 
                string.IsNullOrEmpty(ex.Message) ? Translate.EX_Unknown : string.Format(Translate.EX_Exception, ex.Message)
        };

        return Show(message, Translate.Error, MessageBoxImage.Error);
    }
    public static MessageBoxResult ShowMessage(string message)
    {
        return Show(message, Translate.Message, MessageBoxImage.Information);
    }
    public static MessageBoxResult ShowWarning(string message)
    {
        return Show(message, Translate.Warning, MessageBoxImage.Warning);
    }

    public static MessageBoxResult ShowMessageConfirm(string message)
    {
        return MessageBox.Show(message, Translate.Message, MessageBoxButton.YesNo, MessageBoxImage.Information);
    }
    public static MessageBoxResult ShowWarningConfirm(string message)
    {
        return MessageBox.Show(message, Translate.Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning);
    }
}
