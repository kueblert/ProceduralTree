using UnityEngine;
using System.Collections;

public class testPoissonSampling : MonoBehaviour {

    public Vector3 position = new Vector3(5, 6, 7);
    public Vector3 size = new Vector3(10, 5, 3);
    public float minDist = 0.1f;
    public int nSamples = 1000;

    private PoissonSampling _sampling;

    // Use this for initialization
    void Start () {


        //PoissonSampling sampling = new GridJitterSampling2(position, size , minDist, nSamples);

        _sampling = new PoissonDiskSampling2(size.x, size.y, size.z, minDist, 30, nSamples);

        _sampling.sample();
        _sampling.visualize();
    }
	
	// Update is called once per frame
	void Update () {

	}
}
