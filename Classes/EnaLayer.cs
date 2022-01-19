using System.Windows.Media;

namespace MachineControlsLibrary.Classes
{
    public class EnaLayer
    {
        public EnaLayer(string layer, bool enable, Brush color)
        {
            Layer = layer;
            Enable = enable;
            Color = color;
        }
        public Brush Color { get; set; }
        public string Layer { get; set; }
        public bool Enable {  get; set; }
    }
}
