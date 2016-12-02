using System;
using System.IO;
using System.Text.RegularExpressions;

namespace WindowsAppStudio.Convert.Xamarin
{
    public static class StringExtensions
    {
        public static string SanitizePath(this string pathFromUser)
        {
            return pathFromUser.SanitizePath(Environment.CurrentDirectory);
        }
        public static string SanitizePath(this string pathFromUser, string defaultValue)
        {
            return string.IsNullOrEmpty(pathFromUser) ? defaultValue : pathFromUser.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        public static string SanitizeFile(this string pathFromUser)
        {
            return pathFromUser.SanitizeFile(Environment.CurrentDirectory);
        }
        public static string SanitizeFile(this string pathFromUser, string defaultValue)
        {
            return string.IsNullOrEmpty(pathFromUser) ? defaultValue : pathFromUser.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
