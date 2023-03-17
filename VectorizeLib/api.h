#pragma once

#if defined(_WINDOWS)
#ifdef VECTORIZELIB_EXPORTS
#define VECTORIZELIB_FUNCTION extern "C" __declspec(dllexport)
#else
#define VECTORIZELIB_FUNCTION extern "C" __declspec(dllimport)
#endif
#endif

#if defined(__APPLE__)
#ifdef VECTORIZELIB_EXPORTS
#define VECTORIZELIB_FUNCTION extern "C" __attribute__ ((visibility ("default")))
#endif
#endif
#pragma once

/////////////////////////////////////////////////
// potrace_param_t helpers

VECTORIZELIB_FUNCTION potrace_param_t* potrace_param_New();
VECTORIZELIB_FUNCTION void potrace_param_Delete(potrace_param_t* pParam);
VECTORIZELIB_FUNCTION void potrace_param_SetDefault(potrace_param_t* pParam);
VECTORIZELIB_FUNCTION double potrace_param_GetSetDouble(potrace_param_t* pParam, int which, bool set, double setValue);
VECTORIZELIB_FUNCTION int potrace_param_GetSetInt(potrace_param_t* pParam, int which, bool set, int setValue);


/////////////////////////////////////////////////
// potrace_bitmap_t helpers

VECTORIZELIB_FUNCTION potrace_bitmap_t* potrace_bitmap_New(int width, int height);
VECTORIZELIB_FUNCTION void potrace_bitmap_Delete(potrace_bitmap_t* pBitmap);
VECTORIZELIB_FUNCTION void potrace_bitmap_Clear(potrace_bitmap_t* pBitmap);
VECTORIZELIB_FUNCTION potrace_bitmap_t* potrace_bitmap_Duplicate(potrace_bitmap_t* pBitmap);
VECTORIZELIB_FUNCTION void potrace_bitmap_Invert(potrace_bitmap_t* pBitmap);
VECTORIZELIB_FUNCTION void potrace_bitmap_Flip(potrace_bitmap_t* pBitmap);
VECTORIZELIB_FUNCTION bool potrace_bitmap_GetPixel(potrace_bitmap_t* pBitmap, int x, int y);
VECTORIZELIB_FUNCTION void potrace_bitmap_SetPixel(potrace_bitmap_t* pBitmap, int x, int y);
VECTORIZELIB_FUNCTION void potrace_bitmap_ClearPixel(potrace_bitmap_t* pBitmap, int x, int y);
VECTORIZELIB_FUNCTION void potrace_bitmap_InvertPixel(potrace_bitmap_t* pBitmap, int x, int y);
VECTORIZELIB_FUNCTION void potrace_bitmap_PutPixel(potrace_bitmap_t* pBitmap, int x, int y, bool set);
VECTORIZELIB_FUNCTION void potrace_bitmap_PutPixels(potrace_bitmap_t* pBitmap, int count, /*ARRAY*/const bool* pValues);

/////////////////////////////////////////////////
// potrace_state_t helpers

VECTORIZELIB_FUNCTION potrace_state_t* potrace_state_Trace(potrace_bitmap_t* pBitmap, potrace_param_t* pParam);
VECTORIZELIB_FUNCTION void potrace_state_Delete(potrace_state_t* pState);
VECTORIZELIB_FUNCTION potrace_path_t* potrace_state_PathList(potrace_state_t* pState);

/////////////////////////////////////////////////
// potrace_path_t helpers

VECTORIZELIB_FUNCTION int potrace_path_Area(potrace_path_t* pPath);
VECTORIZELIB_FUNCTION bool potrace_path_Sign(potrace_path_t* pPath);
VECTORIZELIB_FUNCTION potrace_path_t* potrace_path_Next(potrace_path_t* pPath);
VECTORIZELIB_FUNCTION int potrace_path_SegmentCount(potrace_path_t* pPath);
VECTORIZELIB_FUNCTION int potrace_path_SegmentTag(potrace_path_t* pPath, int index);
VECTORIZELIB_FUNCTION bool potrace_path_SegmentCornerPoints(potrace_path_t* pPath, int index, int bufferSize, /*ARRAY*/double* pBuffer);
VECTORIZELIB_FUNCTION bool potrace_path_SegmentCurvePoints(potrace_path_t* pPath, int index, int bufferSize, /*ARRAY*/double* pBuffer);
