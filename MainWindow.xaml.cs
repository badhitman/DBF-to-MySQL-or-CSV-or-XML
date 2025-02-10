using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace DBF_to_MySQL__CSV_and_XML;

/// <summary>
/// Логика взаимодействия для MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private delegate void SaveFileDelegate(string FileOutputName, string type_file, bool inc_del, Action<int, string> UpdateStatus);
    SaveFileDelegate SaveFileDelegateObj;
    string curr_ext_file = "mysql";
    public MainWindow()
    {
        InitializeComponent();
        g.RegManager = new ModifyRegistry();
    }
    private void DBFRefresh_Click(object sender, RoutedEventArgs e)
    {
        string LastSelectedFilePath = g.RegManager.Read("LastSelectedFilePath", "C:\\");
        try
        {
            OpenFileDialog ofd = new()
            {
                Filter = "dBASE files (*.dbf)|*.dbf"
            };
            if (File.Exists(LastSelectedFilePath))
                ofd.FileName = Path.GetFileName(LastSelectedFilePath);
            if (Directory.Exists(Path.GetDirectoryName(LastSelectedFilePath)))
                ofd.InitialDirectory = Path.GetDirectoryName(LastSelectedFilePath);

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            if (ofd.FileName.Length > 0)
            {
                SelectedFilePath.Text = ofd.FileName;
                g.RegManager.Write("LastSelectedFilePath", ofd.FileName);
                ReloadDT();
            }
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(ex.Message + "\r\r" + ex.StackTrace, "Exception!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        g.curr_lng = g.RegManager.Read("lng", "eng");
        g.dict = [];
        try
        {
            // Do not initialize this variable here.
            g.dict.Source = new Uri("..\\Resources\\StringResources-" + g.curr_lng + ".xaml", UriKind.Relative);
        }
        catch
        {
            g.dict.Source = new Uri("..\\Resources\\StringResources-eng.xaml", UriKind.Relative);
        }
        this.Resources.MergedDictionaries.Clear();
        this.Resources.MergedDictionaries.Add(g.dict);
        string curr_name_enc = g.RegManager.Read("last_encoding", "");
        foreach (EncodingInfo ei in Encoding.GetEncodings())
        {
            Encodings.Items.Add(new ExComboBoxItem(ei.DisplayName, ei.Name));
            if (ei.Name == curr_name_enc)
                Encodings.SelectedIndex = Encodings.Items.Count - 1;
        }
        if (Encodings.SelectedIndex == -1)
            Encodings.SelectedIndex = 0;
        del_inc_CheckBox.IsChecked = g.RegManager.Read("del_inc_CheckBox", "Y") == "Y";

        foreach (System.Windows.Controls.MenuItem i in AlLngItems.Items)
            if (i.Tag.ToString() == g.curr_lng)
                i.IsChecked = true;
        foreach (System.Windows.Controls.MenuItem i in AlLngItems.Items)
            if (i.Tag.ToString() == g.curr_lng)
                SetLng(i, null);


    }
    private void ReloadDT()
    {
        if (!File.Exists(SelectedFilePath.Text))
            return;
        try
        {
            ParseDBF my_parser = new ParseDBF(SelectedFilePath.Text, Encoding.GetEncoding(((ComboBoxItem)Encodings.SelectedItem).Tag.ToString()), this);
            if (!my_parser.DataBaseDone)
            {
                my_parser.Close();
                return;
            }
            grdDBF.DataContext = my_parser.GetRandomRowsAsDataTable(55);
            StatusText.Text = g.dict["MessStatusFileDone"].ToString() + ": " + Path.GetFileName(SelectedFilePath.Text) + " - " + my_parser.CountRows + " " + g.dict["MessStatusCountRows"].ToString() + " - " + g.SizeFileAsString(my_parser.Length_File);
            if (my_parser.CountRows > 0)
                myProgressBar.Maximum = my_parser.CountRows;
            myProgressBar.Value = -1;
            my_parser.Close();
        }
        catch
        {
            StatusText.Text = StatusText.Tag.ToString();
        }
    }
    private void Encodings_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        g.RegManager.Write("last_encoding", ((ComboBoxItem)(Encodings.SelectedItem)).Tag.ToString());
        ReloadDT();
    }
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(SelectedFilePath.Text))
        {
            System.Windows.MessageBox.Show(g.dict["MessErrNotSelectDBFFile"].ToString(), g.dict["MessErrNotSelectDBFFileTitle"].ToString());
            SelectedFilePathButton.Focus();
            return;
        }
        Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
        {
            AddExtension = true,
            //sfd.CheckFileExists = true;
            CheckPathExists = true,
            DefaultExt = curr_ext_file
        };
        string DirSaveAsLastFile = g.RegManager.Read("DirSaveAsLastFile", "");
        if (Directory.Exists(DirSaveAsLastFile))
            sfd.InitialDirectory = DirSaveAsLastFile;
        else
            sfd.InitialDirectory = Path.GetDirectoryName(SelectedFilePath.Text);
        sfd.FileName = Path.GetFileNameWithoutExtension(SelectedFilePath.Text);
        sfd.OverwritePrompt = true;
        sfd.Filter = curr_ext_file.ToUpper() + " " + g.dict["MessStringFilesInSaveAs"].ToString() + " (*." + curr_ext_file.ToLower() + ")|*." + curr_ext_file.ToLower() + "|" + g.dict["MessStringAllFilesInSaveAs"].ToString() + " (*.*)|*.*";
        sfd.ValidateNames = true;
        if (!sfd.ShowDialog(this).Value)
            return;
        string selected_file_path = sfd.FileName;
        if (Path.GetExtension(selected_file_path).ToLower().Substring(1) != curr_ext_file.ToLower())
            selected_file_path = sfd.FileName + "." + curr_ext_file;
        g.RegManager.Write("SaveAsLastFile", selected_file_path);
        ParseDBF my_parser = new(SelectedFilePath.Text, Encoding.GetEncoding(((ComboBoxItem)Encodings.SelectedItem).Tag.ToString()), this);
        if (!my_parser.DataBaseDone)
        {
            my_parser.Close();
            return;
        }
        myProgressBar.Value++;
        SaveFileDelegateObj = new SaveFileDelegate(my_parser.SaveAs);
        SaveFileDelegateObj.BeginInvoke(selected_file_path, curr_ext_file, del_inc_CheckBox.IsChecked.Value, UpdateStatus, delegate { Dispatcher.Invoke(new Action(() => { System.Windows.MessageBox.Show(this, g.dict["MessSaveFileDone"].ToString(), g.dict["StatusTextDone"].ToString()); myProgressBar.Value = 0; })); }, null);
    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        curr_ext_file = ((System.Windows.Controls.RadioButton)sender).Content.ToString().ToLower();
        if (curr_ext_file.ToLower() == MySQLRadioButton.Content.ToString().ToLower())
            curr_ext_file = "mysql";
    }
    private void UpdateStatus(int curr_row, string info)
    {
        myProgressBar.Value = curr_row;
        StatusText.Text = info;
    }
    private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (myProgressBar.Value <= 0)
        {
            StatusText.Text = StatusText.Tag.ToString();
            My_Top_Menu.IsEnabled = true;
            SelectedFilePathButton.IsEnabled = true;
            Encodings.IsEnabled = true;
            del_inc_CheckBox.IsEnabled = true;
            MySQLRadioButton.IsEnabled = true;
            CSVRadioButton.IsEnabled = true;
            XMLRadioButton.IsEnabled = true;
            SaveAsButton.IsEnabled = true;
        }
        else
        {
            My_Top_Menu.IsEnabled = false;
            SelectedFilePathButton.IsEnabled = false;
            Encodings.IsEnabled = false;
            del_inc_CheckBox.IsEnabled = false;
            MySQLRadioButton.IsEnabled = false;
            CSVRadioButton.IsEnabled = false;
            XMLRadioButton.IsEnabled = false;
            SaveAsButton.IsEnabled = false;
        }
    }

    private void GrdDBF_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ReloadDT();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        g.RegManager.Write("del_inc_CheckBox", (del_inc_CheckBox.IsChecked.Value ? "Y" : "N"));
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SetLng(object sender, RoutedEventArgs e)
    {
        if (sender == null)
            return;
        System.Windows.Controls.MenuItem item = ((System.Windows.Controls.MenuItem)sender);
        g.RegManager.Write("lng", item.Tag.ToString());
        item.IsChecked = true;
        g.curr_lng = item.Tag.ToString();
        foreach (System.Windows.Controls.MenuItem i in AlLngItems.Items)
            if (i != item)
                i.IsChecked = false;
        g.dict = [];
        try
        {
            // Do not initialize this variable here.
            g.dict.Source = new Uri("..\\Resources\\StringResources-" + g.curr_lng + ".xaml", UriKind.Relative);
        }
        catch
        {
            g.dict.Source = new Uri("..\\Resources\\StringResources-eng.xaml", UriKind.Relative);
        }
        this.Resources.MergedDictionaries.Clear();
        this.Resources.MergedDictionaries.Add(g.dict);
        Title = g.dict["TitleMainWindow"].ToString() + " - " + g.preficsBildProgramm;
    }

    private void MenuItem_Click_1(object sender, RoutedEventArgs e)
    {
        AboutBox1 ab = new();
        ab.ShowDialog();
    }

    private void MenuItem_Click_URL_GoTo(object sender, RoutedEventArgs e)
    {
        bool is_sourceforge = ((System.Windows.Controls.MenuItem)sender).Tag.ToString().Contains("sourceforge");
        if (((System.Windows.Controls.MenuItem)sender).Tag != null && !is_sourceforge)
            System.Diagnostics.Process.Start(((System.Windows.Controls.MenuItem)sender).Tag.ToString() + "&usa_app=DBF_to_MySQL&ver=" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
        else if (((System.Windows.Controls.MenuItem)sender).Tag != null)
            System.Diagnostics.Process.Start(((System.Windows.Controls.MenuItem)sender).Tag.ToString());
    }
}