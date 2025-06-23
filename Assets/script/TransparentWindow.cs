///
/// Copyright (c) 2019 wakagomo
///
/// This source code is released under the MIT License.
/// http://opensource.org/licenses/mit-license.php
///

using UnityEngine;

using System;
using System.Runtime.InteropServices;
using System.Collections;

/// <summary>
/// Make the window transparent.
/// </summary>
public class TransparentWindow : MonoBehaviour
{


    #region WINDOWS API
    /// <summary>
    /// Returned by the GetThemeMargins function to define the margins of windows that have visual styles applied.
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/uxtheme/ns-uxtheme-_margins
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    /// <summary>
    /// Retrieves the window handle to the active window attached to the calling thread's message queue.
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getactivewindow
    [DllImport("User32.dll")]
    private static extern IntPtr GetActiveWindow();
    /// <summary>
    /// Changes an attribute of the specified window. The function also sets the 32-bit (long) value at the specified offset into the extra window memory.
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-setwindowlonga
    [DllImport("User32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    /// <summary>
    /// Changes the size, position, and Z order of a child, pop-up, or top-level window. These windows are ordered according to their appearance on the screen. The topmost window receives the highest rank and is the first window in the Z order.
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-setwindowpos
    [DllImport("User32.dll")]
    private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    /// <summary>
    /// Extends the window frame into the client area.
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/dwmapi/nf-dwmapi-dwmextendframeintoclientarea
    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
    #endregion

    /// <summary>
    /// Should operation be transparene?
    /// </summary>
    private bool isClickThrough = false;

    /// <summary>
    /// Is the mouse pointer on an opaque pixel?
    /// </summary>
    private bool isOnOpaquePixel = true;

    /// <summary>
    /// The cut off threshold of alpha value.
    /// </summary>
    private float opaqueThreshold = 0.1f;

    /// <summary>
    /// An instance of current camera.
    /// </summary>
    private Camera currentCamera;

    /// <summary>
    /// 1x1 texture
    /// </summary>
    private Texture2D colorPickerTexture = null;

    /// <summary>
    /// Window handle
    /// </summary>
    private IntPtr windowHandle;

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
    private void Awake()
    {
        windowHandle = GetActiveWindow();

        { // SetWindowLong
            const int GWL_STYLE = -16;
            const uint WS_POPUP = 0x80000000;

            SetWindowLong(windowHandle, GWL_STYLE, WS_POPUP);
        }

        { // Set extended window style
            SetClickThrough(true);
        }

        { // SetWindowPos
            IntPtr HWND_TOPMOST = new IntPtr(-1);
            const uint SWP_NOSIZE = 0x0001;
            const uint SWP_NOMOVE = 0x0002;
            const uint SWP_NOACTIVE = 0x0010;
            const uint SWP_SHOWWINDOW = 0x0040;

            SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVE | SWP_SHOWWINDOW);
        }

        { // DwmExtendFrameIntoClientArea
            MARGINS margins = new MARGINS()
            {
                cxLeftWidth = -1
            };

            DwmExtendFrameIntoClientArea(windowHandle, ref margins);
        }
    }
#endif // !UNITY_EDITOR && UNITY_STANDALONE_WIN

    private void SetClickThrough(bool through)
    {
        const int GWL_EXSTYLE = -20;
        const uint WS_EX_LAYERD = 0x080000;
        const uint WS_EX_TRANSPARENT = 0x00000020;
        const uint WS_EX_LEFT = 0x00000000;

        if (through)
        {
            SetWindowLong(windowHandle, GWL_EXSTYLE, WS_EX_LAYERD | WS_EX_TRANSPARENT);
        }
        else
        {
            SetWindowLong(windowHandle, GWL_EXSTYLE, WS_EX_LEFT);
        }
    }

    void Start()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;

            if (!currentCamera)
            {
                currentCamera = FindFirstObjectByType<Camera>();
            }
        }

        colorPickerTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);

        StartCoroutine(PickColorCoroutine());
    }

    void Update()
    {
        UpdateClickThrough();
    }

    void UpdateClickThrough()
    {
        if (isClickThrough)
        {
            if (isOnOpaquePixel)
            {
                SetClickThrough(false);
                isClickThrough = false;
            }
        }
        else
        {
            if (!isOnOpaquePixel)
            {
                SetClickThrough(true);
                isClickThrough = true;
            }
        }
    }

    private IEnumerator PickColorCoroutine()
    {
        while (Application.isPlaying)
        {
            yield return new WaitForEndOfFrame();
            ObservePixelUnderCursor(currentCamera);
        }
        yield return null;
    }

    void ObservePixelUnderCursor(Camera cam)
    {
        if (!cam) return;

        Vector2 mousePos = Input.mousePosition;
        Rect camRect = cam.pixelRect;

        if (camRect.Contains(mousePos))
        {
            try
            {
                colorPickerTexture.ReadPixels(new Rect(mousePos, Vector2.one), 0, 0);
                Color color = colorPickerTexture.GetPixel(0, 0);

                isOnOpaquePixel = (color.a >= opaqueThreshold);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                isOnOpaquePixel = false;
            }
        }
        else
        {
            isOnOpaquePixel = false;
        }
    }
}