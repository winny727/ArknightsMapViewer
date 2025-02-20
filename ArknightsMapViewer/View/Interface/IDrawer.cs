using System;
using System.Collections.Generic;
using System.Drawing;

namespace ArknightsMapViewer
{
    public interface IDrawer
    {
        public void Draw(Bitmap bitmap);
    }
}