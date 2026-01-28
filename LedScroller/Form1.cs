using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace LedScroller
{
    public partial class Form1 : Form
    {

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);




        private Bitmap textBitmap;
        private Graphics bitmapGraphics;
        private Font drawFont = new Font("Lcd Phone", 30,FontStyle.Bold);
        
        private SolidBrush drawBrush = new SolidBrush(Color.Black);
        private LinearGradientBrush gradBrush =
            new LinearGradientBrush(new Rectangle(0, 0, 200, 50), Color.Red, Color.Yellow, 45);

        private string scrollString = "Quote of the day!!!!!!";
        private int xPos = 0;
        private System.Windows.Forms.Timer scrollTimer = new System.Windows.Forms.Timer();

        public Form1()
        {
            InitializeComponent();
            //this.Width = 200;
            this.ClientSize = new Size(400, 50);
            this.DoubleBuffered = true; // Prevents flickering
            InitializeTextRendering();
            SetupTimer();
        }

        private void InitializeTextRendering()
        {
            // Create an off-screen bitmap for rendering (adjust size as needed for your LED matrix)
            textBitmap = new Bitmap(400, 50); // Example: 200 pixels wide, 8 pixels tall
            bitmapGraphics = Graphics.FromImage(textBitmap);
            // Clear the background to black
            bitmapGraphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, textBitmap.Width, textBitmap.Height);
        }

        private void SetupTimer()
        {
            scrollTimer.Interval = 25; // Adjust speed (milliseconds)
            scrollTimer.Tick += new EventHandler(Timer_Tick);
            scrollTimer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Shift text position to the left
            xPos -= 1;

            // If the text has scrolled off the left edge, reset its position to the right
            // You'll need to measure the string width to ensure it scrolls completely off
            if (xPos <= -MeasureStringWidth(scrollString))
            {
                xPos = this.Width; // Or the right edge of your LED matrix area
            }

            // Redraw the bitmap with the new position
            bitmapGraphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, textBitmap.Width, textBitmap.Height);

            bitmapGraphics.DrawString(scrollString, drawFont, gradBrush, xPos, 10);

            var trans = 100;

            bitmapGraphics.DrawRectangle(new Pen(new SolidBrush(Color.FromArgb(trans, Color.LightGray)), 1), new Rectangle(0, 0, 399, 49));
            var rowHeight = 10;
            var currentTop = 0;

            while(currentTop < 100)
            {
                bitmapGraphics.DrawLine(new Pen(new SolidBrush(Color.FromArgb(trans, Color.LightGray)), 1), 0, currentTop, 399, currentTop);
                currentTop += rowHeight;
            }

            var colWidth = 10;
            var currentLeft = 0;

            while (currentLeft < 399)
            {
                bitmapGraphics.DrawLine(new Pen(new SolidBrush(Color.FromArgb(trans, Color.LightGray)), 1), currentLeft,1, currentLeft,99);
                currentLeft += colWidth;
            }


          



            // Invalidate the form to trigger a Paint event and display the updated bitmap
            this.Invalidate();
        }

        private int MeasureStringWidth(string text)
        {
            // Measure string width for proper looping
            return (int)bitmapGraphics.MeasureString(text, drawFont).Width;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw the off-screen bitmap onto the form (or control)
            e.Graphics.DrawImage(textBitmap, 0, 0);
            // From here, you would typically extract the pixel data to send to the LED hardware
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            IntPtr handle = this.Handle;
            int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
            SetWindowLong(handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

        }

        protected override void WndProc(ref Message m)
        {
            const int RESIZE_HANDLE_SIZE = 10;

            switch (m.Msg)
            {
                case 0x0084/*NCHITTEST*/ :
                    base.WndProc(ref m);

                    if ((int)m.Result == 0x01/*HTCLIENT*/)
                    {
                        Point screenPoint = new Point(m.LParam.ToInt32());
                        Point clientPoint = this.PointToClient(screenPoint);
                        if (clientPoint.Y <= RESIZE_HANDLE_SIZE)
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)13/*HTTOPLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)12/*HTTOP*/ ;
                            else
                                m.Result = (IntPtr)14/*HTTOPRIGHT*/ ;
                        }
                        else if (clientPoint.Y <= (Size.Height - RESIZE_HANDLE_SIZE))
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)10/*HTLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)2/*HTCAPTION*/ ;
                            else
                                m.Result = (IntPtr)11/*HTRIGHT*/ ;
                        }
                        else
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)16/*HTBOTTOMLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)15/*HTBOTTOM*/ ;
                            else
                                m.Result = (IntPtr)17/*HTBOTTOMRIGHT*/ ;
                        }
                    }
                    return;
            }
            base.WndProc(ref m);
        }


    }
}
