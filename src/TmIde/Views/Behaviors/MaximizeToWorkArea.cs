using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TmIde.Views.Behaviors;

/// <summary>
/// Hace que una ventana con chrome propio (WindowStyle=None + WindowChrome), al
/// maximizarse, ocupe el área útil del monitor y no la pantalla entera: sin esto
/// Windows la dibuja bajo la barra de tareas, con 7 px de desborde por cada lado.
/// Responde a WM_GETMINMAXINFO con la posición y el tamaño del área de trabajo.
/// </summary>
public static class MaximizeToWorkArea
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const int MonitorDefaultToNearest = 2;

    public static void Attach(Window window)
    {
        window.SourceInitialized += (_, _) =>
            HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook(FitToWorkArea);
    }

    private static IntPtr FitToWorkArea(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmGetMinMaxInfo)
        {
            return IntPtr.Zero;
        }

        var monitor = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(hwnd, MonitorDefaultToNearest), ref monitor))
        {
            return IntPtr.Zero;
        }

        // La posición se da relativa al monitor; el resto de límites los completa WPF.
        var limits = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        limits.MaxPosition = new Point32(monitor.Work.Left - monitor.Monitor.Left, monitor.Work.Top - monitor.Monitor.Top);
        limits.MaxSize = new Point32(monitor.Work.Right - monitor.Work.Left, monitor.Work.Bottom - monitor.Work.Top);
        Marshal.StructureToPtr(limits, lParam, fDeleteOld: true);

        return IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point32(int x, int y)
    {
        public int X = x;
        public int Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect32
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point32 Reserved;
        public Point32 MaxSize;
        public Point32 MaxPosition;
        public Point32 MinTrackSize;
        public Point32 MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect32 Monitor;
        public Rect32 Work;
        public int Flags;
    }
}
