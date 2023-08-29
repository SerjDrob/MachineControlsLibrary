namespace MachineControlsLibrary.Classes
{
    internal class LgToElAdapter
    {
        private LgToElAdapter()
        {
        }
        public static EnaLayer NewEnaLayer(LayerGeometryCollection layerGeometryCollection)
        {
            return new EnaLayer(layerGeometryCollection.LayerName,
                                layerGeometryCollection.LayerEnable,
                                layerGeometryCollection.LayerColor);
        }
    }
}
