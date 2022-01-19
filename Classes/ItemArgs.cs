using System;

namespace MachineControlsLibrary.Classes
{
    internal class ItemArgs : EventArgs
    {
        public ItemArgs(int layerNum, bool enable)
        {
            LayerName = layerNum;
            Enable = enable;
        }

        public int LayerName { get; set; }
        public bool Enable { get; set; }
    }
}
