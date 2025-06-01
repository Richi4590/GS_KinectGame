using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArucoUnity.Objects.Trackers;
using ArucoUnity.Plugin;
using UnityEditor.Rendering;
using ArucoUnity.Utilities;
using System.Runtime.InteropServices;
using System;
using UnityEngine.Rendering;
using ArucoUnity.Objects;
using ArucoUnity.Cameras;
using OpenCvSharp;
using Windows.Kinect;
using System.Linq;

public class ArucoGetIDFromImage : MonoBehaviour
{
    public bool DebugWithTex = false;
    public Texture2D debugTex;

    public ArucoWebcam webCam;
    public Aruco.PredefinedDictionaryName markerTypeDict = Aruco.PredefinedDictionaryName.Dict4x4_50;
    public bool scanByItself = false;
    public FlipMode flipMode;
    private Std.VectorVectorPoint2f markerCorners, rejectedCandidateCorners;
    private Std.VectorInt markerIds;
    private Cv.Mat matTex, debugMatTex;
    private Mat cvMatTex;
    private Texture2D texFromCamera;
    private int cameraWidth;
    private int cameraHeight;

    private byte[] pixelBuffer; // CPU-side pixel buffer

    private void Awake()
    {
    }

    // Start is called before the first frame update
    private void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(ReadIDFromImage(debugTex));

        if (scanByItself)
        {
            if (DebugWithTex)
            {
                debugMatTex = new Cv.Mat(debugTex.height, debugTex.width, Cv.Type.CV_8UC3, debugTex.GetRawTextureData());

                Cv.Flip(debugMatTex, debugMatTex, (int)Cv.verticalFlipCode);

                Aruco.DetectMarkers(debugMatTex, Aruco.GetPredefinedDictionary(markerTypeDict), out markerCorners, out markerIds, GetComponent<DetectorParametersController>().DetectorParameters, out rejectedCandidateCorners);

                int count = (int)markerIds.Size();
                if (count > 0)
                {
                    int[] idsArray = new int[count];
                    string consoleString = "";

                    for (int i = 0; i < count; i++)
                    {
                        int id = markerIds.At((uint)i);
                        idsArray[i] = id;
                        consoleString = consoleString + ", " + id;
                    }

                    Debug.Log("Detected Marker ID: " + consoleString);
                }
                else
                {
                    Debug.Log("No markers detected.");
                }

            }
            else
            {
                ReadIDsFromImageSelf();
            }
        }
    }

    private void ReadIDsFromImageSelf()
    {
        if (webCam == null)
            return;

        matTex = webCam.Images[0];

        Aruco.DetectMarkers(matTex, Aruco.GetPredefinedDictionary(markerTypeDict), out markerCorners, out markerIds, GetComponent<DetectorParametersController>().DetectorParameters, out rejectedCandidateCorners);

        int count = (int)markerIds.Size();
        if (count > 0)
        {
            int[] idsArray = new int[count];
            string consoleString = "";

            for (int i = 0; i < count; i++)
            {
                int id = markerIds.At((uint)i);
                idsArray[i] = id;
                consoleString = consoleString + ", " + id;
            }

            Debug.Log("Detected Marker ID: " + consoleString);
        }
        else
        {
            Debug.Log("No markers detected.");
        }
    }

    public List<int> ReadIDsFromImage(Texture2D tex)
    {
        matTex = new Cv.Mat(tex.height, tex.width, Cv.Type.CV_8UC3, tex.GetRawTextureData());

        Cv.Flip(matTex, matTex, (int)Cv.verticalFlipCode);
        
        Aruco.DetectMarkers(matTex, Aruco.GetPredefinedDictionary(markerTypeDict), out markerCorners, out markerIds, GetComponent<DetectorParametersController>().DetectorParameters, out rejectedCandidateCorners);

        int count = (int)markerIds.Size();
        if (count > 0)
        {
            int[] idsArray = new int[count];
            string consoleString = "";

            for (int i = 0; i < count; i++)
            {
                int id = markerIds.At((uint)i);
                idsArray[i] = id;
                consoleString = consoleString + ", " + id;
            }

            //Debug.Log("Detected Marker ID: " + consoleString);

            return idsArray.ToList();
        }
        else
        {
            //Debug.Log("No markers detected.");
            return new List<int>(0);
        }
    }

    public int ReadIDFromImage(Texture2D tex)
    {
        matTex = new Cv.Mat(tex.height, tex.width, Cv.Type.CV_8UC3);
        cvMatTex = new Mat(tex.height, tex.width, MatType.CV_8UC3, tex.GetRawTextureData()); 
        Cv2.Flip(cvMatTex, cvMatTex, flipMode);
        // Convert to grayscale and enhance contrast 
        
        /*
        Mat gray = new Mat();
        Cv2.CvtColor(cvMatTex, cvMatTex, ColorConversionCodes.RGB2GRAY);
        Cv2.EqualizeHist(cvMatTex, cvMatTex); // Improve contrast
        */

        matTex.DataIntPtr = cvMatTex.Data;

        Aruco.DetectMarkers(matTex, Aruco.GetPredefinedDictionary(markerTypeDict), out markerCorners, out markerIds, GetComponent<DetectorParametersController>().DetectorParameters, out rejectedCandidateCorners);


        int count = (int)markerIds.Size();
        if (count > 0)
        {
            /*
            int[] idsArray = new int[count];
            string consoleString = "";


            for (int i = 0; i < count; i++)
            {
                int id = markerIds.At((uint)i);
                idsArray[i] = id;
                consoleString = consoleString + ", " + id;
            }

            Debug.Log("Detected Marker ID: " + consoleString);
            */
            return markerIds.At(0);
        }
        else
        {
            //Debug.Log("No markers detected.");
            return -1;
        }
    }

} 
