using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace QuickDraw
{
    public class ReadComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadComponent()
          : base("Read QD", "Read",
              "Reads a list of QuickDraw files supplied by the Load component, and prepares a specified amount of drawings for the Draw component starting at the the index you supply. Will be slow the first time it reads the files (or the Load component seed is changed), but changing the starting index or amount should be fairly fast after that.",
              "Extra", "QuickDrawGH")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Paths", "P", "The files to read the drawings from. It is slower the more files it has to read.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Amount", "A", "Amount of drawings to supply to the Draw component from each category.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Start Index", "S", "The index to start getting the drawings from. Using Amount and Start Index together you can get any range of drawings from the file. Wraps around.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Drawings", "D", "The range of drawings from the files specified, in raw string format.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> paths = new List<string>();
            DA.GetDataList(0, paths);

            int amount = new int();
            DA.GetData(1, ref amount);

            int startIndex = new int();
            DA.GetData(2, ref startIndex);

            //make the overall string list for eventual output
            List<string> allEntries = new List<string>();

            int drawingsCount = 0;

            //for each file supplied
            Parallel.For(0, paths.Count, i =>
            {
                //-------------------------------------------------------------------------------------
                //this reads all lines a little faster
                List<string> entriesList = new List<string>();
                
                
                const Int32 BufferSize = 2048;
                using (var fileStream = File.OpenRead(paths[i]))
                {
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null && line.Length > 0)
                        {
                            //process the line
                            lock (entriesList)
                            {
                                entriesList.Add(line);
                            }
                            Interlocked.Increment(ref drawingsCount);
                        }
                    }
                }
                

                //-------------------------------------------------------------------------------------

                //sanity
                if (amount > entriesList.Count) //if the amount of drawings im asking for is more than whats in the file
                {
                    AddRuntimeMessage(Grasshopper.Kernel.GH_RuntimeMessageLevel.Warning, "Amount is greater than the number of drawings in one or more of the files, affected files returned all their drawings.");
                    amount = entriesList.Count;  //just set it to the amount thats available
                }

                //a list so can be resizeable
                List<string> cutOffEntriesList = new List<string>();

                for (int j = 0; j < amount; j++) //for the amount of drawings we want from the file
                {
                    //wrap around and add the entries to the output list, its not null we checked earlier
                    cutOffEntriesList.Add(entriesList[(startIndex + j) % entriesList.Count]);
                }
                lock (allEntries)
                {
                    allEntries.AddRange(cutOffEntriesList);
                }
            });

            DA.SetDataList(0, allEntries);
            Message = "Read " + (amount * paths.Count).ToString("N0") + "/" + drawingsCount.ToString("N0");
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
                return Properties.Resources.QDicons_READ;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("981737d2-2dec-4c6f-9763-3c6d04bfa50e"); }
        }
    }
}