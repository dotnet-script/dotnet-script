using System;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    /// <summary>
    /// <see cref="IConsoleDevice"/> implementation on top of <see cref="Console"/>.
    /// Cursor, color and geometry members swallow failures because those APIs throw or
    /// misbehave when no terminal is attached; reading and writing deliberately do not,
    /// since a failure there is not something the editor can recover from.
    /// </summary>
    public sealed class SystemConsoleDevice : IConsoleDevice
    {
        private const int FallbackWidth = 80;
        private const int FallbackHeight = 24;

        private readonly bool _supportsColors;

        public SystemConsoleDevice()
        {
            _supportsColors = DetectColorSupport();
        }

        public int Width
        {
            get
            {
                try
                {
                    var width = Console.BufferWidth;
                    return width > 1 ? width : FallbackWidth;
                }
                catch (Exception)
                {
                    return FallbackWidth;
                }
            }
        }

        public int Height
        {
            get
            {
                try
                {
                    var height = Console.BufferHeight;
                    return height > 0 ? height : FallbackHeight;
                }
                catch (Exception)
                {
                    return FallbackHeight;
                }
            }
        }

        public int CursorLeft
        {
            get
            {
                try
                {
                    return Console.CursorLeft;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public int CursorTop
        {
            get
            {
                try
                {
                    return Console.CursorTop;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        // Only ever set. The getter is not supported on Unix.
        public bool CursorVisible
        {
            set
            {
                try
                {
                    Console.CursorVisible = value;
                }
                catch (Exception)
                {
                    // Not supported by the current terminal.
                }
            }
        }

        public ConsoleColor ForegroundColor
        {
            get
            {
                try
                {
                    return Console.ForegroundColor;
                }
                catch (Exception)
                {
                    return ConsoleColor.Gray;
                }
            }
            set
            {
                try
                {
                    Console.ForegroundColor = value;
                }
                catch (Exception)
                {
                    // Colors are unavailable.
                }
            }
        }

        public bool TreatControlCAsInput
        {
            get
            {
                try
                {
                    return Console.TreatControlCAsInput;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            set
            {
                try
                {
                    Console.TreatControlCAsInput = value;
                }
                catch (Exception)
                {
                    // Not supported when no terminal is attached.
                }
            }
        }

        public bool SupportsColors => _supportsColors;

        public bool KeyAvailable
        {
            get
            {
                try
                {
                    return Console.KeyAvailable;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

        public void Write(string value) => Console.Write(value);

        public void SetCursorPosition(int left, int top)
        {
            try
            {
                Console.SetCursorPosition(Math.Max(0, left), Math.Max(0, top));
            }
            catch (Exception)
            {
                // Out of range on a resized window; the next full render recovers.
            }
        }

        public void ResetColor()
        {
            try
            {
                Console.ResetColor();
            }
            catch (Exception)
            {
                // Colors are unavailable.
            }
        }

        public void Clear()
        {
            try
            {
                Console.Clear();
            }
            catch (Exception)
            {
                // Not supported when no terminal is attached.
            }
        }

        private static bool DetectColorSupport()
        {
            try
            {
                if (Console.IsOutputRedirected)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
                {
                    return false;
                }

                var term = Environment.GetEnvironmentVariable("TERM");
                if (string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
