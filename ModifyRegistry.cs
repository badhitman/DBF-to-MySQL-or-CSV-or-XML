////////////////////////////////////////////////
// © https://github.com/badhitman 
////////////////////////////////////////////////
using System;
using Microsoft.Win32;

namespace DBF_to_MySQL__CSV_and_XML;

public class ModifyRegistry
{
    private bool showError = false;
    /// <summary>
    /// A property to show or hide error messages 
    /// (default = false)
    /// </summary>
    public bool ShowError
    {
        get { return showError; }
        set { showError = value; }
    }
    /// <summary>
    /// A property to set the SubKey value
    /// </summary>
    public string SubKey { get; set; } = "SOFTWARE\\" + System.Reflection.Assembly.GetEntryAssembly().GetName().Name + " ru.usa@mail.ru";
    /// <summary>
    /// A property to set the BaseRegistryKey value.
    /// (default = Registry.LocalMachine)
    /// </summary>
    public RegistryKey BaseRegistryKey { get; set; } = Registry.CurrentUser;

    /// <summary>
    /// To read a registry key.
    /// input: KeyName (string)
    /// output: value (string) 
    /// </summary>
    public string Read(string KeyName, string DefaultValue = null)
    {
        // Opening the registry key
        RegistryKey rk = BaseRegistryKey;
        // Open a subKey as read-only
        RegistryKey sk1 = rk.OpenSubKey(SubKey);
        // If the RegistrySubKey doesn't exist -> (null)
        if (sk1 == null)
        {
            return DefaultValue;
        }
        else
        {
            try
            {
                // If the RegistryKey exists I get its value
                // or null is returned.
                string returnedValue = (string)sk1.GetValue(KeyName);
                return returnedValue == null ? DefaultValue : returnedValue;
            }
            catch (Exception e)
            {
                ShowErrorMessage(e, "Reading registry " + KeyName);
                return null;
            }
        }
    }

    /// <summary>
    /// To write into a registry key.
    /// input: KeyName (string) , Value (object)
    /// output: true or false 
    /// </summary>
    public bool Write(string KeyName, object Value)
    {
        try
        {
            // Setting
            RegistryKey rk = BaseRegistryKey;
            // I have to use CreateSubKey 
            // (create or open it if already exits), 
            // 'cause OpenSubKey open a subKey as read-only
            RegistryKey sk1 = rk.CreateSubKey(SubKey);
            // Save the value
            sk1.SetValue(KeyName, Value);

            return true;
        }
        catch (Exception e)
        {
            ShowErrorMessage(e, "Writing registry " + KeyName);
            return false;
        }
    }

    /// <summary>
    /// To delete a registry key.
    /// input: KeyName (string)
    /// output: true or false 
    /// </summary>
    public bool DeleteKey(string KeyName)
    {
        try
        {
            // Setting
            RegistryKey rk = BaseRegistryKey;
            RegistryKey sk1 = rk.CreateSubKey(SubKey);
            // If the RegistrySubKey doesn't exists -> (true)
            if (sk1 == null)
                return true;
            else
                sk1.DeleteValue(KeyName);

            return true;
        }
        catch (Exception e)
        {
            ShowErrorMessage(e, "Deleting SubKey " + SubKey);
            return false;
        }
    }

    /// <summary>
    /// To delete a sub key and any child.
    /// input: void
    /// output: true or false 
    /// </summary>
    public bool DeleteSubKeyTree()
    {
        try
        {
            // Setting
            RegistryKey rk = BaseRegistryKey;
            RegistryKey sk1 = rk.OpenSubKey(SubKey);
            // If the RegistryKey exists, I delete it
            if (sk1 != null)
                rk.DeleteSubKeyTree(SubKey);

            return true;
        }
        catch (Exception e)
        {
            ShowErrorMessage(e, "Deleting SubKey " + SubKey);
            return false;
        }
    }

    /// <summary>
    /// Retrive the count of subkeys at the current key.
    /// input: void
    /// output: number of subkeys
    /// </summary>
    public int SubKeyCount()
    {
        try
        {
            // Setting
            RegistryKey rk = BaseRegistryKey;
            RegistryKey sk1 = rk.OpenSubKey(SubKey);
            // If the RegistryKey exists...
            if (sk1 != null)
                return sk1.SubKeyCount;
            else
                return 0;
        }
        catch (Exception e)
        {
            ShowErrorMessage(e, "Retriving subkeys of " + SubKey);
            return 0;
        }
    }

    /// <summary>
    /// Retrive the count of values in the key.
    /// input: void
    /// output: number of keys
    /// </summary>
    public int ValueCount()
    {
        try
        {
            // Setting
            RegistryKey rk = BaseRegistryKey;
            RegistryKey sk1 = rk.OpenSubKey(SubKey);
            // If the RegistryKey exists...
            if (sk1 != null)
                return sk1.ValueCount;
            else
                return 0;
        }
        catch (Exception e)
        {
            ShowErrorMessage(e, "Retriving keys of " + SubKey);
            return 0;
        }
    }

    private void ShowErrorMessage(Exception e, string Title)
    {
        if (showError == true)
            System.Windows.MessageBox.Show(e.Message,
                            Title
                            , System.Windows.MessageBoxButton.OK
                            , System.Windows.MessageBoxImage.Error);
    }
}