////////////////////////////////////////////////
// © https://github.com/badhitman 
////////////////////////////////////////////////
using System.Windows.Controls;

namespace DBF_to_MySQL__CSV_and_XML
{
    class exComboBoxItem : ComboBoxItem
    {
        public exComboBoxItem(string Content, string SysName)
        {
            this.Content = Content;
            this.ToolTip = g.dict["MessSysytemNameEnc"].ToString() + ": " + SysName;
            this.Tag = SysName;
        }
    }
}
