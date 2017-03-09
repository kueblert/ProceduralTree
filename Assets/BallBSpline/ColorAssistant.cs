using UnityEngine;

public class ColorAssistant
{
    
    // Qualitative color map from colorbrewer2.org
    private static Color[] mQualitativeColorMap = {
        new Color(228.0f / 255.0f, 26.0f / 255.0f, 28.0f / 255.0f),
        new Color(55.0f / 255.0f, 126.0f / 255.0f, 184.0f / 255.0f),
        new Color(77.0f / 255.0f, 175.0f / 255.0f, 74.0f / 255.0f),
        new Color(152.0f / 255.0f, 78.0f / 255.0f, 163.0f / 255.0f),
        new Color(255.0f / 255.0f, 127.0f / 255.0f, 0.0f / 255.0f),
        new Color(255.0f / 255.0f, 255.0f / 255.0f, 51.0f / 255.0f),
        new Color(166.0f / 255.0f, 86.0f / 255.0f, 40.0f / 255.0f)
    };

    private static Color[] mDivergingColorMap = {
        new Color(215.0f/255.0f,48.0f/255.0f,39.0f/255.0f),
        new Color(252.0f/255.0f,141.0f/255.0f,89.0f/255.0f),
        new Color(254.0f/255.0f,224.0f/255.0f,139.0f/255.0f),
        new Color(255.0f/255.0f,255.0f/255.0f,191.0f/255.0f),
        new Color(217.0f/255.0f,239.0f/255.0f,139.0f/255.0f),
        new Color(145.0f/255.0f,207.0f/255.0f,96.0f/255.0f),
        new Color(26.0f/255.0f,152.0f/255.0f,80.0f/255.0f)
    };

    public static Color getQualitativeColor(int idx)
    {
            return mQualitativeColorMap[idx % mQualitativeColorMap.Length];
    }

    public static Color getDivergingColor(int idx)
    {
        return mDivergingColorMap[idx % mDivergingColorMap.Length];
    }

}
