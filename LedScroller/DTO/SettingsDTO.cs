using System;
using System.Collections.Generic;
using System.Text;

namespace LedScroller.DTO
{
    internal class SettingsDTO
    {
        public string Message { get; set; }
        public int Iterations { get; set; }
        public int ScrollSpeed { get; set; }
        public string Color1 {  get; set; }
        public string Color2 { get; set; }

        public int Height { get; set; } = 85;
        public int Width { get; set; } = 800;
        public int FontSize { get; set; } = 60;
    }
}
