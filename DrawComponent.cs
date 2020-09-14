using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace QuickDraw
{
    public class DrawComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public DrawComponent()
          : base("Draw QD", "Draw",
              "Randomly draws from a list of QuickDraw drawings supplied by the Read component.",
              "Extra", "QuickDrawGH")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Drawings", "D", "The list of drawings to choose from.", GH_ParamAccess.list);
            pManager.AddPointParameter("Locations", "L", "Points to place the drawings on. Only will draw as many drawings as there are locations to place them on.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Seed", "S", "Seed to randomize which of the supplied drawings to draw.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polylines", "P", "Drawings output as a tree of polylines.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> drawings = new List<string>();
            DA.GetDataList(0, drawings);

            List<Point3d> locations = new List<Point3d>();
            DA.GetDataList(1, locations);

            int seed = new int();
            DA.GetData(2, ref seed);

            //sanity
            if(drawings.Count < locations.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Supplied drawings must be equal or greater in number to the number of supplied locations.");
                return;
            }

            //------------------------------------------------------------------------------
            //here we go

            string[] entries = drawings.ToArray();

            //-----------------------------------------------------------
            //SHUFFLING THE ARRAY smartly, no repeats

            //make a random to shuffle the array
            Random rand = new Random(seed);

            //one for each DRAWING
            int[] drawingIndexes = Enumerable.Range(0, drawings.Count).ToArray();

            //shuffle those ints
            for (int i = 0; i < drawingIndexes.Length; i++)
            {
                int r = rand.Next(drawingIndexes.Length);
                int temp = drawingIndexes[r];
                drawingIndexes[r] = drawingIndexes[i];
                drawingIndexes[i] = temp;
            }

            //take the first locations.Count of those ints and use as indexes for string[] shuffledEntries
            string[] shuffledEntries = new string[locations.Count];
            //use the now shuffled drawingIndexes to fill the array
            for (int i = 0; i < locations.Count; i++)
            {
                shuffledEntries[i] = entries[drawingIndexes[i]];
            }

            //-----------------------------------------------------------

            string[] cleanEntries = new string[shuffledEntries.Length];

            for (int i = 0; i < shuffledEntries.Length; i++)
            {
                if (shuffledEntries[i] != null && shuffledEntries[i].Length != 0)
                {
                    //get substring
                    string[] s = shuffledEntries[i].Split(new[] { "[[[" }, StringSplitOptions.None);
                    string dirtyCoords = s[1].Insert(0, "[[[");
                    string coords = dirtyCoords.Split('}')[0];

                    cleanEntries[i] = coords;
                }
            }

            //----------------------------------------------------------------------------

            //have to make a dataTree for outputting the polyline[] that make up each drawing separately
            GH_Structure<GH_Curve> dataTree = new GH_Structure<GH_Curve>();

            //for each drawing in cleanEntries
            for (int i = 0; i < locations.Count; i++)
            {
                GH_Path pth = new GH_Path(i);

                Polyline[] polys = GetStrokes(cleanEntries[i]); //the polyline array

                //-----------------------------------------------------------------------
                //move, orient, and resize to unit square
                //(these transforms and stuff could be condensed probably)

                //get the bounding square of the drawing
                List<Point3d> allVerts = new List<Point3d>();
                for (int j = 0; j < polys.Length; j++)
                {
                    allVerts.AddRange(polys[j].ToList());
                }
                BoundingBox bb = new BoundingBox(allVerts);
                //now make a rectangle from those points
                Point3d[] corns = bb.GetCorners();
                Rectangle3d bRect = new Rectangle3d(Plane.WorldXY, corns[0], corns[2]);
                double side = Math.Max(bRect.Width, bRect.Height);  //get longest side length for bsquare
                Plane sqPln = new Plane(bRect.Center, Plane.WorldXY.XAxis, Plane.WorldXY.YAxis); //make plane for this shitty constructor
                sqPln.Transform(Transform.Translation(new Vector3d(-side / 2.0, -side / 2.0, 0)));  //plane is a corner not center, so move accordingly
                Rectangle3d bSquare = new Rectangle3d(sqPln, side, side); //the bounding square

                //translate each polyline in polys from bsquare cen to unit square cen
                Vector3d cenToCen = new Vector3d(new Point3d(0.5, 0.5, 0) - bSquare.Center);
                Transform move = Transform.Translation(cenToCen);

                //move each polyline in polys
                for (int j = 0; j < polys.Length; j++)
                {
                    polys[j].Transform(move);
                }
                bSquare.Transform(move);

                //now rotate it upright
                double angle = Math.PI; //180 degrees
                Transform rotate = Transform.Rotation(angle, bSquare.Center);
                //rotate each polyline in polys
                for (int j = 0; j < polys.Length; j++)
                {
                    polys[j].Transform(rotate);
                }

                //now reflect it over the yaxis at 0.5,0.5,0
                //make the reflect plane
                Plane refPlane = new Plane(new Point3d(0.5, 0.5, 0), Plane.WorldYZ.XAxis, Plane.WorldYZ.YAxis);
                Transform reflect = Transform.Mirror(refPlane);
                for (int j = 0; j < polys.Length; j++)
                {
                    polys[j].Transform(reflect);
                }

                //now scale it to the unit square (scale factor is 1/bSquare side length)
                Transform scale = Transform.Scale(new Point3d(0.5, 0.5, 0), 1 / side);
                //scale each polyline in polys
                for (int j = 0; j < polys.Length; j++)
                {
                    polys[j].Transform(scale);
                }

                //finally, move each drawing to the location
                for (int j = 0; j < polys.Length; j++)
                {
                    polys[j].Transform(Transform.Translation(new Vector3d(locations[i] - new Point3d(0.5, 0.5, 0))));
                }


                //------------------------------------------------------------------------

                List<GH_Curve> ghCrvList = new List<GH_Curve>();
                for(int j = 0; j < polys.Length; j++)
                {
                    GH_Curve temp = new GH_Curve();
                    GH_Convert.ToGHCurve(polys[j], GH_Conversion.Both, ref temp);

                    ghCrvList.Add(temp);
                }
                dataTree.AppendRange(ghCrvList, pth);
            }

            DA.SetDataTree(0, dataTree);

            Message = "Drew " + locations.Count.ToString("N0") + "/" + drawings.Count.ToString("N0");
        }
        //----------------------------------------------------------------------------------
        //functions

        //input an entire drawing as a string and get back the strokes as a polyline[]
        public Polyline[] GetStrokes(string s)
        {
            //first split the drawing into each stroke, roughly
            string[] roughStrokes = s.Split(new[] { "]],[[" }, StringSplitOptions.None);

            for (int i = 0; i < roughStrokes.Length; i++)
            {
                if (i == 0)//if its the first one get rid of the [[[ start brackets
                {
                    roughStrokes[i] = roughStrokes[i].Split(new[] { "[[[" }, StringSplitOptions.None)[1];
                }
                else if (i == roughStrokes.Length)//if its the last one get rid of the ]]] end brackets
                {
                    roughStrokes[i] = roughStrokes[i].Split(new[] { "]]]" }, StringSplitOptions.None)[0];
                }
            }

            //polyline LIST for ability to remove last item, and for output (converted to array)
            List<Polyline> polysList = new List<Polyline>();

            //now that you have the strokes of the drawing as a rough string[], use GetStroke() for each
            for (int i = 0; i < roughStrokes.Length; i++)
            {
                Polyline p = GetStroke(roughStrokes[i]);

                //------------------------------------------
                //THIS BLOCK IN THIS IF STATEMENT IS TO FIX THE WEIRD LAST POLYLINE'S LAST VERTEX ISSUE
                //get the last polyline
                if (i == roughStrokes.Length - 1)
                {
                    //make a copy of the polyline
                    Polyline pCopy = p;
                    //test if deleting the last vertex of copy leaves it still valid
                    pCopy.RemoveAt(pCopy.Count - 1);
                    if (pCopy.IsValid && p.Length > 0.0001)
                    {
                        polysList.Add(pCopy);
                    }
                }
                else  //otherwise just add the whole segment
                {
                    if (p.IsValid && p.Length > 0.0001)
                    {
                        polysList.Add(p);
                    }
                }//------------------------------------------
            }
            return polysList.ToArray();
        }

        //feed this function one stroke string chunk and get back the polyline
        //to be used within the GetStrokes() function
        public Polyline GetStroke(string s)
        {
            //split away the remaining brackets separating xs and ys
            string[] split1 = s.Split(new[] { "],[" }, StringSplitOptions.None);
            //split those x coord strings
            string[] xs = split1[0].Split(',');
            //split those y coord strings
            string[] ys = split1[1].Split(',');

            //make a point array to hold the combined coords
            Point3d[] points = new Point3d[xs.Length];

            for (int i = 0; i < points.Length; i++)
            {
                int x = 0;
                Int32.TryParse(xs[i], out x);
                points[i].X = x;

                int y = 0;
                Int32.TryParse(ys[i], out y);
                points[i].Y = y;
            }

            //make the polyline from the point array
            Polyline poly = new Polyline(points);
            return poly;
        }


        //----------------------------------------------------------------------------------



        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return Properties.Resources.QDicons_DRAW;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("059386f8-81c8-44a4-9c20-4175ef76229e"); }
        }
    }
}
