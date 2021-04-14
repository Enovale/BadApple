using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace BadApple
{
    public class AppContext : ApplicationContext
    {
        private int _width = 26;
        private int _height = 26;
        private NotifyIcon[,] _icons;
        private Bitmap[] _sequence;
        private Bitmap _whiteBmp;
        private Bitmap _grayBmp;
        private Bitmap _blackBmp;
        private Icon _whiteIcon;
        private Icon _grayIcon;
        private Icon _blackIcon;
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        
        public AppContext()
        {
            //AllocConsole();
            _whiteBmp = new Bitmap(1, 1);
            _grayBmp = new Bitmap(1, 1);
            _blackBmp = new Bitmap(1, 1);
            using (var g = Graphics.FromImage(_whiteBmp))
                g.Clear(Color.White);
            using (var g = Graphics.FromImage(_grayBmp))
                g.Clear(Color.Gray);
            using (var g = Graphics.FromImage(_blackBmp))
                g.Clear(Color.Black);
            _whiteIcon = Icon.FromHandle(_whiteBmp.GetHicon());
            _grayIcon = Icon.FromHandle(_grayBmp.GetHicon());
            _blackIcon = Icon.FromHandle(_blackBmp.GetHicon());
            
            _icons = new NotifyIcon[_width, _height];
            for (var x = _width-1; x >= 0; x--)
            {
                for (var y = _height-1; y >= 0; y--)
                {
                    _icons[x, y] = new NotifyIcon()
                    {
                        Visible = true,
                        Icon = _blackIcon,
                        Text = $"Position: {x}, {y}"
                    };
                }
            }

            var files = Directory.GetFiles("Assets/Sequence");
            _sequence = new Bitmap[files.Length];
            for (var i = 0; i < _sequence.Length; i++)
            {
                _sequence[i] = (Bitmap)Image.FromFile(files[i]);
            }
            
            // Signify start of the sequence
            _icons[0,0].Icon = _whiteIcon;
            _icons[1,0].Icon = _whiteIcon;
            _icons[0,1].Icon = _whiteIcon;
            _icons[_width-1, _height-1].Icon = _whiteIcon;
            Thread.Sleep(1000);
            _icons[0, 0].Icon = _blackIcon;
            _icons[1,0].Icon = _blackIcon;
            _icons[0,1].Icon = _blackIcon;
            _icons[_width-1, _height-1].Icon = _blackIcon;

            var watch = new Stopwatch();
            if(File.Exists("out.txt"))
                File.Delete("out.txt");
            using var file = new StreamWriter("out.txt");
            var frame = 0;
            foreach (var item in _sequence)
            {
                frame++;
                watch.Reset();
                watch.Start();
                for (var x = 0; x < _width; x++)
                {
                    for (var y = 0; y < _height; y++)
                    {
                        var icon = _icons[x, y];
                        var newY = y + 1;
                        if (newY >= _width)
                            newY = 0;
                        var bright = item.GetPixel(newY, x).GetBrightness();
                        if (bright <= 0.3)
                            icon.Icon = _blackIcon;
                        else if (bright > 0.3 && bright < 0.7)
                            icon.Icon = _grayIcon;
                        else if (bright >= 0.7)
                            icon.Icon = _whiteIcon;
                    }
                }
                watch.Stop();
                if (watch.ElapsedMilliseconds > 1000)
                {
                    file.WriteLineAsync($"Frame {frame} in {watch.ElapsedMilliseconds}ms, scaling required to meet: " +
                                        (watch.ElapsedMilliseconds / (1000 / 30)) + "x");
                }
                Thread.Sleep(Math.Max(0, (int) (1000 - watch.ElapsedMilliseconds)));
            }
        }

        protected override void OnMainFormClosed(object? sender, EventArgs e)
        {
            foreach (var notifyIcon in _icons)
            {
                notifyIcon.Visible = false;
                notifyIcon.Icon.Dispose();
                notifyIcon.Dispose();
            }

            base.OnMainFormClosed(sender, e);
        }
    }
}