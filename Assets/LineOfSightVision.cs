using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class LineOfSightVision : MonoBehaviour
{
    
    // Amount of directions within FOV to check 
    public int Resolution = 1024;
    public bool DrawDebugLines = false;
    public float Range = 25;
    public float FieldOfView = 90;
    public float DegreeOffset = 90;
    public float MaxAngle = 60;
    private MeshFilter LOSMeshFilter;

    public Material material;

    private Mesh meshDraw;
    
    void Start() {        
        LOSMeshFilter = GetComponent<MeshFilter>();
        GetComponent<MeshRenderer>().sharedMaterial = material != null ? material : new Material(Shader.Find("Standard"));        
    }

    void Update() {

        List<Vector3>[] pointLines = new List<Vector3>[Resolution + 1];

        for(int i = 0; i <= Resolution; i++) {
            float degrees = (((float)i / Resolution) * FieldOfView);
            degrees -= FieldOfView / 2;
            degrees -= transform.parent.rotation.eulerAngles.y;
            degrees += DegreeOffset;

            pointLines[i] = GetPointLine(transform.position, RadiansToVector3(degrees * Mathf.Deg2Rad), Range);
        }

            if (DrawDebugLines) {
            for (int i = 0; i < pointLines.Length; i++) {
                Debug.DrawLine(transform.position, pointLines[i][0] + transform.position, Color.red, 0);
                for(int j = 0; j < pointLines[i].Count - 1; j++) {
                    Debug.DrawLine(pointLines[i][j] + transform.position, pointLines[i][j + 1] + transform.position, Color.red, 0);
                }                
            }
        }
        LOSMeshFilter.mesh = CreateMeshFromPointLines(pointLines);     
    }

    List<Vector3> GetPointLine(Vector3 origin, Vector3 direction, float maxDist) {
        List<Vector3> pointLine = new List<Vector3>();

        float travelledDist = 0;
        
        // Loop until cant travel more or angle is too large
        while(travelledDist < maxDist && Vector3.Angle(direction, new Vector3(direction.x, 0, direction.z)) < MaxAngle) {
            RaycastHit hit;
            if(Physics.Raycast(origin, direction, out hit, maxDist-travelledDist)) { // If a surface was hit
                pointLine.Add(hit.point - transform.position);
                travelledDist += hit.distance;
                origin = hit.point;
                Vector3 newDir = DoubleCross(direction, hit.normal);    
                if(Vector3.Angle(newDir, direction) > MaxAngle) {
                    break;
                } else {
                    direction = newDir;
                }
            } else {
                pointLine.Add(origin + (direction.normalized) * (maxDist - travelledDist) - transform.position);
                break;
            }
        }

        return pointLine;
    }

    Vector3 DoubleCross(Vector3 u, Vector3 n) {
        // v = Cross u x n
        Vector3 v = Vector3.Cross(u, n);
        // d = Cross n x v
        Vector3 d = Vector3.Cross(n, v);
        return d;
    }

    Vector3[] SimpleGetPoints() {
        Vector3[] points = new Vector3[Resolution + 2];
        transform.rotation = Quaternion.identity;
        for(int i = 1; i <= Resolution + 1; i++) {

            RaycastHit hitInfo;
            float degrees = (((float)(i - 1) / Resolution) * FieldOfView);
            degrees -= FieldOfView / 2;
            degrees -= transform.parent.rotation.eulerAngles.y;
            degrees += DegreeOffset;


            if(Physics.Raycast(new Ray(transform.position, RadiansToVector3(degrees * Mathf.Deg2Rad)), out hitInfo, Range)) {
                points[i] = hitInfo.point - transform.position;
            } else {
                points[i] = RadiansToVector3(degrees * Mathf.Deg2Rad).normalized * Range;
            }
        }
        return points;
    }

    /// <summary>
    /// Gives a point within unit sphere with angle rads
    /// </summary>
    Vector3 RadiansToVector3(float rads) {
        return new Vector3(Mathf.Cos(rads), 0, Mathf.Sin(rads));
    }

    Mesh CreateMeshFromPointLines(List<Vector3>[] pointLines) {
        Mesh mesh = new Mesh();
        mesh.name = "LOSMesh";

        int pointCount = 1;
        foreach(List<Vector3> pointLine in pointLines) {
            pointCount += pointLine.Count;
        }

        Vector3[] vertices = new Vector3[pointCount];
        int vertCount = 1;
        for(int i = 0; i < pointLines.Length; i++) {
            for(int j = 0; j < pointLines[i].Count; j++) {
                vertices[vertCount] = pointLines[i][j] + new Vector3(0, 0.01f, 0); // Add padding to avoid z-fighting
                vertCount++;
            }
        }

        int[] triangles = new int[9000];

        int triCount = 0;
        int vertOffset = 1; // Offset from center
        // For every point line
        for(int i = 0; i < pointLines.Length - 1; i++) {
            int thisIndex = 0; // Start at center for current line
            int nextIndex = 1; // Start at 1st point for next line

            int firstVertInNextLineIndex = vertOffset + pointLines[i].Count;
            int lastVertIntThisLineIndex = vertOffset + pointLines[i].Count - 1;

            for(thisIndex = 0; thisIndex < pointLines[i].Count; thisIndex++) {

                triangles[triCount] = thisIndex == 0 ? 0 : thisIndex + vertOffset - 1;
                triangles[triCount + 1] = firstVertInNextLineIndex;
                triangles[triCount + 2] = thisIndex + vertOffset;
                //Debug.Log(thisIndex);
                //Debug.Log(triangles[triCount] + ": " + vertices[triangles[triCount]]);
                //Debug.Log(triangles[triCount + 1] + ": " + vertices[triangles[triCount + 1]]);
                //Debug.Log(triangles[triCount + 2] + ": " + vertices[triangles[triCount + 2]]);
                triCount += 3;
            }
            for(nextIndex = 0; nextIndex < pointLines[i + 1].Count - 1; nextIndex++) {
                triangles[triCount] = pointLines[i].Count + vertOffset + nextIndex;
                triangles[triCount + 1] = pointLines[i].Count + vertOffset + nextIndex + 1;
                triangles[triCount + 2] = lastVertIntThisLineIndex;
                triCount += 3;
            }

            vertOffset += pointLines[i].Count; // Offset by points in this pointLine
        }

        meshDraw = mesh;
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }

    Mesh CreateMeshFromPoints(Vector3[] points) {
        Mesh m = new Mesh();
        m.name = "LOSMesh";

        points[0] = Vector3.zero;
        m.vertices = points;

        int[] trianglesArray = new int[(m.vertices.Length-1) * 3];

        int count = 1;
        for (int i = 0; i < trianglesArray.Length-3; i+=3) {
            trianglesArray[i] = count;
            trianglesArray[i + 1] = 0;
            trianglesArray[i + 2] = count + 1;
            count++;
        }
        //trianglesArray[trianglesArray.Length-3] = m.vertices.Length-1;
        //trianglesArray[trianglesArray.Length-2] = 0;
        //trianglesArray[trianglesArray.Length-1] = 1;
        
        m.triangles = trianglesArray;       

        m.RecalculateNormals();

        return m;
    }

    void OnDrawGizmos() {
        for(int i = 0; i < meshDraw.vertices.Length; i++) {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(meshDraw.vertices[i] + transform.position, 0.05f);
        }
    }
}
