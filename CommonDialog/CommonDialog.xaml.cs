using System.Windows.Controls;

namespace MachineControlsLibrary.CommonDialog
{
    public partial class CommonDialog : UserControl, IHasTitle
    {
        public void SetTitle(string title) => Title.Text = title;
        public CommonDialog()
        {
            InitializeComponent();
        }
    }
}