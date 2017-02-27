using UnityEngine;
using System.Collections;

public class testBallBSpline : MonoBehaviour {

	// Use this for initialization
	void Start () {

        BallBSpline spline = new BallBSpline();
        spline.visualize();
        spline.visualizeSurface();
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
