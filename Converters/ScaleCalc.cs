namespace MachineControlsLibrary.Converters
{
    internal class ScaleCalc
    {
        private readonly double _width;// = 1;
        private readonly double _height;// = 1;
        private readonly double _fieldSizeX; //= 1;
        private readonly double _fieldSizeY; //= 1;
        private readonly double _margin; //= 0;
        private readonly bool _xProportion;
        private readonly bool _yProportion;
        private readonly bool _autoProportion;
        private readonly double _specSizeX;
        private readonly double _specSizeY;


        public ScaleCalc(double width, double height, double fieldSizeX, double fieldSizeY, double margin, bool xProportion, bool yProportion, bool autoProportion, double specSizeX, double specSizeY)
        {
            this._width = width;
            this._height = height;
            this._fieldSizeX = fieldSizeX;
            this._fieldSizeY = fieldSizeY;
            this._margin = margin;
            this._xProportion = xProportion;
            this._yProportion = yProportion;
            this._autoProportion = autoProportion;
            _specSizeX = specSizeX;
            _specSizeY = specSizeY;
        }
        public void Calc(out double scalex, out double scaley, out double marginx, out double marginy)
        {
            scalex = (_width - _margin * _width) / _fieldSizeX;
            scaley = (_height - _margin * _width) / _fieldSizeY;
            marginx = (double)0;
            marginy = (double)0;
            if (_autoProportion & !(_xProportion | _yProportion))
            {
                //(scalex, scaley) = (_fieldSizeX / _width) > (_fieldSizeY / _height) ? (scalex, scalex) : (scaley, scaley);
                //(marginx, marginy) = (_fieldSizeX / _width) > (_fieldSizeY / _height) ? (_margin * _width/2, (_height - _fieldSizeY * scaley) / 2) : ((_width - _fieldSizeX * scalex) / 2, _margin * _height/2);

                var selector = (_fieldSizeX / _width) > (_fieldSizeY / _height);
                (scalex, scaley) = selector ? (scalex, scalex) : (scaley, scaley);
                (marginx, marginy) = selector ? (_margin * _width / 2, (_height - _specSizeY * scaley) / 2) : ((_width - _specSizeX * scalex) / 2, _margin * _height / 2);
            }
            else if (!_xProportion & _yProportion)
            {
                if (_xProportion)
                {
                    (scalex, scaley) = (scalex, scalex);
                    //(marginx, marginy) = (_margin, (_height - _fieldSizeY * scaley) / 2);
                    (marginx, marginy) = (_margin, (_height - _specSizeY * scaley) / 2);
                }
                if (_yProportion)
                {
                    (scalex, scaley) = (scaley, scaley);
                    //(marginx, marginy) = ((_width - _fieldSizeX * scalex) / 2, _margin);
                    (marginx, marginy) = ((_width - _specSizeX * scalex) / 2, _margin);

                }
            }
        }
    }
}
