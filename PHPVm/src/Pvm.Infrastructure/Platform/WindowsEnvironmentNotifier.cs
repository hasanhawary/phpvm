using System.Runtime.InteropServices;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Platform;

/// <summary>
/// Implements environment notification on Windows by broadcasting WM_SETTINGCHANGE via SendMessageTimeout.
/// </summary>
public sealed class WindowsEnvironmentNotifier : IEnvironmentNotifier
{
    private const int HWND_BROADCAST = 0xffff;
    private const int WM_SETTINGCHANGE = 0x001A;
    private const int SMTO_ABORTIFHUNG = 0x0002;
    private const int TIMEOUT_MS = 1000;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint SendMessageTimeout(
        nint hWnd,
        int Msg,
        nint wParam,
        string lParam,
        int fuFlags,
        int uTimeout,
        out nint lpdwResult);

    public Result NotifyEnvironmentChanged()
    {
        try
        {
            var result = SendMessageTimeout(
                (nint)HWND_BROADCAST,
                WM_SETTINGCHANGE,
                nint.Zero,
                "Environment",
                SMTO_ABORTIFHUNG,
                TIMEOUT_MS,
                out _);

            if (result == nint.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                // 0 or ERROR_TIMEOUT (1460) may occur if a window is hung, but the broadcast generally still reaches Explorer
                if (error != 0 && error != 1460)
                {
                    return Result.Fail($"SendMessageTimeout failed with Win32 error code {error}.");
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to broadcast environment change: {ex.Message}");
        }
    }
}
