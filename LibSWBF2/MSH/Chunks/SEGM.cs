﻿using System;
using System.Collections.Generic;
using LibSWBF2;
using LibSWBF2.Types;
using LibSWBF2.MSH.Types;

namespace LibSWBF2.MSH.Chunks {
    /// <summary>
    /// Mesh Segment Containing all the necessary Geometry Information like Vertices, Normals, UVs
    /// </summary>
    public class SEGM : BaseChunk {
        /// <summary>
        /// The Material of this Mesh Segment
        /// </summary>
        public MATD Material { get; set; }
        private int matIndex;

        /// <summary>
        /// All Vertices in this Mesh Segment
        /// </summary>
        public Vertex[] Vertices { get { return vertices.ToArray(); } }
        private List<Vertex> vertices = new List<Vertex>();

        /// <summary>
        /// List of Polygons created from Vertices. Each entry contains the index to a Vertex
        /// </summary>
        public Polygon[] Polygons { get { return polygons.ToArray(); } }
        public List<Polygon> polygons = new List<Polygon>();

        private bool hasUVs = false;


        public SEGM(MSH owner) : base(owner) {
            ChunkName = "SEGM";
        }

        public SEGM(BaseChunk from) : base(from) {
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            Polygon currentPoly = new Polygon(vertices);
            bool lastBoundry = false;

            while (!EndOfData) {
                BaseChunk nextChunk = ReadChunk();
                int count = 0;

                switch (nextChunk.ChunkName) {
                    case "MATI":
                        matIndex = nextChunk.ReadInt32();
                        break;
                    case "POSL":
                        count = nextChunk.ReadInt32();
                        for (int i = 0; i < count; i++) {
                            verts.Add(nextChunk.ReadVector3());
                        }
                        break;
                    case "NRML":
                        count = nextChunk.ReadInt32();
                        for (int i = 0; i < count; i++) {
                            normals.Add(nextChunk.ReadVector3());
                        }
                        break;
                    case "UV0L":
                        count = nextChunk.ReadInt32();
                        for (int i = 0; i < count; i++) {
                            uvs.Add(nextChunk.ReadVector2());
                        }
                        break;
                    case "STRP":
                        count = nextChunk.ReadInt32();
                        for (int i = 0; i < count; i++) {
                            VertexIndex next = nextChunk.ReadVertexIndex();

                            if (next.polyBoundary && !lastBoundry) {
                                    //polygon finished, add to buffer
                                    if (currentPoly.VertexIndices.Count > 0)
                                        polygons.Add(currentPoly);

                                    //start new polygon
                                    currentPoly = new Polygon(vertices);

                                    //write first index value to polygon
                                    currentPoly.VertexIndices.Add(next.index);
                            }
                            else {
                                if (currentPoly != null) {
                                    currentPoly.VertexIndices.Add(next.index);
                                }
                                else {
                                    //this should never happen
                                    Log.Add("Warning: Lone Vertex in Strip Buffer!", LogType.Warning);
                                }
                            }

                            lastBoundry = next.polyBoundary;
                        }
                        break;
                }
            }

            if (uvs.Count > 0)
                hasUVs = true;

            for (int i = 0; i < verts.Count; i++) {
                //since uv coordinates are optional, deliver empty ones if non existent
                Vector2 uv = (i < uvs.Count) ? uvs[i] : new Vector2();

                vertices.Add(new Vertex(verts[i], normals[i], uv));
            }

            //Add last Polygon
            if (currentPoly != null && currentPoly.VertexIndices.Count > 0)
                polygons.Add(currentPoly);
        }

        public override void WriteData() {
            base.WriteData();

            WriteHeader("MATI");
            WriteInt32(4);
            WriteInt32(Owner.Materials.FindIndex(mat => mat == Material));

            WriteHeader("POSL");
            WriteInt32(vertices.Count * 12 + 4);    //float 4 bytes * vector3, 3 floats  =  12 bytes per Position
            WriteInt32(vertices.Count);         //first integer indicates number of vertices that follow
            foreach (Vertex vert in vertices)
                WriteVector3(vert.position);

            WriteHeader("NRML");
            WriteInt32(vertices.Count * 12 + 4);
            WriteInt32(vertices.Count);
            foreach (Vertex vert in vertices)
                WriteVector3(vert.normal);

            if (hasUVs) {
                WriteHeader("UV0L");
                WriteInt32(vertices.Count * 8 + 4);
                WriteInt32(vertices.Count);
                foreach (Vertex vert in vertices)
                    WriteVector2(vert.uvCoordinate);
            }

            //Write Polygon Strips
            WriteHeader("STRP");

            //lets build up an index buffer from our stored polygons
            List<short> vertexBuffer = new List<short>();

            foreach (Polygon poly in polygons) {
                for (int i = 0; i < poly.VertexIndices.Count; i++) {
                    short vertInd = poly.VertexIndices[i];

                    //the first two indices are always tagged as begin/end
                    if (i == 0 || i == 1) {
                        //if index is a polygon boundary (begin/end), set the highest bit, e.g.:
                        // (using logical OR)
                        // value  0000000000000111   = 7
                        // mask   1000000000000000   = 0x8000 in hex
                        // result 1000000000000111   = desired index value (-32761 in dec)
                        vertInd = (short)(vertInd ^ 0x8000);
                    }

                    vertexBuffer.Add(vertInd);
                }
            }

            WriteInt32(vertexBuffer.Count * 2 + 4);
            WriteInt32(vertexBuffer.Count);

            foreach (short vertInd in vertexBuffer) {
                WriteInt16(vertInd);
            }
            

            WriteChunkLength();
        }

        public void ApplyReferences(MATD[] materials) {
            //Apply Material from material index
            if (matIndex >= 0 && matIndex < materials.Length) {
                Material = materials[matIndex];
            }
        }

        /// <summary>
        /// Checks the integrity of the Chunk. Reports Error Messages if values are missing
        /// </summary>
        /// <returns></returns>
        public override CheckResult CheckIntegrity() {
            CheckResult result = new CheckResult();

            if (Material == null) {
                result.AddError("Material of Mesh Segment is set to NULL!");
            }

            try {
                result = CheckResult.Merge(result, CheckResult.Merge(vertices.ToArray()));
            }
            catch (ArgumentNullException ex) {
                result.AddError(ex.Message);
                return result;
            }
            catch (Exception ex) {
                result.AddError("An Unknown Error occured! " + ex.Message);
                return result;
            }

            return result;
        }
    }
}