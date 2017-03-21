using UnityEngine;
using System.Collections;


public abstract class PoissonSampling {
    protected Vector3 _center;
    protected Vector3 _size;

    protected PoissonSampling(Vector3 center, Vector3 size)
    {
        _center = center;
        _size = size;
    }

    public abstract Vector3[] sample();

    public abstract void visualize();

}
