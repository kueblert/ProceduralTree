using UnityEngine;
using System.Collections;

    public class Ellipsoid : MonoBehaviour
    {
    public bool useUpperhalfOnly = false;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

    // test whether point is inside of the ellipsoid
    public bool testPoint(Vector3 p)
    {
        Vector3 mid = gameObject.transform.position;
        Vector3 axesLengths = gameObject.transform.lossyScale /2; // diameter / 2
        //Debug.Log("lossy: " + gameObject.transform.lossyScale);
        //Debug.Log("local: " + gameObject.transform.localScale);
        Vector3 midToP = p - mid;
        if (useUpperhalfOnly)
        {
            if (midToP.y < 0) return false;
        }

            return Mathf.Pow(midToP.x, 2) / Mathf.Pow(axesLengths.x, 2) + Mathf.Pow(midToP.y, 2) / Mathf.Pow(axesLengths.y, 2) + Mathf.Pow(midToP.z, 2) / Mathf.Pow(axesLengths.z, 2) <= 1.0f;
        
    }

}

