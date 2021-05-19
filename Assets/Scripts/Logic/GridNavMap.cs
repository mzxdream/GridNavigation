using UnityEngine;

namespace GridNav
{
    public class NavMap
    {
        private Vector3 bmin;
        private int xsize;
        private int zsize;
        private float squareSize;
        private int[] squareTypeMap; // xsize * zsize origin data
        private float[] cornerHeightMap; // (xsize + 1) * (zsize + 1) origin data
        private float[] centerHeightMap; // xsize * zsize
        private Vector3[] faceNormals; // xsize * zsize * 2
        private Vector3[] centerNormals; // xsize * zsize
        private Vector3[] centerNormals2D; // xsize * zsize
        private float[] slopeMap; // (xsize / 2) * (zsize / 2)

        public int XSize { get => xsize; }
        public int ZSize { get => zsize; }
        public float SquareSize { get => squareSize; }

        public bool Init(Vector3 bmin, int xsize, int zsize, float squareSize)
        {
            if (xsize < 1 || zsize < 1 || squareSize <= 0.0f)
            {
                return false;
            }
            this.bmin = bmin;
            this.xsize = xsize;
            this.zsize = zsize;
            this.squareSize = squareSize;
            this.squareTypeMap = new int[xsize * zsize];
            this.cornerHeightMap = new float[(xsize + 1) * (zsize + 1)];
            this.centerHeightMap = new float[xsize * zsize];
            this.faceNormals = new Vector3[xsize * zsize * 2];
            this.centerNormals = new Vector3[xsize * zsize];
            this.centerNormals2D = new Vector3[xsize * zsize];
            this.slopeMap = new float[(xsize / 2) * (zsize / 2)];
            return true;
        }
        public void AfterInit()
        {
            UpdateHeightMap(0, xsize - 1, 0, zsize - 1);
        }
        public void Clear()
        {
        }
        public void SetSquareType(int x, int z, int type)
        {
            Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
            squareTypeMap[x + z * xsize] = type;
        }
        public void SetCornerHeight(int x, int z, float height)
        {
            Debug.Assert(x >= 0 && x <= xsize && z >= 0 && z <= zsize);
            cornerHeightMap[x + z * (xsize + 1)] = height;
        }
        public void UpdateHeightMap(int xmin, int xmax, int zmin, int zmax)
        {
            UpdateCenterHeightMap(xmin, xmax, zmin, zmax);
            UpdateFaceNormals(xmin, xmax, zmin, zmax);
            UpdateSlopeMap(xmin, xmax, zmin, zmax);
        }
        public void GetSquareXZ(Vector3 pos, out int x, out int z)
        {
            x = (int)((pos.x - bmin.x) / squareSize);
            z = (int)((pos.z - bmin.z) / squareSize);
            x = Mathf.Clamp(x, 0, xsize - 1);
            z = Mathf.Clamp(z, 0, zsize - 1);
        }
        public void ClampInBounds(Vector3 pos, out int nearestX, out int nearestZ, out Vector3 nearestPos)
        {
            nearestX = (int)((pos.x - bmin.x) / squareSize);
            nearestZ = (int)((pos.z - bmin.z) / squareSize);
            if (nearestX >= 0 && nearestX < xsize && nearestZ >= 0 && nearestZ < zsize)
            {
                nearestPos = pos;
                return;
            }
            nearestX = Mathf.Clamp(nearestX, 0, xsize - 1);
            nearestZ = Mathf.Clamp(nearestZ, 0, zsize - 1);
            nearestPos = GetSquarePos(nearestX, nearestZ);
        }
        public Vector3 GetSquarePos(int x, int z)
        {
            Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
            return new Vector3(bmin.x + (x + 0.5f) * squareSize, 0, bmin.z + (z + 0.5f) * squareSize);
        }
        public int GetSquareType(int x, int z)
        {
            Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
            return squareTypeMap[x + z * xsize];
        }
        public float GetSquareSlope(int x, int z)
        {
            Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
            return slopeMap[(x >> 1) + (z >> 1) * (xsize >> 1)];
        }
        public Vector3 GetSquareCenterNormal2D(int x, int z)
        {
            Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
            return centerNormals2D[x + z * xsize];
        }
        private void UpdateCenterHeightMap(int xmin, int xmax, int zmin, int zmax)
        {
            for (int z = zmin; z <= zmax; z++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    float height = cornerHeightMap[x + z * (xsize + 1)]
                        + cornerHeightMap[x + 1 + z * (xsize + 1)]
                        + cornerHeightMap[x + (z + 1) * (xsize + 1)]
                        + cornerHeightMap[(x + 1) + (z + 1) * (xsize + 1)];

                    centerHeightMap[x + z * xsize] = height * 0.25f;
                }
            }
        }
        private void UpdateFaceNormals(int xmin, int xmax, int zmin, int zmax)
        {
            xmin = Mathf.Max(0, xmin - 1);
            xmax = Mathf.Min(xsize - 1, xmax + 1);
            zmin = Mathf.Max(0, zmin - 1);
            zmax = Mathf.Min(zsize - 1, zmax + 1);
            for (int z = zmin; z <= zmax; z++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    float hTL = cornerHeightMap[x + z * (xsize + 1)];
                    float hTR = cornerHeightMap[x + 1 + z * (xsize + 1)];
                    float hBL = cornerHeightMap[x + (z + 1) * (xsize + 1)];
                    float hBR = cornerHeightMap[(x + 1) + (z + 1) * (xsize + 1)];
                    //  *---> e1
                    //  |
                    //  |
                    //  v
                    //  e2
                    // Vector3 e1(squareSize, hTR - hTL, 0);
                    // Vector3 e2(0, hBL - hTL, squareSize);
                    // Vector3 fnTL = Vector3.Cross(e2, e1).normalized
                    Vector3 fnTL = new Vector3(-(hTR - hTL), squareSize, -(hBL - hTL)).normalized;
                    //         e3
                    //         ^
                    //         |
                    //         |
                    //  e4 <---*
                    // Vector3 e3(-squareSize, hBL - hBR, 0);
                    // Vector3 e4(0, hTR - hBR, -squareSize);
                    // Vector3 fnBR = Vector3.Cross(e4, e3).normalized;
                    Vector3 fnBR = new Vector3(hBL - hBR, squareSize, hTR - hBR).normalized;
                    faceNormals[(x + z * xsize) * 2] = fnTL;
                    faceNormals[(x + z * xsize) * 2 + 1] = fnBR;
                    centerNormals[x + z * xsize] = (fnTL + fnBR).normalized;
                    centerNormals2D[x + z * xsize] = NavMathUtils.Normalized2D(fnTL + fnBR);
                }
            }
        }
        private void UpdateSlopeMap(int xmin, int xmax, int zmin, int zmax)
        {
            xmin = Mathf.Max(0, xmin / 2 - 1);
            xmax = Mathf.Min(xsize / 2 - 1, xmax / 2 + 1);
            zmin = Mathf.Max(0, zmin / 2 - 1);
            zmax = Mathf.Min(zsize / 2 - 1, zmax / 2 + 1);

            for (int z = zmin; z <= zmax; z++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    int idx0 = x * 2 + (z * 2) * xsize;
                    int idx1 = x * 2 + (z * 2 + 1) * xsize;

                    float avgSlope = 0.0f;
                    avgSlope += faceNormals[(idx0) * 2].y;
                    avgSlope += faceNormals[(idx0) * 2 + 1].y;
                    avgSlope += faceNormals[(idx0 + 1) * 2].y;
                    avgSlope += faceNormals[(idx0 + 1) * 2 + 1].y;
                    avgSlope += faceNormals[(idx1) * 2].y;
                    avgSlope += faceNormals[(idx1) * 2 + 1].y;
                    avgSlope += faceNormals[(idx1 + 1) * 2].y;
                    avgSlope += faceNormals[(idx1 + 1) * 2 + 1].y;
                    avgSlope *= 0.125f;

                    float maxSlope = faceNormals[(idx0) * 2].y;
                    maxSlope = Mathf.Min(maxSlope, faceNormals[(idx0) * 2 + 1].y);
                    maxSlope = Mathf.Min(maxSlope, faceNormals[(idx0 + 1) * 2].y);
                    maxSlope = Mathf.Min(maxSlope, faceNormals[(idx0 + 1) * 2 + 1].y);
                    maxSlope = Mathf.Min(maxSlope, faceNormals[(idx1) * 2].y);
                    maxSlope = Mathf.Min(maxSlope, faceNormals[(idx1) * 2 + 1].y);
                    maxSlope = Mathf.Min(maxSlope, faceNormals[(idx1 + 1) * 2].y);
                    maxSlope = Mathf.Min(maxSlope, faceNormals[(idx1 + 1) * 2 + 1].y);

                    float slope = maxSlope + (avgSlope - maxSlope) * (maxSlope / avgSlope);
                    slopeMap[x + z * (xsize / 2)] = 1.0f - slope;
                }
            }
        }
    }
}