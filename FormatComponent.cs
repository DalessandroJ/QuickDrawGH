using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace QuickDraw
{
    public class FormatComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public FormatComponent()
          : base("Format QD", "Format",
              "Goes through the raw QuickDraw data and keeps only the Recognized:true drawings, and partitions them into separate files of up to 10,000 lines each. Start Directory should be the folder where the original .NDJSON files are stored. End Directory should be a new, empty folder you'd like the partitioned folders/files to be put into.",
              "Extra", "QuickDrawGH")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Start Directory", "SD", "The directory where the original .NDJSON QuickDraw files are kept.", GH_ParamAccess.item);
            pManager.AddTextParameter("End Directory", "ED", "The directory where the partitioned data should be put into.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Connect a button to here and click it once to run the script, copying and partitioning the files from the start directory to the end directory.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string startDirectory = string.Empty;
            DA.GetData(0, ref startDirectory);

            string endDirectory = string.Empty;
            DA.GetData(1, ref endDirectory);

            bool run = new bool();
            DA.GetData(2, ref run);

            //--------------------------------------

            List<string> files = Directory.GetFiles(startDirectory).ToList();
            int chunk = 10000;

            if (run)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    string newFolder = endDirectory + Path.GetFileNameWithoutExtension(files[i]);
                    Directory.CreateDirectory(newFolder);

                    string[] allLines = File.ReadAllLines(files[i]);

                    //separate out the true lines only
                    List<string> trueLines = new List<string>();
                    for (int j = 0; j < allLines.Length; j++)
                    {
                        if (allLines[j].Contains("true"))
                        {
                            trueLines.Add(allLines[j]);
                        }
                    }

                    //for each 10000 lines in the trueLines list
                    //separate it into a new list and add to a listoflists "chunks"
                    var chunks = new List<List<string>>();
                    int chunkCount = (trueLines.Count / chunk);

                    if (trueLines.Count % chunk > 0)
                    {
                        chunkCount++;
                    }
                    for (int j = 0; j < chunkCount; j++)
                    {
                        chunks.Add(trueLines.Skip(j * chunk).Take(chunk).ToList());
                    }


                    //take each list in chunks and write all the lines to a new file
                    //with the format {aircraft carrier 1.NDJSON, aircraft carrier 2.NDJSON, etc}
                    for (int j = 0; j < chunks.Count; j++)
                    {
                        string name = Path.GetFileNameWithoutExtension(files[i]) + " " + (j + 1).ToString() + ".NDJSON";
                        string path = Path.Combine(newFolder, name);

                        using (StreamWriter sw = new StreamWriter(path))
                        {
                            foreach (string line in chunks[j])
                            {
                                sw.WriteLine(line);
                            }
                        }
                    }
                }
            }
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
                return Properties.Resources.QDicons_FORMAT;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("eb530d6d-10e9-485e-80f1-213b9e1c9229"); }
        }
    }
}