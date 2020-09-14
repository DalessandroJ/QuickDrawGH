using System;
using System.IO;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace QuickDraw
{
    public class LoadComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public LoadComponent()
          : base("Load QD", "Load",
              "Gets a list of QuickDraw files in .NDJSON format from a master directory containing folders which contain partitioned files that match the supplied categories. Picks one partitioned set randomly for each category.",
              "Extra", "QuickDrawGH")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Directory", "D", "The directory to search for QuickDraw .NDJSON files within.", GH_ParamAccess.item);
            pManager.AddTextParameter("Categories", "C", "The categories of QuickDraw drawing files you want to retrieve.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Seed", "S", "Seed to randomize which partition of each file to retrieve.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Paths", "P", "The files to supply to the Read component.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string directory = string.Empty;
            DA.GetData(0, ref directory);

            List<string> categories = new List<string>();
            DA.GetDataList(1, categories);

            int seed = new int();
            DA.GetData(2, ref seed);

            //go through each folder requested, and pick a random file from it
            string[] output = new string[categories.Count];
            Random rand = new Random(seed);

            for (int i = 0; i < categories.Count; i++)
            {
                //find the folder matching name of categories[i] and count the number of files inside it
                string folder = Directory.GetDirectories(directory, categories[i], SearchOption.TopDirectoryOnly)[0];
                int filesCount = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Length;

                //pick a random file from the folder and add it to the output list
                int randInt = rand.Next(filesCount) + 1;
                output[i] = Path.Combine(folder, categories[i] + " " + randInt + ".ndjson");
            }

            Message = output.Length + " files";
            DA.SetDataList(0, output);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.QDicons_LOAD;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b7cd33ff-59d3-4906-a4ab-bedc91be0a0c"); }
        }
    }
}