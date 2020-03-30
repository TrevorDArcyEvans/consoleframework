using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;
using Xaml;

// TODO : Autoindent
// TODO : Ctrl+Home/Ctrl+End
// TODO : Alt+Backspace deletes word
// TODO : Shift+Delete deletes line
// TODO : Scrollbars full support
// TODO : Ctrl+arrows
// TODO : Selection
// TODO : Selection copy/paste/cut/delete
// TODO : Undo/Redo, commands autogrouping
// TODO : Read only mode
// TODO : Tabs (converting to spaces when loading?)
namespace ConsoleFramework.Controls {
    public class TextHolder {
        // TODO : change to more appropriate data structure
        private List<string> lines;
        private readonly string newLine = Environment.NewLine;

        public TextHolder(string text, string newLine) {
            this.newLine = newLine;
            setText(text);
        }

        public TextHolder(string text) {
            setText(text);
        }

        private void setText(string text) {
            lines = new List<string>(text.Split(new[] { newLine }, StringSplitOptions.None));
        }

        public string Text {
            get => string.Join(newLine, lines);
            set => setText(value);
        }

        public IList<string> Lines => lines.AsReadOnly();

        public int LinesCount => lines.Count;
        public int ColumnsCount => lines.Max(it => it.Length);

        /// <summary>
        /// Inserts string after specified position with respect to newline symbols.
        /// Returns the coords (col+ln) of next symbol after inserted.
        /// TODO : write unit test to check return value
        /// </summary>
        public Point Insert(int ln, int col, string s) {
            // There are at least one empty line even if no text at all
            if (ln >= lines.Count) {
                throw new ArgumentException("ln is out of range", nameof(ln));
            }

            string currentLine = lines[ln];
            if (col > currentLine.Length) {
                throw new ArgumentException("col is out of range", nameof(col));
            }

            string leftPart = currentLine.Substring(0, col);
            string rightPart = currentLine.Substring(col);

            string[] linesToInsert = s.Split(new[] {Environment.NewLine}, StringSplitOptions.None);

            if (linesToInsert.Length == 1) {
                lines[ln] = leftPart + linesToInsert[0] + rightPart;
                return new Point(leftPart.Length + linesToInsert[0].Length, ln);
            } else {
                lines[ln] = leftPart + linesToInsert[0];
                lines.InsertRange(ln + 1, linesToInsert.Skip(1).Take(linesToInsert.Length - 1));
                string lastStrLeftPart = lines[ln + linesToInsert.Length - 1];
                lines[ln + linesToInsert.Length - 1] = lastStrLeftPart + rightPart;
                return new Point(lastStrLeftPart.Length, ln + linesToInsert.Length - 1);
            }
        }

        /// <summary>
        /// Will write the content of text editor to matrix constrained with _width/_height,
        /// starting from (left, top) coord. Coords may be negative.
        /// If there are any gap before (or after) text due to margin, window will be filled
        /// with spaces there.
        /// Window size should be equal to _width/_height passed.
        /// </summary>
        public void WriteToWindow(int left, int top, int width, int height, char[,] window) {
            if (window.GetLength(0) != height) {
                throw new ArgumentException("window _height differs from viewport _height");
            }

            if (window.GetLength(1) != width) {
                throw new ArgumentException("window _width differs from viewport _width");
            }

            for (int y = top; y < 0; y++) {
                for (int x = 0; x < width; x++) {
                    window[y - top, x] = ' ';
                }
            }

            for (int y = Math.Max(0, top); y < Math.Min(top + height, lines.Count); y++) {
                string line = lines[y];
                for (int x = left; x < 0; x++) {
                    window[y - top, x - left] = ' ';
                }

                for (int x = Math.Max(0, left); x < Math.Min(left + width, line.Length); x++) {
                    window[y - top, x - left] = line[x];
                }

                for (int x = Math.Max(line.Length, left); x < left + width; x++) {
                    window[y - top, x - left] = ' ';
                }
            }

            for (int y = lines.Count; y < top + height; y++) {
                for (int x = 0; x < width; x++) {
                    window[y - top, x] = ' ';
                }
            }
        }

        /// <summary>
        /// Deletes text from lnFrom+colFrom to lnTo+colTo (exclusive).
        /// </summary>
        public void Delete(int lnFrom, int colFrom, int lnTo, int colTo) {
            if (lnFrom > lnTo) {
                throw new ArgumentException("lnFrom should be <= lnTo");
            }
            if (lnFrom == lnTo && colFrom >= colTo) {
                throw new ArgumentException("colFrom should be < colTo on the same line");
            }
            //
            lines[lnFrom] = lines[lnFrom].Substring(0, colFrom) + lines[lnTo].Substring(colTo);
            lines.RemoveRange(lnFrom + 1, lnTo - lnFrom);
        }
    }


    /// <summary>
    /// Multiline text editor.
    /// </summary>
    [ContentProperty("Text")]
    public class TextEditor : Control {
        private TextEditorController controller;
        private char[,] buffer;
        private ScrollBar horizontalScrollbar;
        private ScrollBar verticalScrollbar;

        public string Text {
            get => controller.Text;
            set {
                if (value != controller.Text) {
                    controller.Text = value;
                    CursorPosition = controller.CursorPos;
                    Invalidate();
                }
            }
        }

        // TODO : Scrollbars always visible

        private void applyCommand(TextEditorController.ICommand cmd) {
            var oldCursorPos = controller.CursorPos;
            if (cmd.Do(controller)) {
                Invalidate();
            }

            if (oldCursorPos != controller.CursorPos) {
                CursorPosition = controller.CursorPos;
            }
        }

        public TextEditor() {
            controller = new TextEditorController("", 0, 0);
            KeyDown += OnKeyDown;
            MouseDown += OnMouseDown;
            CursorVisible = true;
            CursorPosition = new Point(0, 0);
            Focusable = true;

            horizontalScrollbar = new ScrollBar {
                Orientation = Orientation.Horizontal,
                Visibility = Visibility.Hidden
            };
            verticalScrollbar = new ScrollBar {
                Orientation = Orientation.Vertical,
                Visibility = Visibility.Hidden
            };
            AddChild(horizontalScrollbar);
            AddChild(verticalScrollbar);
        }

        protected override Size MeasureOverride(Size availableSize) {
            verticalScrollbar.Measure(new Size(1, availableSize.Height));
            horizontalScrollbar.Measure(new Size(availableSize.Width, 1));
            return new Size(0, 0);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            if (controller.LinesCount > finalSize.Height) {
                verticalScrollbar.Visibility = Visibility.Visible;
                verticalScrollbar.MaxValue =
                    controller.LinesCount + TextEditorController.LINES_BOTTOM_MAX_GAP - controller.Window.Height;
                verticalScrollbar.Value = controller.Window.Top;
                verticalScrollbar.Invalidate();
            } else {
                verticalScrollbar.Visibility = Visibility.Collapsed;
                verticalScrollbar.Value = 0;
                verticalScrollbar.MaxValue = 10;
            }
            if (controller.ColumnsCount >= finalSize.Width) {
                horizontalScrollbar.Visibility = Visibility.Visible;
                horizontalScrollbar.MaxValue =
                    controller.ColumnsCount + TextEditorController.COLUMNS_RIGHT_MAX_GAP - controller.Window.Width;
                horizontalScrollbar.Value = controller.Window.Left;
                horizontalScrollbar.Invalidate();
            } else {
                horizontalScrollbar.Visibility = Visibility.Collapsed;
                horizontalScrollbar.Value = 0;
                horizontalScrollbar.MaxValue = 10;
            }
            horizontalScrollbar.Arrange(new Rect(
                0,
                Math.Max(0, finalSize.Height - 1),
                Math.Max(0, finalSize.Width -
                            (verticalScrollbar.Visibility == Visibility.Visible
                             || horizontalScrollbar.Visibility != Visibility.Visible
                                ? 1
                                : 0)),
                1
            ));
            verticalScrollbar.Arrange(new Rect(
                Math.Max(0, finalSize.Width - 1),
                0,
                1,
                Math.Max(0, finalSize.Height -
                            (horizontalScrollbar.Visibility == Visibility.Visible
                             || verticalScrollbar.Visibility != Visibility.Visible
                                ? 1
                                : 0))
            ));
            Size contentSize = new Size(
                Math.Max(0, finalSize.Width - (verticalScrollbar.Visibility == Visibility.Visible ? 1 : 0)),
                Math.Max(0, finalSize.Height - (horizontalScrollbar.Visibility == Visibility.Visible ? 1 : 0))
            );
            controller.Window = new Rect(controller.Window.TopLeft, contentSize);
            buffer = new char[contentSize.Height, contentSize.Width];
            return finalSize;
        }

        public override void Render(RenderingBuffer buffer) {
            var attrs = Colors.Blend(Color.Green, Color.DarkBlue);
            buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', attrs);

            controller.WriteToWindow(this.buffer);
            Size contentSize = controller.Window.Size;
            for (int y = 0; y < contentSize.Height; y++) {
                for (int x = 0; x < contentSize.Width; x++) {
                    buffer.SetPixel(x, y, this.buffer[y, x]);
                }
            }

            if (verticalScrollbar.Visibility == Visibility.Visible
                && horizontalScrollbar.Visibility == Visibility.Visible) {
                buffer.SetPixel(buffer.Width - 1, buffer.Height - 1,
                    UnicodeTable.SingleFrameBottomRightCorner,
                    Colors.Blend(Color.DarkCyan, Color.DarkBlue));
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs) {
            Point position = mouseButtonEventArgs.GetPosition(this);
            Point constrained = new Point(
                Math.Max(0, Math.Min(controller.Window.Size.Width - 1, position.X)),
                Math.Max(0, Math.Min(controller.Window.Size.Height - 1, position.Y))
            );
            applyCommand(new TextEditorController.TrySetCursorCmd(constrained));
            mouseButtonEventArgs.Handled = true;
        }

        private void OnKeyDown(object sender, KeyEventArgs args) {
            ConsoleKeyInfo keyInfo = new ConsoleKeyInfo(args.UnicodeChar,
                (ConsoleKey) args.wVirtualKeyCode,
                (args.dwControlKeyState & ControlKeyState.SHIFT_PRESSED) == ControlKeyState.SHIFT_PRESSED,
                (args.dwControlKeyState & ControlKeyState.LEFT_ALT_PRESSED) == ControlKeyState.LEFT_ALT_PRESSED
                || (args.dwControlKeyState & ControlKeyState.RIGHT_ALT_PRESSED) == ControlKeyState.RIGHT_ALT_PRESSED,
                (args.dwControlKeyState & ControlKeyState.LEFT_CTRL_PRESSED) == ControlKeyState.LEFT_CTRL_PRESSED
                || (args.dwControlKeyState & ControlKeyState.RIGHT_CTRL_PRESSED) == ControlKeyState.RIGHT_CTRL_PRESSED
            );
            if (!char.IsControl(keyInfo.KeyChar)) {
                applyCommand(new TextEditorController.AppendStringCmd(new string(keyInfo.KeyChar, 1)));
            }

            switch (keyInfo.Key) {
                case ConsoleKey.Enter:
                    applyCommand(new TextEditorController.AppendStringCmd(Environment.NewLine));
                    break;
                case ConsoleKey.UpArrow:
                    applyCommand(new TextEditorController.MoveCursorCmd(TextEditorController.Direction.Up));
                    break;
                case ConsoleKey.DownArrow:
                    applyCommand(new TextEditorController.MoveCursorCmd(TextEditorController.Direction.Down));
                    break;
                case ConsoleKey.LeftArrow:
                    applyCommand(new TextEditorController.MoveCursorCmd(TextEditorController.Direction.Left));
                    break;
                case ConsoleKey.RightArrow:
                    applyCommand(new TextEditorController.MoveCursorCmd(TextEditorController.Direction.Right));
                    break;
                case ConsoleKey.Backspace:
                    applyCommand(new TextEditorController.DeleteLeftSymbolCmd());
                    break;
                case ConsoleKey.Delete:
                    applyCommand(new TextEditorController.DeleteRightSymbolCmd());
                    break;
                case ConsoleKey.PageDown:
                    applyCommand(new TextEditorController.PageDownCmd());
                    break;
                case ConsoleKey.PageUp:
                    applyCommand(new TextEditorController.PageUpCmd());
                    break;
                case ConsoleKey.Home:
                    applyCommand(new TextEditorController.HomeCommand());
                    break;
                case ConsoleKey.End:
                    applyCommand(new TextEditorController.EndCommand());
                    break;
            }
        }
    }
}
