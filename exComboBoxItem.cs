////////////////////////////////////////////////
// © https://github.com/badhitman 
////////////////////////////////////////////////
using System.Windows.Controls;

namespace DBF_to_MySQL__CSV_and_XML;

class ExComboBoxItem : ComboBoxItem
{
    public ExComboBoxItem(string Content, string SysName)
    {
        this.Content = Content;
        ToolTip = g.dict["MessSysytemNameEnc"].ToString() + ": " + SysName;
        Tag = SysName;
    }
}
