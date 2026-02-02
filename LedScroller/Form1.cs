using LedScroller.DTO;
using Microsoft.Extensions.Configuration;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
namespace LedScroller
{
    public partial class Form1 : Form
    {

        private int _iterationCount = 0;
        private int _maxIterations = 1;
        private bool _launchTimerTriggered = false;

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private Bitmap textBitmap;
        private Graphics bitmapGraphics;
        private Font drawFont = //new Font("LED Board-7", 27, FontStyle.Bold);
         new Font("Lcd Phone", 30, FontStyle.Bold);

        private SolidBrush drawBrush = new SolidBrush(Color.Black);
        private LinearGradientBrush gradBrush;
            

        private string scrollString = "Quote of the day!!!!!!";
        private int xPos = 0;
        private System.Windows.Forms.Timer scrollTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer LaunchTimer = new System.Windows.Forms.Timer();
        private SettingsDTO _settings = new SettingsDTO();


        public Form1()
        {
            InitializeComponent();
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("AppSettings.json", false)
                .Build();

            _settings = builder.Get<SettingsDTO>();

            scrollString = _settings.Message;
            _maxIterations = _settings.Iterations;

          gradBrush = new LinearGradientBrush(new Rectangle(0, 0, 200, 50), Color.FromName(_settings.Color1), Color.FromName(_settings.Color2), 45);


            //this.Width = 200;
            this.ClientSize = new Size(400, 50);
            this.DoubleBuffered = true; // Prevents flickering
            InitializeTextRendering();
            SetLocation();
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
            LaunchTimer.Interval = 1000 * 60; // Adjust speed (milliseconds)
            LaunchTimer.Tick += new EventHandler(LaunchTimer_Tick);
            LaunchTimer.Start();


            scrollTimer.Interval = _settings.ScrollSpeed; // Adjust speed (milliseconds)
            scrollTimer.Tick += new EventHandler(ScrollTimer_Tick);
            scrollTimer.Start();
        }

        private void LaunchTimer_Tick(object sender, EventArgs e)
        {
            var rightNow = DateTime.Now;
            if (rightNow.Minute == 00 || 
                rightNow.Minute == 15 ||
                rightNow.Minute == 30 ||
                rightNow.Minute == 45 )
            {
                if (!_launchTimerTriggered)
                {
                    scrollTimer.Start();
                    _launchTimerTriggered = true;
                }
                
            }

        }

        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            // Shift text position to the left
            xPos -= 1;

            // If the text has scrolled off the left edge, reset its position to the right
            // You'll need to measure the string width to ensure it scrolls completely off
            if (xPos <= -MeasureStringWidth(scrollString))
            {
                xPos = this.Width; // Or the right edge of your LED matrix area
                _iterationCount++;
                if(_iterationCount >= _maxIterations)
                {
                    _iterationCount = 0;
                    scrollTimer.Stop();
                    _launchTimerTriggered = false;
                }
            }

            // Redraw the bitmap with the new position
            bitmapGraphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, textBitmap.Width, textBitmap.Height);

            bitmapGraphics.DrawString(scrollString, drawFont, gradBrush, xPos, 10);

            var trans = 100;

            bitmapGraphics.DrawRectangle(new Pen(new SolidBrush(Color.FromArgb(trans, Color.LightGray)), 1), new Rectangle(0, 0, 399, 49));
            var rowHeight = 10;
            var currentTop = 0;

            while (currentTop < 100)
            {
                bitmapGraphics.DrawLine(new Pen(new SolidBrush(Color.FromArgb(trans, Color.LightGray)), 1), 0, currentTop, 399, currentTop);
                currentTop += rowHeight;
            }

            var colWidth = 10;
            var currentLeft = 0;

            while (currentLeft < 399)
            {
                bitmapGraphics.DrawLine(new Pen(new SolidBrush(Color.FromArgb(trans, Color.LightGray)), 1), currentLeft, 1, currentLeft, 99);
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
        private void SetLocation()
        {
            // Check location to see if it is offscreen
            //
            var savedLocation = Properties.Settings.Default.Location;

            if (savedLocation.X >= 0 && savedLocation.Y >= 0)
            {
                // Ensure the saved location is visible on any connected screen
                if (IsLocationVisible(savedLocation, this.Size))
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = savedLocation;
                }
                else
                {
                    // If not visible, center on primary screen
                    this.StartPosition = FormStartPosition.CenterScreen;
                }
            }


        }
        private void SaveLocation()
        {
            Properties.Settings.Default.Location = Location;

            Properties.Settings.Default.Save();
        }
        private bool IsLocationVisible(Point location, Size size)
        {
            Rectangle formRect = new Rectangle(location, size);
            return Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(formRect));
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveLocation();
        }

        private async void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
            if (e.Control && e.KeyCode == Keys.S)
            {
                string input = string.Empty;
                ShowInputDialog(ref input, "Enter message:", "Input Dialog" ,_settings);
                scrollString = input;

                var settings = File.ReadAllText("AppSettings.json");
                var settingsDTO = JsonSerializer.Deserialize<SettingsDTO>(settings);

                settingsDTO.Message = scrollString;
                
                await using FileStream stream = File.Create("AppSettings.json");
                await JsonSerializer.SerializeAsync(stream, settingsDTO,new JsonSerializerOptions { WriteIndented=true});

                _settings = settingsDTO;
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private static DialogResult ShowInputDialog(ref string input, string prompt, string title,SettingsDTO settings)
        {
            Form dialog = new Form
            {
                Width = 300,
                Height = 150,
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen
            };

            Label label = new Label { Text = prompt, Left = 10, Top = 10, Width = 260 };
            TextBox textBox = new TextBox { Left = 10, Top = 40, Width = 260 };
            Button okButton = new Button { Text = "OK", Left = 100, Width = 80, Top = 70, DialogResult = DialogResult.OK };

            dialog.Controls.Add(label);
            dialog.Controls.Add(textBox);
            dialog.Controls.Add(okButton);
            dialog.AcceptButton = okButton;
            textBox.Text = settings.Message;
            DialogResult result = dialog.ShowDialog();
            input = textBox.Text;
            return result;
        }

    }
}
