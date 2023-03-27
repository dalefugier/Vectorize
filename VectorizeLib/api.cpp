#include "pch.h"
#include "potracelib.h"
#include "bitmap.h"
#include "auxiliary.h"
#include "api.h"

#define UNSET_POSITIVE_VALUE 1.23432101234321e+308
#define UNSET_VALUE -UNSET_POSITIVE_VALUE
#define POINT_STRIDE 2 // A point has two doubles
#define CURVE_STRIDE 4 // A curve segment has four points
#define CLAMP(V,L,H) ( (V) < (L) ? (L) : ( (V) > (H) ? (H) : (V) ) )

/////////////////////////////////////////////////
// potrace_param_t helpers

VECTORIZELIB_FUNCTION potrace_param_t* potrace_param_New()
{
  return potrace_param_default();
}

VECTORIZELIB_FUNCTION void potrace_param_Delete(potrace_param_t* pParam)
{
  if (pParam)
    potrace_param_free(pParam);
}

VECTORIZELIB_FUNCTION void potrace_param_SetDefault(potrace_param_t* pParam)
{
  if (pParam)
    potrace_param_setdefault(pParam);
}

VECTORIZELIB_FUNCTION double potrace_param_GetSetDouble(potrace_param_t* pParam, int which, bool set, double setValue)
{
  const int idx_alphamax = 0;
  const int idx_opttolerance = 1;
  double rc = setValue;
  if (pParam)
  {
    if (set)
    {
      switch (which)
      {
      case idx_alphamax:
        // The useful range is from 0.0 (polygon) to 1.3334 (no corners).
        // But 1.3334 looks bad in UI. So round up.
        pParam->alphamax = CLAMP(setValue, 0.0, 1.34);
        break;
      case idx_opttolerance:
        pParam->opttolerance = CLAMP(setValue, 0.0, 1.0);
        break;
      }
    }
    else
    {
      switch (which)
      {
      case idx_alphamax:
        rc = pParam->alphamax;
        break;
      case idx_opttolerance:
        rc = pParam->opttolerance;
        break;
      }
    }
  }
  return rc;
}

VECTORIZELIB_FUNCTION int potrace_param_GetSetInt(potrace_param_t* pParam, int which, bool set, int setValue)
{
  const int idx_turdsize = 0;
  const int idx_turnpolicy = 1;
  const int idx_opticurve = 2;
  int rc = setValue;
  if (pParam)
  {
    if (set)
    {
      switch (which)
      {
      case idx_turdsize:
        pParam->turdsize = CLAMP(setValue, 0, 100);
        break;
      case idx_turnpolicy:
        pParam->turnpolicy = CLAMP(setValue, 0, 6);
        break;
      case idx_opticurve:
        pParam->opticurve = CLAMP(setValue, 0, 1);
        break;
      }
    }
    else
    {
      switch (which)
      {
      case idx_turdsize:
        rc = pParam->turdsize;
        break;
      case idx_turnpolicy:
        rc = pParam->turnpolicy;
        break;
      case idx_opticurve:
        rc = pParam->opticurve;
        break;
      }
    }
  }
  return rc;
}

/////////////////////////////////////////////////
// potrace_bitmap_t helpers

VECTORIZELIB_FUNCTION potrace_bitmap_t* potrace_bitmap_New(int width, int height)
{
  potrace_bitmap_t* rc = nullptr;
  if (width > 0 && height > 0)
    rc = bm_new(width, height);
  return rc;
}

VECTORIZELIB_FUNCTION potrace_bitmap_t* potrace_bitmap_New2(int width, int height, int count, /*ARRAY*/const bool* pValues)
{
  potrace_bitmap_t* rc = nullptr;
  if (width > 0 && height > 0 && width * height == count && pValues)
  {
    rc = bm_new(width, height);
    if (rc)
    {
      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          if (pValues[x + width * y])
            BM_SET(rc, x, y);
        }
      }
    }
  }
  return rc;
}

VECTORIZELIB_FUNCTION void potrace_bitmap_Delete(potrace_bitmap_t* pBitmap)
{
  if (pBitmap)
    bm_free(pBitmap);
}

VECTORIZELIB_FUNCTION void potrace_bitmap_Clear(potrace_bitmap_t* pBitmap)
{
  if (pBitmap)
    bm_clear(pBitmap, 0);
}

VECTORIZELIB_FUNCTION potrace_bitmap_t* potrace_bitmap_Duplicate(potrace_bitmap_t* pBitmap)
{
  potrace_bitmap_t* rc = nullptr;
  if (pBitmap)
    rc = bm_dup(pBitmap);
  return rc;
}

VECTORIZELIB_FUNCTION void potrace_bitmap_Invert(potrace_bitmap_t* pBitmap)
{
  if (pBitmap)
    bm_invert(pBitmap);
}

VECTORIZELIB_FUNCTION void potrace_bitmap_Flip(potrace_bitmap_t* pBitmap)
{
  if (pBitmap)
    bm_flip(pBitmap);
}

VECTORIZELIB_FUNCTION bool potrace_bitmap_GetPixel(potrace_bitmap_t* pBitmap, int x, int y)
{
  int rc = 0;
  if (pBitmap)
    rc = BM_GET(pBitmap, x, y);
  return rc ? true : false;
}

VECTORIZELIB_FUNCTION void potrace_bitmap_SetPixel(potrace_bitmap_t* pBitmap, int x, int y)
{
  if (pBitmap)
    BM_SET(pBitmap, x, y);
}

VECTORIZELIB_FUNCTION void potrace_bitmap_ClearPixel(potrace_bitmap_t* pBitmap, int x, int y)
{
  if (pBitmap)
    BM_CLR(pBitmap, x, y);
}

VECTORIZELIB_FUNCTION void potrace_bitmap_InvertPixel(potrace_bitmap_t* pBitmap, int x, int y)
{
  if (pBitmap)
    BM_INV(pBitmap, x, y);
}

VECTORIZELIB_FUNCTION void potrace_bitmap_PutPixel(potrace_bitmap_t* pBitmap, int x, int y, bool set)
{
  if (pBitmap)
    BM_PUT(pBitmap, x, y, set ? 1 : 0);
}

/////////////////////////////////////////////////
// potrace_state_t helpers

VECTORIZELIB_FUNCTION potrace_state_t* potrace_state_New(potrace_bitmap_t* pBitmap, potrace_param_t* pParam)
{
  potrace_state_t* rc = nullptr;
  if (pBitmap && pParam)
  {
    potrace_state_t* st = potrace_trace(pParam, pBitmap);
    if (st)
    {
      if (st->status != POTRACE_STATUS_OK)
      {
        potrace_state_free(st);
        st = nullptr;
      }
      else
      {
        rc = st;
      }
    }
  }
  return rc;
}

VECTORIZELIB_FUNCTION void potrace_state_Delete(potrace_state_t* pState)
{
  if (pState)
    potrace_state_free(pState);
}

VECTORIZELIB_FUNCTION potrace_path_t* potrace_state_PathList(const potrace_state_t* pState)
{
  potrace_path_t* rc = nullptr;
  if (pState)
    rc = pState->plist;
  return rc;
}

/////////////////////////////////////////////////
// potrace_path_t helpers

VECTORIZELIB_FUNCTION potrace_path_t* potrace_path_Next(const potrace_path_t* pPath)
{
  potrace_path_t* rc = nullptr;
  if (pPath && pPath->next)
    rc = pPath->next;
  return rc;
}

VECTORIZELIB_FUNCTION int potrace_path_SegmentCount(const potrace_path_t* pPath)
{
  int rc = 0;
  if (pPath)
    rc = pPath->curve.n;
  return rc;
}

VECTORIZELIB_FUNCTION bool potrace_path_SegmentPoints(const potrace_path_t* pPath, int bufferSize, /*ARRAY*/double* pBuffer)
{
  bool rc = false;
  if (
    pPath
    && bufferSize >= 0
    && bufferSize == pPath->curve.n * POINT_STRIDE * CURVE_STRIDE
    && pBuffer
    )
  {
    const int n = pPath->curve.n;
    int i = 0;
    for (int index = 0; index < pPath->curve.n; index++)
    {
      potrace_dpoint_t* c = pPath->curve.c[index];
      potrace_dpoint_t* c1 = pPath->curve.c[mod(index - 1, n)];
      pBuffer[i++] = c1[2].x;
      pBuffer[i++] = c1[2].y;
      if (POTRACE_CORNER == pPath->curve.tag[index])
      {
        pBuffer[i++] = UNSET_VALUE;
        pBuffer[i++] = UNSET_VALUE;
      }
      else
      {
        pBuffer[i++] = c[0].x;
        pBuffer[i++] = c[0].y;
      }
      pBuffer[i++] = c[1].x;
      pBuffer[i++] = c[1].y;
      pBuffer[i++] = c[2].x;
      pBuffer[i++] = c[2].y;
    }
    rc = true;
  }
  return rc;
}

VECTORIZELIB_FUNCTION int potrace_path_SegmentTag(const potrace_path_t* pPath, int index)
{
  int rc = 0;
  if (pPath && index >= 0 && index < pPath->curve.n)
    rc = pPath->curve.tag[index];
  return rc;
}

VECTORIZELIB_FUNCTION bool potrace_path_SegmentCornerPoints(const potrace_path_t* pPath, int index, int bufferSize, /*ARRAY*/double* pBuffer)
{
  bool rc = false;
  if (
    pPath
    && index >= 0
    && index < pPath->curve.n
    && POTRACE_CORNER == pPath->curve.tag[index]
    && bufferSize
    && pBuffer
    )
  {
    int i = 0;
    const int n = pPath->curve.n;
    potrace_dpoint_t* c = pPath->curve.c[index];
    potrace_dpoint_t* c1 = pPath->curve.c[mod(index - 1, n)];
    pBuffer[i++] = c1[2].x;
    pBuffer[i++] = c1[2].y;
    // c[0] is unused
    pBuffer[i++] = c[1].x;
    pBuffer[i++] = c[1].y;
    pBuffer[i++] = c[2].x;
    pBuffer[i++] = c[2].y;
    rc = true;
  }
  return rc;
}

VECTORIZELIB_FUNCTION bool potrace_path_SegmentCurvePoints(const potrace_path_t* pPath, int index, int bufferSize, /*ARRAY*/double* pBuffer)
{
  bool rc = false;
  if (
    pPath
    && index >= 0
    && index < pPath->curve.n
    && POTRACE_CURVETO == pPath->curve.tag[index]
    && bufferSize
    && pBuffer
    )
  {
    int i = 0;
    const int n = pPath->curve.n;
    potrace_dpoint_t* c = pPath->curve.c[index];
    potrace_dpoint_t* c1 = pPath->curve.c[mod(index - 1, n)];
    pBuffer[i++] = c1[2].x;
    pBuffer[i++] = c1[2].y;
    pBuffer[i++] = c[0].x;
    pBuffer[i++] = c[0].y;
    pBuffer[i++] = c[1].x;
    pBuffer[i++] = c[1].y;
    pBuffer[i++] = c[2].x;
    pBuffer[i++] = c[2].y;
    rc = true;
  }
  return rc;
}

VECTORIZELIB_FUNCTION int potrace_path_Area(potrace_path_t* pPath)
{
  int rc = 0;
  if (pPath)
    rc = pPath->area;
  return rc;
}

VECTORIZELIB_FUNCTION bool potrace_path_Sign(potrace_path_t* pPath)
{
  bool rc = true;
  if (pPath)
    rc = (pPath->sign == '+') ? true : false;
  return rc;
}