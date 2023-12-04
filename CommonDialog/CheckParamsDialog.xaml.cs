using System.Windows.Controls;

namespace MachineControlsLibrary.CommonDialog
{
    /// <summary>
    /// Interaction logic for CheckParamsDialog.xaml
    /// </summary>
    public partial class CheckParamsDialog : UserControl, IHaveTitle
    {
        public void SetTitle(string title) => Title.Text = title;
        public CheckParamsDialog()
        {
            InitializeComponent();
        }
    }
}
