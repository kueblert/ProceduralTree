//
// Author: Ryan Seghers
//
// Copyright (C) 2013-2014 Ryan Seghers
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the irrevocable, perpetual, worldwide, and royalty-free
// rights to use, copy, modify, merge, publish, distribute, sublicense, 
// display, perform, create derivative works from and/or sell copies of 
// the Software, both in source and object code form, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using UnityEngine;

/// <summary>
/// Cubic spline interpolation.
/// Call Fit (or use the corrector constructor) to compute spline coefficients, then Eval to evaluate the spline at other X coordinates.
/// </summary>
/// <remarks>
/// <para>
/// This is implemented based on the wikipedia article:
/// http://en.wikipedia.org/wiki/Spline_interpolation
/// I'm not sure I have the right to include a copy of the article so the equation numbers referenced in 
/// comments will end up being wrong at some point.
/// </para>
/// <para>
/// This is not optimized, and is not MT safe.
/// This can extrapolate off the ends of the splines.
/// You must provide points in X sort order.
/// </para>
/// </remarks>
public class CubicSpline
    {
        #region Fields

        // N-1 spline coefficients for N points
        //private float[] a;
        //private float[] b;
    private Vector4[] aVector;
    private Vector4[] bVector;

        // Save the original x and y for Eval
        private float[] xOrig;
        //private float[] yOrig;
        private Vector4[] yOrigVector;

    #endregion

    #region Ctor

    /// <summary>
    /// Default ctor.
    /// </summary>
    public CubicSpline()
        {
        }

    #endregion

    #region Private Methods

    /// <summary>
    /// Throws if Fit has not been called.
    /// </summary>
    private void CheckAlreadyFitted()
        {
            //if (a == null) throw new Exception("Fit must be called before you can evaluate.");
            if(aVector == null) throw new Exception("Fit must be called before you can evaluate.");
    }

        private int _lastIndex = 0;

        /// <summary>
        /// Find where in xOrig the specified x falls, by simultaneous traverse.
        /// This allows xs to be less than x[0] and/or greater than x[n-1]. So allows extrapolation.
        /// This keeps state, so requires that x be sorted and xs called in ascending order, and is not multi-thread safe.
        /// </summary>
        private int GetNextXIndex(float x)
        {
            if (x < xOrig[_lastIndex])
            {
                throw new ArgumentException("The X values to evaluate must be sorted.");
            }

            while ((_lastIndex < xOrig.Length - 2) && (x > xOrig[_lastIndex + 1]))
            {
                _lastIndex++;
            }

            return _lastIndex;
        }

    /// <summary>
    /// Evaluate the specified x value using the specified spline.
    /// </summary>
    /// <param name="x">The x value.</param>
    /// <param name="j">Which spline to use.</param>
    /// <param name="debug">Turn on console output. Default is false.</param>
    /// <returns>The y value.</returns>
    private Vector4 EvalSpline(float x, int j, out Vector4 dy, out Vector4 ddy, bool debug = false)
    {
        float dx = xOrig[j + 1] - xOrig[j];
        float t = (x - xOrig[j]) / dx;
        Vector4 y = (1 - t) * yOrigVector[j] + t * yOrigVector[j + 1] + t * (1 - t) * (aVector[j] * (1 - t) + bVector[j] * t); // equation 9
        // here we need the first and second derivative as well. They can easily be derived from equation 9.
        dy = -yOrigVector[j] + yOrigVector[j + 1] + aVector[j] + t * (-4 * aVector[j] + 2 * bVector[j]) + 3 * t * t * (aVector[j] - bVector[j]);
        ddy = -4 * aVector[j] + 2 * bVector[j] + 6 * t * (aVector[j] - bVector[j]);
        //if (debug) Console.WriteLine("xs = {0}, j = {1}, t = {2}", x, j, t);
        return y;
    }

    #endregion

    #region Fit*

    /// <summary>
    /// Fit x,y and then eval at points xs and return the corresponding y's.
    /// This does the "natural spline" style for ends.
    /// This can extrapolate off the ends of the splines.
    /// You must provide points in X sort order.
    /// </summary>
    /// <param name="x">Input. X coordinates to fit.</param>
    /// <param name="y">Input. Y coordinates to fit.</param>
    /// <param name="xs">Input. X coordinates to evaluate the fitted curve at.</param>
    /// <param name="startSlope">Optional slope constraint for the first point. Single.NaN means no constraint.</param>
    /// <param name="endSlope">Optional slope constraint for the final point. Single.NaN means no constraint.</param>
    /// <param name="debug">Turn on console output. Default is false.</param>
    /// <returns>The computed y values for each xs.</returns>
    public Vector4[] FitAndEval(float[] x, Vector4[] y, float[] xs, out Vector4[] dy, out Vector4[] ddy, Vector4 startSlope, Vector4 endSlope, bool debug = false)
    {
        Fit(x, y, startSlope, endSlope, debug);
        return Eval(xs, out dy, out ddy, debug);
    }

    /// <summary>
    /// Compute spline coefficients for the specified x,y points.
    /// This does the "natural spline" style for ends.
    /// This can extrapolate off the ends of the splines.
    /// You must provide points in X sort order.
    /// </summary>
    /// <param name="x">Input. X coordinates to fit.</param>
    /// <param name="y">Input. Y coordinates to fit.</param>
    /// <param name="startSlope">Optional slope constraint for the first point. Single.NaN means no constraint.</param>
    /// <param name="endSlope">Optional slope constraint for the final point. Single.NaN means no constraint.</param>
    /// <param name="debug">Turn on console output. Default is false.</param> 
    public void Fit(float[] x, Vector4[] y, Vector4 startSlope, Vector4 endSlope, bool debug = false)
    {
        if (float.IsInfinity(startSlope.x) || float.IsInfinity(startSlope.y) || float.IsInfinity(startSlope.z) || float.IsInfinity(endSlope.x) || float.IsInfinity(endSlope.y) || float.IsInfinity(endSlope.z))
        {
            throw new Exception("startSlope and endSlope cannot be null.");
        }

        // Save x and y for eval
        this.xOrig = x;
        this.yOrigVector = y;

        int n = x.Length;
        Vector4[] r = new Vector4[n]; // the right hand side numbers: wikipedia page overloads b

        TriDiagonalMatrixF m = new TriDiagonalMatrixF(n);
        float dx1, dx2;
        Vector4 dy1, dy2;

        // First row is different (equation 16 from the article)
        if (startSlope.x == 0 && startSlope.y == 0 && startSlope.z == 0)
        {
            dx1 = x[1] - x[0];
            m.C[0] = 1.0f / dx1;
            m.B[0] = 2.0f * m.C[0];
            r[0] = 3 * (y[1] - y[0]) / (dx1 * dx1);
        }
        else
        {
            m.B[0] = 1;
            r[0] = startSlope;
        }

        // Body rows (equation 15 from the article)
        for (int i = 1; i < n - 1; i++)
        {
            dx1 = x[i] - x[i - 1];
            dx2 = x[i + 1] - x[i];

            m.A[i] = 1.0f / dx1;
            m.C[i] = 1.0f / dx2;
            m.B[i] = 2.0f * (m.A[i] + m.C[i]);

            dy1 = y[i] - y[i - 1];
            dy2 = y[i + 1] - y[i];
            r[i] = 3 * (dy1 / (dx1 * dx1) + dy2 / (dx2 * dx2));
        }

        // Last row also different (equation 17 from the article)
        if (endSlope.x == 0 && endSlope.y == 0 && endSlope.z == 0)
        {
            dx1 = x[n - 1] - x[n - 2];
            dy1 = y[n - 1] - y[n - 2];
            m.A[n - 1] = 1.0f / dx1;
            m.B[n - 1] = 2.0f * m.A[n - 1];
            r[n - 1] = 3 * (dy1 / (dx1 * dx1));
        }
        else
        {
            m.B[n - 1] = 1;
            r[n - 1] = endSlope;
        }

        //if (debug) Console.WriteLine("Tri-diagonal matrix:\n{0}", m.ToDisplayString(":0.0000", "  "));
        //if (debug) Console.WriteLine("r: {0}", ArrayUtil.ToString<float>(r));

        // k is the solution to the matrix
        Vector4[] k = new Vector4[r.Length];

        for (int dim = 0; dim < 4; dim++) { 
        float[] currentR = extractFromVectors(r, dim);

        float[] currentK = m.Solve(currentR);
            for(int j=0; j < currentK.Length; j++)
            {
                if(dim==0)
                    k[j].x = currentK[j];
                if (dim == 1)
                    k[j].y = currentK[j];
                if (dim == 2)
                    k[j].z = currentK[j];
                if (dim == 3)
                    k[j].w = currentK[j];
            }
        }
        //if (debug) Console.WriteLine("k = {0}", ArrayUtil.ToString<float>(k));

        // a and b are each spline's coefficients

        this.aVector = new Vector4[n - 1];
        this.bVector = new Vector4[n - 1];

        for (int i = 1; i < n; i++)
        {
            dx1 = x[i] - x[i - 1];
            dy1 = y[i] - y[i - 1];
            aVector[i - 1] = k[i - 1] * dx1 - dy1; // equation 10 from the article
            bVector[i - 1] = -k[i] * dx1 + dy1; // equation 11 from the article
        }

        //if (debug) Console.WriteLine("a: {0}", ArrayUtil.ToString<float>(a));
        //if (debug) Console.WriteLine("b: {0}", ArrayUtil.ToString<float>(b));
    }

    private float[] extractFromVectors(Vector4[] array, int dim)
    {
        float[] output = new float[array.Length];
        for(int i=0; i < array.Length; i++)
        {
            if (dim == 0)
                output[i] = array[i].x;
            if (dim == 1)
                output[i] = array[i].y;
            if (dim == 2)
                output[i] = array[i].z;
            if (dim == 3)
                output[i] = array[i].w;
        }
        return output;
    }

    private void infuseVectors(ref Vector4[] array, float[] val, int dim)
    {
        for (int i = 0; i < val.Length; i++)
        {
            if (dim == 0)
                array[i].x = val[i];
            if (dim == 1)
                array[i].y = val[i];
            if (dim == 2)
                array[i].z = val[i];
            if (dim == 3)
                array[i].w = val[i];
        }
    }

    private float extractFromVector(Vector4 v, int dim)
    {
        float output = 0;
            if (dim == 0)
                output = v.x;
            if (dim == 1)
                output = v.y;
            if (dim == 2)
                output = v.z;
            if (dim == 3)
                output = v.w;
        return output;
    }

    #endregion

    #region Eval*

    /// <summary>
    /// Evaluate the spline at the specified x coordinates.
    /// This can extrapolate off the ends of the splines.
    /// You must provide X's in ascending order.
    /// The spline must already be computed before calling this, meaning you must have already called Fit() or FitAndEval().
    /// </summary>
    /// <param name="x">Input. X coordinates to evaluate the fitted curve at.</param>
    /// <param name="debug">Turn on console output. Default is false.</param>
    /// <returns>The computed y values for each x.</returns>
    public Vector4[] Eval(float[] x, out Vector4[]dy, out Vector4[] ddy, bool debug = false)
        {
            CheckAlreadyFitted();

            int n = x.Length;
            Vector4[] y = new Vector4[n];
            dy = new Vector4[n];
            ddy = new Vector4[n];

        _lastIndex = 0; // Reset simultaneous traversal in case there are multiple calls

            for (int i = 0; i < n; i++)
            {
                // Find which spline can be used to compute this x (by simultaneous traverse)
                int j = GetNextXIndex(x[i]);

                // Evaluate using j'th spline
                y[i] = EvalSpline(x[i], j, out dy[i], out ddy[i], debug);
            }

            return y;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Static all-in-one method to fit the splines and evaluate at X coordinates.
        /// </summary>
        /// <param name="x">Input. X coordinates to fit.</param>
        /// <param name="y">Input. Y coordinates to fit.</param>
        /// <param name="xs">Input. X coordinates to evaluate the fitted curve at.</param>
        /// <param name="startSlope">Optional slope constraint for the first point. Single.NaN means no constraint.</param>
        /// <param name="endSlope">Optional slope constraint for the final point. Single.NaN means no constraint.</param>
        /// <param name="debug">Turn on console output. Default is false.</param>
        /// <returns>The computed y values for each xs.</returns>
    public static Vector4[] Compute(float[] dists, Vector4[] points, float[]  times, out Vector4[] dy, out Vector4[] ddy, Vector4 startSlope, Vector4 endSlope, bool debug = false)
    {
        CubicSpline spline = new CubicSpline();
        return spline.FitAndEval(dists, points, times, out dy, out ddy, startSlope, endSlope, debug);
    }

    public static void FitParametric(Vector4[] points, int nOutputPoints, out Vector4[] interpolation, out Vector4[] dinterpolation, out Vector4[] ddinterpolation, Vector4 firstDerivative = new Vector4(), Vector4 lastDerivative = new Vector4())
    {
        // Compute distances
        int n = points.Length;
        float[] dists = new float[n]; // cumulative distance
        dists[0] = 0;
        float totalDist = 0;

        for (int i = 1; i < n; i++)
        {
            Vector4 derivative = points[i] - points[i - 1];
            float dist = new Vector3(derivative.x, derivative.y, derivative.z).magnitude; // radius does not counts towards distance covered
            totalDist += dist;
            dists[i] = totalDist;
        }

        // Create 'times' to interpolate to
        float dt = totalDist / (nOutputPoints - 1);
        float[] times = new float[nOutputPoints];
        times[0] = 0;

        for (int i = 1; i < nOutputPoints; i++)
        {
            times[i] = times[i - 1] + dt;
        }

        // Normalize the slopes, if specified
        firstDerivative.Normalize();
        lastDerivative.Normalize();
        

        // Spline fit both x and y to times
        CubicSpline xSpline = new CubicSpline();
        
        interpolation = xSpline.FitAndEval(dists, points, times, out dinterpolation, out ddinterpolation, firstDerivative / dt, lastDerivative / dt);
    }

        #endregion
    }
