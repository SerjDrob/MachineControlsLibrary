using System.Windows.Controls;

namespace MachineControlsLibrary.CommonDialog
{
    /// <summary>
    /// Interaction logic for CommonDialog.xaml
    /// </summary>
    public partial class CommonDialog : UserControl
    {
        public void SetTitle(string title) => Title.Text = title;
        public CommonDialog()
        {
            InitializeComponent();
        }
    }
}