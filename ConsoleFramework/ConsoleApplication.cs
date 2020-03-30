#if !WIN32 && !DOTNETCORE
    #define MONO
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using ConsoleFramework.Controls;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;
#if MONO
using Mono.Unix;
using Mono.Unix.Native;
#endif
using Xaml;

namespace ConsoleFramework
{
  public delegate void TerminalSizeChangedHandler(object sender, TerminalSizeChangedEventArgs args);

  /// <summary>
  /// Console application entry point.
  /// Encapsulates messages loop and application lifecycle.
  /// Supports Windows and Linux.
  /// </summary>
  public sealed class ConsoleApplication : IDisposable
  {
    private bool _maximized;
    private Size _savedBufferSize;
    private Rect _savedWindowRect;

    private IntPtr _consoleWindowHwnd;

    private IntPtr GetConsoleWindowHwnd()
    {
      if (IntPtr.Zero == _consoleWindowHwnd)
      {
        _consoleWindowHwnd = Win32.GetConsoleWindow();
      }

      return _consoleWindowHwnd;
    }

    /// <summary>
    /// Maximizes the terminal window size and terminal buffer size.
    /// Current size is stored.
    /// </summary>
    public void Maximize()
    {
      if (_usingLinux)
      {
        // Doesn't work in Konsole
        Console.Write("\x1B[9;1t");
        return;
      }

      if (_maximized)
      {
        return;
      }

      //
      _savedBufferSize = new Size(Console.BufferWidth, Console.BufferHeight);
      Win32.SendMessage(GetConsoleWindowHwnd(), Win32.WM_SYSCOMMAND, Win32.SC_MAXIMIZE, IntPtr.Zero);
      var maxWidth = Console.LargestWindowWidth;
      var maxHeight = Console.LargestWindowHeight;
      Console.SetWindowPosition(0, 0);
      Console.SetBufferSize(maxWidth, maxHeight);
      Console.SetWindowSize(maxWidth, maxHeight);

      // Apply new sizes to Canvas
      CanvasSize = new Size(maxWidth, maxHeight);
      _renderer.RootElementRect = new Rect(_canvas.Size);
      _renderer.UpdateLayout();

      _maximized = true;
    }

    /// <summary>
    /// Restores the terminal window size and terminal buffer to stored state.
    /// </summary>
    public void Restore()
    {
      if (_usingLinux)
      {
        // Doesn't work in Konsole
        Console.Write("\x1B[9;0t");
        return;
      }

      if (!_maximized)
      {
        return;
      }

      //
      Win32.SendMessage(GetConsoleWindowHwnd(), Win32.WM_SYSCOMMAND, Win32.SC_RESTORE, IntPtr.Zero);
      Console.SetWindowPosition(0, 0);

      // Get largest size again - because resolution of screen can change
      // between maximize and restore calls
      int maxWidth = Console.LargestWindowWidth;
      int maxHeight = Console.LargestWindowHeight;

      Console.SetWindowSize(Math.Min(_savedWindowRect.Width, maxWidth), Math.Min(_savedWindowRect.Height, maxHeight));
      Console.SetWindowPosition(_savedWindowRect.Left, _savedWindowRect.Top);

      // Apply new sizes to Canvas
      CanvasSize = new Size(_savedWindowRect.Width, _savedWindowRect.Height);
      _renderer.RootElementRect = new Rect(_canvas.Size);
      _renderer.UpdateLayout();

      _maximized = false;
    }

    /// <summary>
    /// Fires when console buffer size is changed.
    /// </summary>
    public event TerminalSizeChangedHandler TerminalSizeChanged;

    /// <summary>
    /// Default TerminalSizeChanged event handler. Invoked when
    /// initial CanvasSize and RootElementRect are empty and no another
    /// TerminalSizeChanged handler is attached.
    /// </summary>
    public void OnTerminalSizeChangedDefault(object sender, TerminalSizeChangedEventArgs args)
    {
      if (!this.userCanvasSize.IsEmpty)
      {
        throw new InvalidOperationException("Assertion failed.");
      }

      if (!this.userRootElementRect.IsEmpty)
      {
        throw new InvalidOperationException("Assertion failed.");
      }

      if (this.TerminalSizeChanged != null)
      {
        throw new InvalidOperationException("Assertion failed.");
      }

      _canvas.Size = new Size(args.Width, args.Height);
      _renderer.RootElementRect = new Rect(_canvas.Size);
      _renderer.UpdateLayout();
    }

    private Size userCanvasSize;

    /// <summary>
    /// Gets or sets a size of canvas. Whet set, old canvas image will be
    /// copied to new one.
    /// </summary>
    public Size CanvasSize
    {
      get
      {
        if (_running && userCanvasSize.IsEmpty)
        {
          return _canvas.Size;
        }

        return userCanvasSize;
      }
      set
      {
        if (_running && value != _canvas.Size)
        {
          _canvas.Size = value;
        }

        userCanvasSize = value;
      }
    }

    private Rect userRootElementRect;

    /// <summary>
    /// Gets or sets the root element rect.
    /// When set, root element will be added to invalidation queue automatically.
    /// </summary>
    public Rect RootElementRect
    {
      get
      {
        if (_running && userRootElementRect.IsEmpty)
        {
          return _renderer.RootElementRect;
        }

        return userRootElementRect;
      }
      set
      {
        if (_running && value != _renderer.RootElementRect)
        {
          _renderer.RootElementRect = value;
        }

        userRootElementRect = value;
      }
    }

    private volatile bool _running;
    private PhysicalCanvas _canvas;

    public static Control LoadFromXaml(string xamlResourceName, object dataContext)
    {
      var assembly = Assembly.GetEntryAssembly();
      using (var stream = assembly.GetManifestResourceStream(xamlResourceName))
      {
        if (null == stream)
        {
          throw new ArgumentException("Resource not found.", nameof(xamlResourceName));
        }

        using (StreamReader reader = new StreamReader(stream))
        {
          var result = reader.ReadToEnd();
          var namespaces = new List<string>()
          {
            "clr-namespace:Xaml;assembly=Xaml",
            "clr-namespace:ConsoleFramework.Xaml;assembly=ConsoleFramework",
            "clr-namespace:ConsoleFramework.Controls;assembly=ConsoleFramework"
          };
          var assyNamespaces = assembly
            .GetTypes()
            .Select(t => t.Namespace)
            .Distinct()
            .Select(x => $"clr-namespace:{x};assembly={assembly.GetName().Name}");
          namespaces.AddRange(assyNamespaces);
          var control = XamlParser.CreateFromXaml<Control>(result, dataContext, namespaces);
          control.DataContext = dataContext;
          control.Created();
          return control;
        }
      }
    }

    private static readonly bool _usingLinux;
    private static readonly bool _isDarwin;

    static ConsoleApplication()
    {
#if DOTNETCORE
      _usingLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
      _isDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#else
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    usingLinux = false;
                    break;
                case PlatformID.Unix:
                    usingLinux = true;
    #if MONO
                    Utsname uname;
                    Syscall.uname(out uname);
                    if (uname.sysname == "Darwin") {
                        isDarwin = true;
                    }
    #endif
                    break;
                case PlatformID.MacOSX:
                case PlatformID.Xbox:
                    throw new NotSupportedException();
            }
#endif
    }

    private ConsoleApplication()
    {
      _eventManager = new EventManager();
      _focusManager = new FocusManager(_eventManager);

      _exitWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
      _invokeWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
    }

    private static volatile ConsoleApplication instance;
    private static readonly object syncRoot = new object();

    /// <summary>
    /// Instance of Application object.
    /// </summary>
    public static ConsoleApplication Instance
    {
      get
      {
        if (instance == null)
        {
          lock (syncRoot)
          {
            if (instance == null)
            {
              instance = new ConsoleApplication();
            }
          }
        }

        return instance;
      }
    }

    private IntPtr _stdInputHandle;
    private IntPtr _stdOutputHandle;
    private readonly EventWaitHandle _exitWaitHandle;
    private readonly EventWaitHandle _invokeWaitHandle;
    private int? _mainThreadId;

    private struct ActionInfo
    {
      public readonly Action action;
      public readonly EventWaitHandle waitHandle;

      public ActionInfo(Action action, EventWaitHandle waitHandle)
      {
        this.action = action;
        this.waitHandle = waitHandle;
      }
    }

    private readonly List<ActionInfo> _actionsToBeInvoked = new List<ActionInfo>();
    private readonly object _actionsLocker = new object();

    /// <summary>
    /// Signals the message loop to be finished.
    /// Application shutdowns after that.
    /// </summary>
    public void Exit()
    {
      if (_usingLinux)
      {
        int res = Libc.writeInt64(_pipeFds[1], 1);
        if (-1 == res)
        {
          throw new InvalidOperationException("Cannot write to self-pipe.");
        }
      }
      else
      {
        _exitWaitHandle.Set();
      }
    }

    private readonly Renderer _renderer = new Renderer();

    public Renderer Renderer
    {
      get { return _renderer; }
    }

    private Control _mainControl;

    /// <summary>
    /// Returns the root control of the application.
    /// </summary>
    public Control RootControl
    {
      get { return _mainControl; }
    }


    private readonly FocusManager _focusManager;

    public FocusManager FocusManager
    {
      get { return _focusManager; }
    }

    private readonly EventManager _eventManager;

    public EventManager EventManager
    {
      get { return _eventManager; }
    }

    internal void SetCursorPosition(Point position)
    {
      if (!_usingLinux)
      {
        Win32.SetConsoleCursorPosition(_stdOutputHandle, new COORD((short) position._x, (short) position._y));
      }
      else
      {
        NCurses.move(position._y, position._x);
        NCurses.refresh();
      }
    }

    /// <summary>
    /// Состояние курсора консоли для избежания повторных вызовов Show и Hide.
    /// Консистентность этого свойства может быть нарушена, если пользоваться в приложении
    /// нативными функциями для работы с курсором напрямую.
    /// </summary>
    internal bool CursorIsVisible { get; private set; }

    /// <summary>
    /// Делает курсор консоли видимым и устанавливает значение CursorIsVisible в true.
    /// </summary>
    internal void ShowCursor()
    {
      if (!_usingLinux)
      {
        CONSOLE_CURSOR_INFO consoleCursorInfo = new CONSOLE_CURSOR_INFO
        {
          Size = 5,
          Visible = true
        };
        Win32.SetConsoleCursorInfo(_stdOutputHandle, ref consoleCursorInfo);
      }
      else
      {
        NCurses.curs_set(CursorVisibility.Visible);
      }

      CursorIsVisible = true;
    }

    /// <summary>
    /// Делает курсор консоли невидимым и устанавливает значение
    /// CursorIsVisible в false.
    /// </summary>
    internal void HideCursor()
    {
      if (!_usingLinux)
      {
        var consoleCursorInfo = new CONSOLE_CURSOR_INFO
        {
          Size = 5,
          Visible = false
        };
        Win32.SetConsoleCursorInfo(_stdOutputHandle, ref consoleCursorInfo);
      }
      else
      {
        NCurses.curs_set(CursorVisibility.Invisible);
      }

      CursorIsVisible = false;
    }

    /// <summary>
    /// Runs application using specified control as root control.
    /// Application will run until method <see cref="Exit"/> is called.
    /// </summary>
    /// <param name="control"></param>
    public void Run(Control control)
    {
      try
      {
        if (_usingLinux)
        {
          RunLinux(control);
        }
        else
        {
          RunWindows(control);
        }
      }
      finally
      {
        this._running = false;
        this._mainThreadId = null;
      }
    }

    public void Run(Control control, Size canvasSize, Rect rectToUse)
    {
      userCanvasSize = canvasSize;
      userRootElementRect = rectToUse;
      Run(control);
    }

    /// <summary>
    /// File descriptors for self-pipe.
    /// First descriptor is used to read from pipe, second - to write.
    /// </summary>
    private readonly int[] _pipeFds = new int[2];

    private IntPtr _termkeyHandle = IntPtr.Zero;

    private void RunLinux(Control control)
    {
      this._mainControl = control;

      if (userCanvasSize.IsEmpty)
      {
        // Create physical canvas with actual terminal size
        var ws = Libc.GetTerminalSize(_isDarwin);
        _canvas = new PhysicalCanvas(ws.ws_col, ws.ws_row);
      }
      else
      {
        _canvas = new PhysicalCanvas(userCanvasSize.Width, userCanvasSize.Height);
      }

      _renderer.Canvas = _canvas;
      _renderer.RootElementRect = userRootElementRect.IsEmpty
        ? new Rect(_canvas.Size)
        : userRootElementRect;
      _renderer.RootElement = _mainControl;
      //
      _mainControl.Invalidate();

      // Terminal initialization sequence

#if MONO
            // This is magic workaround to avoid messing up terminal after program finish
            // The bug is described at https://bugzilla.xamarin.com/show_bug.cgi?id=15118
            bool ignored = Console.KeyAvailable;
#endif

      // Because .NET Core runtime changes locale to something wrong on startup,
      // we have to change it to default system locale
      // See https://stackoverflow.com/a/6249265
      // And https://github.com/dotnet/coreclr/issues/1012
      Libc.setlocale(Libc.LC_ALL, "");

      // Save all terminal properties
      Termios termios;
      if (0 != Libc.tcgetattr(Libc.STDIN_FILENO, out termios))
      {
        throw new Exception(String.Format("Failed to call tcgetattr(). LastError is {0}", Marshal.GetLastWin32Error()));
      }

      var stdscr = NCurses.initscr();
      NCurses.cbreak();
      NCurses.noecho();
      NCurses.nonl();
      NCurses.intrflush(stdscr, false);
      NCurses.keypad(stdscr, true);
      NCurses.start_color();

      HideCursor();
      try
      {
        _renderer.UpdateLayout();
        _renderer.FinallyApplyChangesToCanvas();

        _termkeyHandle = LibTermKey.termkey_new(Libc.STDIN_FILENO, TermKeyFlag.TERMKEY_FLAG_SPACESYMBOL);

        // Setup the input mode
        Console.Write("\x1B[?1002h");
        var fd = new PollFd();
        fd.fd = Libc.STDIN_FILENO;
        fd.events = POLL_EVENTS.POLLIN;

        var fds = new PollFd[2];
        fds[0] = fd;
        fds[1] = new PollFd();
        var pipeResult = Libc.pipe(_pipeFds);
        if (pipeResult == -1)
        {
          throw new InvalidOperationException("Cannot create self-pipe.");
        }

        fds[1].fd = _pipeFds[0];
        fds[1].events = POLL_EVENTS.POLLIN;

        try
        {
#if MONO
                    // Catch SIGWINCH to handle terminal resizing
                    UnixSignal[] signals = new UnixSignal [] {
                        new UnixSignal (Signum.SIGWINCH)
                    };
                    Thread signal_thread = new Thread (delegate () {
                        while (true) {
                            // Wait for a signal to be delivered
                            int index = UnixSignal.WaitAny (signals, -1);
                            Signum signal = signals [index].Signum;
                            Libc.writeInt64 (pipeFds[1], 2);
                        }
                    }
                    );
                    signal_thread.IsBackground = false;
                    signal_thread.Start ();
#elif DOTNETCORE
          Libc.signal(28, arg => { Libc.writeInt64(_pipeFds[1], 2); });
#endif
          var key = new TermKeyKey();

          this._running = true;
          this._mainThreadId = Thread.CurrentThread.ManagedThreadId;

          var nextwait = -1;
          while (true)
          {
            var pollRes = Libc.poll(fds, 2, nextwait);
            if (pollRes == 0)
            {
              if (nextwait == -1)
              {
                throw new InvalidOperationException("Assertion failed.");
              }

              if (TermKeyResult.TERMKEY_RES_KEY == LibTermKey.termkey_getkey_force(_termkeyHandle, ref key))
              {
                ProcessLinuxInput(key);
              }
            }

            if (pollRes == -1)
            {
              var errorCode = Marshal.GetLastWin32Error();
              if (errorCode != Libc.EINTR)
              {
                throw new InvalidOperationException(string.Format("poll() returned with error code {0}", errorCode));
              }
            }

            if (fds[1].revents != POLL_EVENTS.NONE)
            {
              ulong u;
              Libc.readInt64(fds[1].fd, out u);
              if (u == 1)
              {
                // Exit from application
#if MONO
                                signal_thread.Abort ();
#endif
                break;
              }

              if (u == 2)
              {
                // Get new term size and process appropriate INPUT_RECORD event
                var inputRecord = new INPUT_RECORD();
                inputRecord.EventType = EventType.WINDOW_BUFFER_SIZE_EVENT;

                var ws = Libc.GetTerminalSize(_isDarwin);

                inputRecord.WindowBufferSizeEvent.dwSize.X = (short) ws.ws_col;
                inputRecord.WindowBufferSizeEvent.dwSize.Y = (short) ws.ws_row;
                ProcessInputEvent(inputRecord);
              }

              if (u == 3)
              {
                // It is signal from async actions invocation stuff
              }
            }

            if ((fds[0].revents & POLL_EVENTS.POLLIN) == POLL_EVENTS.POLLIN ||
                (fds[0].revents & POLL_EVENTS.POLLHUP) == POLL_EVENTS.POLLHUP ||
                (fds[0].revents & POLL_EVENTS.POLLERR) == POLL_EVENTS.POLLERR)
            {
              LibTermKey.termkey_advisereadable(_termkeyHandle);
            }

            var result = (LibTermKey.termkey_getkey(_termkeyHandle, ref key));
            while (result == TermKeyResult.TERMKEY_RES_KEY)
            {
              ProcessLinuxInput(key);
              result = (LibTermKey.termkey_getkey(_termkeyHandle, ref key));
            }

            if (result == TermKeyResult.TERMKEY_RES_AGAIN)
            {
              nextwait = LibTermKey.termkey_get_waittime(_termkeyHandle);
            }
            else
            {
              nextwait = -1;
            }

            while (true)
            {
              bool anyInvokeActions = IsAnyInvokeActions();
              bool anyRoutedEvent = !EventManager.IsQueueEmpty();
              bool anyLayoutToRevalidate = _renderer.AnyControlInvalidated;

              if (!anyInvokeActions && !anyRoutedEvent && !anyLayoutToRevalidate)
                break;

              EventManager.ProcessEvents();
              ProcessInvokeActions();
              _renderer.UpdateLayout();
            }

            _renderer.FinallyApplyChangesToCanvas();
          }
        }
        finally
        {
          LibTermKey.termkey_destroy(_termkeyHandle);
          Libc.close(_pipeFds[0]);
          Libc.close(_pipeFds[1]);
          Console.Write("\x1B[?1002l");
        }
      }
      finally
      {
        // Restore cursor visibility before exit
        ShowCursor();

        NCurses.endwin();

        // Restore all terminal parameters
        if (0 != Libc.tcsetattr(Libc.STDIN_FILENO, Libc.TCSANOW, ref termios))
        {
          throw new Exception(String.Format("Failed to call tcsetattr(). LastError is {0}", Marshal.GetLastWin32Error()));
        }
      }

      _renderer.RootElement = null;
    }

    private void ProcessLinuxInput(TermKeyKey key)
    {
      // If any special button has been _pressed (Tab, Enter, etc)
      // we should convert its code to INPUT_RECORD.KeyEvent
      // Because INPUT_RECORD.KeyEvent depends on Windows' scan codes,
      // we convert codes retrieved from LibTermKey to Windows virtual scan codes
      // In the future, this logic may be changed (for example, both Windows and Linux
      // raw codes can be converted into ConsoleFramework's own abstract enum)
      if (key.type == TermKeyType.TERMKEY_TYPE_KEYSYM)
      {
        var inputRecord = new INPUT_RECORD();
        inputRecord.EventType = EventType.KEY_EVENT;
        inputRecord.KeyEvent.bKeyDown = true;
        inputRecord.KeyEvent.wRepeatCount = 1;
        switch (key.code.sym)
        {
          case TermKeySym.TERMKEY_SYM_TAB:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Tab;
            break;

          case TermKeySym.TERMKEY_SYM_ENTER:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Return;
            break;

          // in gnome-terminal it is backspace by default
          // (see default compatibility settings in Profile's settings)
          case TermKeySym.TERMKEY_SYM_DEL:
          case TermKeySym.TERMKEY_SYM_BACKSPACE:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Back;
            break;

          case TermKeySym.TERMKEY_SYM_DELETE:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Delete;
            break;

          case TermKeySym.TERMKEY_SYM_HOME:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Home;
            break;

          case TermKeySym.TERMKEY_SYM_END:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.End;
            break;

          case TermKeySym.TERMKEY_SYM_PAGEUP:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Prior;
            break;
          case TermKeySym.TERMKEY_SYM_PAGEDOWN:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Next;
            break;

          case TermKeySym.TERMKEY_SYM_SPACE:
            inputRecord.KeyEvent.UnicodeChar = ' ';
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Space;
            break;

          case TermKeySym.TERMKEY_SYM_ESCAPE:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Escape;
            break;

          case TermKeySym.TERMKEY_SYM_INSERT:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Insert;
            break;

          case TermKeySym.TERMKEY_SYM_UP:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Up;
            break;

          case TermKeySym.TERMKEY_SYM_DOWN:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Down;
            break;

          case TermKeySym.TERMKEY_SYM_LEFT:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Left;
            break;

          case TermKeySym.TERMKEY_SYM_RIGHT:
            inputRecord.KeyEvent.wVirtualKeyCode = VirtualKeys.Right;
            break;

          default:
            throw new NotSupportedException("Not supported keyboard code detected: " + key.code.sym);
        }

        inputRecord.KeyEvent.dwControlKeyState = 0;
        if ((key.modifiers & 4) == 4)
        {
          inputRecord.KeyEvent.dwControlKeyState |= ControlKeyState.LEFT_CTRL_PRESSED;
        }

        if ((key.modifiers & 2) == 2)
        {
          inputRecord.KeyEvent.dwControlKeyState |= ControlKeyState.LEFT_ALT_PRESSED;
        }

        ProcessInputEvent(inputRecord);
      }
      else if (key.type == TermKeyType.TERMKEY_TYPE_UNICODE)
      {
        var data = new byte[7];
        data[0] = key.utf8_0;
        data[1] = key.utf8_1;
        data[2] = key.utf8_2;
        data[3] = key.utf8_3;
        data[4] = key.utf8_4;
        data[5] = key.utf8_5;
        data[6] = key.utf8_6;
        var d = System.Text.Encoding.UTF8.GetString(data);
        var unicodeCharacter = d[0];
        var inputRecord = new INPUT_RECORD();
        inputRecord.EventType = EventType.KEY_EVENT;
        inputRecord.KeyEvent.bKeyDown = true;
        inputRecord.KeyEvent.wRepeatCount = 1;
        inputRecord.KeyEvent.UnicodeChar = unicodeCharacter;
        inputRecord.KeyEvent.dwControlKeyState = 0;
        if (char.IsLetterOrDigit(unicodeCharacter))
        {
          if (char.IsDigit(unicodeCharacter))
          {
            inputRecord.KeyEvent.wVirtualKeyCode = (VirtualKeys) (unicodeCharacter - '0' + (int) VirtualKeys.N0);
          }
          else
          {
            var lowercased = char.ToLowerInvariant(unicodeCharacter);

            // Only english characters can be converted to VirtualKeys
            if (lowercased >= 'a' && lowercased <= 'z')
            {
              inputRecord.KeyEvent.wVirtualKeyCode =
                (VirtualKeys) (lowercased - 'a' + (int) VirtualKeys.A);
            }
          }
        }

        if ((key.modifiers & 4) == 4)
        {
          inputRecord.KeyEvent.dwControlKeyState |= ControlKeyState.LEFT_CTRL_PRESSED;
        }

        if ((key.modifiers & 2) == 2)
        {
          inputRecord.KeyEvent.dwControlKeyState |= ControlKeyState.LEFT_ALT_PRESSED;
        }

        // todo : remove hardcoded exit combo after testing
        if (unicodeCharacter == 'd' && key.modifiers == 4)
        {
          Exit();
        }

        ProcessInputEvent(inputRecord);
        //
      }
      else if (key.type == TermKeyType.TERMKEY_TYPE_MOUSE)
      {
        LibTermKey.termkey_interpret_mouse(_termkeyHandle, ref key, out var ev, out var button, out var line, out var col);

        var inputRecord = new INPUT_RECORD();
        inputRecord.EventType = EventType.MOUSE_EVENT;
        if (ev == TermKeyMouseEvent.TERMKEY_MOUSE_PRESS || ev == TermKeyMouseEvent.TERMKEY_MOUSE_RELEASE)
        {
          inputRecord.MouseEvent.dwEventFlags = MouseEventFlags.PRESSED_OR_RELEASED;
        }

        if (ev == TermKeyMouseEvent.TERMKEY_MOUSE_DRAG)
        {
          inputRecord.MouseEvent.dwEventFlags = MouseEventFlags.MOUSE_MOVED;
        }

        inputRecord.MouseEvent.dwMousePosition = new COORD((short) (col - 1), (short) (line - 1));
        if (ev == TermKeyMouseEvent.TERMKEY_MOUSE_RELEASE)
        {
          inputRecord.MouseEvent.dwButtonState = 0;
        }
        else if (ev == TermKeyMouseEvent.TERMKEY_MOUSE_DRAG || ev == TermKeyMouseEvent.TERMKEY_MOUSE_PRESS)
        {
          if (1 == button)
          {
            inputRecord.MouseEvent.dwButtonState = MOUSE_BUTTON_STATE.FROM_LEFT_1ST_BUTTON_PRESSED;
          }
          else if (2 == button)
          {
            inputRecord.MouseEvent.dwButtonState = MOUSE_BUTTON_STATE.FROM_LEFT_2ND_BUTTON_PRESSED;
          }
          else if (3 == button)
          {
            inputRecord.MouseEvent.dwButtonState = MOUSE_BUTTON_STATE.RIGHTMOST_BUTTON_PRESSED;
          }
        }

        ProcessInputEvent(inputRecord);
      }
    }

    private void RunWindows(Control control)
    {
      this._mainControl = control;

      _stdInputHandle = Win32.GetStdHandle(StdHandleType.STD_INPUT_HANDLE);
      _stdOutputHandle = Win32.GetStdHandle(StdHandleType.STD_OUTPUT_HANDLE);
      var handles = new[]
      {
        _exitWaitHandle.GetSafeWaitHandle().DangerousGetHandle(),
        _stdInputHandle,
        _invokeWaitHandle.GetSafeWaitHandle().DangerousGetHandle()
      };

      // Set console mode to enable mouse and window resizing events
      const uint ENABLE_WINDOW_INPUT = 0x0008;
      const uint ENABLE_MOUSE_INPUT = 0x0010;
      uint consoleMode;
      Win32.GetConsoleMode(_stdInputHandle, out consoleMode);
      Win32.SetConsoleMode(_stdInputHandle, consoleMode | ENABLE_MOUSE_INPUT | ENABLE_WINDOW_INPUT);

      // Get console screen buffer size
      CONSOLE_SCREEN_BUFFER_INFO screenBufferInfo;
      Win32.GetConsoleScreenBufferInfo(_stdOutputHandle, out screenBufferInfo);

      // Set Canvas size to current console window size (not to whole buffer size)
      _savedWindowRect = new Rect(new Point(Console.WindowLeft, Console.WindowTop), new Size(Console.WindowWidth, Console.WindowHeight));
      CanvasSize = new Size(_savedWindowRect.Width, _savedWindowRect.Height);

      _canvas = userCanvasSize.IsEmpty
        ? new PhysicalCanvas(screenBufferInfo.dwSize.X, screenBufferInfo.dwSize.Y, _stdOutputHandle)
        : new PhysicalCanvas(userCanvasSize.Width, userCanvasSize.Height, _stdOutputHandle);
      _renderer.Canvas = _canvas;

      // Fill the canvas by default
      _renderer.RootElementRect = userRootElementRect.IsEmpty
        ? new Rect(new Point(0, 0), _canvas.Size)
        : userRootElementRect;
      _renderer.RootElement = _mainControl;

      _mainControl.Invalidate();
      _renderer.UpdateLayout();
      _renderer.FinallyApplyChangesToCanvas();

      // Initially hide the console cursor
      HideCursor();

      this._running = true;
      this._mainThreadId = Thread.CurrentThread.ManagedThreadId;

      while (true)
      {
        // 100 ms instead of Win32.INFINITE to check console window Zoomed and Iconic
        // state periodically (because if user presses Maximize/Restore button
        // there are no input event generated).
        uint waitResult = Win32.WaitForMultipleObjects(3, handles, false, 100);
        if (waitResult == 0)
        {
          break;
        }

        if (waitResult == 1)
        {
          ProcessInput();
        }

        if (waitResult == 2)
        {
          // Do nothing special - because invokeActions will be invoked in loop anyway
        }

        // If we received WAIT_TIMEOUT - check window Zoomed and Iconic state
        // and correct buffer size and console window size
        if (waitResult == 0x00000102)
        {
          var consoleWindow = GetConsoleWindowHwnd();
          var isZoomed = Win32.IsZoomed(consoleWindow);
          var isIconic = Win32.IsIconic(consoleWindow);
          if (_maximized != isZoomed && !isIconic)
          {
            if (isZoomed)
              Maximize();
            else
              Restore();
          }

          if (!_maximized)
          {
            _savedWindowRect = new Rect(new Point(Console.WindowLeft, Console.WindowTop),
              new Size(Console.WindowWidth, Console.WindowHeight));
          }
        }

        // WAIT_FAILED
        if (waitResult == 0xFFFFFFFF)
        {
          throw new InvalidOperationException("Invalid wait result of WaitForMultipleObjects.");
        }

        while (true)
        {
          var anyInvokeActions = IsAnyInvokeActions();
          var anyRoutedEvent = !EventManager.IsQueueEmpty();
          var anyLayoutToRevalidate = _renderer.AnyControlInvalidated;

          if (!anyInvokeActions && !anyRoutedEvent && !anyLayoutToRevalidate)
          {
            break;
          }

          EventManager.ProcessEvents();
          ProcessInvokeActions();
          _renderer.UpdateLayout();
        }

        _renderer.FinallyApplyChangesToCanvas();
      }

      // Restore cursor visibility before exit
      ShowCursor();

      // Restore console mode before exit
      Win32.SetConsoleMode(_stdInputHandle, consoleMode);

      _renderer.RootElement = null;

      // todo : restore attributes of console output
    }

    private bool IsAnyInvokeActions()
    {
      lock (_actionsLocker)
      {
        return (_actionsToBeInvoked.Count != 0);
      }
    }

    private void ProcessInvokeActions()
    {
      for (;;)
      {
        ActionInfo top;
        lock (_actionsLocker)
        {
          if (_actionsToBeInvoked.Count != 0)
          {
            top = _actionsToBeInvoked[0];
            _actionsToBeInvoked.RemoveAt(0);
          }
          else
          {
            break;
          }
        }

        top.action.Invoke();
        if (top.waitHandle != null)
        {
          top.waitHandle.Set();
        }
      }
    }

    private void ProcessInput()
    {
      var buffer = new INPUT_RECORD[10];
      var bReaded = Win32.ReadConsoleInput(_stdInputHandle, buffer, (uint) buffer.Length, out var read);
      if (!bReaded)
      {
        throw new InvalidOperationException("ReadConsoleInput method failed.");
      }

      for (var i = 0; i < read; ++i)
      {
        ProcessInputEvent(buffer[i]);
      }
    }

    private void ProcessInputEvent(INPUT_RECORD inputRecord)
    {
      if (inputRecord.EventType == EventType.WINDOW_BUFFER_SIZE_EVENT)
      {
        if (_usingLinux)
        {
          // Reinitializing ncurses to deal with new dimensions
          // http://stackoverflow.com/questions/13707137/ncurses-resizing-glitch
          NCurses.endwin();
          // Needs to be called after an endwin() so ncurses will initialize
          // itself with the new terminal dimensions.
          NCurses.refresh();
          NCurses.clear();
        }

        COORD dwSize = inputRecord.WindowBufferSizeEvent.dwSize;

        // Invoke default handler if no custom handlers attached and
        // userCanvasSize and userRootElementRect are not defined
        if (TerminalSizeChanged == null
            && userCanvasSize.IsEmpty
            && userRootElementRect.IsEmpty)
        {
          OnTerminalSizeChangedDefault(this, new TerminalSizeChangedEventArgs(dwSize.X, dwSize.Y));
        }
        else if (TerminalSizeChanged != null)
        {
          TerminalSizeChanged.Invoke(this, new TerminalSizeChangedEventArgs(dwSize.X, dwSize.Y));
        }

        // Refresh whole display
        _renderer.FinallyApplyChangesToCanvas(true);

        return;
      }

      _eventManager.ParseInputEvent(inputRecord, _mainControl);
    }

    /// <summary>
    /// Checks if current thread is same thread from which Run() method
    /// was called.
    /// </summary>
    /// <returns></returns>
    public bool IsUiThread()
    {
      return Thread.CurrentThread.ManagedThreadId == this._mainThreadId;
    }

    /// <summary>
    /// Invokes action in UI thread synchronously.
    /// If run loop was not started yet, nothing will be done.
    /// </summary>
    /// <param name="action"></param>
    public void RunOnUiThread(Action action)
    {
      // If run loop is not started, do nothing
      if (!this._running)
      {
        return;
      }

      // If current thread is UI thread, invoke action directly
      if (IsUiThread())
      {
        action.Invoke();
        return;
      }

      using (EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset))
      {
        lock (_actionsLocker)
        {
          _actionsToBeInvoked.Add(new ActionInfo(action, waitHandle));
        }

        if (_usingLinux)
        {
          Libc.writeInt64(_pipeFds[1], 3);
        }
        else
        {
          _invokeWaitHandle.Set();
        }

        waitHandle.WaitOne();
      }
    }

    /// <summary>
    /// Invokes action in main loop thread asynchronously.
    /// If run loop was not started yet, nothing will be done.
    /// </summary>
    public void Post(Action action)
    {
      // If run loop is not started, nothing to do
      if (!this._running)
      {
        return;
      }

      lock (_actionsLocker)
      {
        _actionsToBeInvoked.Add(new ActionInfo(action, null));
      }

      if (!IsUiThread())
      {
        if (_usingLinux)
        {
          Libc.writeInt64(_pipeFds[1], 3);
        }
        else
        {
          _invokeWaitHandle.Set();
        }
      }
    }

    private readonly object _timersLock = new object();

    /// <summary>
    /// This structure is required to avoid active timer to be collected by GC
    /// before action execution.
    /// </summary>
    private readonly List<Timer> _activeTimers = new List<Timer>();

    /// <summary>
    /// Invokes action in main loop thread (UI thread) asynchronously and after delay.
    /// If run loop will not start to delayed time, nothing will be done.
    /// </summary>
    public void Post(Action action, TimeSpan delay)
    {
      lock (_timersLock)
      {
        var array = new Timer[1];
        var timer = new Timer(state =>
        {
          this.Post(action);
          lock (_timersLock)
          {
            _activeTimers.Remove(array[0]);
          }
        }, null, delay, TimeSpan.FromMilliseconds(-1));
        array[0] = timer;
        _activeTimers.Add(timer);
      }
    }

    /// <summary>
    /// Начинает захват мыши и маршрутизируемых событий
    /// указанным элементом управления. После этого контрол принимает все события от мыши
    /// в качестве источника события (вне зависимости от позиции курсора мыши), а все маршрутизируемые
    /// события передаются только в этот контрол и к его потомкам.
    /// Используется, например, при обработке клика на кнопке - после нажатия ввод захватывается, и
    /// события приходят только в кнопку. Когда пользователь отпускает кнопку мыши, захват прекращается.
    /// </summary>
    public void BeginCaptureInput(Control control)
    {
      _eventManager.BeginCaptureInput(control);
    }

    /// <summary>
    /// Завершает захват мыши и маршрутизируемых событий.
    /// </summary>
    public void EndCaptureInput(Control control)
    {
      _eventManager.EndCaptureInput(control);
    }

    private void Dispose(bool isDisposing)
    {
      if (isDisposing)
      {
        if (_exitWaitHandle != null)
        {
          _exitWaitHandle.Dispose();
        }
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    ~ConsoleApplication()
    {
      Dispose(false);
    }
  }
}
