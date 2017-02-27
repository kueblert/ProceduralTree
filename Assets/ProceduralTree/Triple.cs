using UnityEngine;
using System.Collections;

/**
 * Triple for indexing a 3D grid
 **/
public class Triple {
        public int x, y, z;

        public Triple(int px, int py, int pz)
    {
        x = px; y = py; z = pz;
    }

        public static Triple operator +(Triple t1, Triple t2)
    {
        return new Triple(t1.x + t2.x, t1.y + t2.y, t1.z + t2.z);
    }

        public override string ToString()
    {
        return "(" + x + "," + y + "," + z + ")";
    }
}
