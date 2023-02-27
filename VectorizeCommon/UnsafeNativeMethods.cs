﻿using System;
using System.Runtime.InteropServices;

namespace VectorizeCommon
{
  internal class Import
  {
    private Import() { }
    public const string lib = "VectorizeLib";
  }

  internal static class UnsafeNativeMethods
  {
    #region PotraceParameters helpers

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr potrace_param_New();

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_param_Delete(IntPtr pParam);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_param_SetDefault(IntPtr pParam);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern double potrace_param_GetSetDouble(IntPtr pParam, int which, [MarshalAs(UnmanagedType.U1)] bool set, double setValue);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int potrace_param_GetSetInt(IntPtr pParam, int which, [MarshalAs(UnmanagedType.U1)] bool set, int setValue);

    #endregion // PotraceParameters helpers

    #region PotraceBitmap helpers

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr potrace_bitmap_New(int width, int height);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_bitmap_Delete(IntPtr pBitmap);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_bitmap_Clear(IntPtr pBitmap);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr potrace_bitmap_Duplicate(IntPtr pBitmap);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_bitmap_Invert(IntPtr pBitmap);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_bitmap_Flip(IntPtr pBitmap);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool potrace_bitmap_GetPixel(IntPtr pBitmap, int x, int y);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_bitmap_SetPixel(IntPtr pBitmap, int x, int y);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_bitmap_ClearPixel(IntPtr pBitmap, int x, int y);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_bitmap_InvertPixel(IntPtr pBitmap, int x, int y);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_bitmap_PutPixel(IntPtr pBitmap, int x, int y, [MarshalAs(UnmanagedType.U1)] bool set);

    #endregion // PotraceBitmap helpers

    #region Potrace helpers

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr potrace_state_Trace(IntPtr pBitmap, IntPtr pParam);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void potrace_state_Delete(IntPtr pState);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr potrace_state_PathList(IntPtr pState);

    #endregion // Potrace helpers

    #region PotracePath helpers

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int potrace_path_Area(IntPtr pPath);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool potrace_path_Sign(IntPtr pPath);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr potrace_path_Next(IntPtr pPath);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int potrace_path_SegmentCount(IntPtr pPath);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int potrace_path_SegmentTag(IntPtr pPath, int index);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool potrace_path_SegmentCornerPoints(IntPtr pPath, int index, int bufferSize, double[] buffer);

    [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool potrace_path_SegmentCurvePoints(IntPtr pPath, int index, int bufferSize, double[] buffer);

    #endregion // PotracePath helpers

  }
}
