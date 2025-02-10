////////////////////////////////////////////////
// © https://github.com/badhitman 
////////////////////////////////////////////////
using System;

namespace DBF_to_MySQL__CSV_and_XML;

public static class g
{
    public static string curr_lng = "";
    public static System.Windows.ResourceDictionary dict;
    public static string MySQLEscape(string str)
    {
        return System.Text.RegularExpressions.Regex.Replace(str, @"[\x00'""\b\n\r\t\cZ\\%_]",
            delegate (System.Text.RegularExpressions.Match match)
            {
                string v = match.Value;
                switch (v)
                {
                    case "\x00":            // ASCII NUL (0x00) character
                        return "\\0";
                    case "\b":              // BACKSPACE character
                        return "\\b";
                    case "\n":              // NEWLINE (linefeed) character
                        return "\\n";
                    case "\r":              // CARRIAGE RETURN character
                        return "\\r";
                    case "\t":              // TAB
                        return "\\t";
                    case "\u001A":          // Ctrl-Z
                        return "\\Z";
                    default:
                        return "\\" + v;
                }
            });
    }
    public static ModifyRegistry RegManager;
    public static string AppDir
    {
        get
        {
            System.Reflection.AssemblyProductAttribute myProduct = (System.Reflection.AssemblyProductAttribute)System.Reflection.AssemblyProductAttribute.GetCustomAttribute(System.Reflection.Assembly.GetExecutingAssembly(), typeof(System.Reflection.AssemblyProductAttribute));
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + System.IO.Path.DirectorySeparatorChar.ToString() + myProduct.Product;
        }
    }
    public static string ByteToString(byte[] bytes, System.Text.Encoding currentENCODING)
    {
        return currentENCODING.GetString(bytes);
    }
    public static byte[] StringToByte(string str)
    {
        return System.Text.Encoding.UTF8.GetBytes(str);
    }
    public static string preficsBildProgramm = "(ver. " + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion + " rc)";
    public static long CountRow = 0;
    public static string SizeFileAsString(long SizeFile)
    {
        if (SizeFile < 1024)
            return SizeFile.ToString() + " байт";
        else if (SizeFile < 1024 * 1024)
            return Math.Round((double)SizeFile / 1024, 2).ToString() + " Кб";
        else
            return Math.Round((double)SizeFile / 1024 / 1024, 2).ToString() + " Мб";
    }
    public static byte[] ByteNewLine = new byte[] { 10 };
    public static string CSVEscape(string str)
    {
        bool need_cuote = (str.IndexOf("\"") > -1 || str.IndexOf(";") > -1);
        return (need_cuote ? "\"" : "") + str.Replace("\"", "\"\"") + (need_cuote ? "\"" : "");
    }
    public static T[] SubArray<T>(this T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }
}