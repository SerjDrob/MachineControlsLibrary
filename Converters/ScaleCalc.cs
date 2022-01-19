using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MachineControlsLibrary.Converters
{
    internal class ScaleCalc
    {
        private readonly double _width;// = 1;
        private readonly double _height;// = 1;
        private readonly double _sizex; //= 1;
        private readonly double _sizey; //= 1;
        private readonly double _margin; //= 0;
        private readonly bool _xProportion;
        private readonly bool _yProportion;
        private readonly bool _autoProportion;

        public ScaleCalc(double width, double height, double sizex, double sizey, double margin, bool xProportion, bool yProportion, bool autoProportion)
        {
            this._width = width;
            this._height = height;
            this._sizex = sizex;
            this._sizey = sizey;
            this._margin = margin;
            this._xProportion = xProportion;
            this._yProportion = yProportion;
            this._autoProportion = autoProportion;
        }
        public void Calc(out double scalex, out double scaley, out double marginx, out double marginy) 
        {
            scalex = ((_width - _margin * _width) / _sizex);
            scaley = ((_height - _margin * _width) / _sizey);
            marginx = (double)0;
            marginy = (double)0;
            if (_autoProportion & !(_xProportion | _yProportion))
            {
                (scalex, scaley) = (_sizex / _width) > (_sizey / _height) ? (scalex, scalex) : (scaley, scaley);
                (marginx, marginy) = (_sizex / _width) > (_sizey / _height) ? (_margin * _width/2, (_height - _sizey * scaley) / 2) : ((_width - _sizex * scalex) / 2, _margin * _height/2);
            }
            else if (!_xProportion & _yProportion)
            {
                if (_xProportion)
                {
                    (scalex, scaley) = (scalex, scalex);
                    (marginx, marginy) = (_margin, (_height - _sizey * scaley) / 2);
                }
                if (_yProportion)
                {
                    (scalex, scaley) = (scaley, scaley);
                    (marginx, marginy) = ((_width - _sizex * scalex) / 2, _margin);
                }
            }
        }
    }
}
