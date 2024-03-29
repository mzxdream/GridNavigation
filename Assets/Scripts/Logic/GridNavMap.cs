using UnityEngine;

namespace GridNav
{
    public class NavMap
    {
        private Vector3 bmin;
        private int xsize;
        private int zsize;
        private int xsizeh;
        private int zsizeh;
        private float squareSize;
        private int[] squareTypeMap; // (xsize / 2) * (zsize / 2) origin data
        private float[] cornerHeightMap; // (xsize + 1) * (zsize + 1) origin data
        private float[] centerHeightMap; // xsize * zsize
        private Vector3[] faceNormals; // xsize * zsize * 2
        private Vector3[] centerNormals; // xsize * zsize
        private Vector3[] centerNormals2D; // xsize * zsize
        private float[] slopeMap; // (xsize / 2) * (zsize / 2)

        public Vector3 BMin { get => bmin; }
        public int XSize { get => xsize; }
        public int ZSize { get => zsize; }
        public int XSizeH { get => xsizeh; }
        public int ZSizeH { get => zsizeh; }
        public float SquareSize { get => squareSize; }

        public bool Init(Vector3 bmin, int xsize, int zsize, float squareSize, int[] squareTypeMap, float[] cornerHeightMap)
        {
            Debug.Assert(xsize > 1 && zsize > 1 && squareSize > NavMathUtils.EPSILON);
            Debug.Assert((xsize & 1) == 0 && (zsize & 1) == 0);
            Debug.Assert(squareTypeMap != null && squareTypeMap.Length == (xsize >> 1) * (zsize >> 1));
            Debug.Assert(cornerHeightMap != null && cornerHeightMap.Length == (xsize + 1) * (zsize + 1));
            this.bmin = bmin;
            this.xsize = xsize;
            this.zsize = zsize;
            this.xsizeh = (xsize >> 1);
            this.zsizeh = (zsize >> 1);
            this.squareSize = squareSize;
            this.squareTypeMap = squareTypeMap;
            this.cornerHeightMap = cornerHeightMap;
            this.centerHeightMap = new float[xsize * zsize];
            this.faceNormals = new Vector3[xsize * zsize * 2];
            this.centerNormals = new Vector3[xsize * zsize];
            this.centerNormals2D = new Vector3[xsize * zsize];
            this.slopeMap = new float[xsizeh * zsizeh];
            UpdateHeightMap(0, xsize - 1, 0, zsize - 1);
            return true;
        }
        public void Clear()
        {
        }
        public void SetSquareType(int x, int z, int type)
        {
            Debug.Assert(x >= 0 && x < xsizeh && z >= 0 && z < zsizeh);
            squareTypeMap[x + z * xsizeh] = type;
        }
        public void SetCornerHeight(int x, int z, float height)
        {
            Debug.Assert(x >= 0 && x <= xsize && z >= 0 && z <= zsize);
            cornerHeightMap[x + z * (xsize + 1)] = height;
        }
        public void UpdateHeightMap()
        {
            UpdateHeightMap(0, xsize - 1, 0, zsize - 1);
        }
        public void UpdateHeightMap(int xmin, int xmax, int zmin, int zmax)
        {
            UpdateCenterHeightMap(xmin, xmax, zmin, zmax);
            UpdateFaceNormals(xmin, xmax, zmin, zmax);
            UpdateSlopeMap(xmin, xmax, zmin, zmax);
        }
        public void GetSquareXZ(float posX, float posZ, out int x, out int z)
        {
            x = (int)((posX - bmin.x) / squareSize);
            z = (int)((posZ - bmin.z) / squareSize);
            x = Mathf.Clamp(x, 0, xsize - 1);
            z = Mathf.Clamp(z, 0, zsize - 1);
        }
        public void GetSquareXZ(Vector3 pos, out int x, out int z)
        {
            GetSquareXZ(pos.x, pos.z, out x, out z);
        }
        public void ClampInBounds(Vector3 pos, out int nearestX, out int nearestZ, out Vector3 nearestPos)
        {
            nearestX = (int)((pos.x - bmin.x) / squareSize);
            nearestZ = (int)((pos.z - bmin.z) / squareSize);
            if (nearestX >= 0 && nearestX < xsize && nearestZ >= 0 && nearestZ < zsize)
            {
                nearestPos = pos;
                nearestPos.y = GetHeight(nearestPos);
                return;
            }
            nearestX = Mathf.Clamp(nearestX, 0, xsize - 1);
            nearestZ = Mathf.Clamp(nearestZ, 0, zsize - 1);
            nearestPos = GetSquarePos(nearestX, nearestZ);
        }
        public Vector3 GetSquarePos(int x, int z)
        {
            Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
            var pos = new Vector3(bmin.x + (x + 0.5f) * squareSize, 0, bmin.z + (z + 0.5f) * squareSize);
            pos.y = GetHeight(pos);
            return pos;
        }
        public Vector3 GetSquareCornerPos(int x, int z)
        {
            Debug.Assert(x >= 0 && x <= xsize && z >= 0 && z <= zsize);
            var pos = new Vector3(bmin.x + x * squareSize, 0, bmin.z + z * squareSize);
            pos.y = cornerHeightMap[x + z * (xsize + 1)];
            return pos;
        }
        public int GetSquareType(int x, int z)
        {
            Debug.Assert(x >= 0 && x < xsizeh && z >= 0 && z < zsizeh);
            return squareTypeMap[x + z * xsizeh];
        }
        public float GetSquareSlope(int x, int z)
        {
            Debug.Assert(x >= 0 && x < xsizeh && z >= 0 && z < zsizeh);
            return slopeMap[x + z * xsizeh];
        }
        public Vector3 GetSquareCenterNormal2D(int x, int z)
        {
            Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
            return centerNormals2D[x + z * xsize];
        }
        public float GetHeight(float posX, float posZ)
        {
            float x = Mathf.Clamp((posX - bmin.x) / squareSize, 0.0f, (float)(xsize - 1));
            float z = Mathf.Clamp((posZ - bmin.z) / squareSize, 0.0f, (float)(zsize - 1));
            int ix = (int)x;
            int iz = (int)z;
            float dx = x - ix;
            float dz = z - iz;
            int index = ix + iz * (xsize + 1);

            if (dx + dz < 1.0f)
            {
                float hTL = cornerHeightMap[index];
                float hTR = cornerHeightMap[index + 1];
                float hBL = cornerHeightMap[index + (xsize + 1)];
                float xDiff = dx * (hTR - hTL);
                float zDiff = dz * (hBL - hTL);
                return hTL + xDiff + zDiff;
            }
            else
            {
                float hTR = cornerHeightMap[index + 1];
                float hBL = cornerHeightMap[index + (xsize + 1)];
                float hBR = cornerHeightMap[index + 1 + (xsize + 1)];
                float xDiff = (1.0f - dx) * (hBL - hBR);
                float zDiff = (1.0f - dz) * (hTR - hBR);
                return hBR + xDiff + zDiff;
            }
        }
        public float GetHeight(Vector3 pos)
        {
            return GetHeight(pos.x, pos.z);
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
                    avgSlope += faceNormals[(idx0) * 2].y; // TL fTL
                    avgSlope += faceNormals[(idx0) * 2 + 1].y; // TL fBR
                    avgSlope += faceNormals[(idx0 + 1) * 2].y; // TR fTL
                    avgSlope += faceNormals[(idx0 + 1) * 2 + 1].y; // TR fBR
                    avgSlope += faceNormals[(idx1) * 2].y; // BL fTL
                    avgSlope += faceNormals[(idx1) * 2 + 1].y; // BL fBR
                    avgSlope += faceNormals[(idx1 + 1) * 2].y; // BR fTL
                    avgSlope += faceNormals[(idx1 + 1) * 2 + 1].y; // BR fBR
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