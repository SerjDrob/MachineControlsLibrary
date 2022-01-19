using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MachineControlsLibrary.Classes
{
    public record LayerGeometryCollection
    (
     GeometryCollection Geometries,
     string LayerName,
     bool LayerEnable,
     Brush LayerColor,
     Brush GeometryColor
    );
}
