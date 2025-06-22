using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WindowTransparency : MonoBehaviour
{
    [Header("Window Settings")]
    public bool makeTransparent = true;
    public bool clickThrough = false;
    public bool alwaysOnTop = true;

    // Windows API constants
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOPMOST = 0x00000008;

    // Windows API functions
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("Dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    private IntPtr windowHandle;
    private bool isTransparent = false;

    void Start()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        StartCoroutine(SetupTransparency());
#endif
    }

    private System.Collections.IEnumerator SetupTransparency()
    {
        // Wait a frame to ensure window is created
        yield return new WaitForEndOfFrame();

        windowHandle = GetActiveWindow();

        if (makeTransparent)
        {
            MakeWindowTransparent();
        }

        if (alwaysOnTop)
        {
            SetAlwaysOnTop();
        }
    }

    private void MakeWindowTransparent()
    {
        // Get current window style
        int style = GetWindowLong(windowHandle, GWL_EXSTYLE);

        // Add layered flag
        style |= WS_EX_LAYERED;

        // Add transparent flag if click-through is enabled
        if (clickThrough)
        {
            style |= WS_EX_TRANSPARENT;
        }

        // Apply new style
        SetWindowLong(windowHandle, GWL_EXSTYLE, style);

        // Extend frame into client area for true transparency
        MARGINS margins = new MARGINS
        {
            cxLeftWidth = -1,
            cxRightWidth = -1,
            cyTopHeight = -1,
            cyBottomHeight = -1
        };

        DwmExtendFrameIntoClientArea(windowHandle, ref margins);

        isTransparent = true;
    }

    private void SetAlwaysOnTop()
    {
        IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;

        SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    public void ToggleTransparency()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (isTransparent)
        {
            RemoveTransparency();
        }
        else
        {
            MakeWindowTransparent();
        }
#endif
    }

    private void RemoveTransparency()
    {
        int style = GetWindowLong(windowHandle, GWL_EXSTYLE);
        style &= ~WS_EX_LAYERED;
        style &= ~WS_EX_TRANSPARENT;
        SetWindowLong(windowHandle, GWL_EXSTYLE, style);
        isTransparent = false;
    }

    public void SetClickThrough(bool enabled)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        clickThrough = enabled;
        if (isTransparent)
        {
            MakeWindowTransparent(); // Reapply with new settings
        }
#endif
    }

    void Update()
    {
        // Optional: Toggle transparency with F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleTransparency();
        }

        // Optional: Toggle click-through with F2
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SetClickThrough(!clickThrough);
        }
    }
}